using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBTMud.Core.Models;
using Moq;

namespace Lantean.QBTMud.Application.Test.Services
{
    public sealed class StorageRecoveryTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task GIVEN_MigrationCleanupReadingJournal_WHEN_UnrelatedMutationFails_THEN_ShouldRetainAndRecoverTheNewChange(bool retainOtherEntry, bool remove)
        {
            const string migratedKey = "QbtMud.WebUiLocalization.PreferredLocale.v1";
            const string changedKey = "ThemeManager.SelectedThemeId";
            const string journalKey = "SettingsStorage.PendingClientData.v1";
            var cancellationToken = TestContext.Current.CancellationToken;
            var cache = new ClientDataCacheState();
            var pending = new Dictionary<string, JsonElement?> { [migratedKey] = JsonSerializer.SerializeToElement("Locale") };
            if (retainOtherEntry)
            {
                pending["QbtMud.Other"] = JsonSerializer.SerializeToElement("Other");
            }
            var snapshot = new Dictionary<string, JsonElement?>(pending);
            var cleanupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var cleanupResponse = new TaskCompletionSource<Dictionary<string, JsonElement?>?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var committed = false;
            var cleanupBlocked = false;
            var localStorage = new Mock<ILocalStorageService>();
            localStorage.Setup(service => service.GetItemAsync<StorageRoutingSettings>(StorageRoutingSettings.StorageKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings { MasterStorageType = StorageType.ClientData });
            localStorage.Setup(service => service.SetItemAsync(StorageRoutingSettings.StorageKey, It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .Callback(() => committed = true).Returns(ValueTask.CompletedTask);
            localStorage.Setup(service => service.GetItemAsync<Dictionary<string, JsonElement?>>(journalKey, It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    if (committed && !cleanupBlocked)
                    {
                        cleanupBlocked = true;
                        cleanupStarted.SetResult();
                        return new ValueTask<Dictionary<string, JsonElement?>?>(cleanupResponse.Task);
                    }
                    return ValueTask.FromResult<Dictionary<string, JsonElement?>?>(new Dictionary<string, JsonElement?>(pending));
                });
            localStorage.Setup(service => service.SetItemAsync(journalKey, It.IsAny<Dictionary<string, JsonElement?>>(), It.IsAny<CancellationToken>()))
                .Callback<string, Dictionary<string, JsonElement?>, CancellationToken>((_, entries, _) => pending = new Dictionary<string, JsonElement?>(entries))
                .Returns(ValueTask.CompletedTask);
            localStorage.Setup(service => service.RemoveItemAsync(journalKey, It.IsAny<CancellationToken>()))
                .Callback(() => pending.Clear()).Returns(ValueTask.CompletedTask);
            var adapter = new Mock<IClientDataStorageAdapter>();
            var failStore = !remove;
            adapter.Setup(service => service.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    var result = failStore ? ClientDataStorageResult.Failure : ClientDataStorageResult.Success;
                    failStore = false;
                    return Task.FromResult(result);
                });
            var failRemoval = remove;
            adapter.Setup(service => service.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns((IEnumerable<string> keys, CancellationToken _) =>
                {
                    if (failRemoval && keys.Contains("QbtMud." + changedKey))
                    {
                        failRemoval = false;
                        return Task.FromResult(ClientDataStorageResult.Failure);
                    }
                    return Task.FromResult(ClientDataStorageResult.Success);
                });
            adapter.Setup(service => service.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement> { ["QbtMud." + changedKey] = JsonSerializer.SerializeToElement("Original") }));
            var capability = new Mock<IWebApiCapabilityService>();
            capability.Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            var feedback = Mock.Of<IApiFeedbackWorkflow>();
            var routing = new StorageRoutingService(localStorage.Object, adapter.Object, capability.Object, new StorageCatalogService(),
                Mock.Of<ILocalStorageEntryAdapter>(), new ClientDataCacheInvalidationService(cache), feedback, cache);
            var target = new SettingsStorageService(cache, localStorage.Object, routing, capability.Object, adapter.Object, feedback);
            var migration = routing.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData,
                ItemStorageTypes = new Dictionary<string, StorageType> { ["language.preferred-locale"] = StorageType.LocalStorage }
            }, cancellationToken);
            await cleanupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            var mutation = remove ? target.RemoveItemAsync(changedKey, cancellationToken).AsTask()
                : target.SetItemAsStringAsync(changedKey, "Updated", cancellationToken).AsTask();
            var mutationWaited = !mutation.IsCompleted;
            cleanupResponse.SetResult(snapshot);
            await Task.WhenAll(migration, mutation).WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            mutationWaited.Should().BeTrue();
            pending.Should().NotContainKey(migratedKey);
            pending.Should().ContainKey("QbtMud." + changedKey);
            pending.ContainsKey("QbtMud.Other").Should().Be(retainOtherEntry);
            (await target.GetItemAsStringAsync(changedKey, cancellationToken)).Should().Be(remove ? null : "Updated");
            pending.Should().BeEmpty();
            if (remove)
            {
                adapter.Verify(service => service.RemovePrefixedEntriesAsync(It.Is<IEnumerable<string>>(keys => keys.Contains("QbtMud." + changedKey)), It.IsAny<CancellationToken>()), Times.Exactly(2));
            }
            else
            {
                adapter.Verify(service => service.StorePrefixedEntriesAsync(It.Is<IReadOnlyDictionary<string, object?>>(entries => entries.ContainsKey("QbtMud." + changedKey)), It.IsAny<CancellationToken>()), Times.Exactly(2));
            }
        }

