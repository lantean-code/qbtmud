using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBTMud.Core.Models;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Application.Test.Services
{
    public sealed class SettingsStorageServiceTests
    {
        private readonly TestLocalStorageService _localStorageService;
        private readonly IStorageRoutingService _storageRoutingService;
        private readonly IWebApiCapabilityService _webApiCapabilityService;
        private readonly IClientDataStorageAdapter _clientDataStorageAdapter;
        private readonly IApiFeedbackWorkflow _apiFeedbackWorkflow;
        private readonly IClientDataCacheInvalidationService _clientDataCacheInvalidationService;
        private readonly SettingsStorageService _target;

        public SettingsStorageServiceTests()
        {
            _localStorageService = new TestLocalStorageService();
            _storageRoutingService = Mock.Of<IStorageRoutingService>();
            _webApiCapabilityService = Mock.Of<IWebApiCapabilityService>();
            _clientDataStorageAdapter = Mock.Of<IClientDataStorageAdapter>();
            _apiFeedbackWorkflow = Mock.Of<IApiFeedbackWorkflow>();
            var clientDataCacheState = new ClientDataCacheState();
            _clientDataCacheInvalidationService = new ClientDataCacheInvalidationService(clientDataCacheState);

            Mock.Get(_storageRoutingService)
                .Setup(service => service.ResolveEffectiveStorageType(It.IsAny<string>(), It.IsAny<StorageRoutingSettings>(), It.IsAny<bool>()))
                .Returns((string _, StorageRoutingSettings settings, bool supportsClientData) =>
                {
                    if (supportsClientData)
                    {
                        return settings.MasterStorageType;
                    }

                    return StorageType.LocalStorage;
                });
            Mock.Get(_apiFeedbackWorkflow)
                .Setup(workflow => workflow.HandleFailureAsync(
                    It.IsAny<ApiResultBase>(),
                    It.IsAny<Func<string?, string>?>(),
                    It.IsAny<Severity>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _target = new SettingsStorageService(
                clientDataCacheState,
                _localStorageService,
                _storageRoutingService,
                _webApiCapabilityService,
                _clientDataStorageAdapter,
                _apiFeedbackWorkflow);
        }

        [Theory]
        [InlineData("typed", false)]
        [InlineData("typed", true)]
        [InlineData("string", false)]
        [InlineData("string", true)]
        [InlineData("remove", false)]
        [InlineData("remove", true)]
        public async Task GIVEN_PendingChangeAndLoadingRead_WHEN_NewerMutationStarts_THEN_ShouldPersistNewerState(string operation, bool pendingRemoval)
        {
            ConfigureClientDataRouting();
            var cancellationToken = TestContext.Current.CancellationToken;
            await _localStorageService.SetItemAsync("SettingsStorage.PendingClientData.v1",
                new Dictionary<string, JsonElement?> { ["QbtMud.Key"] = pendingRemoval ? null : JsonSerializer.SerializeToElement("Pending") }, cancellationToken);
            var response = new TaskCompletionSource<ClientDataLoadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            string? remoteValue = "Original";
            Mock.Get(_clientDataStorageAdapter).Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    started.SetResult();
                    return response.Task;
                });
            Mock.Get(_clientDataStorageAdapter).Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Returns((IReadOnlyDictionary<string, object?> entries, CancellationToken _) =>
                {
                    remoteValue = entries["QbtMud.Key"] is JsonElement element ? element.GetString() : (string?)entries["QbtMud.Key"];
                    return Task.FromResult(ClientDataStorageResult.Success);
                });
            Mock.Get(_clientDataStorageAdapter).Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    remoteValue = null;
                    return Task.FromResult(ClientDataStorageResult.Success);
                });
            var read = _target.GetItemAsStringAsync("Key", cancellationToken).AsTask();
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            var mutation = operation switch
            {
                "typed" => _target.SetItemAsync("Key", "Updated", cancellationToken).AsTask(),
                "string" => _target.SetItemAsStringAsync("Key", "Updated", cancellationToken).AsTask(),
                _ => _target.RemoveItemAsync("Key", cancellationToken).AsTask()
            };
            response.SetResult(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>
            {
                ["QbtMud.Key"] = JsonSerializer.SerializeToElement("Original")
            }));
            await Task.WhenAll(read, mutation).WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

            remoteValue.Should().Be(operation == "remove" ? null : "Updated");
            (await _target.GetItemAsStringAsync("Key", cancellationToken)).Should().Be(remoteValue);
            (await _localStorageService.GetItemAsStringAsync("SettingsStorage.PendingClientData.v1", cancellationToken)).Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_FailedTypedWrite_WHEN_RemoteReadsRecoverAfterReload_THEN_ShouldReplayThePendingValue()
        {
            ConfigureClientDataRouting();
            Mock.Get(_clientDataStorageAdapter)
                .SetupSequence(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Failure)
                .ReturnsAsync(ClientDataStorageResult.Success);
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>
                {
                    ["QbtMud.Key"] = JsonSerializer.SerializeToElement(new Dictionary<string, bool> { ["Enabled"] = false })
                }));
            await _target.SetItemAsync("Key", new Dictionary<string, bool> { ["Enabled"] = true }, TestContext.Current.CancellationToken);
            var journal = await _localStorageService.GetItemAsStringAsync("SettingsStorage.PendingClientData.v1", TestContext.Current.CancellationToken);
            await _localStorageService.SetItemAsStringAsync("SettingsStorage.PendingClientData.v1", journal!, TestContext.Current.CancellationToken);
            var target = new SettingsStorageService(new ClientDataCacheState(), _localStorageService,
                _storageRoutingService, _webApiCapabilityService, _clientDataStorageAdapter, _apiFeedbackWorkflow);

            var result = await target.GetItemAsync<Dictionary<string, bool>>("Key", TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result!["Enabled"].Should().BeTrue();
            Mock.Get(_clientDataStorageAdapter).Verify(adapter => adapter.StorePrefixedEntriesAsync(
                It.Is<IReadOnlyDictionary<string, object?>>(entries => entries.ContainsKey("QbtMud.Key")
                    && ((JsonElement)entries["QbtMud.Key"]!).GetProperty("Enabled").GetBoolean()),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
            (await _localStorageService.GetItemAsStringAsync("SettingsStorage.PendingClientData.v1", TestContext.Current.CancellationToken)).Should().BeNull();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_FailedStringWriteOrRemoval_WHEN_ReadsRecoverBeforeWrites_THEN_ShouldKeepPendingChangesUntilReplaySucceeds(bool remove)
        {
            ConfigureClientDataRouting();
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>
                {
                    ["QbtMud.Key"] = JsonSerializer.SerializeToElement("Original")
                }));
            Mock.Get(_clientDataStorageAdapter)
                .SetupSequence(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Failure)
                .ReturnsAsync(ClientDataStorageResult.Failure)
                .ReturnsAsync(ClientDataStorageResult.Success);
            Mock.Get(_clientDataStorageAdapter)
                .SetupSequence(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Failure)
                .ReturnsAsync(ClientDataStorageResult.Failure)
                .ReturnsAsync(ClientDataStorageResult.Success);
            if (remove)
            {
                await _target.RemoveItemAsync("Key", TestContext.Current.CancellationToken);
            }
            else
            {
                await _target.SetItemAsStringAsync("Key", "Updated", TestContext.Current.CancellationToken);
            }

            var journal = await _localStorageService.GetItemAsStringAsync("SettingsStorage.PendingClientData.v1", TestContext.Current.CancellationToken);
            await _localStorageService.SetItemAsStringAsync("SettingsStorage.PendingClientData.v1", journal!, TestContext.Current.CancellationToken);
            var first = await _target.GetItemAsStringAsync("Key", TestContext.Current.CancellationToken);
            var pending = await _localStorageService.GetItemAsStringAsync("SettingsStorage.PendingClientData.v1", TestContext.Current.CancellationToken);
            var second = await _target.GetItemAsStringAsync("Key", TestContext.Current.CancellationToken);

            first.Should().Be(remove ? null : "Updated");
            second.Should().Be(first);
            pending.Should().NotBeNull();
            (await _localStorageService.GetItemAsStringAsync("SettingsStorage.PendingClientData.v1", TestContext.Current.CancellationToken)).Should().BeNull();
            Mock.Get(_clientDataStorageAdapter).Verify(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_PendingChange_WHEN_AReplacementWriteSucceeds_THEN_ShouldDiscardTheObsoletePendingValue(bool remove)
        {
            ConfigureClientDataRouting();
            Mock.Get(_clientDataStorageAdapter)
                .SetupSequence(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Failure)
                .ReturnsAsync(ClientDataStorageResult.Success);
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Success);
            await _target.SetItemAsStringAsync("Key", "Pending", TestContext.Current.CancellationToken);

            if (remove)
            {
                await _target.RemoveItemAsync("Key", TestContext.Current.CancellationToken);
            }
            else
            {
                await _target.SetItemAsStringAsync("Key", "Updated", TestContext.Current.CancellationToken);
            }

            (await _localStorageService.GetItemAsStringAsync("SettingsStorage.PendingClientData.v1", TestContext.Current.CancellationToken)).Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_PendingWriteAndRemoval_WHEN_OnlyWritesRecover_THEN_ShouldRetainAndRetryOnlyTheRemoval()
        {
            ConfigureClientDataRouting();
            var updated = JsonSerializer.SerializeToElement("Updated");
            Mock.Get(_clientDataStorageAdapter)
                .SetupSequence(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>
                {
                    ["QbtMud.Write"] = JsonSerializer.SerializeToElement("Original"),
                    ["QbtMud.Remove"] = JsonSerializer.SerializeToElement("Original")
                }))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>
                {
                    ["QbtMud.Write"] = updated,
                    ["QbtMud.Remove"] = JsonSerializer.SerializeToElement("Original")
                }));
            Mock.Get(_clientDataStorageAdapter)
                .SetupSequence(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Failure)
                .ReturnsAsync(ClientDataStorageResult.Success);
            Mock.Get(_clientDataStorageAdapter)
                .SetupSequence(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Failure)
                .ReturnsAsync(ClientDataStorageResult.Failure)
                .ReturnsAsync(ClientDataStorageResult.Success);
            await _target.SetItemAsStringAsync("Write", "Updated", TestContext.Current.CancellationToken);
            await _target.RemoveItemAsync("Remove", TestContext.Current.CancellationToken);

            var result = await _target.GetItemAsStringAsync("Write", TestContext.Current.CancellationToken);
            var pending = await _localStorageService.GetItemAsStringAsync("SettingsStorage.PendingClientData.v1", TestContext.Current.CancellationToken);
            var removed = await _target.GetItemAsStringAsync("Remove", TestContext.Current.CancellationToken);

            result.Should().Be("Updated");
            using var pendingDocument = JsonDocument.Parse(pending!);
            pendingDocument.RootElement.EnumerateObject().Select(property => property.Name).Should().Equal("QbtMud.Remove");
            removed.Should().BeNull();
            Mock.Get(_clientDataStorageAdapter).Verify(adapter => adapter.StorePrefixedEntriesAsync(
                It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_LocalStorageRouting_WHEN_SetAndGetItem_THEN_ShouldUseLocalStorageStorageType()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.LocalStorage
                });

            await _target.SetItemAsync("AppSettings.State.v1", new { enabled = true }, TestContext.Current.CancellationToken);

            var result = await _target.GetItemAsync<Dictionary<string, bool>>("AppSettings.State.v1", TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result!["enabled"].Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ClientDataRoutingWithSupport_WHEN_SetItem_THEN_ShouldStoreClientDataPayload()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));

            IReadOnlyDictionary<string, object?>? payload = null;
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyDictionary<string, object?>, CancellationToken>((entries, _) => payload = entries)
                .ReturnsAsync(ClientDataStorageResult.Success);

            await _target.SetItemAsync("AppSettings.State.v1", new { enabled = true }, TestContext.Current.CancellationToken);

            payload.Should().NotBeNull();
            payload!.Should().ContainKey("QbtMud.AppSettings.State.v1");
            payload["QbtMud.AppSettings.State.v1"].Should().BeOfType<JsonElement>();
        }

        [Fact]
        public async Task GIVEN_ClientDataRoutingWithoutSupport_WHEN_SetItemAsString_THEN_ShouldFallbackToLocalStorage()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 12, 0), false));

            await _target.SetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", "en", TestContext.Current.CancellationToken);

            var storedValue = await _localStorageService.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            storedValue.Should().Be("en");
            Mock.Get(_clientDataStorageAdapter)
                .Verify(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ClientDataStoreFails_WHEN_SetItemAsString_THEN_ShouldFallbackToLocalStorage()
        {
            var apiResult = CreateFailureResult();

            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.FromFailure(apiResult));

            await _target.SetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", "en_GB", TestContext.Current.CancellationToken);

            var storedValue = await _localStorageService.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            storedValue.Should().Be("en_GB");
            VerifyFailureHandled(apiResult);
        }

        [Fact]
        public async Task GIVEN_ClientDataRoutingWithSupportAndMissingValue_WHEN_GetItemAsync_THEN_ShouldReturnDefault()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)));

            var result = await _target.GetItemAsync<string>("AppSettings.State.v1", TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ClientDataLoadFails_WHEN_GetItemAsync_THEN_ShouldFallbackToLocalStorage()
        {
            var apiResult = CreateFailureResult();

            await _localStorageService.SetItemAsync("AppSettings.State.v1", new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["enabled"] = true
            }, TestContext.Current.CancellationToken);

            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromFailure(apiResult));

            var result = await _target.GetItemAsync<Dictionary<string, bool>>("AppSettings.State.v1", TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result!["enabled"].Should().BeTrue();
            VerifyFailureHandled(apiResult);
        }

        [Fact]
        public async Task GIVEN_ClientDataLoadCancelled_WHEN_GetItemAsync_THEN_ShouldPropagateCancellation()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException("cancelled"));

            Func<Task> action = async () =>
            {
                await _target.GetItemAsync<Dictionary<string, bool>>("AppSettings.State.v1", TestContext.Current.CancellationToken);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GIVEN_ClientDataStringValue_WHEN_GetItemAsStringAsync_THEN_ShouldReturnStringValue()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.WebUiLocalization.PreferredLocale.v1"] = JsonDocument.Parse("\"en\"").RootElement.Clone()
                    }));

            var result = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);

            result.Should().Be("en");
        }

        [Fact]
        public async Task GIVEN_ClientDataObjectValue_WHEN_GetItemAsStringAsync_THEN_ShouldReturnRawJson()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.AppSettings.State.v1"] = JsonDocument.Parse("{\"enabled\":true}").RootElement.Clone()
                    }));

            var result = await _target.GetItemAsStringAsync("AppSettings.State.v1", TestContext.Current.CancellationToken);

            result.Should().Be("{\"enabled\":true}");
        }

        [Fact]
        public async Task GIVEN_ClientDataGetAsStringFails_WHEN_GetItemAsStringAsync_THEN_ShouldFallbackToLocalStorage()
        {
            await _localStorageService.SetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", "en_GB", TestContext.Current.CancellationToken);

            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.Failure);

            var result = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);

            result.Should().Be("en_GB");
        }

        [Fact]
        public async Task GIVEN_ClientDataGetAsStringCancelled_WHEN_GetItemAsStringAsync_THEN_ShouldPropagateCancellation()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException("cancelled"));

            Func<Task> action = async () =>
            {
                await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GIVEN_ClientDataRemoveFails_WHEN_RemoveItemAsync_THEN_ShouldFallbackToLocalStorageRemoval()
        {
            var apiResult = CreateFailureResult();

            await _localStorageService.SetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", "en_GB", TestContext.Current.CancellationToken);

            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.FromFailure(apiResult));

            await _target.RemoveItemAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);

            var storedValue = await _localStorageService.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            storedValue.Should().BeNull();
            VerifyFailureHandled(apiResult);
        }

        [Fact]
        public async Task GIVEN_ClientDataRoutingAndPrefixedKey_WHEN_SetItemAsync_THEN_ShouldNotDoublePrefixStoredKey()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));

            IReadOnlyDictionary<string, object?>? payload = null;
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyDictionary<string, object?>, CancellationToken>((entries, _) => payload = entries)
                .ReturnsAsync(ClientDataStorageResult.Success);

            await _target.SetItemAsync("QbtMud.AppSettings.State.v1", new { enabled = true }, TestContext.Current.CancellationToken);

            payload.Should().NotBeNull();
            payload!.Should().ContainSingle();
            payload.Keys.Single().Should().Be("QbtMud.AppSettings.State.v1");
        }

        [Fact]
        public async Task GIVEN_ClientDataRoutingAndUndefinedValue_WHEN_GetItemAsStringAsync_THEN_ShouldReturnNull()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.WebUiLocalization.PreferredLocale.v1"] = default
                    }));

            var result = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ClientDataSetFails_WHEN_SetItemAsync_THEN_ShouldFallbackToLocalStorage()
        {
            var apiResult = CreateFailureResult();

            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.FromFailure(apiResult));

            await _target.SetItemAsync("AppSettings.State.v1", new { enabled = true }, TestContext.Current.CancellationToken);

            var result = await _localStorageService.GetItemAsync<Dictionary<string, bool>>("AppSettings.State.v1", TestContext.Current.CancellationToken);
            result.Should().NotBeNull();
            result!["enabled"].Should().BeTrue();
            VerifyFailureHandled(apiResult);
        }

        [Fact]
        public async Task GIVEN_ClientDataSetCancelled_WHEN_SetItemAsync_THEN_ShouldPropagateCancellation()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException("cancelled"));

            Func<Task> action = async () =>
            {
                await _target.SetItemAsync("AppSettings.State.v1", new { enabled = true }, TestContext.Current.CancellationToken);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GIVEN_ClientDataRoutingWithSupport_WHEN_RemoveItemAsync_THEN_ShouldCallClientDataAdapter()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Success);

            await _target.RemoveItemAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);

            Mock.Get(_clientDataStorageAdapter).Verify(
                adapter => adapter.RemovePrefixedEntriesAsync(
                    It.Is<IEnumerable<string>>(keys => keys.Contains("QbtMud.WebUiLocalization.PreferredLocale.v1", StringComparer.Ordinal)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_LocalStorageRouting_WHEN_GetItemAsStringAsync_THEN_ShouldReadFromLocalStorage()
        {
            await _localStorageService.SetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", "en", TestContext.Current.CancellationToken);
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.LocalStorage
                });

            var result = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);

            result.Should().Be("en");
            Mock.Get(_clientDataStorageAdapter).Verify(
                adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_ClientDataRoutingWithSupport_WHEN_GetItemAsync_THEN_ShouldDeserializeClientDataValue()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.AppSettings.State.v1"] = JsonDocument.Parse("{\"enabled\":true}").RootElement.Clone()
                    }));

            var result = await _target.GetItemAsync<Dictionary<string, bool>>("AppSettings.State.v1", TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result!["enabled"].Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ClientDataRoutingWithSupport_WHEN_GetMultipleItems_THEN_ShouldLoadClientDataOnce()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.AppSettings.State.v1"] = JsonDocument.Parse("{\"enabled\":true}").RootElement.Clone(),
                        ["QbtMud.WebUiLocalization.PreferredLocale.v1"] = JsonDocument.Parse("\"en\"").RootElement.Clone()
                    }));

            var item = await _target.GetItemAsync<Dictionary<string, bool>>("AppSettings.State.v1", TestContext.Current.CancellationToken);
            var locale = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);

            item.Should().NotBeNull();
            item!["enabled"].Should().BeTrue();
            locale.Should().Be("en");
            Mock.Get(_clientDataStorageAdapter)
                .Verify(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClientDataCacheInitialized_WHEN_SetItemAsync_THEN_ShouldUpdateCachedValue()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.AppSettings.State.v1"] = JsonDocument.Parse("{\"enabled\":false}").RootElement.Clone()
                    }));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Success);

            var initial = await _target.GetItemAsync<Dictionary<string, bool>>("AppSettings.State.v1", TestContext.Current.CancellationToken);
            await _target.SetItemAsync("AppSettings.State.v1", new { enabled = true }, TestContext.Current.CancellationToken);
            var updated = await _target.GetItemAsync<Dictionary<string, bool>>("AppSettings.State.v1", TestContext.Current.CancellationToken);

            initial.Should().NotBeNull();
            initial!["enabled"].Should().BeFalse();
            updated.Should().NotBeNull();
            updated!["enabled"].Should().BeTrue();
            Mock.Get(_clientDataStorageAdapter)
                .Verify(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClientDataCacheInitializedAndSetItemFails_WHEN_GetItemAsyncAfterFallback_THEN_ShouldInvalidateCacheAndUseLocalStorageValue()
        {
            var apiResult = CreateFailureResult();

            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .SetupSequence(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.AppSettings.State.v1"] = JsonDocument.Parse("{\"enabled\":false}").RootElement.Clone()
                    }))
                .ReturnsAsync(ClientDataLoadResult.FromFailure(apiResult));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.FromFailure(apiResult));

            var initial = await _target.GetItemAsync<Dictionary<string, bool>>("AppSettings.State.v1", TestContext.Current.CancellationToken);
            await _target.SetItemAsync("AppSettings.State.v1", new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["enabled"] = true
            }, TestContext.Current.CancellationToken);
            var updated = await _target.GetItemAsync<Dictionary<string, bool>>("AppSettings.State.v1", TestContext.Current.CancellationToken);

            initial.Should().NotBeNull();
            initial!["enabled"].Should().BeFalse();
            updated.Should().NotBeNull();
            updated!["enabled"].Should().BeTrue();
            Mock.Get(_clientDataStorageAdapter)
                .Verify(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_ClientDataCacheInitialized_WHEN_RemoveItemAsync_THEN_ShouldRemoveCachedValue()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.WebUiLocalization.PreferredLocale.v1"] = JsonDocument.Parse("\"en\"").RootElement.Clone()
                    }));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Success);

            var initial = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            await _target.RemoveItemAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            var removed = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);

            initial.Should().Be("en");
            removed.Should().BeNull();
            Mock.Get(_clientDataStorageAdapter)
                .Verify(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClientDataCacheInitializedAndSetItemAsStringFails_WHEN_GetItemAsStringAsyncAfterFallback_THEN_ShouldInvalidateCacheAndUseLocalStorageValue()
        {
            var apiResult = CreateFailureResult();

            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .SetupSequence(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.WebUiLocalization.PreferredLocale.v1"] = JsonDocument.Parse("\"en\"").RootElement.Clone()
                    }))
                .ReturnsAsync(ClientDataLoadResult.FromFailure(apiResult));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.FromFailure(apiResult));

            var initial = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            await _target.SetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", "en_GB", TestContext.Current.CancellationToken);
            var updated = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);

            initial.Should().Be("en");
            updated.Should().Be("en_GB");
            Mock.Get(_clientDataStorageAdapter)
                .Verify(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_ClientDataCacheInitializedAndRemoveFails_WHEN_GetItemAsStringAsyncAfterFallback_THEN_ShouldInvalidateCacheAndUseLocalStorageValue()
        {
            var apiResult = CreateFailureResult();

            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .SetupSequence(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.WebUiLocalization.PreferredLocale.v1"] = JsonDocument.Parse("\"en\"").RootElement.Clone()
                    }))
                .ReturnsAsync(ClientDataLoadResult.FromFailure(apiResult));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.FromFailure(apiResult));

            var initial = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            await _target.RemoveItemAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            var removed = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);

            initial.Should().Be("en");
            removed.Should().BeNull();
            Mock.Get(_clientDataStorageAdapter)
                .Verify(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_ClientDataCacheInitialized_WHEN_Invalidated_THEN_ShouldReloadClientDataOnNextRead()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            var loadCallCount = 0;
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    loadCallCount++;
                    return ClientDataLoadResult.FromEntries(
                        new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                        {
                            ["QbtMud.WebUiLocalization.PreferredLocale.v1"] = loadCallCount == 1
                                ? JsonDocument.Parse("\"en\"").RootElement.Clone()
                                : JsonDocument.Parse("\"en_GB\"").RootElement.Clone()
                        });
                });

            var initial = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            await _clientDataCacheInvalidationService.InvalidateClientDataCacheAsync(TestContext.Current.CancellationToken);
            var reloaded = await _target.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);

            initial.Should().Be("en");
            reloaded.Should().Be("en_GB");
            Mock.Get(_clientDataStorageAdapter)
                .Verify(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_ClientDataRoutingWithSupport_WHEN_SetItemAsStringAsync_THEN_ShouldStoreUsingClientDataAdapter()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));

            IReadOnlyDictionary<string, object?>? payload = null;
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyDictionary<string, object?>, CancellationToken>((entries, _) => payload = entries)
                .ReturnsAsync(ClientDataStorageResult.Success);

            await _target.SetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", "en", TestContext.Current.CancellationToken);

            payload.Should().NotBeNull();
            payload!.Should().ContainSingle();
            payload.Keys.Single().Should().Be("QbtMud.WebUiLocalization.PreferredLocale.v1");
            payload["QbtMud.WebUiLocalization.PreferredLocale.v1"].Should().Be("en");
        }

        [Fact]
        public async Task GIVEN_ClientDataSetAsStringCancelled_WHEN_SetItemAsStringAsync_THEN_ShouldPropagateCancellation()
        {
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException("cancelled"));

            Func<Task> action = async () =>
            {
                await _target.SetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", "en", TestContext.Current.CancellationToken);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GIVEN_LocalStorageRouting_WHEN_RemoveItemAsync_THEN_ShouldRemoveFromLocalStorage()
        {
            await _localStorageService.SetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", "en", TestContext.Current.CancellationToken);
            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.LocalStorage
                });

            await _target.RemoveItemAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);

            var result = await _localStorageService.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            result.Should().BeNull();
            Mock.Get(_clientDataStorageAdapter).Verify(
                adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_ClientDataRemoveCancelled_WHEN_RemoveItemAsync_THEN_ShouldPropagateCancellation()
        {
            await _localStorageService.SetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", "en", TestContext.Current.CancellationToken);

            Mock.Get(_storageRoutingService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.ClientData
                });
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException("cancelled"));

            Func<Task> action = async () =>
            {
                await _target.RemoveItemAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();

            var result = await _localStorageService.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            result.Should().Be("en");
        }

        private void ConfigureClientDataRouting()
        {
            Mock.Get(_storageRoutingService).Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new StorageRoutingSettings { MasterStorageType = StorageType.ClientData });
            Mock.Get(_webApiCapabilityService).Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
        }

        private void VerifyFailureHandled(ApiResultBase apiResult)
        {
            Mock.Get(_apiFeedbackWorkflow)
                .Verify(workflow => workflow.HandleFailureAsync(
                    apiResult,
                    It.IsAny<Func<string?, string>?>(),
                    It.IsAny<Severity>(),
                    It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        private static ApiResult CreateFailureResult()
        {
            return ApiResult.CreateFailure(new ApiFailure
            {
                Kind = ApiFailureKind.ServerError,
                Operation = "Operation",
                UserMessage = "Failure"
            });
        }
    }
}
