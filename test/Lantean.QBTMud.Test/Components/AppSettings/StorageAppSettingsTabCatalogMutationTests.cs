using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.AppSettingsTabs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.AppSettingsTabs
{
    public sealed class StorageAppSettingsTabCatalogMutationTests : RazorComponentTestBase<StorageAppSettingsTab>
    {
        private readonly List<StorageCatalogGroupDefinition> _storageCatalogGroups;
        private readonly StorageRoutingSettings _storageRoutingSettings;
        private int _storageRoutingChangedCount;

        public StorageAppSettingsTabCatalogMutationTests()
        {
            _storageCatalogGroups =
            [
                new(
                    id: "themes",
                    displayNameSource: "Themes",
                    items:
                    [
                        new StorageCatalogItemDefinition("themes.selected-theme", "themes", "Selected theme", StorageCatalogItemMatchMode.ExactKey, "ThemeManager.SelectedThemeId", StorageItemSerializationMode.Json)
                    ])
            ];

            var storageCatalogServiceMock = TestContext.AddSingletonMock<IStorageCatalogService>();
            storageCatalogServiceMock
                .SetupGet(service => service.Groups)
                .Returns(_storageCatalogGroups);
            storageCatalogServiceMock
                .SetupGet(service => service.Items)
                .Returns(() => _storageCatalogGroups.SelectMany(group => group.Items).ToList());
            storageCatalogServiceMock
                .Setup(service => service.MatchItemByKey(It.IsAny<string>()))
                .Returns((string key) => _storageCatalogGroups
                    .SelectMany(group => group.Items)
                    .FirstOrDefault(item => string.Equals(item.MatchPattern, key, StringComparison.Ordinal)));

            var storageDiagnosticsServiceMock = TestContext.AddSingletonMock<IStorageDiagnosticsService>();
            storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<AppStorageEntry>());

            var webApiCapabilityServiceMock = TestContext.AddSingletonMock<IWebApiCapabilityService>();
            webApiCapabilityServiceMock
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.11.0", new Version(2, 11, 0), true));

            _storageRoutingSettings = StorageRoutingSettings.Default.Clone();
        }

        [Fact]
        public async Task GIVEN_GroupRemovedAfterRender_WHEN_GroupStorageTypeChanged_THEN_ChangeSavesWithoutGroupLookup()
        {
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageGroupStorageType-themes");
            });
            var groupStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageGroupStorageType-themes");
            var callbackCountBeforeChange = _storageRoutingChangedCount;

            _storageCatalogGroups.Clear();
            await target.InvokeAsync(() => groupStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));

            _storageRoutingSettings.GroupStorageTypes["themes"].Should().Be(StorageType.ClientData);
            _storageRoutingChangedCount.Should().Be(callbackCountBeforeChange + 1);
        }

        [Fact]
        public async Task GIVEN_GroupRemovedAfterRender_WHEN_ClearOverridesClicked_THEN_ClearReturnsWithoutChanges()
        {
            _storageRoutingSettings.ItemStorageTypes["themes.selected-theme"] = StorageType.ClientData;
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageGroupStorageType-themes");
            });
            target.Render();
            var clearButton = FindButton(target, "AppSettingsStorageClearGroupOverrides-themes");
            var callbackCountBeforeClear = _storageRoutingChangedCount;

            _storageCatalogGroups.Clear();
            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            _storageRoutingSettings.ItemStorageTypes.Should().ContainKey("themes.selected-theme");
            _storageRoutingChangedCount.Should().Be(callbackCountBeforeClear);
        }

        private IRenderedComponent<StorageAppSettingsTab> RenderTarget()
        {
            return TestContext.Render<StorageAppSettingsTab>(parameters =>
            {
                parameters.Add(component => component.StorageRoutingSettings, _storageRoutingSettings);
                parameters.Add(component => component.IsActive, true);
                parameters.Add(component => component.ReloadToken, 0);
                parameters.Add(component => component.StorageRoutingChanged, EventCallback.Factory.Create(this, OnStorageRoutingChanged));
                parameters.Add(component => component.BusyChanged, EventCallback.Factory.Create<bool>(this, _ => { }));
            });
        }

        private void OnStorageRoutingChanged()
        {
            _storageRoutingChangedCount++;
        }
    }
}
