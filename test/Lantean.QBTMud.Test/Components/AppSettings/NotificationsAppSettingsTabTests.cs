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
        private readonly Mock<ITorrentCompletionNotificationService> _notificationServiceMock;
        private readonly AppSettingsModel _settings;
        private int _settingsChangedCount;

        public NotificationsAppSettingsTabTests()
        {
            _notificationServiceMock = TestContext.AddSingletonMock<ITorrentCompletionNotificationService>();
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Default);
            _notificationServiceMock
                .Setup(service => service.RequestPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);

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
        public void GIVEN_GetPermissionThrowsInvalidOperation_WHEN_Rendered_THEN_FallsBackToUnsupportedPermission()
        {
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("PermissionUnavailable"));

            var target = RenderTarget(1);
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Default);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Unsupported");
        }

        [Fact]
        public void GIVEN_GetPermissionThrowsHttpRequest_WHEN_Rendered_THEN_FallsBackToUnsupportedPermission()
        {
            _notificationServiceMock
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("PermissionUnavailable"));

            var target = RenderTarget(1);
            var permissionIndicator = FindComponentByTestId<MudChip<string>>(target, "AppSettingsNotificationPermission");

            permissionIndicator.Instance.Color.Should().Be(Color.Default);
            GetChildContentText(permissionIndicator.Instance.ChildContent).Should().Be("Permission: Unsupported");
        }

        private IRenderedComponent<NotificationsAppSettingsTab> RenderTarget(int reloadToken = 0)
        {
            return TestContext.Render<NotificationsAppSettingsTab>(parameters =>
            {
                parameters.Add(component => component.Settings, _settings);
                parameters.Add(component => component.ReloadToken, reloadToken);
                parameters.Add(component => component.SettingsChanged, EventCallback.Factory.Create(this, OnSettingsChanged));
            });
        }

        private void OnSettingsChanged()
        {
            _settingsChangedCount++;
        }
    }
}
