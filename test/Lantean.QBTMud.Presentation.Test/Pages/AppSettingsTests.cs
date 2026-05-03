using AwesomeAssertions;
using Bunit;

#if DEBUG

#endif

using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Components.AppSettingsTabs;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;

#if DEBUG

#endif

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using System.Text.Json;
using AppSettingsModel = Lantean.QBTMud.Core.Models.AppSettings;
using AppSettingsPage = Lantean.QBTMud.Pages.AppSettings;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Presentation.Test.Pages
{
    public sealed class AppSettingsTests : RazorComponentTestBase<AppSettingsPage>
    {
        private readonly IAppBuildInfoService _appBuildInfoService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly IAppUpdateService _appUpdateService;
        private readonly IBrowserNotificationService _browserNotificationService;
        private readonly IThemeManagerService _themeManagerService;
        private readonly IStorageDiagnosticsService _storageDiagnosticsService;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly ISnackbar _snackbar;
        private readonly IPwaInstallPromptService _pwaInstallPromptService;

        public AppSettingsTests()
        {
            _appBuildInfoService = Mock.Of<IAppBuildInfoService>();
            _appSettingsService = Mock.Of<IAppSettingsService>();
            _appUpdateService = Mock.Of<IAppUpdateService>();
            _browserNotificationService = Mock.Of<IBrowserNotificationService>();
            _themeManagerService = Mock.Of<IThemeManagerService>();
            _storageDiagnosticsService = Mock.Of<IStorageDiagnosticsService>();
            _dialogWorkflow = Mock.Of<IDialogWorkflow>();
            _snackbar = Mock.Of<ISnackbar>();
            _pwaInstallPromptService = Mock.Of<IPwaInstallPromptService>();

            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettingsModel());
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettingsModel());
            Mock.Get(_appSettingsService)
                .Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppSettingsModel settings, CancellationToken _) => settings.Clone());
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.0.0", "v1.0.0", "https://example.invalid", DateTime.UtcNow),
                    false,
                    true,
                    DateTime.UtcNow));
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Default);
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);
            Mock.Get(_browserNotificationService)
                .Setup(service => service.SubscribePermissionChangesAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            Mock.Get(_browserNotificationService)
                .Setup(service => service.UnsubscribePermissionChangesAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new AppStorageEntry(StorageType.LocalStorage, "QbtMud.AppSettings.State.v1", "AppSettings.State.v1", "{\"value\":true}", "{\"value\":true}", 14)
                ]);
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.RemoveEntryAsync(It.IsAny<StorageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState());
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("dismissed");
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.ShowInstallPromptTestAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    CanPrompt = true
                });

            TestContext.Services.RemoveAll<IAppBuildInfoService>();
            TestContext.Services.RemoveAll<IAppSettingsService>();
            TestContext.Services.RemoveAll<IAppUpdateService>();
            TestContext.Services.RemoveAll<IBrowserNotificationService>();
            TestContext.Services.RemoveAll<IThemeManagerService>();
            TestContext.Services.RemoveAll<IStorageDiagnosticsService>();
            TestContext.Services.RemoveAll<IDialogWorkflow>();
            TestContext.Services.RemoveAll<ISnackbar>();
            TestContext.Services.RemoveAll<IPwaInstallPromptService>();
            TestContext.Services.AddSingleton(_appBuildInfoService);
            TestContext.Services.AddSingleton(_appSettingsService);
            TestContext.Services.AddSingleton(_appUpdateService);
            TestContext.Services.AddSingleton(_browserNotificationService);
            TestContext.Services.AddSingleton(_themeManagerService);
            TestContext.Services.AddSingleton(_storageDiagnosticsService);
            TestContext.Services.AddSingleton(_dialogWorkflow);
            TestContext.Services.AddSingleton(_snackbar);
            TestContext.Services.AddSingleton(_pwaInstallPromptService);
        }

        [Fact]
        public async Task GIVEN_UpdateChecksToggle_WHEN_Disabled_THEN_TracksSettingWithoutPersisting()
        {
            var target = RenderPage();
            var updateChecksSwitch = FindSwitch(target, "AppSettingsUpdateChecksEnabled");
            _appSettingsService.ClearInvocations();

            await target.InvokeAsync(() => updateChecksSwitch.Instance.ValueChanged.InvokeAsync(false));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void GIVEN_PageRendered_WHEN_VisualTabVisible_THEN_RendersThemeModePreferenceSelect()
        {
            var target = RenderPage();

            var themeModeSelect = FindComponentByTestId<MudSelect<ThemeModePreference>>(target, "AppSettingsThemeModePreference");

            themeModeSelect.Instance.GetState(x => x.Value).Should().Be(ThemeModePreference.System);
        }

        [Fact]
        public void GIVEN_PageRendered_WHEN_TabsLoaded_THEN_VisualTabIsFirst()
        {
            var target = RenderPage();
            var tabPanels = target.FindComponents<MudTabPanel>();

            tabPanels.First().Instance.Text.Should().Be("Visual");
        }

        [Fact]
        public async Task GIVEN_ThemeModePreferenceChanged_WHEN_DarkSelected_THEN_TracksSettingWithoutPersisting()
        {
            var target = RenderPage();
            var themeModeSelect = FindComponentByTestId<MudSelect<ThemeModePreference>>(target, "AppSettingsThemeModePreference");
            _appSettingsService.ClearInvocations();

            await target.InvokeAsync(() => themeModeSelect.Instance.ValueChanged.InvokeAsync(ThemeModePreference.Dark));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_ThemeRepositoryUrlChanged_WHEN_ValidHttpsSelected_THEN_TracksSettingWithoutPersisting()
        {
            var target = RenderPage();
            var repositoryUrlField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsThemeRepositoryIndexUrl");
            _appSettingsService.ClearInvocations();

            await target.InvokeAsync(() => repositoryUrlField.Instance.ValueChanged.InvokeAsync("https://example.com/index.json"));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_TrackedChanges_WHEN_SaveClicked_THEN_PersistsPendingSettings()
        {
            var target = RenderPage();
            var updateChecksSwitch = FindSwitch(target, "AppSettingsUpdateChecksEnabled");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");
            _appSettingsService.ClearInvocations();
            _themeManagerService.ClearInvocations();
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => updateChecksSwitch.Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettingsModel>(settings => !settings.UpdateChecksEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_themeManagerService).Verify(
                service => service.ApplyPersistedThemeModePreference(It.IsAny<ThemeModePreference>()),
                Times.Never);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "App settings saved.", StringComparison.Ordinal)),
                    Severity.Success,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ThemeModePreferenceChanged_WHEN_SaveClicked_THEN_NotifiesThemeManager()
        {
            var target = RenderPage();
            var themeModeSelect = FindComponentByTestId<MudSelect<ThemeModePreference>>(target, "AppSettingsThemeModePreference");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");
            _themeManagerService.ClearInvocations();

            await target.InvokeAsync(() => themeModeSelect.Instance.ValueChanged.InvokeAsync(ThemeModePreference.Dark));
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_themeManagerService).Verify(
                service => service.ApplyPersistedThemeModePreference(ThemeModePreference.Dark),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_TrackedChanges_WHEN_UndoClicked_THEN_RevertsWithoutPersisting()
        {
            var target = RenderPage();
            var themeModeSelect = FindComponentByTestId<MudSelect<ThemeModePreference>>(target, "AppSettingsThemeModePreference");
            var undoButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsUndoButton");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");
            _appSettingsService.ClearInvocations();

            await target.InvokeAsync(() => themeModeSelect.Instance.ValueChanged.InvokeAsync(ThemeModePreference.Dark));
            await target.InvokeAsync(() => undoButton.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NoPendingChanges_WHEN_ReloadClicked_THEN_RefreshesStateWithoutConfirmation()
        {
            var target = RenderPage();
            var reloadButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsReloadButton");
            _appSettingsService.ClearInvocations();
            _dialogWorkflow.ClearInvocations();

            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_appSettingsService).Verify(
                service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_dialogWorkflow).Verify(
                workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_PendingChangesAndReloadCancelled_WHEN_ReloadClicked_THEN_KeepsDraftAndSkipsRefresh()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var target = RenderPage();
            var updateChecksSwitch = FindSwitch(target, "AppSettingsUpdateChecksEnabled");
            var reloadButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsReloadButton");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");
            _appSettingsService.ClearInvocations();

            await target.InvokeAsync(() => updateChecksSwitch.Instance.ValueChanged.InvokeAsync(false));
            saveButton.Instance.Disabled.Should().BeFalse();

            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_appSettingsService).Verify(
                service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
            saveButton.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_PendingChangesAndReloadConfirmed_WHEN_ReloadClicked_THEN_DiscardsDraftAndRefreshesState()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettingsModel
                {
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = true,
                    ThemeModePreference = ThemeModePreference.Dark,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false
                });

            var target = RenderPage();
            var updateChecksSwitch = FindSwitch(target, "AppSettingsUpdateChecksEnabled");
            var reloadButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsReloadButton");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            await target.InvokeAsync(() => updateChecksSwitch.Instance.ValueChanged.InvokeAsync(false));
            saveButton.Instance.Disabled.Should().BeFalse();

            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_appSettingsService).Verify(
                service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
            saveButton.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ReloadInProgress_WHEN_ReloadClickedAgain_THEN_SecondCallIsIgnored()
        {
            var refreshTaskSource = new TaskCompletionSource<AppSettingsModel>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .Returns(refreshTaskSource.Task);

            var target = RenderPage();
            var reloadButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsReloadButton");
            _appSettingsService.ClearInvocations();

            var firstReloadTask = target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_appSettingsService).Verify(
                    service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_appSettingsService).Verify(
                service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            refreshTaskSource.SetResult(AppSettingsModel());
            await firstReloadTask;
        }

        [Fact]
        public async Task GIVEN_ReloadFails_WHEN_ReloadClicked_THEN_ShowsErrorSnackbar()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("ReloadError"));

            var target = RenderPage();
            var reloadButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsReloadButton");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to refresh app settings.", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public void GIVEN_PageRendered_WHEN_NoTrackedChanges_THEN_SaveAndUndoAreDisabled()
        {
            var target = RenderPage();
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");
            var undoButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsUndoButton");

            saveButton.Instance.Disabled.Should().BeTrue();
            undoButton.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NotificationsToggle_WHEN_Enabled_THEN_RequestsPermissionWithoutPersisting()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            _appSettingsService.ClearInvocations();

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_browserNotificationService).Verify(
                service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_BrowserNotificationsEnabled_WHEN_DownloadCompletedUnchecked_THEN_TracksSettingWithoutPersisting()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));
            _appSettingsService.ClearInvocations();

            var downloadCompletedCheckbox = FindComponentByTestId<MudCheckBox<bool>>(target, "AppSettingsDownloadFinishedNotificationsEnabled");
            await target.InvokeAsync(() => downloadCompletedCheckbox.Instance.ValueChanged.InvokeAsync(false));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void GIVEN_DefaultPermission_WHEN_PageRendered_THEN_PermissionIndicatorUsesWarningColor()
        {
            var target = RenderPage();
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Warning);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Not requested");
        }

        [Fact]
        public async Task GIVEN_GrantedPermission_WHEN_NotificationsEnabled_THEN_PermissionIndicatorUsesSuccessColor()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Success);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Granted");
        }

        [Fact]
        public async Task GIVEN_AddedTorrentNotificationsToggle_WHEN_Enabled_THEN_TracksSettingWithoutPersisting()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));
            _appSettingsService.ClearInvocations();

            var addedNotificationsCheckbox = FindComponentByTestId<MudCheckBox<bool>>(target, "AppSettingsTorrentAddedNotificationsEnabled");

            await target.InvokeAsync(() => addedNotificationsCheckbox.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void GIVEN_NotificationsDisabled_WHEN_PageRendered_THEN_NotificationTypeCheckboxesAndSnackbarToggleAreHidden()
        {
            var target = RenderPage();

            var hasSnackbarToggle = target
                .FindComponents<FieldSwitch>()
                .Any(component => HasTestId(component, "AppSettingsTorrentAddedSnackbarsEnabledWithNotifications"));
            var hasDownloadCompletedCheckbox = target
                .FindComponents<MudCheckBox<bool>>()
                .Any(component => HasTestId(component, "AppSettingsDownloadFinishedNotificationsEnabled"));
            var hasTorrentAddedCheckbox = target
                .FindComponents<MudCheckBox<bool>>()
                .Any(component => HasTestId(component, "AppSettingsTorrentAddedNotificationsEnabled"));

            hasSnackbarToggle.Should().BeFalse();
            hasDownloadCompletedCheckbox.Should().BeFalse();
            hasTorrentAddedCheckbox.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_NotificationsEnabledAndTorrentAddedEnabled_WHEN_SnackbarToggleEnabled_THEN_TracksSettingWithoutPersisting()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));
            var addedNotificationsCheckbox = FindComponentByTestId<MudCheckBox<bool>>(target, "AppSettingsTorrentAddedNotificationsEnabled");
            await target.InvokeAsync(() => addedNotificationsCheckbox.Instance.ValueChanged.InvokeAsync(true));
            _appSettingsService.ClearInvocations();

            var snackbarToggle = FindSwitch(target, "AppSettingsTorrentAddedSnackbarsEnabledWithNotifications");
            await target.InvokeAsync(() => snackbarToggle.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NotificationsEnabledAndTorrentAddedDisabled_WHEN_PageRendered_THEN_SnackbarToggleIsHidden()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            var hasSnackbarToggle = target
                .FindComponents<FieldSwitch>()
                .Any(component => HasTestId(component, "AppSettingsTorrentAddedSnackbarsEnabledWithNotifications"));

            hasSnackbarToggle.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_StorageActions_WHEN_DeleteAndClearInvoked_THEN_CallsStorageDiagnosticsService()
        {
            var target = RenderPage();
            await ActivateStorageTab(target);
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsStorageDelete-LocalStorage-AppSettings.State.v1");
            var clearButton = FindButton(target, "AppSettingsStorageClearAll");

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.RemoveEntryAsync(StorageType.LocalStorage, "QbtMud.AppSettings.State.v1", It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_StorageTabActive_WHEN_ReloadClicked_THEN_RefreshesStorageEntries()
        {
            var target = RenderPage();
            await ActivateStorageTab(target);
            var reloadButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsReloadButton");
            _storageDiagnosticsService.ClearInvocations();

            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.GetEntriesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_StorageCleared_WHEN_ClearAllClicked_THEN_NavigatesHomeWithForceReload()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("/app-settings");
            var target = RenderPage();
            await ActivateStorageTab(target);
            var clearButton = FindButton(target, "AppSettingsStorageClearAll");

            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public void GIVEN_InitializationPending_WHEN_PageRendered_THEN_ShowsLoadingIndicator()
        {
            var settingsTaskSource = new TaskCompletionSource<AppSettingsModel>();
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .Returns(settingsTaskSource.Task);

            var target = RenderPage();

            var loadingIndicator = FindComponentByTestId<MudProgressLinear>(target, "AppSettingsLoading");
            loadingIndicator.Instance.Indeterminate.Should().BeTrue();

            settingsTaskSource.SetResult(AppSettingsModel());
        }

        [Fact]
        public async Task GIVEN_DrawerClosed_WHEN_BackClicked_THEN_NavigatesHome()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("/app-settings");
            var target = RenderPage(drawerOpen: false);

            var backButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsBackButton");
            await target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public async Task GIVEN_DrawerClosedWithPendingChangesAndExitCancelled_WHEN_BackClicked_THEN_RemainsOnPage()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("/app-settings");
            var target = RenderPage(drawerOpen: false);
            var updateChecksSwitch = FindSwitch(target, "AppSettingsUpdateChecksEnabled");
            await target.InvokeAsync(() => updateChecksSwitch.Instance.ValueChanged.InvokeAsync(false));

            var backButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsBackButton");
            await target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_dialogWorkflow).Verify(
                    workflow => workflow.ShowConfirmDialog(
                        It.Is<string>(title => string.Equals(title, "Unsaved Changes", StringComparison.Ordinal)),
                        It.Is<string>(message => string.Equals(message, "Are you sure you want to leave without saving your changes?", StringComparison.Ordinal))),
                    Times.Once);
            });
            navigationManager.Uri.Should().EndWith("/app-settings");
        }

        [Fact]
        public async Task GIVEN_DrawerClosedWithPendingChangesAndExitConfirmed_WHEN_BackClicked_THEN_NavigatesHome()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("/app-settings");
            var target = RenderPage(drawerOpen: false);
            var updateChecksSwitch = FindSwitch(target, "AppSettingsUpdateChecksEnabled");
            await target.InvokeAsync(() => updateChecksSwitch.Instance.ValueChanged.InvokeAsync(false));

            var backButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsBackButton");
            await target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_dialogWorkflow).Verify(
                    workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()),
                    Times.Once);
            });
            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public void GIVEN_DrawerOpen_WHEN_PageRendered_THEN_BackButtonHidden()
        {
            var target = RenderPage(drawerOpen: true);

            var hasBackButton = target
                .FindComponents<MudIconButton>()
                .Any(component => HasTestId(component, "AppSettingsBackButton"));

            hasBackButton.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_NoPendingChanges_WHEN_UndoClicked_THEN_NoStateChangesOccur()
        {
            var target = RenderPage();
            var undoButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsUndoButton");

            await target.InvokeAsync(() => undoButton.Instance.OnClick.InvokeAsync());

            undoButton.Instance.Disabled.Should().BeTrue();
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_ThemeModePreferenceUnchanged_WHEN_SystemSelected_THEN_LeavesPendingStateUnchanged()
        {
            var target = RenderPage();
            var themeModeSelect = FindComponentByTestId<MudSelect<ThemeModePreference>>(target, "AppSettingsThemeModePreference");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            await target.InvokeAsync(() => themeModeSelect.Instance.ValueChanged.InvokeAsync(ThemeModePreference.System));

            saveButton.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NoOverridesAndSameMasterStorageType_WHEN_MasterStorageTypeChanged_THEN_NoPendingChangesAreCreated()
        {
            var target = RenderPage();
            await ActivateStorageTab(target);
            var masterStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            await target.InvokeAsync(() => masterStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.LocalStorage));

            saveButton.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_GroupOverrideSelectedThenCleared_WHEN_GroupStorageTypeChanged_THEN_PendingChangesReset()
        {
            var target = RenderPage();
            await ActivateStorageTab(target);
            var groupStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageGroupStorageType-themes");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            await target.InvokeAsync(() => groupStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));
            saveButton.Instance.Disabled.Should().BeFalse();

            await target.InvokeAsync(() => groupStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.LocalStorage));

            saveButton.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ItemOverrideSelectedThenCleared_WHEN_ItemStorageTypeChanged_THEN_PendingChangesReset()
        {
            var target = RenderPage();
            await ActivateStorageTab(target);
            var overridesPanel = FindComponentByTestId<MudExpansionPanel>(target, "AppSettingsStorageGroupOverridesPanel-themes");
            await target.InvokeAsync(() => overridesPanel.Instance.ExpandAsync());
            var itemStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageItemStorageType-themes.selected-theme");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            await target.InvokeAsync(() => itemStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));
            saveButton.Instance.Disabled.Should().BeFalse();

            await target.InvokeAsync(() => itemStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.LocalStorage));

            saveButton.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NoStorageEntries_WHEN_PageRendered_THEN_ShowsEmptyStorageMessage()
        {
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<AppStorageEntry>());

            var target = RenderPage();
            await ActivateStorageTab(target);
            var emptyMessage = FindComponentByTestId<MudText>(target, "AppSettingsStorageEmpty");

            GetChildContentText(emptyMessage.Instance.ChildContent).Should().Be("No qbtmud storage entries found.");
        }

        [Fact]
        public void GIVEN_UpdateStatusUnavailable_WHEN_PageRendered_THEN_ShowsNotAvailableAndHidesReleaseLink()
        {
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Message"));

            var target = RenderPage();
            var latestRelease = FindComponentByTestId<MudText>(target, "AppSettingsLatestRelease");
            var updateStatus = FindComponentByTestId<MudText>(target, "AppSettingsUpdateStatus");
            var hasReleaseLink = target
                .FindComponents<MudLink>()
                .Any(component => HasTestId(component, "AppSettingsLatestReleaseLink"));

            GetChildContentText(latestRelease.Instance.ChildContent).Should().Be("Not available");
            GetChildContentText(updateStatus.Instance.ChildContent).Should().Be("Not available");
            hasReleaseLink.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_UpdateStatusWithoutRelease_WHEN_PageRendered_THEN_ShowsNotAvailableAndHidesReleaseLink()
        {
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    latestRelease: null,
                    isUpdateAvailable: false,
                    canCompareVersions: true,
                    checkedAtUtc: DateTime.UtcNow));

            var target = RenderPage();
            var latestRelease = FindComponentByTestId<MudText>(target, "AppSettingsLatestRelease");
            var hasReleaseLink = target
                .FindComponents<MudLink>()
                .Any(component => HasTestId(component, "AppSettingsLatestReleaseLink"));

            GetChildContentText(latestRelease.Instance.ChildContent).Should().Be("Not available");
            hasReleaseLink.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ReleaseUrlWhitespace_WHEN_PageRendered_THEN_HidesReleaseLink()
        {
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.0.0", "v1.0.0", "   ", DateTime.UtcNow),
                    isUpdateAvailable: false,
                    canCompareVersions: true,
                    checkedAtUtc: DateTime.UtcNow));

            var target = RenderPage();

            var hasReleaseLink = target
                .FindComponents<MudLink>()
                .Any(component => HasTestId(component, "AppSettingsLatestReleaseLink"));

            hasReleaseLink.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_UpdateAvailable_WHEN_PageRendered_THEN_StatusTextShowsUpdateAvailable()
        {
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v2.0.0", "v2.0.0", "https://example.invalid", DateTime.UtcNow),
                    isUpdateAvailable: true,
                    canCompareVersions: true,
                    checkedAtUtc: DateTime.UtcNow));

            var target = RenderPage();
            var updateStatus = FindComponentByTestId<MudText>(target, "AppSettingsUpdateStatus");

            GetChildContentText(updateStatus.Instance.ChildContent).Should().Be("Update available");
        }

        [Fact]
        public void GIVEN_ComparableAndNoUpdate_WHEN_PageRendered_THEN_StatusTextShowsUpToDate()
        {
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.0.0", "v1.0.0", "https://example.invalid", DateTime.UtcNow),
                    isUpdateAvailable: false,
                    canCompareVersions: true,
                    checkedAtUtc: DateTime.UtcNow));

            var target = RenderPage();
            var updateStatus = FindComponentByTestId<MudText>(target, "AppSettingsUpdateStatus");

            GetChildContentText(updateStatus.Instance.ChildContent).Should().Be("Up to date");
        }

        [Fact]
        public void GIVEN_UpdateStatusNotComparable_WHEN_PageRendered_THEN_StatusTextShowsNotAvailable()
        {
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.0.0-preview", "v1.0.0-preview", "https://example.invalid", DateTime.UtcNow),
                    isUpdateAvailable: false,
                    canCompareVersions: false,
                    checkedAtUtc: DateTime.UtcNow));

            var target = RenderPage();
            var updateStatus = FindComponentByTestId<MudText>(target, "AppSettingsUpdateStatus");

            GetChildContentText(updateStatus.Instance.ChildContent).Should().Be("Not available");
        }

        [Fact]
        public void GIVEN_DeniedPermission_WHEN_PageRendered_THEN_PermissionIndicatorUsesErrorColorAndDeniedText()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            var target = RenderPage();
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Error);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Denied");
        }

        [Fact]
        public void GIVEN_DeniedPermissionAndNotificationsEnabled_WHEN_PageRendered_THEN_NotificationsAreTurnedOffAndPersisted()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettingsModel
                {
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false
                });

            var target = RenderPage();
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            target.WaitForAssertion(() =>
            {
                var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
                notificationsSwitch.Instance.Value.Should().BeFalse();
                saveButton.Instance.Disabled.Should().BeTrue();
                Mock.Get(_appSettingsService).Verify(
                    service => service.SaveSettingsAsync(
                        It.Is<AppSettingsModel>(settings =>
                            !settings.NotificationsEnabled
                            && settings.UpdateChecksEnabled
                            && settings.DownloadFinishedNotificationsEnabled
                            && !settings.TorrentAddedNotificationsEnabled
                            && !settings.TorrentAddedSnackbarsEnabledWithNotifications),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            });
        }

        [Fact]
        public void GIVEN_DefaultPermissionAndNotificationsEnabled_WHEN_PageRendered_THEN_NotificationsAreTurnedOffAndPersisted()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Default);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettingsModel
                {
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false
                });

            var target = RenderPage();
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            target.WaitForAssertion(() =>
            {
                var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
                notificationsSwitch.Instance.Value.Should().BeFalse();
                saveButton.Instance.Disabled.Should().BeTrue();
                Mock.Get(_appSettingsService).Verify(
                    service => service.SaveSettingsAsync(
                        It.Is<AppSettingsModel>(settings =>
                            !settings.NotificationsEnabled
                            && settings.UpdateChecksEnabled
                            && settings.DownloadFinishedNotificationsEnabled
                            && !settings.TorrentAddedNotificationsEnabled
                            && !settings.TorrentAddedSnackbarsEnabledWithNotifications),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            });
        }

        [Fact]
        public void GIVEN_DeniedPermissionAndCorrectionSaveFails_WHEN_PageRendered_THEN_NotificationsRemainDirtyForManualSave()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettingsModel
                {
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false
                });
            Mock.Get(_appSettingsService)
                .Setup(service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("StorageError"));

            var target = RenderPage();
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            target.WaitForAssertion(() =>
            {
                var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
                notificationsSwitch.Instance.Value.Should().BeFalse();
                saveButton.Instance.Disabled.Should().BeFalse();
                Mock.Get(_appSettingsService).Verify(
                    service => service.SaveSettingsAsync(
                        It.Is<AppSettingsModel>(settings => !settings.NotificationsEnabled),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_GrantedPermissionAndNotificationsEnabled_WHEN_PermissionChangesToDeniedWithoutReload_THEN_NotificationsAreTurnedOffAndPersisted()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettingsModel
                {
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false
                });

            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");
            var notificationsTab = target.FindComponent<NotificationsAppSettingsTab>();

            notificationsSwitch.Instance.Value.Should().BeTrue();

            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            await target.InvokeAsync(() => notificationsTab.Instance.OnNotificationPermissionChanged());

            target.WaitForAssertion(() =>
            {
                var updatedNotificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
                updatedNotificationsSwitch.Instance.Value.Should().BeFalse();
                saveButton.Instance.Disabled.Should().BeTrue();
                Mock.Get(_appSettingsService).Verify(
                    service => service.SaveSettingsAsync(
                        It.Is<AppSettingsModel>(settings =>
                            !settings.NotificationsEnabled
                            && settings.UpdateChecksEnabled
                            && settings.DownloadFinishedNotificationsEnabled
                            && !settings.TorrentAddedNotificationsEnabled
                            && !settings.TorrentAddedSnackbarsEnabledWithNotifications),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_PendingThemeDraft_WHEN_NotificationPermissionChangesToDenied_THEN_PendingThemeDraftRemainsDirtyWhilePersistingNotificationCorrection()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettingsModel
                {
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = true,
                    ThemeModePreference = ThemeModePreference.System,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false
                });

            var target = RenderPage();
            var themeModeSelect = FindComponentByTestId<MudSelect<ThemeModePreference>>(target, "AppSettingsThemeModePreference");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");
            var notificationsTab = target.FindComponent<NotificationsAppSettingsTab>();

            await target.InvokeAsync(() => themeModeSelect.Instance.ValueChanged.InvokeAsync(ThemeModePreference.Dark));
            saveButton.Instance.Disabled.Should().BeFalse();

            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            await target.InvokeAsync(() => notificationsTab.Instance.OnNotificationPermissionChanged());

            target.WaitForAssertion(() =>
            {
                themeModeSelect.Instance.GetState(x => x.Value).Should().Be(ThemeModePreference.Dark);
                saveButton.Instance.Disabled.Should().BeFalse();
                Mock.Get(_appSettingsService).Verify(
                    service => service.SaveSettingsAsync(
                        It.Is<AppSettingsModel>(settings =>
                            !settings.NotificationsEnabled
                            && settings.ThemeModePreference == ThemeModePreference.System
                            && settings.UpdateChecksEnabled
                            && settings.DownloadFinishedNotificationsEnabled
                            && !settings.TorrentAddedNotificationsEnabled
                            && !settings.TorrentAddedSnackbarsEnabledWithNotifications),
                        It.IsAny<CancellationToken>()),
                    Times.Once);
            });
        }

        [Fact]
        public void GIVEN_UnsupportedPermission_WHEN_PageRendered_THEN_PermissionIndicatorUsesDefaultColorAndUnsupportedText()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Unsupported);

            var target = RenderPage();
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Default);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Unsupported");
        }

        [Fact]
        public void GIVEN_InsecurePermission_WHEN_PageRendered_THEN_PermissionIndicatorUsesWarningColorAndInsecureText()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Insecure);

            var target = RenderPage();
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Warning);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Insecure");
        }

        [Fact]
        public void GIVEN_UnsupportedPermission_WHEN_PageRendered_THEN_NotificationsSwitchIsDisabled()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Unsupported);

            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            notificationsSwitch.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_UnknownPermissionAndNotificationsEnabled_WHEN_PageRendered_THEN_NotificationsRemainEnabledWithoutSilentSave()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Unknown);
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettingsModel
                {
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false
                });

            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            notificationsSwitch.Instance.Value.Should().BeTrue();
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_UnsupportedPermission_WHEN_NotificationsEnabledRequested_THEN_RequestIsIgnored()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Unsupported);

            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            _browserNotificationService.ClearInvocations();
            _appSettingsService.ClearInvocations();

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_browserNotificationService).Verify(
                service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()),
                Times.Never);
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void GIVEN_UnexpectedPermissionValue_WHEN_PageRendered_THEN_PermissionIndicatorFallsBackToUnsupported()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((BrowserNotificationPermission)999);

            var target = RenderPage();
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Default);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Unsupported");
        }

        [Fact]
        public void GIVEN_GetPermissionThrowsJsException_WHEN_PageRendered_THEN_PermissionIndicatorFallsBackToUnknown()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("Denied"));

            var target = RenderPage();
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Default);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Unknown");
        }

        [Fact]
        public async Task GIVEN_UpdateChecksToggleUnchanged_WHEN_ValueIsSame_THEN_DoesNotPersist()
        {
            var target = RenderPage();
            var updateChecksSwitch = FindSwitch(target, "AppSettingsUpdateChecksEnabled");
            _appSettingsService.ClearInvocations();

            await target.InvokeAsync(() => updateChecksSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NotificationPermissionDenied_WHEN_EnablingNotifications_THEN_ShowsWarningAndKeepsDisabled()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            _appSettingsService.ClearInvocations();
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Browser notification permission was not granted.", StringComparison.Ordinal)),
                    Severity.Warning,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RequestPermissionThrowsJsException_WHEN_EnablingNotifications_THEN_ShowsErrorWithoutPersisting()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("PermissionError"));

            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            _appSettingsService.ClearInvocations();
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to update notification permission: PermissionError", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NotificationsInitiallyEnabled_WHEN_DisablingNotifications_THEN_RefreshesPermissionWithoutPersisting()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettingsModel
                {
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false
                });

            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            _browserNotificationService.ClearInvocations();
            _appSettingsService.ClearInvocations();

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(false));

            Mock.Get(_browserNotificationService).Verify(
                service => service.GetPermissionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NotificationToggleInProgress_WHEN_EnabledAgain_THEN_SecondRequestIsIgnored()
        {
            var permissionTaskSource = new TaskCompletionSource<BrowserNotificationPermission>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_browserNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .Returns(permissionTaskSource.Task);

            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            _browserNotificationService.ClearInvocations();

            var firstToggleTask = target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            target.WaitForAssertion(() =>
            {
                Mock.Get(_browserNotificationService).Verify(
                    service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            var pendingSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            await target.InvokeAsync(() => pendingSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_browserNotificationService).Verify(
                service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            permissionTaskSource.SetResult(BrowserNotificationPermission.Granted);
            await firstToggleTask;
        }

        [Fact]
        public async Task GIVEN_DownloadFinishedToggleUnchanged_WHEN_ValueIsSame_THEN_DoesNotPersist()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));
            _appSettingsService.ClearInvocations();

            var downloadCompletedCheckbox = FindComponentByTestId<MudCheckBox<bool>>(target, "AppSettingsDownloadFinishedNotificationsEnabled");
            await target.InvokeAsync(() => downloadCompletedCheckbox.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_TorrentAddedToggleUnchanged_WHEN_ValueIsSame_THEN_DoesNotPersist()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));
            _appSettingsService.ClearInvocations();

            var addedNotificationsCheckbox = FindComponentByTestId<MudCheckBox<bool>>(target, "AppSettingsTorrentAddedNotificationsEnabled");
            await target.InvokeAsync(() => addedNotificationsCheckbox.Instance.ValueChanged.InvokeAsync(false));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_TorrentAddedSnackbarToggleUnchanged_WHEN_ValueIsSame_THEN_DoesNotPersist()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));
            var addedNotificationsCheckbox = FindComponentByTestId<MudCheckBox<bool>>(target, "AppSettingsTorrentAddedNotificationsEnabled");
            await target.InvokeAsync(() => addedNotificationsCheckbox.Instance.ValueChanged.InvokeAsync(true));
            _appSettingsService.ClearInvocations();

            var snackbarToggle = FindSwitch(target, "AppSettingsTorrentAddedSnackbarsEnabledWithNotifications");
            await target.InvokeAsync(() => snackbarToggle.Instance.ValueChanged.InvokeAsync(false));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_CheckNowClicked_WHEN_UpdateCheckSucceeds_THEN_UsesForceRefresh()
        {
            var target = RenderPage();
            var checkNowButton = FindButton(target, "AppSettingsCheckNow");
            _appUpdateService.ClearInvocations();

            await target.InvokeAsync(() => checkNowButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_appUpdateService).Verify(
                service => service.GetUpdateStatusAsync(true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_CheckNowFails_WHEN_Clicked_THEN_ShowsWarningSnackbar()
        {
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(true, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("NetworkError"));

            var target = RenderPage();
            var checkNowButton = FindButton(target, "AppSettingsCheckNow");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => checkNowButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to check for updates.", StringComparison.Ordinal)),
                    Severity.Warning,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_CheckNowAlreadyInProgress_WHEN_ClickedAgain_THEN_SecondCallIsIgnored()
        {
            var updateTaskSource = new TaskCompletionSource<AppUpdateStatus>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(true, It.IsAny<CancellationToken>()))
                .Returns(updateTaskSource.Task);

            var target = RenderPage();
            var checkNowButton = FindButton(target, "AppSettingsCheckNow");
            _appUpdateService.ClearInvocations();

            var firstCheckTask = target.InvokeAsync(() => checkNowButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_appUpdateService).Verify(
                    service => service.GetUpdateStatusAsync(true, It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            await target.InvokeAsync(() => checkNowButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_appUpdateService).Verify(
                service => service.GetUpdateStatusAsync(true, It.IsAny<CancellationToken>()),
                Times.Once);

            updateTaskSource.SetResult(new AppUpdateStatus(
                new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                new AppReleaseInfo("v1.0.0", "v1.0.0", "https://example.invalid", DateTime.UtcNow),
                false,
                true,
                DateTime.UtcNow));
            await firstCheckTask;
        }

        [Fact]
        public async Task GIVEN_PwaTabActivated_WHEN_StatusLoaded_THEN_RendersPwaStatus()
        {
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    CanPrompt = true,
                    IsIos = true
                });

            var target = RenderPage();
            await ActivatePwaTab(target);

            var statusField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsPwaStatus");
            var promptStatusField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsPwaInstallPromptStatus");
            var platformField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsPwaPlatform");

            statusField.Instance.GetState(x => x.Value).Should().Be("Not installed");
            promptStatusField.Instance.GetState(x => x.Value).Should().Be("Install prompt available");
            platformField.Instance.GetState(x => x.Value).Should().Be("iOS browser");
        }

        [Fact]
        public async Task GIVEN_PwaInstallAvailable_WHEN_InstallNowClicked_THEN_RequestsInstallPromptAndRefreshesStatus()
        {
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    CanPrompt = true
                });
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("accepted");

            var target = RenderPage();
            await ActivatePwaTab(target);
            var installNowButton = FindButton(target, "AppSettingsPwaInstallNow");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => installNowButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_pwaInstallPromptService).Verify(
                service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_pwaInstallPromptService).Verify(
                service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()),
                Times.AtLeast(2));
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Install prompt result: accepted", StringComparison.Ordinal)),
                    Severity.Info,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_PwaStateIsIosWithoutPrompt_WHEN_PwaTabActivated_THEN_RendersIosInstallHint()
        {
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    IsIos = true,
                    CanPrompt = false
                });

            var target = RenderPage();
            await ActivatePwaTab(target);

            var hintText = FindComponentByTestId<MudText>(target, "AppSettingsPwaIosHint");
            GetChildContentText(hintText.Instance.ChildContent).Should().Be("On iPhone or iPad, tap Share, then Add to Home Screen.");
        }

        [Fact]
        public async Task GIVEN_PwaStateIsInstalledOnIos_WHEN_PwaTabActivated_THEN_DoesNotRenderIosInstallHint()
        {
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    IsIos = true,
                    IsInstalled = true
                });

            var target = RenderPage();
            await ActivatePwaTab(target);

            var hintTexts = target.FindComponents<MudText>()
                .Where(component => HasTestId(component, "AppSettingsPwaIosHint"));
            hintTexts.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_RefreshPwaStatusThrowsJsException_WHEN_RefreshStatusClicked_THEN_ShowsWarningSnackbar()
        {
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("StatusError"));

            var target = RenderPage();
            await ActivatePwaTab(target);
            var refreshStatusButton = FindButton(target, "AppSettingsPwaRefreshStatus");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => refreshStatusButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to read app install status.", StringComparison.Ordinal)),
                    Severity.Warning,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RefreshPwaStatusThrowsInvalidOperationException_WHEN_RefreshStatusClicked_THEN_ShowsWarningSnackbar()
        {
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("StatusError"));

            var target = RenderPage();
            await ActivatePwaTab(target);
            var refreshStatusButton = FindButton(target, "AppSettingsPwaRefreshStatus");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => refreshStatusButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to read app install status.", StringComparison.Ordinal)),
                    Severity.Warning,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RequestPwaInstallThrowsJsException_WHEN_InstallNowClicked_THEN_ShowsWarningSnackbar()
        {
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    CanPrompt = true
                });
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("RequestError"));

            var target = RenderPage();
            await ActivatePwaTab(target);
            var installNowButton = FindButton(target, "AppSettingsPwaInstallNow");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => installNowButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to request app install.", StringComparison.Ordinal)),
                    Severity.Warning,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RequestPwaInstallThrowsInvalidOperationException_WHEN_InstallNowClicked_THEN_ShowsWarningSnackbar()
        {
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    CanPrompt = true
                });
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("RequestError"));

            var target = RenderPage();
            await ActivatePwaTab(target);
            var installNowButton = FindButton(target, "AppSettingsPwaInstallNow");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => installNowButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to request app install.", StringComparison.Ordinal)),
                    Severity.Warning,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_PwaStatusRefreshInProgress_WHEN_RefreshStatusClickedTwice_THEN_SecondRefreshIsIgnored()
        {
            var statusTaskSource = new TaskCompletionSource<PwaInstallPromptState>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    CanPrompt = true
                });

            var target = RenderPage();
            await ActivatePwaTab(target);

            var invocationCount = 0;
            Mock.Get(_pwaInstallPromptService)
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    invocationCount++;
                    return invocationCount == 1
                        ? statusTaskSource.Task
                        : Task.FromResult(new PwaInstallPromptState());
                });

            var refreshStatusButton = FindButton(target, "AppSettingsPwaRefreshStatus");
            _pwaInstallPromptService.ClearInvocations();

            var firstRefreshTask = target.InvokeAsync(() => refreshStatusButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                invocationCount.Should().Be(1);
            });
            var invocationCountBeforeSecondRefresh = invocationCount;

            await target.InvokeAsync(() => refreshStatusButton.Instance.OnClick.InvokeAsync());

            invocationCount.Should().Be(invocationCountBeforeSecondRefresh);

            statusTaskSource.SetResult(new PwaInstallPromptState
            {
                CanPrompt = true
            });
            await firstRefreshTask;
        }

