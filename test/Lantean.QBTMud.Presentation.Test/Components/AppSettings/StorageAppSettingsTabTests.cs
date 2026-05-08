using System.Text.Json;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.AppSettingsTabs;
using Lantean.QBTMud.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Presentation.Test.Components.AppSettings
{
    public sealed class StorageAppSettingsTabTests : RazorComponentTestBase<StorageAppSettingsTab>
    {
        private readonly Mock<IStorageDiagnosticsService> _storageDiagnosticsServiceMock;
        private readonly Mock<IWebApiCapabilityService> _webApiCapabilityServiceMock;
        private readonly StorageRoutingSettings _storageRoutingSettings;
        private int _storageRoutingChangedCount;
        private int _busyChangedCount;

        public StorageAppSettingsTabTests()
        {
            _storageDiagnosticsServiceMock = TestContext.AddSingletonMock<IStorageDiagnosticsService>();
            _webApiCapabilityServiceMock = TestContext.AddSingletonMock<IWebApiCapabilityService>();
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<AppStorageEntry>());
            _webApiCapabilityServiceMock
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.11.0", new Version(2, 11, 0), true));

            _storageRoutingSettings = StorageRoutingSettings.Default.Clone();
            _storageRoutingSettings.GroupStorageTypes["themes"] = StorageType.ClientData;
            _storageRoutingSettings.ItemStorageTypes["themes.selected-theme"] = StorageType.ClientData;
        }

        [Fact]
        public async Task GIVEN_MasterStorageTypeChanged_WHEN_ClientDataSelected_THEN_ClearsOverridesAndRaisesCallback()
        {
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
            var masterStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");

            await target.InvokeAsync(() => masterStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));

            _storageRoutingSettings.MasterStorageType.Should().Be(StorageType.ClientData);
            _storageRoutingSettings.GroupStorageTypes.Should().BeEmpty();
            _storageRoutingSettings.ItemStorageTypes.Should().BeEmpty();
            _storageRoutingChangedCount.Should().Be(1);
        }

        [Fact]
        public void GIVEN_GroupStorageTypeControl_WHEN_Rendered_THEN_UsesHelperText()
        {
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
            var groupStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageGroupStorageType-themes");

            groupStorageTypeSelect.Instance.HelperText.Should().Be("Applies to all items in this group.");
        }

        [Fact]
        public async Task GIVEN_GroupStorageTypeUnchangedAndNoOverrides_WHEN_ValueSelected_THEN_DoesNotRaiseCallback()
        {
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
            var groupStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageGroupStorageType-general");
            var callbackCountBeforeChange = _storageRoutingChangedCount;

            await target.InvokeAsync(() => groupStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.LocalStorage));

            _storageRoutingChangedCount.Should().Be(callbackCountBeforeChange);
        }

        [Fact]
        public async Task GIVEN_GroupOverrideStoredAsExistingValue_WHEN_NewOverrideSelected_THEN_UpdatesStoredGroupOverride()
        {
            _storageRoutingSettings.GroupStorageTypes["themes"] = StorageType.LocalStorage;
            var target = RenderTarget(reloadToken: 14);
            var groupStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageGroupStorageType-themes");
            var callbackCountBeforeChange = _storageRoutingChangedCount;

            await target.InvokeAsync(() => groupStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));

            _storageRoutingSettings.GroupStorageTypes["themes"].Should().Be(StorageType.ClientData);
            _storageRoutingChangedCount.Should().Be(callbackCountBeforeChange + 1);
        }

        [Fact]
        public async Task GIVEN_GroupContainsItemOverrides_WHEN_GroupStorageTypeSetToMaster_THEN_ClearsOverridesAndRaisesCallback()
        {
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
            var groupStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageGroupStorageType-themes");
            var callbackCountBeforeChange = _storageRoutingChangedCount;

            await target.InvokeAsync(() => groupStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.LocalStorage));

            _storageRoutingSettings.ItemStorageTypes.Should().BeEmpty();
            _storageRoutingChangedCount.Should().Be(callbackCountBeforeChange + 1);
        }

        [Fact]
        public async Task GIVEN_ItemStorageTypeUnchanged_WHEN_SameValueSelected_THEN_DoesNotRaiseCallback()
        {
            _storageRoutingSettings.GroupStorageTypes.Remove("themes");
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
            target.Render();
            var overridesPanel = FindComponentByTestId<MudExpansionPanel>(target, "AppSettingsStorageGroupOverridesPanel-themes");
            await target.InvokeAsync(() => overridesPanel.Instance.ExpandAsync());
            var itemStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageItemStorageType-themes.selected-theme");
            var callbackCountBeforeChange = _storageRoutingChangedCount;

            await target.InvokeAsync(() => itemStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));

            _storageRoutingChangedCount.Should().Be(callbackCountBeforeChange);
        }

        [Fact]
        public async Task GIVEN_GroupOverridesPanelExpanded_WHEN_Collapsed_THEN_CollapsesWithoutErrors()
        {
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
            var overridesPanel = FindComponentByTestId<MudExpansionPanel>(target, "AppSettingsStorageGroupOverridesPanel-themes");

            await target.InvokeAsync(() => overridesPanel.Instance.ExpandAsync());
            await target.InvokeAsync(() => overridesPanel.Instance.CollapseAsync());

            overridesPanel.Instance.Expanded.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_GroupHasNoItemOverrides_WHEN_ClearOverridesClicked_THEN_DoesNotRaiseCallback()
        {
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
            var clearButton = FindButton(target, "AppSettingsStorageClearGroupOverrides-general");
            var callbackCountBeforeClear = _storageRoutingChangedCount;

            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            _storageRoutingChangedCount.Should().Be(callbackCountBeforeClear);
        }

        [Fact]
        public async Task GIVEN_GroupHasItemOverrides_WHEN_ClearOverridesClicked_THEN_ClearsOverridesAndRaisesCallback()
        {
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
            var clearButton = FindButton(target, "AppSettingsStorageClearGroupOverrides-themes");
            var callbackCountBeforeClear = _storageRoutingChangedCount;

            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            _storageRoutingSettings.ItemStorageTypes.Should().BeEmpty();
            _storageRoutingChangedCount.Should().Be(callbackCountBeforeClear + 1);
        }

        [Fact]
        public async Task GIVEN_StorageEntriesLoadInProgress_WHEN_InitiallyRendering_THEN_ShowsLoadingIndicator()
        {
            var loadTaskSource = new TaskCompletionSource<IReadOnlyList<AppStorageEntry>>(TaskCreationOptions.RunContinuationsAsynchronously);
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .Returns(loadTaskSource.Task);

            var target = RenderTarget(reloadToken: 1);

            target.WaitForAssertion(() =>
            {
                target.FindComponents<MudProgressCircular>().Should().NotBeEmpty();
            });

            loadTaskSource.SetResult(Array.Empty<AppStorageEntry>());
            target.WaitForState(() => target.FindComponents<MudProgressCircular>().Count == 0);
        }

        [Fact]
        public async Task GIVEN_RefreshStorageThrowsHttpRequest_WHEN_RefreshClicked_THEN_ShowsErrorStateWithoutThrowing()
        {
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("StorageError"));
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
            var refreshButton = FindButton(target, "AppSettingsStorageRefresh");

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            _busyChangedCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GIVEN_RefreshStorageThrowsJson_WHEN_RefreshClicked_THEN_ShowsErrorStateWithoutThrowing()
        {
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException("StorageError"));
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
            var refreshButton = FindButton(target, "AppSettingsStorageRefresh");

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            _busyChangedCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GIVEN_RefreshStorageThrowsInvalidOperation_WHEN_RefreshClicked_THEN_ShowsErrorStateWithoutThrowing()
        {
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("StorageError"));
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
            var refreshButton = FindButton(target, "AppSettingsStorageRefresh");

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            _busyChangedCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GIVEN_RefreshStorageThrowsJsException_WHEN_RefreshClicked_THEN_ShowsErrorStateWithoutThrowing()
        {
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("StorageError"));
            var target = RenderTarget();
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
            var refreshButton = FindButton(target, "AppSettingsStorageRefresh");

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            _busyChangedCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GIVEN_RemoveEntryThrowsHttpRequest_WHEN_DeleteClicked_THEN_ShowsErrorStateWithoutThrowing()
        {
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new AppStorageEntry(StorageType.LocalStorage, "QbtMud.AppSettings.State.v2", "AppSettings.State.v2", "{\"value\":true}", "{\"value\":true}", 14)
                ]);
            _storageDiagnosticsServiceMock
                .Setup(service => service.RemoveEntryAsync(It.IsAny<StorageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("RemoveError"));

            var target = RenderTarget(reloadToken: 2);
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsStorageDelete-LocalStorage-AppSettings.State.v2");

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            _storageDiagnosticsServiceMock.Verify(
                service => service.RemoveEntryAsync(StorageType.LocalStorage, "QbtMud.AppSettings.State.v2", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RemoveEntryThrowsJson_WHEN_DeleteClicked_THEN_ShowsErrorStateWithoutThrowing()
        {
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new AppStorageEntry(StorageType.LocalStorage, "QbtMud.AppSettings.State.v2", "AppSettings.State.v2", "{\"value\":true}", "{\"value\":true}", 14)
                ]);
            _storageDiagnosticsServiceMock
                .Setup(service => service.RemoveEntryAsync(It.IsAny<StorageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException("RemoveError"));

            var target = RenderTarget(reloadToken: 3);
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsStorageDelete-LocalStorage-AppSettings.State.v2");

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            _storageDiagnosticsServiceMock.Verify(
                service => service.RemoveEntryAsync(StorageType.LocalStorage, "QbtMud.AppSettings.State.v2", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RemoveEntryThrowsInvalidOperation_WHEN_DeleteClicked_THEN_ShowsErrorStateWithoutThrowing()
        {
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new AppStorageEntry(StorageType.LocalStorage, "QbtMud.AppSettings.State.v2", "AppSettings.State.v2", "{\"value\":true}", "{\"value\":true}", 14)
                ]);
            _storageDiagnosticsServiceMock
                .Setup(service => service.RemoveEntryAsync(It.IsAny<StorageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("RemoveError"));

            var target = RenderTarget(reloadToken: 4);
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsStorageDelete-LocalStorage-AppSettings.State.v2");

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            _storageDiagnosticsServiceMock.Verify(
                service => service.RemoveEntryAsync(StorageType.LocalStorage, "QbtMud.AppSettings.State.v2", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RemoveEntryThrowsJsException_WHEN_DeleteClicked_THEN_ShowsErrorStateWithoutThrowing()
        {
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new AppStorageEntry(StorageType.LocalStorage, "QbtMud.AppSettings.State.v2", "AppSettings.State.v2", "{\"value\":true}", "{\"value\":true}", 14)
                ]);
            _storageDiagnosticsServiceMock
                .Setup(service => service.RemoveEntryAsync(It.IsAny<StorageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("RemoveError"));

            var target = RenderTarget(reloadToken: 5);
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsStorageDelete-LocalStorage-AppSettings.State.v2");

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            _storageDiagnosticsServiceMock.Verify(
                service => service.RemoveEntryAsync(StorageType.LocalStorage, "QbtMud.AppSettings.State.v2", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClearEntriesThrowsHttpRequest_WHEN_ClearClicked_THEN_ShowsErrorStateWithoutThrowing()
        {
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new AppStorageEntry(StorageType.LocalStorage, "QbtMud.AppSettings.State.v2", "AppSettings.State.v2", "{\"value\":true}", "{\"value\":true}", 14)
                ]);
            _storageDiagnosticsServiceMock
                .Setup(service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("ClearError"));

            var target = RenderTarget(reloadToken: 6);
            var clearButton = FindButton(target, "AppSettingsStorageClearAll");

            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            _storageDiagnosticsServiceMock.Verify(
                service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClearEntriesThrowsJson_WHEN_ClearClicked_THEN_ShowsErrorStateWithoutThrowing()
        {
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new AppStorageEntry(StorageType.LocalStorage, "QbtMud.AppSettings.State.v2", "AppSettings.State.v2", "{\"value\":true}", "{\"value\":true}", 14)
                ]);
            _storageDiagnosticsServiceMock
                .Setup(service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException("ClearError"));

            var target = RenderTarget(reloadToken: 7);
            var clearButton = FindButton(target, "AppSettingsStorageClearAll");

            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            _storageDiagnosticsServiceMock.Verify(
                service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClearEntriesThrowsInvalidOperation_WHEN_ClearClicked_THEN_ShowsErrorStateWithoutThrowing()
        {
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new AppStorageEntry(StorageType.LocalStorage, "QbtMud.AppSettings.State.v2", "AppSettings.State.v2", "{\"value\":true}", "{\"value\":true}", 14)
                ]);
            _storageDiagnosticsServiceMock
                .Setup(service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("ClearError"));

            var target = RenderTarget(reloadToken: 8);
            var clearButton = FindButton(target, "AppSettingsStorageClearAll");

            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            _storageDiagnosticsServiceMock.Verify(
                service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClearEntriesThrowsJsException_WHEN_ClearClicked_THEN_ShowsErrorStateWithoutThrowing()
        {
            _storageDiagnosticsServiceMock
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new AppStorageEntry(StorageType.LocalStorage, "QbtMud.AppSettings.State.v2", "AppSettings.State.v2", "{\"value\":true}", "{\"value\":true}", 14)
                ]);
            _storageDiagnosticsServiceMock
                .Setup(service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("ClearError"));

            var target = RenderTarget(reloadToken: 9);
            var clearButton = FindButton(target, "AppSettingsStorageClearAll");

            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            _storageDiagnosticsServiceMock.Verify(
                service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public void GIVEN_CapabilityReadThrowsHttpRequest_WHEN_Rendered_THEN_FallsBackToNotSupportedState()
        {
            _webApiCapabilityServiceMock
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("CapabilityError"));

            var target = RenderTarget(reloadToken: 10);
            var supportChip = FindComponentByTestId<MudChip<string>>(target, "AppSettingsStorageSupport");

            GetChildContentText(supportChip.Instance.ChildContent).Should().Be("Not supported");
        }

        [Fact]
        public void GIVEN_CapabilityReadThrowsJson_WHEN_Rendered_THEN_FallsBackToNotSupportedState()
        {
            _webApiCapabilityServiceMock
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException("CapabilityError"));

            var target = RenderTarget(reloadToken: 11);
            var supportChip = FindComponentByTestId<MudChip<string>>(target, "AppSettingsStorageSupport");

            GetChildContentText(supportChip.Instance.ChildContent).Should().Be("Not supported");
        }

        [Fact]
        public void GIVEN_CapabilityReadThrowsInvalidOperation_WHEN_Rendered_THEN_FallsBackToNotSupportedState()
        {
            _webApiCapabilityServiceMock
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("CapabilityError"));

            var target = RenderTarget(reloadToken: 12);
            var supportChip = FindComponentByTestId<MudChip<string>>(target, "AppSettingsStorageSupport");

            GetChildContentText(supportChip.Instance.ChildContent).Should().Be("Not supported");
        }

        [Fact]
        public void GIVEN_CapabilityReadThrowsOperationCanceled_WHEN_Rendered_THEN_CallsCapabilityService()
        {
            _webApiCapabilityServiceMock
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException("CapabilityCanceled"));

            _ = RenderTarget(reloadToken: 13);

            _webApiCapabilityServiceMock.Verify(
                service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        private IRenderedComponent<StorageAppSettingsTab> RenderTarget(bool isActive = true, int reloadToken = 0)
        {
            return TestContext.Render<StorageAppSettingsTab>(parameters =>
            {
                parameters.Add(component => component.StorageRoutingSettings, _storageRoutingSettings);
                parameters.Add(component => component.IsActive, isActive);
                parameters.Add(component => component.ReloadToken, reloadToken);
                parameters.Add(component => component.StorageRoutingChanged, EventCallback.Factory.Create(this, OnStorageRoutingChanged));
                parameters.Add(component => component.BusyChanged, EventCallback.Factory.Create<bool>(this, OnBusyChanged));
            });
        }

        private void OnStorageRoutingChanged()
        {
            _storageRoutingChangedCount++;
        }

        private void OnBusyChanged(bool _)
        {
            _busyChangedCount++;
        }
    }
}