        [Theory]
        [InlineData(false, false, false, true)]
        [InlineData(false, false, false, false)]
        [InlineData(false, true, false, true)]
        [InlineData(true, false, false, true)]
        [InlineData(true, true, false, true)]
        [InlineData(true, false, true, true)]
        public async Task GIVEN_PendingWrite_WHEN_DiagnosticsDeletesDuringRecovery_THEN_ShouldKeepTheEntryDeleted(bool clear, bool recoveryFirst, bool emptyRemote, bool retainOtherEntry)
        {
            const string journalKey = "SettingsStorage.PendingClientData.v1";
            var cancellationToken = TestContext.Current.CancellationToken;
            var cache = new ClientDataCacheState();
            var localStorage = new TestLocalStorageService();
            if (retainOtherEntry)
            {
                await localStorage.SetItemAsync(journalKey, new Dictionary<string, JsonElement?> { ["QbtMud.Other"] = JsonSerializer.SerializeToElement("Other") }, cancellationToken);
            }
            var remote = new Dictionary<string, JsonElement>();
            if (!emptyRemote)
            {
                remote["QbtMud.Key"] = JsonSerializer.SerializeToElement("Original");
            }
            var adapter = new Mock<IClientDataStorageAdapter>();
            var failWrite = true;
            adapter.Setup(service => service.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Returns((IReadOnlyDictionary<string, object?> entries, CancellationToken _) =>
                {
                    if (failWrite)
                    {
                        failWrite = false;
                        return Task.FromResult(ClientDataStorageResult.Failure);
                    }
                    foreach (var (key, value) in entries)
                    {
                        remote[key] = JsonSerializer.SerializeToElement(value);
                    }
                    return Task.FromResult(ClientDataStorageResult.Success);
                });
            adapter.Setup(service => service.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns((IEnumerable<string> keys, CancellationToken _) =>
                {
                    foreach (var key in keys.ToArray())
                    {
                        remote.Remove(key);
                    }
                    return Task.FromResult(ClientDataStorageResult.Success);
                });
            var response = new TaskCompletionSource<ClientDataLoadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var loading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstLoad = recoveryFirst;
            adapter.Setup(service => service.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    if (firstLoad)
                    {
                        firstLoad = false;
                        loading.SetResult();
                        return response.Task;
                    }
                    return Task.FromResult(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>(remote)));
                });
            var capability = new Mock<IWebApiCapabilityService>();
            capability.Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            var routing = new Mock<IStorageRoutingService>();
            routing.Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new StorageRoutingSettings { MasterStorageType = StorageType.ClientData });
            routing.Setup(service => service.ResolveEffectiveStorageType(It.IsAny<string>(), It.IsAny<StorageRoutingSettings>(), true)).Returns(StorageType.ClientData);
            var feedback = Mock.Of<IApiFeedbackWorkflow>();
            var settings = new SettingsStorageService(cache, localStorage, routing.Object, capability.Object, adapter.Object, feedback);
            var target = new StorageDiagnosticsService(Mock.Of<ILocalStorageEntryAdapter>(), adapter.Object, capability.Object,
                new ClientDataCacheInvalidationService(cache), feedback, localStorage, cache);
            await settings.SetItemAsStringAsync("Key", "Updated", cancellationToken);
            Task<string?>? recovery = null;
            if (recoveryFirst)
            {
                recovery = settings.GetItemAsStringAsync("Key", cancellationToken).AsTask();
                await loading.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }

            var deletion = clear ? target.ClearEntriesAsync(StorageType.ClientData, cancellationToken)
                : target.RemoveEntryAsync(StorageType.ClientData, "QbtMud.Key", cancellationToken);
            if (recovery is not null)
            {
                response.SetResult(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>(remote)));
                await recovery.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            await deletion.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            if (!recoveryFirst && !clear && retainOtherEntry)
            {
                (await localStorage.GetItemAsync<Dictionary<string, JsonElement?>>(journalKey, cancellationToken)).Should().ContainSingle().Which.Key.Should().Be("QbtMud.Other");
            }
            (await settings.GetItemAsStringAsync("Key", cancellationToken)).Should().BeNull();
            remote.Should().NotContainKey("QbtMud.Key");
            (await localStorage.GetItemAsync<Dictionary<string, JsonElement?>>(journalKey, cancellationToken)).Should().BeNull();
        }
    }
}
