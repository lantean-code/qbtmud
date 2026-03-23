using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.AppSettingsTabs;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Moq;
using MudBlazor;
using AppSettingsModel = Lantean.QBTMud.Models.AppSettings;

namespace Lantean.QBTMud.Test.Components.AppSettingsTabs
{
    public sealed class NotificationsAppSettingsTabTests : RazorComponentTestBase<NotificationsAppSettingsTab>
    {
        private readonly Mock<IBrowserNotificationService> _notificationServiceMock;
        private readonly Mock<ISnackbar> _snackbarMock;
        private readonly AppSettingsModel _settings;
        private int _settingsChangedCount;

        public NotificationsAppSettingsTabTests()
        {
            _notificationServiceMock = TestContext.AddSingletonMock<IBrowserNotificationService>();
            _snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Default);
            _notificationServiceMock
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);
            _notificationServiceMock
                .Setup(service => service.SubscribePermissionChangesAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _notificationServiceMock
                .Setup(service => service.UnsubscribePermissionChangesAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _settings = AppSettingsModel.Default.Clone();
        }

        [Fact]
        public void GIVEN_DefaultPermission_WHEN_Rendered_THEN_ShowsWarningPermissionIndicatorAndHelperText()
        {
            var target = RenderTarget();
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            permissionIndicator.Instance.Color.Should().Be(Color.Warning);
            notificationsSwitch.Instance.HelperText.Should().Be("Enable browser notifications and choose which events trigger alerts.");
        }

        [Fact]
        public async Task GIVEN_EnableNotifications_WHEN_PermissionGranted_THEN_UpdatesSettingsAndRaisesCallback()
        {
            var target = RenderTarget();
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            _settings.NotificationsEnabled.Should().BeTrue();
            _settingsChangedCount.Should().Be(1);
            _notificationServiceMock.Verify(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void GIVEN_InsecurePermission_WHEN_Rendered_THEN_DisablesNotificationsAndShowsAlert()
        {
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Insecure);

            var target = RenderTarget(1);
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            var insecureAlert = FindComponentByTestId<MudAlert>(target, "AppSettingsNotificationInsecureAlert");

            permissionIndicator.Instance.Color.Should().Be(Color.Warning);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Insecure");
            notificationsSwitch.Instance.Disabled.Should().BeTrue();
            GetChildContentText(insecureAlert.Instance.ChildContent).Should().Be("Browser notifications require HTTPS or localhost.");
        }

        [Fact]
        public async Task GIVEN_InsecurePermission_WHEN_EnableNotificationsInvoked_THEN_DoesNotUpdateSettings()
        {
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Insecure);

            var target = RenderTarget(1);
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            await target.InvokeAsync(() => notificationsSwitch.Instance.ValueChanged.InvokeAsync(true));

            _settings.NotificationsEnabled.Should().BeFalse();
            _settingsChangedCount.Should().Be(0);
            _notificationServiceMock.Verify(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void GIVEN_DeniedPermissionAndNotificationsEnabled_WHEN_Rendered_THEN_DisablesNotificationsAndRaisesCallback()
        {
            _settings.NotificationsEnabled = true;
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            var target = RenderTarget(1);
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            _settings.NotificationsEnabled.Should().BeFalse();
            notificationsSwitch.Instance.Value.Should().BeFalse();
            _settingsChangedCount.Should().Be(1);
        }

        [Fact]
        public void GIVEN_DefaultPermissionAndNotificationsEnabled_WHEN_Rendered_THEN_DisablesNotificationsAndRaisesCallback()
        {
            _settings.NotificationsEnabled = true;
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Default);

            var target = RenderTarget(1);
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            _settings.NotificationsEnabled.Should().BeFalse();
            notificationsSwitch.Instance.Value.Should().BeFalse();
            _settingsChangedCount.Should().Be(1);
        }

        [Fact]
        public void GIVEN_UnknownPermissionAndNotificationsEnabled_WHEN_Rendered_THEN_KeepsNotificationsEnabledWithoutRaisingCallback()
        {
            _settings.NotificationsEnabled = true;
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Unknown);

            var target = RenderTarget(1);
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            _settings.NotificationsEnabled.Should().BeTrue();
            notificationsSwitch.Instance.Value.Should().BeTrue();
            _settingsChangedCount.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_GrantedPermissionAndNotificationsEnabled_WHEN_PermissionChangesToDenied_THEN_DisablesNotificationsAndRaisesCallback()
        {
            _settings.NotificationsEnabled = true;
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            var target = RenderTarget(1);
            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            await target.InvokeAsync(() => target.Instance.OnNotificationPermissionChanged());

            _settings.NotificationsEnabled.Should().BeFalse();
            notificationsSwitch.Instance.Value.Should().BeFalse();
            _settingsChangedCount.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_DeniedPermission_WHEN_PermissionChangesToGranted_THEN_RecreatesNotificationSwitch()
        {
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            var target = RenderTarget(1);
            var initialSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            var initialSwitchInstance = initialSwitch.Instance;

            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);

            await target.InvokeAsync(() => target.Instance.OnNotificationPermissionChanged());

            var updatedSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            ReferenceEquals(initialSwitchInstance, updatedSwitch.Instance).Should().BeFalse();
            updatedSwitch.Instance.Disabled.Should().BeFalse();
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Granted");
        }

        [Fact]
        public async Task GIVEN_RequestTimesOutThenPermissionGranted_WHEN_PermissionChanges_THEN_EnablesNotificationsAndRaisesCallback()
        {
            _notificationServiceMock
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Default);
            _notificationServiceMock
                .SetupSequence(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Default)
                .ReturnsAsync(BrowserNotificationPermission.Granted);

            var target = RenderTarget(1);
            var initialSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            await target.InvokeAsync(() => initialSwitch.Instance.ValueChanged.InvokeAsync(true));
            await target.InvokeAsync(() => target.Instance.OnNotificationPermissionChanged());

            var notificationsSwitch = FindSwitch(target, "AppSettingsNotificationsEnabled");

            _settings.NotificationsEnabled.Should().BeTrue();
            notificationsSwitch.Instance.Value.Should().BeTrue();
            _settingsChangedCount.Should().Be(2);
        }

        [Fact]
        public void GIVEN_UnsupportedPermission_WHEN_Rendered_THEN_ShowsUnsupportedPermissionIndicator()
        {
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Unsupported);

            var target = RenderTarget(1);
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Default);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Unsupported");
        }

        [Fact]
        public async Task GIVEN_NotificationCorrectionCallbackThrows_WHEN_PermissionChanges_THEN_ShowsSynchronizationErrorSnackbar()
        {
            _settings.NotificationsEnabled = true;
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);

            var target = RenderTarget(
                reloadToken: 1,
                notificationsEnabledCorrected: EventCallback.Factory.Create(this, () => throw new InvalidOperationException("Failure")));

            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);
            await target.InvokeAsync(() => target.Instance.OnNotificationPermissionChanged());

            target.WaitForAssertion(() =>
            {
                _snackbarMock.Verify(
                    snackbar => snackbar.Add(
                        It.Is<string>(message => string.Equals(message, "Unable to synchronize notification settings.", StringComparison.Ordinal)),
                        Severity.Error,
                        It.IsAny<Action<SnackbarOptions>>(),
                        It.IsAny<string>()),
                    Times.Once);
            });
        }

        private IRenderedComponent<NotificationsAppSettingsTab> RenderTarget(int reloadToken = 0, EventCallback? notificationsEnabledCorrected = null)
        {
            return TestContext.Render<NotificationsAppSettingsTab>(parameters =>
            {
                parameters.Add(component => component.Settings, _settings);
                parameters.Add(component => component.ReloadToken, reloadToken);
                parameters.Add(component => component.SettingsChanged, EventCallback.Factory.Create(this, OnSettingsChanged));
                if (notificationsEnabledCorrected.HasValue)
                {
                    parameters.Add(component => component.NotificationsEnabledCorrected, notificationsEnabledCorrected.Value);
                }
            });
        }

        private void OnSettingsChanged()
        {
            _settingsChangedCount++;
        }
    }
}
