using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Core.Models;
using Moq;

namespace Lantean.QBTMud.Application.Test.Services
{
    public sealed class StorageRecoveryTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_OlderPendingChangeAndNewerLocalValue_WHEN_MigratingToClientData_THEN_ShouldConsumeOnlyTheMigratedSnapshot(bool pendingRemoval)
        {
            const string key = "ThemeManager.SelectedThemeId";
            const string prefixedKey = "QbtMud.ThemeManager.SelectedThemeId";
            var cancellationToken = TestContext.Current.CancellationToken;
            var localStorage = new TestLocalStorageService();
            await localStorage.SetItemAsStringAsync(key, JsonSerializer.Serialize("Updated"), cancellationToken);
            await localStorage.SetAsync(prefixedKey, pendingRemoval ? null : JsonSerializer.SerializeToElement("Pending"), cancellationToken);
            var remote = new Dictionary<string, JsonElement>();
            var adapter = new Mock<IClientDataStorageAdapter>();
            adapter.Setup(service => service.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Returns((IReadOnlyDictionary<string, object?> entries, CancellationToken _) =>
                {
                    foreach (var (entryKey, value) in entries)
                    {
                        remote[entryKey] = JsonSerializer.SerializeToElement(value);
                    }
                    return Task.FromResult(ClientDataStorageResult.Success);
                });
            adapter.Setup(service => service.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>(remote))));
            var capability = new Mock<IWebApiCapabilityService>();
            capability.Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            var feedback = Mock.Of<IApiFeedbackWorkflow>();
            var cache = new ClientDataCacheState();
            var target = new StorageRoutingService(localStorage, adapter.Object, capability.Object, new StorageCatalogService(),
                Mock.Of<ILocalStorageEntryAdapter>(), new ClientDataCacheInvalidationService(cache), feedback, cache, localStorage);
            var settings = new SettingsStorageService(cache, localStorage, target, capability.Object, adapter.Object, feedback, localStorage);

