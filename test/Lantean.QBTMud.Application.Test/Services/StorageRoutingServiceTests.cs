using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Core.Models;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Application.Test.Services
{
    public sealed class StorageRoutingServiceTests
    {
        private readonly TestLocalStorageService _localStorageService;
        private readonly IClientDataStorageAdapter _clientDataStorageAdapter;
        private readonly IWebApiCapabilityService _webApiCapabilityService;
        private readonly IStorageCatalogService _storageCatalogService;
        private readonly Mock<IJSRuntime> _jsRuntime;
        private readonly IApiFeedbackWorkflow _apiFeedbackWorkflow;
        private readonly StorageRoutingService _target;

        public StorageRoutingServiceTests()
        {
            _localStorageService = new TestLocalStorageService();
            _clientDataStorageAdapter = Mock.Of<IClientDataStorageAdapter>();
            _webApiCapabilityService = Mock.Of<IWebApiCapabilityService>();
            _storageCatalogService = new StorageCatalogService();
            _apiFeedbackWorkflow = Mock.Of<IApiFeedbackWorkflow>();
            _jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<BrowserStorageEntry[]?>(
                    "qbt.getLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ReturnsAsync(Array.Empty<BrowserStorageEntry>());
            Mock.Get(_apiFeedbackWorkflow)
                .Setup(workflow => workflow.HandleFailureAsync(
                    It.IsAny<ApiResultBase>(),
                    It.IsAny<Func<string?, string>?>(),
                    It.IsAny<Severity>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _target = new StorageRoutingService(
                _localStorageService,
                _clientDataStorageAdapter,
                _webApiCapabilityService,
                _storageCatalogService,
                new LocalStorageEntryAdapter(_jsRuntime.Object),
                _apiFeedbackWorkflow);
        }

        [Fact]
        public void GIVEN_MasterGroupAndItemOverrides_WHEN_ResolveEffectiveStorageType_THEN_ShouldApplyItemThenGroupThenMasterPrecedence()
        {
            var settings = new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage,
                GroupStorageTypes = new Dictionary<string, StorageType>(StringComparer.Ordinal)
                {
                    ["themes"] = StorageType.ClientData
                },
                ItemStorageTypes = new Dictionary<string, StorageType>(StringComparer.Ordinal)
                {
                    ["themes.selected-theme"] = StorageType.LocalStorage
                }
            };

            var itemStorageType = _target.ResolveEffectiveStorageType("ThemeManager.SelectedThemeId", settings, supportsClientData: true);
            var groupStorageType = _target.ResolveEffectiveStorageType("ThemeManager.LocalThemes", settings, supportsClientData: true);
            var unknownStorageType = _target.ResolveEffectiveStorageType("Unknown.Key", settings, supportsClientData: true);

            itemStorageType.Should().Be(StorageType.LocalStorage);
            groupStorageType.Should().Be(StorageType.ClientData);
            unknownStorageType.Should().Be(StorageType.LocalStorage);
        }

        [Fact]
        public async Task GIVEN_ExactJsonKeyInLocalStorage_WHEN_SavingClientDataRouting_THEN_ShouldMigrateValueAndRemoveLocalEntry()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            await _localStorageService.SetItemAsStringAsync("AppSettings.State.v2", "{\"theme\":\"dark\"}", TestContext.Current.CancellationToken);

            IReadOnlyDictionary<string, object?>? storedPayload = null;
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyDictionary<string, object?>, CancellationToken>((payload, _) => storedPayload = payload)
                .ReturnsAsync(ClientDataStorageResult.Success);

            var updated = await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);

            updated.MasterStorageType.Should().Be(StorageType.ClientData);
            storedPayload.Should().NotBeNull();
            storedPayload!.Should().ContainKey("QbtMud.AppSettings.State.v2");
            storedPayload["QbtMud.AppSettings.State.v2"].Should().BeOfType<JsonElement>();
            ((JsonElement)storedPayload["QbtMud.AppSettings.State.v2"]!).GetProperty("theme").GetString().Should().Be("dark");

            var localValue = await _localStorageService.GetItemAsStringAsync("AppSettings.State.v2", TestContext.Current.CancellationToken);
            localValue.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_DynamicTablePrefixKeysInLocalStorage_WHEN_SavingClientDataRouting_THEN_ShouldMigrateMatchingPrefixKeys()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<BrowserStorageEntry[]?>(
                    "qbt.getLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ReturnsAsync(
                [
                    new BrowserStorageEntry("QbtMud.DynamicTableTorrent.ColumnSort.T1", "{\"SortColumn\":\"name\",\"SortDirection\":1}"),
                    new BrowserStorageEntry("QbtMud.DynamicTableTorrent.ColumnWidths.T1", "{\"name\":120}")
                ]);

            var payloads = new List<IReadOnlyDictionary<string, object?>>();
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyDictionary<string, object?>, CancellationToken>((payload, _) => payloads.Add(payload))
                .ReturnsAsync(ClientDataStorageResult.Success);

            await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);

            payloads.Count.Should().BeGreaterThanOrEqualTo(2);
            payloads.SelectMany(payload => payload.Keys).Should().Contain("QbtMud.DynamicTableTorrent.ColumnSort.T1");
            payloads.SelectMany(payload => payload.Keys).Should().Contain("QbtMud.DynamicTableTorrent.ColumnWidths.T1");
        }

        [Fact]
        public async Task GIVEN_InvalidJsonDuringMigration_WHEN_SavingClientDataRouting_THEN_ShouldNotPersistRoutingSettings()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            await _localStorageService.SetItemAsStringAsync("AppSettings.State.v2", "{invalid-json}", TestContext.Current.CancellationToken);

            var act = async () => await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<JsonException>();

            var persisted = await _localStorageService.GetItemAsync<StorageRoutingSettings>(StorageRoutingSettings.StorageKey, TestContext.Current.CancellationToken);
            persisted.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_JsonValueInClientData_WHEN_SavingLocalStorageRouting_THEN_ShouldMigrateToLocalAndRemoveClientEntry()
        {
            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<string> keys, CancellationToken _) =>
                {
                    var dictionary = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                    if (keys.Contains("QbtMud.AppSettings.State.v2", StringComparer.Ordinal))
                    {
                        dictionary["QbtMud.AppSettings.State.v2"] = JsonDocument.Parse("{\"notifications\":true}").RootElement.Clone();
                    }

                    return Loaded(dictionary);
                });
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Loaded(new Dictionary<string, JsonElement>(StringComparer.Ordinal)));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Success);

            var updated = await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage
            }, TestContext.Current.CancellationToken);

            updated.MasterStorageType.Should().Be(StorageType.LocalStorage);

            var localValue = await _localStorageService.GetItemAsStringAsync("AppSettings.State.v2", TestContext.Current.CancellationToken);
            localValue.Should().Be("{\"notifications\":true}");
            Mock.Get(_clientDataStorageAdapter)
                .Verify(adapter => adapter.RemovePrefixedEntriesAsync(
                    It.Is<IEnumerable<string>>(keys => keys.Contains("QbtMud.AppSettings.State.v2", StringComparer.Ordinal)),
                    It.IsAny<CancellationToken>()),
                    Times.Once);
        }

        [Fact]
        public async Task GIVEN_InvalidStoredRoutingJson_WHEN_GetSettingsAsync_THEN_ShouldReturnDefaultSettings()
        {
            await _localStorageService.SetItemAsStringAsync(StorageRoutingSettings.StorageKey, "{", TestContext.Current.CancellationToken);

            var result = await _target.GetSettingsAsync(TestContext.Current.CancellationToken);

            result.MasterStorageType.Should().Be(StorageType.LocalStorage);
            result.GroupStorageTypes.Should().BeEmpty();
            result.ItemStorageTypes.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_NoRoutingChanges_WHEN_SaveSettingsAsync_THEN_ShouldReturnCurrentWithoutCapabilityCheck()
        {
            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage
            }, TestContext.Current.CancellationToken);

            var current = await _target.GetSettingsAsync(TestContext.Current.CancellationToken);
            var writesBeforeSave = _localStorageService.WriteCount;

            var result = await _target.SaveSettingsAsync(current.Clone(), TestContext.Current.CancellationToken);

            result.MasterStorageType.Should().Be(StorageType.LocalStorage);
            _localStorageService.WriteCount.Should().Be(writesBeforeSave);
            Mock.Get(_webApiCapabilityService).Verify(
                service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_UnsupportedClientDataGroupSelection_WHEN_SaveSettingsAsync_THEN_ShouldThrowInvalidOperationException()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.12.0", new Version(2, 12, 0), false));

            var act = async () => await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage,
                GroupStorageTypes = new Dictionary<string, StorageType>(StringComparer.Ordinal)
                {
                    ["themes"] = StorageType.ClientData
                }
            }, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_UnsupportedClientDataItemSelection_WHEN_SaveSettingsAsync_THEN_ShouldThrowInvalidOperationException()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.12.0", new Version(2, 12, 0), false));

            var act = async () => await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage,
                ItemStorageTypes = new Dictionary<string, StorageType>(StringComparer.Ordinal)
                {
                    ["themes.selected-theme"] = StorageType.ClientData
                }
            }, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public void GIVEN_ClientDataConfiguredAndUnsupported_WHEN_ResolveEffectiveStorageType_THEN_ShouldFallbackToLocalStorage()
        {
            var settings = new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            };

            var storageType = _target.ResolveEffectiveStorageType("Unknown.Key", settings, supportsClientData: false);

            storageType.Should().Be(StorageType.LocalStorage);
        }

        [Fact]
        public async Task GIVEN_RawStringValueInClientData_WHEN_SavingLocalStorageRouting_THEN_ShouldWriteUnquotedString()
        {
            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<string> keys, CancellationToken _) =>
                {
                    var dictionary = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                    if (keys.Contains("QbtMud.WebUiLocalization.PreferredLocale.v1", StringComparer.Ordinal))
                    {
                        dictionary["QbtMud.WebUiLocalization.PreferredLocale.v1"] = JsonDocument.Parse("\"en_GB\"").RootElement.Clone();
                    }

                    return Loaded(dictionary);
                });
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Loaded(new Dictionary<string, JsonElement>(StringComparer.Ordinal)));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Success);

            await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage
            }, TestContext.Current.CancellationToken);

            var localValue = await _localStorageService.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            localValue.Should().Be("en_GB");
        }

        [Fact]
        public async Task GIVEN_PrefixItemsInClientData_WHEN_SavingLocalStorageRouting_THEN_ShouldMigrateMatchingPrefixedKeys()
        {
            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Loaded(new Dictionary<string, JsonElement>(StringComparer.Ordinal)));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Loaded(new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["QbtMud.DynamicTableTorrent.ColumnSort.T1"] = JsonDocument.Parse("{\"SortColumn\":\"name\"}").RootElement.Clone(),
                    ["QbtMud.OtherKey"] = JsonDocument.Parse("1").RootElement.Clone(),
                    ["NotPrefixed"] = JsonDocument.Parse("2").RootElement.Clone()
                }));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Success);

            await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage
            }, TestContext.Current.CancellationToken);

            var migrated = await _localStorageService.GetItemAsStringAsync("DynamicTableTorrent.ColumnSort.T1", TestContext.Current.CancellationToken);
            migrated.Should().Be("{\"SortColumn\":\"name\"}");
            Mock.Get(_clientDataStorageAdapter).Verify(
                adapter => adapter.RemovePrefixedEntriesAsync(
                    It.Is<IEnumerable<string>>(keys => keys.Contains("QbtMud.DynamicTableTorrent.ColumnSort.T1", StringComparer.Ordinal)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_UnsupportedMasterClientDataSelection_WHEN_SaveSettingsAsync_THEN_ShouldThrowInvalidOperationException()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.12.0", new Version(2, 12, 0), false));

            var act = async () => await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_UnsupportedCapabilityAndCurrentClientData_WHEN_SavingLocalStorageRouting_THEN_ShouldSaveWithoutClientDataMigrationCalls()
        {
            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.12.0", new Version(2, 12, 0), false));

            var result = await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage
            }, TestContext.Current.CancellationToken);

            result.MasterStorageType.Should().Be(StorageType.LocalStorage);
            Mock.Get(_clientDataStorageAdapter).Verify(
                adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
            Mock.Get(_clientDataStorageAdapter).Verify(
                adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()),
                Times.Never);
            Mock.Get(_clientDataStorageAdapter).Verify(
                adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NullClientJsonValue_WHEN_SavingLocalStorageRouting_THEN_ShouldRemoveLocalEntry()
        {
            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);
            await _localStorageService.SetItemAsStringAsync("AppSettings.State.v2", "{\"enabled\":true}", TestContext.Current.CancellationToken);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<string> keys, CancellationToken _) =>
                {
                    var dictionary = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                    if (keys.Contains("QbtMud.AppSettings.State.v2", StringComparer.Ordinal))
                    {
                        dictionary["QbtMud.AppSettings.State.v2"] = JsonDocument.Parse("null").RootElement.Clone();
                    }

                    return Loaded(dictionary);
                });
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Loaded(new Dictionary<string, JsonElement>(StringComparer.Ordinal)));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Success);

            await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage
            }, TestContext.Current.CancellationToken);

            var localValue = await _localStorageService.GetItemAsStringAsync("AppSettings.State.v2", TestContext.Current.CancellationToken);
            localValue.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_RawStringNonStringClientValue_WHEN_SavingLocalStorageRouting_THEN_ShouldPersistRawJsonText()
        {
            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<string> keys, CancellationToken _) =>
                {
                    var dictionary = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                    if (keys.Contains("QbtMud.WebUiLocalization.PreferredLocale.v1", StringComparer.Ordinal))
                    {
                        dictionary["QbtMud.WebUiLocalization.PreferredLocale.v1"] = JsonDocument.Parse("1").RootElement.Clone();
                    }

                    return Loaded(dictionary);
                });
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Loaded(new Dictionary<string, JsonElement>(StringComparer.Ordinal)));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.Success);

            await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage
            }, TestContext.Current.CancellationToken);

            var localValue = await _localStorageService.GetItemAsStringAsync("WebUiLocalization.PreferredLocale.v1", TestContext.Current.CancellationToken);
            localValue.Should().Be("1");
        }

        [Fact]
        public async Task GIVEN_ChangedGroupOverride_WHEN_SaveSettingsAsync_THEN_ShouldNotTreatSettingsAsEquivalent()
        {
            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage,
                GroupStorageTypes = new Dictionary<string, StorageType>(StringComparer.Ordinal)
                {
                    ["themes"] = StorageType.LocalStorage
                }
            }, TestContext.Current.CancellationToken);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            var result = await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage,
                GroupStorageTypes = new Dictionary<string, StorageType>(StringComparer.Ordinal)
                {
                    ["themes"] = StorageType.ClientData
                }
            }, TestContext.Current.CancellationToken);

            result.GroupStorageTypes["themes"].Should().Be(StorageType.ClientData);
        }

        [Fact]
        public async Task GIVEN_ChangedItemOverride_WHEN_SaveSettingsAsync_THEN_ShouldNotTreatSettingsAsEquivalent()
        {
            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage,
                ItemStorageTypes = new Dictionary<string, StorageType>(StringComparer.Ordinal)
                {
                    ["themes.selected-theme"] = StorageType.LocalStorage
                }
            }, TestContext.Current.CancellationToken);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            var result = await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage,
                ItemStorageTypes = new Dictionary<string, StorageType>(StringComparer.Ordinal)
                {
                    ["themes.selected-theme"] = StorageType.ClientData
                }
            }, TestContext.Current.CancellationToken);

            result.ItemStorageTypes["themes.selected-theme"].Should().Be(StorageType.ClientData);
        }

        [Fact]
        public async Task GIVEN_PrefixedCatalogMatchPattern_WHEN_MigratingToClientData_THEN_ShouldNotDoublePrefixKey()
        {
            var customCatalogGroups = new List<StorageCatalogGroupDefinition>
            {
                new(
                    id: "custom",
                    displayNameSource: "Custom",
                    items:
                    [
                        new StorageCatalogItemDefinition(
                            id: "custom.prefixed",
                            groupId: "custom",
                            displayNameSource: "Prefixed",
                            matchMode: StorageCatalogItemMatchMode.ExactKey,
                            matchPattern: "QbtMud.Custom.Key",
                            serializationMode: StorageItemSerializationMode.RawString)
                    ])
            };
            var customCatalogItems = customCatalogGroups
                .SelectMany(group => group.Items)
                .ToList();
            var customCatalogService = new Mock<IStorageCatalogService>(MockBehavior.Strict);
            customCatalogService.SetupGet(service => service.Groups).Returns(customCatalogGroups);
            customCatalogService.SetupGet(service => service.Items).Returns(customCatalogItems);
            customCatalogService
                .Setup(service => service.MatchItemByKey(It.IsAny<string>()))
                .Returns<string>(key => customCatalogItems.SingleOrDefault(item => item.MatchPattern == key));

            var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
            var localStorageService = new TestLocalStorageService();
            var clientDataStorageAdapter = new Mock<IClientDataStorageAdapter>(MockBehavior.Strict);
            var webApiCapabilityService = new Mock<IWebApiCapabilityService>(MockBehavior.Strict);
            var apiFeedbackWorkflow = new Mock<IApiFeedbackWorkflow>(MockBehavior.Strict);

            await localStorageService.SetItemAsStringAsync("QbtMud.Custom.Key", "value", TestContext.Current.CancellationToken);

            webApiCapabilityService
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            IReadOnlyDictionary<string, object?>? payload = null;
            clientDataStorageAdapter
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyDictionary<string, object?>, CancellationToken>((entries, _) => payload = entries)
                .ReturnsAsync(ClientDataStorageResult.Success);

            var customTarget = new StorageRoutingService(
                localStorageService,
                clientDataStorageAdapter.Object,
                webApiCapabilityService.Object,
                customCatalogService.Object,
                new LocalStorageEntryAdapter(jsRuntime.Object),
                apiFeedbackWorkflow.Object);

            await customTarget.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);

            payload.Should().NotBeNull();
            payload!.Should().ContainKey("QbtMud.Custom.Key");
            payload.Keys.Single().Should().Be("QbtMud.Custom.Key");
        }

        [Fact]
        public async Task GIVEN_EqualOverridesInCurrentAndTarget_WHEN_SaveSettingsAsync_THEN_UsesEquivalentPath()
        {
            var existing = new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage,
                GroupStorageTypes = new Dictionary<string, StorageType>(StringComparer.Ordinal)
                {
                    ["themes"] = StorageType.LocalStorage
                },
                ItemStorageTypes = new Dictionary<string, StorageType>(StringComparer.Ordinal)
                {
                    ["themes.selected-theme"] = StorageType.LocalStorage
                }
            };
            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, existing, TestContext.Current.CancellationToken);

            var result = await _target.SaveSettingsAsync(existing.Clone(), TestContext.Current.CancellationToken);

            result.GroupStorageTypes["themes"].Should().Be(StorageType.LocalStorage);
            result.ItemStorageTypes["themes.selected-theme"].Should().Be(StorageType.LocalStorage);
            Mock.Get(_webApiCapabilityService).Verify(
                service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_LocalPrefixEntriesContainInvalidEntries_WHEN_SavingClientDataRouting_THEN_OnlyValidPrefixedEntriesAreMigrated()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<BrowserStorageEntry[]?>(
                    "qbt.getLocalStorageEntriesByPrefix",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ReturnsAsync(
                [
                    null!,
                    new BrowserStorageEntry(string.Empty, "{}"),
                    new BrowserStorageEntry("NotPrefixed", "{}"),
                    new BrowserStorageEntry("QbtMud.DynamicTableTorrent.ColumnSort.T1", "{\"SortColumn\":\"name\"}")
                ]);

            var payloads = new List<IReadOnlyDictionary<string, object?>>();
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyDictionary<string, object?>, CancellationToken>((payload, _) => payloads.Add(payload))
                .ReturnsAsync(ClientDataStorageResult.Success);

            await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);

            payloads.SelectMany(payload => payload.Keys).Should().Contain("QbtMud.DynamicTableTorrent.ColumnSort.T1");
            payloads.SelectMany(payload => payload.Keys).Should().NotContain("NotPrefixed");
        }

        [Fact]
        public async Task GIVEN_ClientDataStoreFails_WHEN_SavingClientDataRouting_THEN_ShouldThrowInvalidOperationException()
        {
            var apiResult = CreateFailureResult();

            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.StorePrefixedEntriesAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.FromFailure(apiResult));

            await _localStorageService.SetItemAsStringAsync("AppSettings.State.v2", "{\"theme\":\"dark\"}", TestContext.Current.CancellationToken);

            var act = async () => await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Unable to migrate storage item 'general.app-settings'.");

            var localValue = await _localStorageService.GetItemAsStringAsync("AppSettings.State.v2", TestContext.Current.CancellationToken);
            localValue.Should().Be("{\"theme\":\"dark\"}");

            var persisted = await _localStorageService.GetItemAsync<StorageRoutingSettings>(StorageRoutingSettings.StorageKey, TestContext.Current.CancellationToken);
            persisted.Should().BeNull();
            VerifyFailureHandled(apiResult);
        }

        [Fact]
        public async Task GIVEN_ClientDataLoadFails_WHEN_SavingLocalStorageRouting_THEN_ShouldThrowInvalidOperationException()
        {
            var apiResult = CreateFailureResult();

            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromFailure(apiResult));

            var act = async () => await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage
            }, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Unable to migrate storage item 'general.app-settings'.");

            var persisted = await _localStorageService.GetItemAsync<StorageRoutingSettings>(StorageRoutingSettings.StorageKey, TestContext.Current.CancellationToken);
            persisted.Should().NotBeNull();
            persisted!.MasterStorageType.Should().Be(StorageType.ClientData);
            VerifyFailureHandled(apiResult);
        }

        [Fact]
        public async Task GIVEN_ClientDataRemoveFails_WHEN_SavingLocalStorageRouting_THEN_ShouldThrowInvalidOperationException()
        {
            var apiResult = CreateFailureResult();

            await _localStorageService.SetItemAsync(StorageRoutingSettings.StorageKey, new StorageRoutingSettings
            {
                MasterStorageType = StorageType.ClientData
            }, TestContext.Current.CancellationToken);
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Loaded(new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["QbtMud.AppSettings.State.v2"] = JsonDocument.Parse("{\"enabled\":true}").RootElement.Clone()
                }));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Loaded(new Dictionary<string, JsonElement>(StringComparer.Ordinal)));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.RemovePrefixedEntriesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataStorageResult.FromFailure(apiResult));

            var act = async () => await _target.SaveSettingsAsync(new StorageRoutingSettings
            {
                MasterStorageType = StorageType.LocalStorage
            }, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Unable to migrate storage item 'general.app-settings'.");

            var localValue = await _localStorageService.GetItemAsStringAsync("AppSettings.State.v2", TestContext.Current.CancellationToken);
            localValue.Should().Be("{\"enabled\":true}");

            var persisted = await _localStorageService.GetItemAsync<StorageRoutingSettings>(StorageRoutingSettings.StorageKey, TestContext.Current.CancellationToken);
            persisted.Should().NotBeNull();
            persisted!.MasterStorageType.Should().Be(StorageType.ClientData);
            VerifyFailureHandled(apiResult);
        }

        [Fact]
        public async Task GIVEN_ConcurrentGetSettingsCalls_WHEN_FirstCallInitializesCache_THEN_SecondCallUsesCachedValueInsideSemaphore()
        {
            var localStorageService = new Mock<ILocalStorageService>(MockBehavior.Strict);
            var clientDataStorageAdapter = new Mock<IClientDataStorageAdapter>(MockBehavior.Strict);
            var webApiCapabilityService = new Mock<IWebApiCapabilityService>(MockBehavior.Strict);
            var storageCatalogService = new StorageCatalogService();
            var jsRuntime = new Mock<IJSRuntime>(MockBehavior.Strict);
            var apiFeedbackWorkflow = new Mock<IApiFeedbackWorkflow>(MockBehavior.Strict);

            var readCompletion = new TaskCompletionSource<StorageRoutingSettings?>(TaskCreationOptions.RunContinuationsAsynchronously);
            localStorageService
                .Setup(service => service.GetItemAsync<StorageRoutingSettings>(StorageRoutingSettings.StorageKey, It.IsAny<CancellationToken>()))
                .Returns((string _, CancellationToken _) => new ValueTask<StorageRoutingSettings?>(readCompletion.Task));

            var target = new StorageRoutingService(
                localStorageService.Object,
                clientDataStorageAdapter.Object,
                webApiCapabilityService.Object,
                storageCatalogService,
                new LocalStorageEntryAdapter(jsRuntime.Object),
                apiFeedbackWorkflow.Object);

            var firstTask = target.GetSettingsAsync(TestContext.Current.CancellationToken);
            var secondTask = target.GetSettingsAsync(TestContext.Current.CancellationToken);

            readCompletion.SetResult(StorageRoutingSettings.Default.Clone());

            var first = await firstTask;
            var second = await secondTask;

            first.MasterStorageType.Should().Be(StorageType.LocalStorage);
            second.MasterStorageType.Should().Be(StorageType.LocalStorage);
            localStorageService.Verify(
                service => service.GetItemAsync<StorageRoutingSettings>(StorageRoutingSettings.StorageKey, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private static ClientDataLoadResult Loaded(IReadOnlyDictionary<string, JsonElement> entries)
        {
            return ClientDataLoadResult.FromEntries(entries);
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
