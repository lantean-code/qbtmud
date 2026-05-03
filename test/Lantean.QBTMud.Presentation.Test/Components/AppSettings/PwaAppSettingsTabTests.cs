using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Components.AppSettingsTabs;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.TestSupport.Infrastructure;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Presentation.Test.Components.AppSettings
{
    public sealed class PwaAppSettingsTabTests : RazorComponentTestBase<PwaAppSettingsTab>
    {
        private const string _dismissedStorageKey = "PwaInstallPrompt.Dismissed.v1";

        private readonly Mock<IPwaInstallPromptService> _pwaInstallPromptServiceMock;

        public PwaAppSettingsTabTests()
        {
            _pwaInstallPromptServiceMock = TestContext.AddSingletonMock<IPwaInstallPromptService>();
            _pwaInstallPromptServiceMock
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    IsInstalled = true,
                    CanPrompt = false,
                    IsIos = false
                });
            _pwaInstallPromptServiceMock
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("accepted");
            _pwaInstallPromptServiceMock
                .Setup(service => service.ShowInstallPromptTestAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    IsInstalled = false,
                    CanPrompt = true,
                    IsIos = false
                });
        }

        [Fact]
        public void GIVEN_ActiveComponent_WHEN_InstallStateLoaded_THEN_DisplaysInstalledStatus()
        {
            var target = RenderTarget();

            target.WaitForAssertion(() =>
            {
                var statusField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsPwaStatus");
                statusField.Instance.GetState(x => x.Value).Should().Be("Installed");
            });
        }

        [Fact]
        public async Task GIVEN_InstallPromptAvailable_WHEN_InstallNowClicked_THEN_RequestsInstallPrompt()
        {
            _pwaInstallPromptServiceMock
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    IsInstalled = false,
                    CanPrompt = true,
                    IsIos = false
                });

            var target = RenderTarget(1);

            target.WaitForAssertion(() =>
            {
                var installNowButton = FindButton(target, "AppSettingsPwaInstallNow");
                installNowButton.Instance.Disabled.Should().BeFalse();
            });

            var installNow = FindButton(target, "AppSettingsPwaInstallNow");
            await target.InvokeAsync(() => installNow.Instance.OnClick.InvokeAsync());

            _pwaInstallPromptServiceMock.Verify(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void GIVEN_InactiveComponent_WHEN_Rendered_THEN_DoesNotRequestPwaStatus()
        {
            _pwaInstallPromptServiceMock.ClearInvocations();

            _ = RenderTarget(isActive: false, reloadToken: 0);

            _pwaInstallPromptServiceMock.Verify(
                service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void GIVEN_ActiveComponent_WHEN_ReloadTokenUnchanged_THEN_DoesNotRefreshStatusAgain()
        {
            var target = RenderTarget();
            _pwaInstallPromptServiceMock.ClearInvocations();

            target.Render(parameters =>
            {
                parameters.Add(component => component.IsActive, true);
                parameters.Add(component => component.ReloadToken, 0);
            });

            _pwaInstallPromptServiceMock.Verify(
                service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void GIVEN_ActiveComponent_WHEN_ReloadTokenChanges_THEN_RefreshesStatusAgain()
        {
            var target = RenderTarget();
            _pwaInstallPromptServiceMock.ClearInvocations();

            target.Render(parameters =>
            {
                parameters.Add(component => component.IsActive, true);
                parameters.Add(component => component.ReloadToken, 1);
            });

            _pwaInstallPromptServiceMock.Verify(
                service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_InstallPromptUnavailable_WHEN_InstallNowClicked_THEN_RequestIsIgnored()
        {
            _pwaInstallPromptServiceMock
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    IsInstalled = false,
                    CanPrompt = false,
                    IsIos = false
                });

            var target = RenderTarget(1);
            target.WaitForAssertion(() =>
            {
                var installNowButton = FindButton(target, "AppSettingsPwaInstallNow");
                installNowButton.Instance.Disabled.Should().BeTrue();
            });

            var installNow = FindButton(target, "AppSettingsPwaInstallNow");
            _pwaInstallPromptServiceMock.ClearInvocations();

            await target.InvokeAsync(() => installNow.Instance.OnClick.InvokeAsync());

            _pwaInstallPromptServiceMock.Verify(
                service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_StatusRefreshInProgress_WHEN_RefreshClickedAgain_THEN_SecondRefreshIsIgnored()
        {
            var target = RenderTarget(1);
            var refreshButton = FindButton(target, "AppSettingsPwaRefreshStatus");
            var refreshTaskSource = new TaskCompletionSource<PwaInstallPromptState>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pwaInstallPromptServiceMock
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .Returns(refreshTaskSource.Task);
            _pwaInstallPromptServiceMock.ClearInvocations();

            var firstRefreshTask = target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                _pwaInstallPromptServiceMock.Verify(
                    service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            _pwaInstallPromptServiceMock.Verify(
                service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            refreshTaskSource.SetResult(new PwaInstallPromptState());
            await firstRefreshTask;
        }

        [Fact]
        public async Task GIVEN_InstallRequestInProgress_WHEN_InstallClickedAgain_THEN_SecondRequestIsIgnored()
        {
            _pwaInstallPromptServiceMock
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PwaInstallPromptState
                {
                    IsInstalled = false,
                    CanPrompt = true,
                    IsIos = false
                });

            var installTaskSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pwaInstallPromptServiceMock
                .Setup(service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()))
                .Returns(installTaskSource.Task);

            var target = RenderTarget(1);
            var installButton = FindButton(target, "AppSettingsPwaInstallNow");
            _pwaInstallPromptServiceMock.ClearInvocations();

            var firstInstallTask = target.InvokeAsync(() => installButton.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                _pwaInstallPromptServiceMock.Verify(
                    service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            await target.InvokeAsync(() => installButton.Instance.OnClick.InvokeAsync());

            _pwaInstallPromptServiceMock.Verify(
                service => service.RequestInstallPromptAsync(It.IsAny<CancellationToken>()),
                Times.Once);

            installTaskSource.SetResult("accepted");
            await firstInstallTask;
        }

        [Fact]
        public void GIVEN_StatusReadThrowsJsException_WHEN_Rendered_THEN_StatusRemainsUnknown()
        {
            _pwaInstallPromptServiceMock
                .Setup(service => service.GetInstallPromptStateAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JSException("StatusError"));

            var target = RenderTarget(1);
            var statusField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsPwaStatus");

            statusField.Instance.GetState(x => x.Value).Should().Be("Unknown");
        }

#if DEBUG

        [Fact]
        public async Task GIVEN_TestPromptButtonClicked_WHEN_ShowInstallPromptTestInvoked_THEN_UsesSharedPromptFlowAndUpdatesStatus()
        {
            await TestContext.LocalStorage.SetItemAsync(_dismissedStorageKey, true, Xunit.TestContext.Current.CancellationToken);
            var target = RenderTarget(1);
            var testPromptButton = FindButton(target, "AppSettingsPwaShowSnackbarTest");

            await target.InvokeAsync(() => testPromptButton.Instance.OnClick.InvokeAsync());

            _pwaInstallPromptServiceMock.Verify(service => service.ShowInstallPromptTestAsync(It.IsAny<CancellationToken>()), Times.Once);
            TestContext.LocalStorage.Snapshot().Should().NotContainKey(_dismissedStorageKey);
            target.WaitForAssertion(() =>
            {
                var promptStatusField = FindComponentByTestId<MudTextField<string>>(target, "AppSettingsPwaInstallPromptStatus");
                promptStatusField.Instance.GetState(x => x.Value).Should().Be("Install prompt available");
            });
        }

#endif

        private IRenderedComponent<PwaAppSettingsTab> RenderTarget(int reloadToken = 0, bool isActive = true)
        {
            return TestContext.Render<PwaAppSettingsTab>(parameters =>
            {
                parameters.Add(component => component.IsActive, isActive);
                parameters.Add(component => component.ReloadToken, reloadToken);
            });
        }
    }
}