            await target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage,
                ItemStorageTypes = new Dictionary<string, StorageType> { ["themes.selected-theme"] = StorageType.ClientData }
            }, cancellationToken);

            (await localStorage.ReadAsync(cancellationToken)).Should().BeEmpty();
            (await localStorage.GetItemAsStringAsync(key, cancellationToken)).Should().BeNull();
            (await settings.GetItemAsStringAsync(key, cancellationToken)).Should().Be("Updated");
            remote[prefixedKey].GetString().Should().Be("Updated");
            adapter.Verify(service => service.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(2, false)]
        public async Task GIVEN_InFlightSuccessfulChange_WHEN_AnotherTabQueuesNewerChange_THEN_ShouldKeepNewerJournalAndInvalidateCachedValue(int operation, bool newerRemoval)
        {
            const string key = "Key";
            const string prefixedKey = "QbtMud.Key";
            var cancellationToken = TestContext.Current.CancellationToken;
            var localStorage = new TestLocalStorageService();
            var remote = new Dictionary<string, JsonElement> { [prefixedKey] = JsonSerializer.SerializeToElement("Original") };
            var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var response = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstRequest = true;
            var failNewChange = true;
            var adapter = new Mock<IClientDataStorageAdapter>();
            adapter.Setup(service => service.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>(remote))));
            adapter.Setup(service => service.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Returns(async (IReadOnlyDictionary<string, object?> entries, CancellationToken token) =>
                {
                    if (firstRequest)
                    {
                        firstRequest = false;
                        requestStarted.SetResult();
                        await response.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
                    }
                    else if (failNewChange)
                    {
                        failNewChange = false;
                        return ClientDataStorageResult.Failure;
                    }
                    foreach (var (entryKey, value) in entries)
                    {
                        remote[entryKey] = JsonSerializer.SerializeToElement(value);
                    }
                    return ClientDataStorageResult.Success;
                });
            adapter.Setup(service => service.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(async (IEnumerable<string> keys, CancellationToken token) =>
                {
                    if (firstRequest)
                    {
                        firstRequest = false;
                        requestStarted.SetResult();
                        await response.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
                    }
                    else if (failNewChange)
                    {
                        failNewChange = false;
                        return ClientDataStorageResult.Failure;
                    }
                    foreach (var entryKey in keys)
                    {
                        remote.Remove(entryKey);
                    }
                    return ClientDataStorageResult.Success;
                });
            var capability = new Mock<IWebApiCapabilityService>();
            capability.Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            var routing = new Mock<IStorageRoutingService>();
            routing.Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new StorageRoutingSettings { MasterStorageType = StorageType.ClientData });
            routing.Setup(service => service.ResolveEffectiveStorageType(It.IsAny<string>(), It.IsAny<StorageRoutingSettings>(), true)).Returns(StorageType.ClientData);
            var feedback = Mock.Of<IApiFeedbackWorkflow>();
            var target = new SettingsStorageService(new ClientDataCacheState(), localStorage, routing.Object, capability.Object, adapter.Object, feedback, localStorage);
            var otherTab = new SettingsStorageService(new ClientDataCacheState(), localStorage, routing.Object, capability.Object, adapter.Object, feedback, localStorage);
            (await target.GetItemAsStringAsync(key, cancellationToken)).Should().Be("Original");
            await localStorage.SetAsync(prefixedKey, JsonSerializer.SerializeToElement("Pending"), cancellationToken);
            var mutation = operation switch
            {
                0 => target.SetItemAsync(key, "Sent", cancellationToken).AsTask(),
                1 => target.SetItemAsStringAsync(key, "Sent", cancellationToken).AsTask(),
                _ => target.RemoveItemAsync(key, cancellationToken).AsTask()
            };
            await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            if (newerRemoval)
            {
                await otherTab.RemoveItemAsync(key, cancellationToken);
            }
            else
            {
                await otherTab.SetItemAsStringAsync(key, "Updated", cancellationToken);
            }
            response.SetResult();
            await mutation.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            var pending = await localStorage.ReadAsync(cancellationToken);
            pending.Should().ContainSingle().Which.Key.Should().Be(prefixedKey);
            (pending[prefixedKey]?.GetString()).Should().Be(newerRemoval ? null : "Updated");
            (await target.GetItemAsStringAsync(key, cancellationToken)).Should().Be(newerRemoval ? null : "Updated");
            (await localStorage.ReadAsync(cancellationToken)).Should().BeEmpty();
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task GIVEN_ReplayInAnotherTab_WHEN_NewerChangeFails_THEN_ShouldPreserveAndRecoverTheNewerChange(bool pendingRemoval, bool newerRemoval)
        {
            const string key = "Key";
            const string prefixedKey = "QbtMud.Key";
            var cancellationToken = TestContext.Current.CancellationToken;
            var localStorage = new TestLocalStorageService();
            await localStorage.SetAsync(prefixedKey, pendingRemoval ? null : JsonSerializer.SerializeToElement("Pending"), cancellationToken);
            var remote = new Dictionary<string, JsonElement> { [prefixedKey] = JsonSerializer.SerializeToElement("Original") };
            var replayStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var replayResponse = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstReplay = true;
            var failNewChange = true;
            var adapter = new Mock<IClientDataStorageAdapter>();
            adapter.Setup(service => service.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>(remote))));
            adapter.Setup(service => service.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Returns(async (IReadOnlyDictionary<string, object?> entries, CancellationToken token) =>
                {
                    if (firstReplay)
                    {
                        firstReplay = false;
                        replayStarted.SetResult();
                        await replayResponse.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
                    }
                    else if (failNewChange)
                    {
                        failNewChange = false;
                        return ClientDataStorageResult.Failure;
                    }
                    foreach (var (entryKey, value) in entries)
                    {
                        remote[entryKey] = JsonSerializer.SerializeToElement(value);
                    }
                    return ClientDataStorageResult.Success;
                });
            adapter.Setup(service => service.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(async (IEnumerable<string> keys, CancellationToken token) =>
                {
                    if (firstReplay)
                    {
                        firstReplay = false;
                        replayStarted.SetResult();
                        await replayResponse.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
                    }
                    else if (failNewChange)
                    {
                        failNewChange = false;
                        return ClientDataStorageResult.Failure;
                    }
                    foreach (var entryKey in keys)
                    {
                        remote.Remove(entryKey);
                    }
                    return ClientDataStorageResult.Success;
                });
            var capability = new Mock<IWebApiCapabilityService>();
            capability.Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            var routing = new Mock<IStorageRoutingService>();
            routing.Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new StorageRoutingSettings { MasterStorageType = StorageType.ClientData });
            routing.Setup(service => service.ResolveEffectiveStorageType(It.IsAny<string>(), It.IsAny<StorageRoutingSettings>(), true)).Returns(StorageType.ClientData);
            var feedback = Mock.Of<IApiFeedbackWorkflow>();
            var target = new SettingsStorageService(new ClientDataCacheState(), localStorage, routing.Object, capability.Object, adapter.Object, feedback, localStorage);
            var otherTab = new SettingsStorageService(new ClientDataCacheState(), localStorage, routing.Object, capability.Object, adapter.Object, feedback, localStorage);
            var recovery = target.GetItemAsStringAsync(key, cancellationToken).AsTask();
            await replayStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            if (newerRemoval)
            {
                await otherTab.RemoveItemAsync(key, cancellationToken);
            }
            else
            {
                await otherTab.SetItemAsStringAsync(key, "Updated", cancellationToken);
            }
            replayResponse.SetResult();

            (await recovery.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken)).Should().Be(newerRemoval ? null : "Updated");
            var pending = await localStorage.ReadAsync(cancellationToken);
            if (pendingRemoval && newerRemoval)
            {
                pending.Should().BeEmpty();
            }
            else
            {
                pending.Should().ContainSingle().Which.Key.Should().Be(prefixedKey);
                (pending[prefixedKey]?.GetString()).Should().Be(newerRemoval ? null : "Updated");
            }
            (await target.GetItemAsStringAsync(key, cancellationToken)).Should().Be(newerRemoval ? null : "Updated");
            (await localStorage.ReadAsync(cancellationToken)).Should().BeEmpty();
            if (newerRemoval)
            {
                remote.Should().NotContainKey(prefixedKey);
            }
            else
            {
                remote[prefixedKey].GetString().Should().Be("Updated");
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task GIVEN_MultipleMigratedSources_WHEN_NewerSaveFailsDuringEarlierCleanup_THEN_ShouldPreserveLaterFallbackAndPendingChange(bool typed, bool remove)
        {
            const string earlierKey = "MainLayout.DrawerOpen";
            const string changedKey = "ThemeManager.SelectedThemeId";
            const string prefixedKey = "QbtMud.ThemeManager.SelectedThemeId";
            var cancellationToken = TestContext.Current.CancellationToken;
            var localStorage = new TestLocalStorageService();
            await localStorage.SetItemAsync(earlierKey, true, cancellationToken);
            await localStorage.SetItemAsStringAsync(changedKey, JsonSerializer.Serialize("Original"), cancellationToken);
            var cleanupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var cleanupResponse = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var journal = new Mock<IPendingClientDataJournal>();
            journal.Setup(service => service.ReadAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken token) => localStorage.ReadAsync(token));
            journal.Setup(service => service.SetAsync(It.IsAny<string>(), It.IsAny<JsonElement?>(), It.IsAny<CancellationToken>()))
                .Returns((string key, JsonElement? value, CancellationToken token) => localStorage.SetAsync(key, value, token));
            journal.Setup(service => service.CompleteAsync(It.IsAny<IReadOnlyDictionary<string, JsonElement?>>(), It.IsAny<CancellationToken>()))
                .Returns((IReadOnlyDictionary<string, JsonElement?> changes, CancellationToken token) => localStorage.CompleteAsync(changes, token));
            journal.Setup(service => service.RemoveLocalSourceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string key, CancellationToken token) => new ValueTask(RemoveSourceAsync(key, token)));
            var remote = new Dictionary<string, JsonElement>();
            var failNewChange = false;
            var adapter = new Mock<IClientDataStorageAdapter>();
            adapter.Setup(service => service.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Returns((IReadOnlyDictionary<string, object?> entries, CancellationToken _) =>
                {
                    if (failNewChange)
                    {
                        failNewChange = false;
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
                    if (failNewChange)
                    {
                        failNewChange = false;
                        return Task.FromResult(ClientDataStorageResult.Failure);
                    }
                    foreach (var key in keys)
                    {
                        remote.Remove(key);
                    }
                    return Task.FromResult(ClientDataStorageResult.Success);
                });
            adapter.Setup(service => service.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>(remote))));
            var capability = new Mock<IWebApiCapabilityService>();
            capability.Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            var feedback = Mock.Of<IApiFeedbackWorkflow>();
            var cache = new ClientDataCacheState();
            var localEntries = new Mock<ILocalStorageEntryAdapter>();
            localEntries.Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<BrowserStorageEntry>());
            var target = new StorageRoutingService(localStorage, adapter.Object, capability.Object, new StorageCatalogService(),
                localEntries.Object, new ClientDataCacheInvalidationService(cache), feedback, cache, journal.Object);
            var settings = new SettingsStorageService(new ClientDataCacheState(), localStorage, target, capability.Object, adapter.Object, feedback, journal.Object);
            var migration = target.SaveSettingsAsync(new StorageRoutingSettings { MasterStorageType = StorageType.ClientData }, cancellationToken);
            await Task.WhenAny(migration, cleanupStarted.Task).WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            if (migration.IsCompleted)
            {
                await migration;
            }
            await cleanupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            failNewChange = true;

            if (remove)
            {
                await settings.RemoveItemAsync(changedKey, cancellationToken);
            }
            else if (typed)
            {
                await settings.SetItemAsync(changedKey, "Updated", cancellationToken);
            }
            else
            {
                await settings.SetItemAsStringAsync(changedKey, "Updated", cancellationToken);
            }
            cleanupResponse.SetResult();
            await migration.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            var pending = await localStorage.ReadAsync(cancellationToken);
            pending.Should().ContainSingle().Which.Key.Should().Be(prefixedKey);
            (pending[prefixedKey]?.GetString()).Should().Be(remove ? null : "Updated");
            (await localStorage.GetItemAsStringAsync(earlierKey, cancellationToken)).Should().BeNull();
            if (typed)
            {
                (await localStorage.GetItemAsync<string>(changedKey, cancellationToken)).Should().Be("Updated");
            }
            else
            {
                (await localStorage.GetItemAsStringAsync(changedKey, cancellationToken)).Should().Be(remove ? null : "Updated");
            }
            (await settings.GetItemAsStringAsync(changedKey, cancellationToken)).Should().Be(remove ? null : "Updated");
            (await localStorage.ReadAsync(cancellationToken)).Should().BeEmpty();
            if (remove)
            {
                remote.Should().NotContainKey(prefixedKey);
            }
            else
            {
                remote[prefixedKey].GetString().Should().Be("Updated");
            }

            async Task RemoveSourceAsync(string key, CancellationToken token)
            {
                if (key == earlierKey)
                {
                    cleanupStarted.SetResult();
                    await cleanupResponse.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
                }
                await localStorage.RemoveLocalSourceAsync(key, token);
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task GIVEN_MigrationCleanupReadingJournal_WHEN_UnrelatedMutationFails_THEN_ShouldRetainAndRecoverTheNewChange(bool retainOtherEntry, bool remove)
        {
            const string migratedKey = "QbtMud.WebUiLocalization.PreferredLocale.v1";
            const string changedKey = "ThemeManager.SelectedThemeId";
            var cancellationToken = TestContext.Current.CancellationToken;
            var cache = new ClientDataCacheState();
            var pending = new Dictionary<string, JsonElement?> { [migratedKey] = JsonSerializer.SerializeToElement("Locale") };
            if (retainOtherEntry)
            {
                pending["QbtMud.Other"] = JsonSerializer.SerializeToElement("Other");
            }
            var cleanupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var cleanupResponse = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var committed = false;
            var cleanupBlocked = false;
            var localStorage = new Mock<ILocalStorageService>();
            var journal = new Mock<IPendingClientDataJournal>();
            localStorage.Setup(service => service.GetItemAsync<StorageRoutingSettings>(StorageRoutingSettings.StorageKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings { MasterStorageType = StorageType.ClientData });
            localStorage.Setup(service => service.SetItemAsync(StorageRoutingSettings.StorageKey, It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .Callback(() => committed = true).Returns(ValueTask.CompletedTask);
            journal.Setup(service => service.ReadAsync(It.IsAny<CancellationToken>()))
                .Returns(() => ValueTask.FromResult(new Dictionary<string, JsonElement?>(pending)));
            journal.Setup(service => service.SetAsync(It.IsAny<string>(), It.IsAny<JsonElement?>(), It.IsAny<CancellationToken>()))
                .Callback<string, JsonElement?, CancellationToken>((key, value, _) => pending[key] = value)
                .Returns(ValueTask.CompletedTask);
            journal.Setup(service => service.CompleteAsync(It.IsAny<IReadOnlyDictionary<string, JsonElement?>>(), It.IsAny<CancellationToken>()))
                .Returns((IReadOnlyDictionary<string, JsonElement?> changes, CancellationToken _) => new ValueTask<Dictionary<string, JsonElement?>>(CompleteChangesAsync(changes)));
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
                Mock.Of<ILocalStorageEntryAdapter>(), new ClientDataCacheInvalidationService(cache), feedback, cache, journal.Object);
            var target = new SettingsStorageService(cache, localStorage.Object, routing, capability.Object, adapter.Object, feedback, journal.Object);
            var migration = routing.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData,
                ItemStorageTypes = new Dictionary<string, StorageType> { ["language.preferred-locale"] = StorageType.LocalStorage }
            }, cancellationToken);
            await cleanupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            var mutation = remove ? target.RemoveItemAsync(changedKey, cancellationToken).AsTask()
                : target.SetItemAsStringAsync(changedKey, "Updated", cancellationToken).AsTask();
            var mutationWaited = !mutation.IsCompleted;
            cleanupResponse.SetResult();
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

            async Task<Dictionary<string, JsonElement?>> CompleteChangesAsync(IReadOnlyDictionary<string, JsonElement?> changes)
            {
                if (committed && !cleanupBlocked)
                {
                    cleanupBlocked = true;
                    cleanupStarted.SetResult();
                    await cleanupResponse.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                }
                foreach (var (key, value) in changes)
                {
                    if (pending.TryGetValue(key, out var current) && current?.GetRawText() == value?.GetRawText())
                    {
                        pending.Remove(key);
                    }
                }
                return new Dictionary<string, JsonElement?>(pending);
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
            var settings = new SettingsStorageService(cache, localStorage, routing.Object, capability.Object, adapter.Object, feedback, localStorage);
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
