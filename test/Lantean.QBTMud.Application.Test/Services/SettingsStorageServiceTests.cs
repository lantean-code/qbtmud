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
        private readonly SettingsStorageService _target;

        public SettingsStorageServiceTests()
        {
            _localStorageService = new TestLocalStorageService();
            _storageRoutingService = Mock.Of<IStorageRoutingService>();
            _webApiCapabilityService = Mock.Of<IWebApiCapabilityService>();
            _clientDataStorageAdapter = Mock.Of<IClientDataStorageAdapter>();
            _apiFeedbackWorkflow = Mock.Of<IApiFeedbackWorkflow>();

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
                _localStorageService,
                _storageRoutingService,
                _webApiCapabilityService,
                _clientDataStorageAdapter,
                _apiFeedbackWorkflow);
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

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
                .ReturnsAsync(new WebApiCapabilityState("2.12.0", new Version(2, 12, 0), false));

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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
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
                adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
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
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
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
