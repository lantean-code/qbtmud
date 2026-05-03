using System.Text.Json;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.AppSettingsTabs;
using Lantean.QBTMud.Core.Models;
using Microsoft.AspNetCore.Components;
using Moq;
using MudBlazor;
using AppSettingsModel = Lantean.QBTMud.Core.Models.AppSettings;

namespace Lantean.QBTMud.Presentation.Test.Components.AppSettings
{
    public sealed class UpdatesAppSettingsTabTests : RazorComponentTestBase<UpdatesAppSettingsTab>
    {
        private readonly Mock<IAppBuildInfoService> _appBuildInfoServiceMock;
        private readonly Mock<IAppUpdateService> _appUpdateServiceMock;
        private readonly AppSettingsModel _settings;
        private int _settingsChangedCount;

        public UpdatesAppSettingsTabTests()
        {
            _appBuildInfoServiceMock = TestContext.AddSingletonMock<IAppBuildInfoService>();
            _appUpdateServiceMock = TestContext.AddSingletonMock<IAppUpdateService>();
            _appBuildInfoServiceMock
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));
            _appUpdateServiceMock
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.0.0", "v1.0.0", "https://example.invalid", DateTime.UtcNow),
                    isUpdateAvailable: false,
                    canCompareVersions: true,
                    checkedAtUtc: DateTime.UtcNow));

            _settings = AppSettingsModel.Default.Clone();
        }

        [Fact]
        public void GIVEN_ReloadTokenChanges_WHEN_ComponentRerenders_THEN_ReloadsUpdateStatus()
        {
            _appUpdateServiceMock.ClearInvocations();
            _ = RenderTarget(1);

            _appUpdateServiceMock.Verify(
                service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_UpdateChecksChanged_WHEN_Disabled_THEN_UpdatesSettingsAndRaisesCallback()
        {
            var target = RenderTarget();
            var updateChecksSwitch = FindSwitch(target, "AppSettingsUpdateChecksEnabled");

            await target.InvokeAsync(() => updateChecksSwitch.Instance.ValueChanged.InvokeAsync(false));

            _settings.UpdateChecksEnabled.Should().BeFalse();
            _settingsChangedCount.Should().Be(1);
        }

        [Fact]
        public void GIVEN_UpdateStatusLoadThrowsHttpRequest_WHEN_Rendered_THEN_ShowsNotAvailableState()
        {
            _appUpdateServiceMock
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("UpdateUnavailable"));

            var target = RenderTarget(1);
            var latestRelease = FindComponentByTestId<MudText>(target, "AppSettingsLatestRelease");
            var updateStatus = FindComponentByTestId<MudText>(target, "AppSettingsUpdateStatus");

            GetChildContentText(latestRelease.Instance.ChildContent).Should().Be("Not available");
            GetChildContentText(updateStatus.Instance.ChildContent).Should().Be("Not available");
        }

        [Fact]
        public void GIVEN_UpdateStatusLoadThrowsJson_WHEN_Rendered_THEN_ShowsNotAvailableState()
        {
            _appUpdateServiceMock
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException("UpdateUnavailable"));

            var target = RenderTarget(1);
            var latestRelease = FindComponentByTestId<MudText>(target, "AppSettingsLatestRelease");
            var updateStatus = FindComponentByTestId<MudText>(target, "AppSettingsUpdateStatus");

            GetChildContentText(latestRelease.Instance.ChildContent).Should().Be("Not available");
            GetChildContentText(updateStatus.Instance.ChildContent).Should().Be("Not available");
        }

        [Fact]
        public async Task GIVEN_CheckNowThrowsHttpRequest_WHEN_Clicked_THEN_ShowsWarningSnackbar()
        {
            _appUpdateServiceMock
                .Setup(service => service.GetUpdateStatusAsync(true, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("UpdateUnavailable"));

            var target = RenderTarget();
            var checkNowButton = FindButton(target, "AppSettingsCheckNow");

            await target.InvokeAsync(() => checkNowButton.Instance.OnClick.InvokeAsync());

            _appUpdateServiceMock.Verify(
                service => service.GetUpdateStatusAsync(true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_CheckNowThrowsJson_WHEN_Clicked_THEN_ShowsWarningSnackbar()
        {
            _appUpdateServiceMock
                .Setup(service => service.GetUpdateStatusAsync(true, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException("UpdateUnavailable"));

            var target = RenderTarget();
            var checkNowButton = FindButton(target, "AppSettingsCheckNow");

            await target.InvokeAsync(() => checkNowButton.Instance.OnClick.InvokeAsync());

            _appUpdateServiceMock.Verify(
                service => service.GetUpdateStatusAsync(true, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private IRenderedComponent<UpdatesAppSettingsTab> RenderTarget(int reloadToken = 0)
        {
            return TestContext.Render<UpdatesAppSettingsTab>(parameters =>
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
