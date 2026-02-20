using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using AppSettingsModel = Lantean.QBTMud.Models.AppSettings;
using AppSettingsPage = Lantean.QBTMud.Pages.AppSettings;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class AppSettingsTests : RazorComponentTestBase<AppSettingsPage>
    {
        private readonly IAppBuildInfoService _appBuildInfoService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly IAppUpdateService _appUpdateService;
        private readonly ITorrentCompletionNotificationService _torrentCompletionNotificationService;
        private readonly IStorageDiagnosticsService _storageDiagnosticsService;
        private readonly ISnackbar _snackbar;

        public AppSettingsTests()
        {
            _appBuildInfoService = Mock.Of<IAppBuildInfoService>();
            _appSettingsService = Mock.Of<IAppSettingsService>();
            _appUpdateService = Mock.Of<IAppUpdateService>();
            _torrentCompletionNotificationService = Mock.Of<ITorrentCompletionNotificationService>();
            _storageDiagnosticsService = Mock.Of<IStorageDiagnosticsService>();
            _snackbar = Mock.Of<ISnackbar>();

            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
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
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Default);
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new AppStorageEntry("QbtMud.AppSettings.State.v1", "AppSettings.State.v1", "{\"value\":true}", "{\"value\":true}", 14)
                ]);
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.RemoveEntryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.ClearEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            TestContext.Services.RemoveAll<IAppBuildInfoService>();
            TestContext.Services.RemoveAll<IAppSettingsService>();
            TestContext.Services.RemoveAll<IAppUpdateService>();
            TestContext.Services.RemoveAll<ITorrentCompletionNotificationService>();
            TestContext.Services.RemoveAll<IStorageDiagnosticsService>();
            TestContext.Services.RemoveAll<ISnackbar>();
            TestContext.Services.AddSingleton(_appBuildInfoService);
            TestContext.Services.AddSingleton(_appSettingsService);
            TestContext.Services.AddSingleton(_appUpdateService);
            TestContext.Services.AddSingleton(_torrentCompletionNotificationService);
            TestContext.Services.AddSingleton(_storageDiagnosticsService);
            TestContext.Services.AddSingleton(_snackbar);
        }

        [Fact]
        public async Task GIVEN_UpdateChecksToggle_WHEN_Disabled_THEN_PersistsSetting()
        {
            var target = RenderPage();
            var updateChecksSwitch = FindSwitch(target, "AppSettingsUpdateChecksEnabled");

            await target.InvokeAsync(() => updateChecksSwitch.Instance.ValueChanged.InvokeAsync(false));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettingsModel>(settings => !settings.UpdateChecksEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NotificationsToggle_WHEN_Enabled_THEN_RequestsPermissionAndPersistsSetting()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_torrentCompletionNotificationService).Verify(
                service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettingsModel>(settings => settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_BrowserNotificationsEnabled_WHEN_DownloadCompletedUnchecked_THEN_PersistsSetting()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            var downloadCompletedCheckbox = FindComponentByTestId<MudCheckBox<bool>>(target, "AppSettingsDownloadFinishedNotificationsEnabled");
            await target.InvokeAsync(() => downloadCompletedCheckbox.Instance.ValueChanged.InvokeAsync(false));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettingsModel>(settings => !settings.DownloadFinishedNotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
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
        public async Task GIVEN_AddedTorrentNotificationsToggle_WHEN_Enabled_THEN_PersistsSetting()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            var addedNotificationsCheckbox = FindComponentByTestId<MudCheckBox<bool>>(target, "AppSettingsTorrentAddedNotificationsEnabled");

            await target.InvokeAsync(() => addedNotificationsCheckbox.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettingsModel>(settings => settings.TorrentAddedNotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
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
        public async Task GIVEN_NotificationsEnabledAndTorrentAddedEnabled_WHEN_SnackbarToggleEnabled_THEN_PersistsSetting()
        {
            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));
            var addedNotificationsCheckbox = FindComponentByTestId<MudCheckBox<bool>>(target, "AppSettingsTorrentAddedNotificationsEnabled");
            await target.InvokeAsync(() => addedNotificationsCheckbox.Instance.ValueChanged.InvokeAsync(true));

            var snackbarToggle = FindSwitch(target, "AppSettingsTorrentAddedSnackbarsEnabledWithNotifications");
            await target.InvokeAsync(() => snackbarToggle.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettingsModel>(settings => settings.TorrentAddedSnackbarsEnabledWithNotifications),
                    It.IsAny<CancellationToken>()),
                Times.Once);
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
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsStorageDelete-AppSettings.State.v1");
            var clearButton = FindButton(target, "AppSettingsStorageClearAll");

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());
            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.RemoveEntryAsync("QbtMud.AppSettings.State.v1", It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.ClearEntriesAsync(It.IsAny<CancellationToken>()),
                Times.Once);
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
        public void GIVEN_DrawerOpen_WHEN_PageRendered_THEN_BackButtonHidden()
        {
            var target = RenderPage(drawerOpen: true);

            var hasBackButton = target
                .FindComponents<MudIconButton>()
                .Any(component => HasTestId(component, "AppSettingsBackButton"));

            hasBackButton.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NoStorageEntries_WHEN_PageRendered_THEN_ShowsEmptyStorageMessage()
        {
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.GetEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<AppStorageEntry>());

            var target = RenderPage();
            var emptyMessage = FindComponentByTestId<MudText>(target, "AppSettingsStorageEmpty");

            GetChildContentText(emptyMessage.Instance.ChildContent).Should().Be("No qbtmud local storage entries found.");
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
        public void GIVEN_DeniedPermission_WHEN_PageRendered_THEN_PermissionIndicatorUsesErrorColorAndDeniedText()
        {
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            var target = RenderPage();
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Error);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Denied");
        }

        [Fact]
        public void GIVEN_UnsupportedPermission_WHEN_PageRendered_THEN_PermissionIndicatorUsesDefaultColorAndUnsupportedText()
        {
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Unsupported);

            var target = RenderPage();
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Default);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Unsupported");
        }

        [Fact]
        public void GIVEN_UnexpectedPermissionValue_WHEN_PageRendered_THEN_PermissionIndicatorFallsBackToUnsupported()
        {
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((BrowserNotificationPermission)999);

            var target = RenderPage();
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Default);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Unsupported");
        }

        [Fact]
        public void GIVEN_GetPermissionThrowsJsException_WHEN_PageRendered_THEN_PermissionIndicatorFallsBackToUnsupported()
        {
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("Denied"));

            var target = RenderPage();
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Default);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Unsupported");
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
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            _appSettingsService.ClearInvocations();
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettingsModel>(settings => !settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Browser notification permission was not granted.", StringComparison.Ordinal)),
                    Severity.Warning,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RequestPermissionThrowsJsException_WHEN_EnablingNotifications_THEN_ShowsErrorAndPersistsDisabled()
        {
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("PermissionError"));

            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            _appSettingsService.ClearInvocations();
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettingsModel>(settings => !settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to update notification permission: PermissionError", StringComparison.Ordinal)),
                    Severity.Error,
                    It.IsAny<Action<SnackbarOptions>>(),
                    It.IsAny<string?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NotificationsInitiallyEnabled_WHEN_DisablingNotifications_THEN_RefreshesPermissionAndPersists()
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
            _torrentCompletionNotificationService.ClearInvocations();
            _appSettingsService.ClearInvocations();

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(false));

            Mock.Get(_torrentCompletionNotificationService).Verify(
                service => service.GetPermissionAsync(It.IsAny<CancellationToken>()),
                Times.Once);
            Mock.Get(_appSettingsService).Verify(
                service => service.SaveSettingsAsync(
                    It.Is<AppSettingsModel>(settings => !settings.NotificationsEnabled),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NotificationToggleInProgress_WHEN_EnabledAgain_THEN_SecondRequestIsIgnored()
        {
            var permissionTaskSource = new TaskCompletionSource<BrowserNotificationPermission>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_torrentCompletionNotificationService)
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .Returns(permissionTaskSource.Task);

            var target = RenderPage();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            _torrentCompletionNotificationService.ClearInvocations();

            var firstToggleTask = target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            target.WaitForAssertion(() =>
            {
                Mock.Get(_torrentCompletionNotificationService).Verify(
                    service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            Mock.Get(_torrentCompletionNotificationService).Verify(
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
        public async Task GIVEN_RefreshStorageFails_WHEN_RefreshClicked_THEN_ShowsErrorSnackbar()
        {
            var initialEntries = new[]
            {
                new AppStorageEntry("QbtMud.AppSettings.State.v1", "AppSettings.State.v1", "{\"value\":true}", "{\"value\":true}", 14)
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
            var refreshButton = FindButton(target, "AppSettingsStorageRefresh");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to load local storage entries.", StringComparison.Ordinal)),
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
                new AppStorageEntry("QbtMud.AppSettings.State.v1", "AppSettings.State.v1", "{\"value\":true}", "{\"value\":true}", 14)
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
                .Setup(service => service.RemoveEntryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("DeleteError"));

            var target = RenderPage();
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsStorageDelete-AppSettings.State.v1");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to remove local storage entry.", StringComparison.Ordinal)),
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
                .Setup(service => service.RemoveEntryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(removeTaskSource.Task);

            var target = RenderPage();
            var deleteButton = FindComponentByTestId<MudIconButton>(target, "AppSettingsStorageDelete-AppSettings.State.v1");
            _storageDiagnosticsService.ClearInvocations();

            var firstDeleteTask = target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_storageDiagnosticsService).Verify(
                    service => service.RemoveEntryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.RemoveEntryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Once);

            removeTaskSource.SetResult();
            await firstDeleteTask;
        }

        [Fact]
        public async Task GIVEN_ClearStorageFails_WHEN_ClearClicked_THEN_ShowsErrorSnackbar()
        {
            Mock.Get(_storageDiagnosticsService)
                .Setup(service => service.ClearEntriesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("ClearError"));

            var target = RenderPage();
            var clearButton = FindButton(target, "AppSettingsStorageClearAll");
            _snackbar.ClearInvocations();

            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add(
                    It.Is<string>(message => string.Equals(message, "Unable to clear local storage entries.", StringComparison.Ordinal)),
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
                .Setup(service => service.ClearEntriesAsync(It.IsAny<CancellationToken>()))
                .Returns(clearTaskSource.Task);

            var target = RenderPage();
            var clearButton = FindButton(target, "AppSettingsStorageClearAll");
            _storageDiagnosticsService.ClearInvocations();

            var firstClearTask = target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                Mock.Get(_storageDiagnosticsService).Verify(
                    service => service.ClearEntriesAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            await target.InvokeAsync(() => clearButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_storageDiagnosticsService).Verify(
                service => service.ClearEntriesAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            clearTaskSource.SetResult(1);
            await firstClearTask;
        }

        private IRenderedComponent<AppSettingsPage> RenderPage(bool drawerOpen = false)
        {
            return TestContext.Render<AppSettingsPage>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", drawerOpen);
            });
        }

        private static AppSettingsModel AppSettingsModel()
        {
            return new AppSettingsModel
            {
                UpdateChecksEnabled = true,
                NotificationsEnabled = false,
                DownloadFinishedNotificationsEnabled = true,
                TorrentAddedNotificationsEnabled = false,
                TorrentAddedSnackbarsEnabledWithNotifications = false
            };
        }
    }
}
