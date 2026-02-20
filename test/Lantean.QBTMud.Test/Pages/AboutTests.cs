using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class AboutTests : RazorComponentTestBase<About>
    {
        private readonly IApiClient _apiClient;
        private readonly IAppBuildInfoService _appBuildInfoService;
        private readonly IAppUpdateService _appUpdateService;
        private readonly IRenderedComponent<About> _target;

        public AboutTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _appBuildInfoService = Mock.Of<IAppBuildInfoService>();
            _appUpdateService = Mock.Of<IAppUpdateService>();

            Mock.Get(_apiClient)
                .Setup(client => client.GetBuildInfo())
                .ReturnsAsync(new BuildInfo(
                    "QTVersion",
                    "LibTorrentVersion",
                    "BoostVersion",
                    "OpenSSLVersion",
                    "ZLibVersion",
                    64));

            Mock.Get(_apiClient)
                .Setup(client => client.GetApplicationVersion())
                .ReturnsAsync("Version");

            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));

            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.1.0", "v1.1.0", "https://example.invalid", DateTime.UtcNow),
                    true,
                    true,
                    DateTime.UtcNow));

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.RemoveAll<IAppBuildInfoService>();
            TestContext.Services.AddSingleton(_appBuildInfoService);
            TestContext.Services.RemoveAll<IAppUpdateService>();
            TestContext.Services.AddSingleton(_appUpdateService);

            _target = RenderPage();
        }

        [Fact]
        public void GIVEN_VersionNotProvided_WHEN_Rendered_THEN_ShowsBuildInfoAndVersion()
        {
            GetChildContentText(FindComponentByTestId<MudText>(_target, "AboutVersionTitle").Instance.ChildContent)
                .Should()
                .Be("qBittorrent Version WebUI (64-bit)");
            GetChildContentText(FindComponentByTestId<MudText>(_target, "QbtMudCurrentBuild").Instance.ChildContent)
                .Should()
                .Be("1.0.0");
            GetChildContentText(FindComponentByTestId<MudText>(_target, "QbtMudLatestRelease").Instance.ChildContent)
                .Should()
                .Be("v1.1.0");
            GetChildContentText(FindComponentByTestId<MudText>(_target, "QbtMudUpdateState").Instance.ChildContent)
                .Should()
                .Be("Update available");

            ActivateTab(5);

            _target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(_target, "QtVersion").Instance.ChildContent).Should().Be("QTVersion");
                GetChildContentText(FindComponentByTestId<MudText>(_target, "LibtorrentVersion").Instance.ChildContent).Should().Be("LibTorrentVersion");
                GetChildContentText(FindComponentByTestId<MudText>(_target, "BoostVersion").Instance.ChildContent).Should().Be("BoostVersion");
                GetChildContentText(FindComponentByTestId<MudText>(_target, "OpenSslVersion").Instance.ChildContent).Should().Be("OpenSSLVersion");
                GetChildContentText(FindComponentByTestId<MudText>(_target, "ZLibVersion").Instance.ChildContent).Should().Be("ZLibVersion");
            });

            Mock.Get(_apiClient).Verify(client => client.GetBuildInfo(), Times.Once);
            Mock.Get(_apiClient).Verify(client => client.GetApplicationVersion(), Times.Once);
        }

        [Fact]
        public void GIVEN_VersionProvided_WHEN_Rendered_THEN_SkipsApplicationVersionRequest()
        {
            _apiClient.ClearInvocations();

            var target = RenderPage("Version");

            GetChildContentText(FindComponentByTestId<MudText>(target, "AboutVersionTitle").Instance.ChildContent)
                .Should()
                .Be("qBittorrent Version WebUI (64-bit)");
            GetChildContentText(FindComponentByTestId<MudText>(target, "QbtMudCurrentBuild").Instance.ChildContent)
                .Should()
                .Be("1.0.0");

            Mock.Get(_apiClient).Verify(client => client.GetBuildInfo(), Times.Once);
            Mock.Get(_apiClient).Verify(client => client.GetApplicationVersion(), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NavigateBack_WHEN_Clicked_THEN_NavigatesHome()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/other");

            var backButton = _target.FindComponents<MudIconButton>()
                .Single(button => button.Instance.Icon == Icons.Material.Outlined.NavigateBefore);

            await _target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public void GIVEN_EmptyVersionProvided_WHEN_Rendered_THEN_ShowsWebUiWithoutVersionText()
        {
            var target = RenderPage(string.Empty);

            GetChildContentText(FindComponentByTestId<MudText>(target, "AboutVersionTitle").Instance.ChildContent)
                .Should()
                .Be("qBittorrent WebUI (64-bit)");
        }

        [Fact]
        public void GIVEN_UpdateNotAvailable_WHEN_Rendered_THEN_ShowsUpToDateState()
        {
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.1.0", "v1.1.0", "https://example.invalid", DateTime.UtcNow),
                    false,
                    true,
                    DateTime.UtcNow));

            var target = RenderPage();

            GetChildContentText(FindComponentByTestId<MudText>(target, "QbtMudUpdateState").Instance.ChildContent)
                .Should()
                .Be("Up to date");
        }

        [Fact]
        public void GIVEN_UpdateServiceThrows_WHEN_Rendered_THEN_ShowsNotAvailableAndNoReleaseLink()
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns((AppBuildInfo)null!);
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Message"));

            var target = RenderPage();

            GetChildContentText(FindComponentByTestId<MudText>(target, "QbtMudCurrentBuild").Instance.ChildContent)
                .Should()
                .Be(string.Empty);
            GetChildContentText(FindComponentByTestId<MudText>(target, "QbtMudLatestRelease").Instance.ChildContent)
                .Should()
                .Be("Not available");
            GetChildContentText(FindComponentByTestId<MudText>(target, "QbtMudUpdateState").Instance.ChildContent)
                .Should()
                .Be("Not available");
            target.FindComponents<MudLink>()
                .Any(component => HasTestId(component, "QbtMudReleaseLink"))
                .Should()
                .BeFalse();
        }

        [Fact]
        public void GIVEN_LatestReleaseMissing_WHEN_Rendered_THEN_ShowsNotAvailableWithoutReleaseLink()
        {
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    null,
                    false,
                    true,
                    DateTime.UtcNow));

            var target = RenderPage();

            GetChildContentText(FindComponentByTestId<MudText>(target, "QbtMudLatestRelease").Instance.ChildContent)
                .Should()
                .Be("Not available");
            target.FindComponents<MudLink>()
                .Any(component => HasTestId(component, "QbtMudReleaseLink"))
                .Should()
                .BeFalse();
        }

        [Fact]
        public void GIVEN_LatestReleaseHtmlUrlWhitespace_WHEN_Rendered_THEN_HidesReleaseLink()
        {
            Mock.Get(_appUpdateService)
                .Setup(service => service.GetUpdateStatusAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppUpdateStatus(
                    new AppBuildInfo("1.0.0", "AssemblyMetadata"),
                    new AppReleaseInfo("v1.1.0", "v1.1.0", " ", DateTime.UtcNow),
                    false,
                    true,
                    DateTime.UtcNow));

            var target = RenderPage();

            GetChildContentText(FindComponentByTestId<MudText>(target, "QbtMudLatestRelease").Instance.ChildContent)
                .Should()
                .Be("v1.1.0");
            target.FindComponents<MudLink>()
                .Any(component => HasTestId(component, "QbtMudReleaseLink"))
                .Should()
                .BeFalse();
        }

        [Fact]
        public void GIVEN_AuthorsTab_WHEN_Activated_THEN_ShowsMaintainerAndOriginalAuthorLinks()
        {
            ActivateTab(1);

            _target.WaitForAssertion(() =>
            {
                _target.FindComponents<MudLink>()
                    .Any(component => string.Equals(component.Instance.Href, "mailto:sledgehammer999@qbittorrent.org", StringComparison.Ordinal))
                    .Should()
                    .BeTrue();
                _target.FindComponents<MudLink>()
                    .Any(component => string.Equals(component.Instance.Href, "mailto:chris@qbittorrent.org", StringComparison.Ordinal))
                    .Should()
                    .BeTrue();
            });
        }

        private IRenderedComponent<About> RenderPage(string? version = null)
        {
            return TestContext.Render<About>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", false);
                if (version is not null)
                {
                    parameters.AddCascadingValue("Version", version);
                }
            });
        }

        private void ActivateTab(int tabIndex)
        {
            var tab = _target.FindAll("div.mud-tab")[tabIndex];

            tab.Click();
        }
    }
}