#if DEBUG

        [Fact]
        public async Task GIVEN_TestPromptRequested_WHEN_ShowInstallPromptClicked_THEN_UsesSharedPromptServiceAndUpdatesPromptStatus()
        {
            await TestContext.LocalStorage.SetItemAsync("PwaInstallPrompt.Dismissed.v1", true, Xunit.TestContext.Current.CancellationToken);
            var target = RenderPage();
            await ActivatePwaTab(target);
            var testSnackbarButton = FindButton(target, "AppSettingsPwaShowSnackbarTest");

            await target.InvokeAsync(() => testSnackbarButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_pwaInstallPromptService).Verify(
                service => service.ShowInstallPromptTestAsync(It.IsAny<CancellationToken>()),
                Times.Once);
            TestContext.LocalStorage.Snapshot().Should().NotContainKey("PwaInstallPrompt.Dismissed.v1");

            var promptStatusField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsPwaInstallPromptStatus");
            promptStatusField.Instance.GetState(x => x.Value).Should().Be("Install prompt available");
        }

#endif

        [Fact]
        public async Task GIVEN_RefreshStorageFails_WHEN_RefreshClicked_THEN_ShowsErrorSnackbar()
        {
            var initialEntries = new[]
            {
                new AppStorageEntry(StorageType.LocalStorage, "QbtMud.AppSettings.State.v1", "AppSettings.State.v1", "{\"value\":true}", "{\"value\":true}", 14)
            };
            var invocationCount = 0;
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    invocationCount++;
                    if (invocationCount == 1)
                    {
                        return initialEntries;
                    }

                    throw new InvalidOperationException("StorageError");
                });

            var target = RenderPage();
            await ActivateStorageTab(target);
            var refreshButton = FindButton(target, "AppSettingsStorageRefresh");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to load storage entries.", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RefreshStorageInProgress_WHEN_RefreshClickedAgain_THEN_SecondRefreshIsIgnored()
        {
            var initialEntries = new[]
            {
                new AppStorageEntry(StorageType.LocalStorage, "QbtMud.AppSettings.State.v1", "AppSettings.State.v1", "{\"value\":true}", "{\"value\":true}", 14)
            };
            var refreshTaskSource = new TaskCompletionSource<IReadOnlyList<AppStorageEntry>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var invocationCount = 0;
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    invocationCount++;
                    if (invocationCount == 1)
                    {
                        return Task.FromResult<IReadOnlyList<AppStorageEntry>>(initialEntries);
                    }

                    return refreshTaskSource.Task;
                });

            var target = RenderPage();
            await ActivateStorageTab(target);
            var refreshButton = FindButton(target, "AppSettingsStorageRefresh");
            _storageDiagnosticsService.ClearInvocations();

            var firstRefreshTask = target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_storageDiagnosticsService).Verify(
                    service => service.GetEntriesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.GetEntriesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            refreshTaskSource.SetResult(initialEntries);
            await firstRefreshTask;
        }

        [Fact]
        public async Task GIVEN_RemoveStorageFails_WHEN_DeleteClicked_THEN_ShowsErrorSnackbar()
        {
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.RemoveEntryAsync(It.IsAny<StorageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DeleteError"));

            var target = RenderPage();
            await ActivateStorageTab(target);
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsStorageDelete-LocalStorage-AppSettings.State.v1");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to remove storage entry.", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RemoveStorageInProgress_WHEN_DeleteClickedAgain_THEN_SecondDeleteIsIgnored()
        {
            var removeTaskSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.RemoveEntryAsync(It.IsAny<StorageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(removeTaskSource.Task);

            var target = RenderPage();
            await ActivateStorageTab(target);
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsStorageDelete-LocalStorage-AppSettings.State.v1");
            _storageDiagnosticsService.ClearInvocations();

            var firstDeleteTask = target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_storageDiagnosticsService).Verify(
                    service => service.RemoveEntryAsync(It.IsAny<StorageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.RemoveEntryAsync(It.IsAny<StorageType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);

            removeTaskSource.SetResult();
            await firstDeleteTask;
        }

        [Fact]
        public async Task GIVEN_ClearStorageFails_WHEN_ClearClicked_THEN_ShowsErrorSnackbar()
        {
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("ClearError"));

            var target = RenderPage();
            await ActivateStorageTab(target);
            var clearButton = FindButton(target, "AppSettingsStorageClearAll");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to clear storage entries.", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ClearStorageInProgress_WHEN_ClearClickedAgain_THEN_SecondClearIsIgnored()
        {
            var clearTaskSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()))
                .Returns(clearTaskSource.Task);

            var target = RenderPage();
            await ActivateStorageTab(target);
            var clearButton = FindButton(target, "AppSettingsStorageClearAll");
            _storageDiagnosticsService.ClearInvocations();

            var firstClearTask = target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_storageDiagnosticsService).Verify(
                    service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.ClearEntriesAsync(null, It.IsAny<CancellationToken>()),
                Times.Once);

            clearTaskSource.SetResult(1);
            await firstClearTask;
        }

        [Fact]
        public async Task GIVEN_StorageRoutingSaveThrowsInvalidOperation_WHEN_SaveClicked_THEN_ShowsStorageErrorAndSkipsSettingsSave()
        {
            var storageRoutingService = new Mock<IStorageRoutingService>(MockBehavior.Strict);
            storageRoutingService
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(StorageRoutingSettings.Default);
            storageRoutingService
                .Setup(service => service.ResolveEffectiveStorageType(It.IsAny<string>(), It.IsAny<StorageRoutingSettings>(), It.IsAny<bool>()))
                .Returns((string _, StorageRoutingSettings settings, bool supportsClientData) =>
                {
                    if (settings.MasterStorageType == StorageType.ClientData && !supportsClientData)
                    {
                        return StorageType.LocalStorage;
                    }

                    return settings.MasterStorageType;
                });
            storageRoutingService
                .Setup(service => service.SaveSettingsAsync(It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("StorageError"));

            var webApiCapabilityService = new Mock<IWebApiCapabilityService>(MockBehavior.Strict);
            webApiCapabilityService
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            TestContext.Services.RemoveAll<IStorageRoutingService>();
            TestContext.Services.RemoveAll<IWebApiCapabilityService>();
            TestContext.Services.AddSingleton(storageRoutingService.Object);
            TestContext.Services.AddSingleton(webApiCapabilityService.Object);

            var target = RenderPage();
            await ActivateStorageTab(target);
            var masterStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            await target.InvokeAsync(() => masterStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to save storage settings: StorageError", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(It.IsAny<AppSettingsModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_StorageRoutingSaveThrowsUnexpectedException_WHEN_SaveClicked_THEN_ShowsGenericStorageError()
        {
            var storageRoutingService = new Mock<IStorageRoutingService>(MockBehavior.Strict);
            storageRoutingService
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(StorageRoutingSettings.Default);
            storageRoutingService
                .Setup(service => service.ResolveEffectiveStorageType(It.IsAny<string>(), It.IsAny<StorageRoutingSettings>(), It.IsAny<bool>()))
                .Returns((string _, StorageRoutingSettings settings, bool supportsClientData) =>
                {
                    if (settings.MasterStorageType == StorageType.ClientData && !supportsClientData)
                    {
                        return StorageType.LocalStorage;
                    }

                    return settings.MasterStorageType;
                });
            storageRoutingService
                .Setup(service => service.SaveSettingsAsync(It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException("StorageError"));

            var webApiCapabilityService = new Mock<IWebApiCapabilityService>(MockBehavior.Strict);
            webApiCapabilityService
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            TestContext.Services.RemoveAll<IStorageRoutingService>();
            TestContext.Services.RemoveAll<IWebApiCapabilityService>();
            TestContext.Services.AddSingleton(storageRoutingService.Object);
            TestContext.Services.AddSingleton(webApiCapabilityService.Object);

            var target = RenderPage();
            await ActivateStorageTab(target);
            var masterStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            await target.InvokeAsync(() => masterStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to save storage settings.", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ReloadThrowsHttpRequest_WHEN_ReloadClicked_THEN_ShowsRefreshErrorSnackbar()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("ReloadError"));

            var target = RenderPage();
            var reloadButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsReloadButton");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to refresh app settings.", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ReloadThrowsJson_WHEN_ReloadClicked_THEN_ShowsRefreshErrorSnackbar()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException("ReloadError"));

            var target = RenderPage();
            var reloadButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsReloadButton");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to refresh app settings.", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ReloadThrowsJsException_WHEN_ReloadClicked_THEN_ShowsRefreshErrorSnackbar()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("ReloadError"));

            var target = RenderPage();
            var reloadButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsReloadButton");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to refresh app settings.", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ReloadThrowsOperationCanceled_WHEN_ReloadClicked_THEN_DoesNotShowRefreshErrorSnackbar()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException("ReloadCanceled"));

            var target = RenderPage();
            var reloadButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsReloadButton");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => reloadButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_appSettingsService).Verify(
                service => service.RefreshSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to refresh app settings.", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_StorageSettingsChangedOnStorageTab_WHEN_SaveClicked_THEN_SavesStorageAndRefreshesTabState()
        {
            var storageRoutingService = new Mock<IStorageRoutingService>(MockBehavior.Strict);
            storageRoutingService
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(StorageRoutingSettings.Default.Clone());
            storageRoutingService
                .Setup(service => service.ResolveEffectiveStorageType(It.IsAny<string>(), It.IsAny<StorageRoutingSettings>(), It.IsAny<bool>()))
                .Returns(StorageType.LocalStorage);
            storageRoutingService
                .Setup(service => service.SaveSettingsAsync(It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StorageRoutingSettings settings, CancellationToken _) => settings.Clone());

            var webApiCapabilityService = new Mock<IWebApiCapabilityService>(MockBehavior.Strict);
            webApiCapabilityService
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            TestContext.Services.RemoveAll<IStorageRoutingService>();
            TestContext.Services.RemoveAll<IWebApiCapabilityService>();
            TestContext.Services.AddSingleton(storageRoutingService.Object);
            TestContext.Services.AddSingleton(webApiCapabilityService.Object);

            var target = RenderPage();
            await ActivateStorageTab(target);
            _storageDiagnosticsService.ClearInvocations();
            storageRoutingService.ClearInvocations();
            var masterStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            await target.InvokeAsync(() => masterStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            storageRoutingService.Verify(
                service => service.SaveSettingsAsync(
                    It.Is<StorageRoutingSettings>(settings => settings.MasterStorageType == StorageType.ClientData),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.GetEntriesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_StorageSettingsChangedAndSavedOffStorageTab_WHEN_ReturningToStorageTab_THEN_RefreshesStorageEntries()
        {
            var storageRoutingService = new Mock<IStorageRoutingService>(MockBehavior.Strict);
            storageRoutingService
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(StorageRoutingSettings.Default.Clone());
            storageRoutingService
                .Setup(service => service.ResolveEffectiveStorageType(It.IsAny<string>(), It.IsAny<StorageRoutingSettings>(), It.IsAny<bool>()))
                .Returns(StorageType.LocalStorage);
            storageRoutingService
                .Setup(service => service.SaveSettingsAsync(It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StorageRoutingSettings settings, CancellationToken _) => settings.Clone());

            var webApiCapabilityService = new Mock<IWebApiCapabilityService>(MockBehavior.Strict);
            webApiCapabilityService
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            TestContext.Services.RemoveAll<IStorageRoutingService>();
            TestContext.Services.RemoveAll<IWebApiCapabilityService>();
            TestContext.Services.AddSingleton(storageRoutingService.Object);
            TestContext.Services.AddSingleton(webApiCapabilityService.Object);

            var target = RenderPage();
            await ActivateStorageTab(target);
            var masterStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            await target.InvokeAsync(() => masterStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));

            var tabs = target.FindComponent<MudTabs>();
            await target.InvokeAsync(() => tabs.Instance.ActivePanelIndexChanged.InvokeAsync(0));

            _storageDiagnosticsService.ClearInvocations();

            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");
            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.GetEntriesAsync(It.IsAny<CancellationToken>()),
                Times.Never);

            await ActivateStorageTab(target);

            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.GetEntriesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_StorageRoutingSaveThrowsHttpRequest_WHEN_SaveClicked_THEN_ShowsGenericStorageError()
        {
            var storageRoutingService = new Mock<IStorageRoutingService>(MockBehavior.Strict);
            storageRoutingService
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(StorageRoutingSettings.Default);
            storageRoutingService
                .Setup(service => service.ResolveEffectiveStorageType(It.IsAny<string>(), It.IsAny<StorageRoutingSettings>(), It.IsAny<bool>()))
                .Returns(StorageType.LocalStorage);
            storageRoutingService
                .Setup(service => service.SaveSettingsAsync(It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("StorageError"));

            var webApiCapabilityService = new Mock<IWebApiCapabilityService>(MockBehavior.Strict);
            webApiCapabilityService
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            TestContext.Services.RemoveAll<IStorageRoutingService>();
            TestContext.Services.RemoveAll<IWebApiCapabilityService>();
            TestContext.Services.AddSingleton(storageRoutingService.Object);
            TestContext.Services.AddSingleton(webApiCapabilityService.Object);

            var target = RenderPage();
            await ActivateStorageTab(target);
            var masterStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            await target.InvokeAsync(() => masterStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to save storage settings.", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_StorageRoutingSaveThrowsJsException_WHEN_SaveClicked_THEN_ShowsGenericStorageError()
        {
            var storageRoutingService = new Mock<IStorageRoutingService>(MockBehavior.Strict);
            storageRoutingService
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(StorageRoutingSettings.Default);
            storageRoutingService
                .Setup(service => service.ResolveEffectiveStorageType(It.IsAny<string>(), It.IsAny<StorageRoutingSettings>(), It.IsAny<bool>()))
                .Returns(StorageType.LocalStorage);
            storageRoutingService
                .Setup(service => service.SaveSettingsAsync(It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("StorageError"));

            var webApiCapabilityService = new Mock<IWebApiCapabilityService>(MockBehavior.Strict);
            webApiCapabilityService
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            TestContext.Services.RemoveAll<IStorageRoutingService>();
            TestContext.Services.RemoveAll<IWebApiCapabilityService>();
            TestContext.Services.AddSingleton(storageRoutingService.Object);
            TestContext.Services.AddSingleton(webApiCapabilityService.Object);

            var target = RenderPage();
            await ActivateStorageTab(target);
            var masterStorageTypeSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            await target.InvokeAsync(() => masterStorageTypeSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            await target.InvokeAsync(() => saveButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to save storage settings.", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_InitialGroupOverridesAndEqualCounts_WHEN_GroupOverrideKeyChanges_THEN_PendingChangesDetected()
        {
            var initialSettings = StorageRoutingSettings.Default.Clone();
            initialSettings.GroupStorageTypes["themes"] = StorageType.ClientData;

            var storageRoutingService = new Mock<IStorageRoutingService>(MockBehavior.Strict);
            storageRoutingService
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(initialSettings.Clone());
            storageRoutingService
                .Setup(service => service.ResolveEffectiveStorageType(It.IsAny<string>(), It.IsAny<StorageRoutingSettings>(), It.IsAny<bool>()))
                .Returns(StorageType.LocalStorage);
            storageRoutingService
                .Setup(service => service.SaveSettingsAsync(It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StorageRoutingSettings settings, CancellationToken _) => settings.Clone());

            var webApiCapabilityService = new Mock<IWebApiCapabilityService>(MockBehavior.Strict);
            webApiCapabilityService
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            TestContext.Services.RemoveAll<IStorageRoutingService>();
            TestContext.Services.RemoveAll<IWebApiCapabilityService>();
            TestContext.Services.AddSingleton(storageRoutingService.Object);
            TestContext.Services.AddSingleton(webApiCapabilityService.Object);

            var target = RenderPage();
            await ActivateStorageTab(target);
            var themesSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageGroupStorageType-themes");
            var generalSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageGroupStorageType-general");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            await target.InvokeAsync(() => themesSelect.Instance.ValueChanged.InvokeAsync(StorageType.LocalStorage));
            await target.InvokeAsync(() => generalSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));

            saveButton.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_InitialItemOverridesAndEqualCounts_WHEN_ItemOverrideKeyChanges_THEN_PendingChangesDetected()
        {
            var initialSettings = StorageRoutingSettings.Default.Clone();
            initialSettings.ItemStorageTypes["themes.selected-theme"] = StorageType.ClientData;

            var storageRoutingService = new Mock<IStorageRoutingService>(MockBehavior.Strict);
            storageRoutingService
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(initialSettings.Clone());
            storageRoutingService
                .Setup(service => service.ResolveEffectiveStorageType(It.IsAny<string>(), It.IsAny<StorageRoutingSettings>(), It.IsAny<bool>()))
                .Returns(StorageType.LocalStorage);
            storageRoutingService
                .Setup(service => service.SaveSettingsAsync(It.IsAny<StorageRoutingSettings>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StorageRoutingSettings settings, CancellationToken _) => settings.Clone());

            var webApiCapabilityService = new Mock<IWebApiCapabilityService>(MockBehavior.Strict);
            webApiCapabilityService
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState("2.13.1", new Version(2, 13, 1), true));

            TestContext.Services.RemoveAll<IStorageRoutingService>();
            TestContext.Services.RemoveAll<IWebApiCapabilityService>();
            TestContext.Services.AddSingleton(storageRoutingService.Object);
            TestContext.Services.AddSingleton(webApiCapabilityService.Object);

            var target = RenderPage();
            await ActivateStorageTab(target);
            var overridesPanel = FindComponentByTestId<MudExpansionPanel>(target, "AppSettingsStorageGroupOverridesPanel-themes");
            await target.InvokeAsync(() => overridesPanel.Instance.ExpandAsync());
            var selectedThemeItemSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageItemStorageType-themes.selected-theme");
            var localThemesItemSelect = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageItemStorageType-themes.local-themes");
            var saveButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsSaveButton");

            await target.InvokeAsync(() => selectedThemeItemSelect.Instance.ValueChanged.InvokeAsync(StorageType.LocalStorage));
            await target.InvokeAsync(() => localThemesItemSelect.Instance.ValueChanged.InvokeAsync(StorageType.ClientData));

            saveButton.Instance.Disabled.Should().BeFalse();
        }

        private IRenderedComponent<AppSettingsPage> RenderPage(bool drawerOpen = false)
        {
            return TestContext.Render<AppSettingsPage>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", drawerOpen);
            });
        }

        private async Task ActivateStorageTab(IRenderedComponent<AppSettingsPage> target)
        {
            var tabs = target.FindComponent<MudTabs>();
            await target.InvokeAsync(() => tabs.Instance.ActivePanelIndexChanged.InvokeAsync(3));
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudSelect<StorageType>>(target, "AppSettingsStorageMasterStorageType");
            });
        }

        private async Task ActivatePwaTab(IRenderedComponent<AppSettingsPage> target)
        {
            var tabs = target.FindComponent<MudTabs>();
            await target.InvokeAsync(() => tabs.Instance.ActivePanelIndexChanged.InvokeAsync(4));
            target.WaitForAssertion(() =>
            {
                _ = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsPwaStatus");
            });
        }

        private static AppSettingsModel AppSettingsModel()
        {
            return new AppSettingsModel
            {
                UpdateChecksEnabled = true,
                NotificationsEnabled = false,
                ThemeModePreference = ThemeModePreference.System,
                DownloadFinishedNotificationsEnabled = true,
                TorrentAddedNotificationsEnabled = false,
                TorrentAddedSnackbarsEnabledWithNotifications = false,
                ThemeRepositoryIndexUrl = "https://lantean-code.github.io/qbtmud-themes/index.json"
            };
        }
    }
}
