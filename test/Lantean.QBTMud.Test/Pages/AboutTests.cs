using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Pages;
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
        private readonly IRenderedComponent<About> _target;

        public AboutTests()
        {
            _apiClient = Mock.Of<IApiClient>();

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

            TestContext.Services.RemoveAll(typeof(IApiClient));
            TestContext.Services.AddSingleton(_apiClient);

            _target = RenderPage();
        }

        [Fact]
        public void GIVEN_VersionNotProvided_WHEN_Rendered_THEN_ShowsBuildInfoAndVersion()
        {
            GetChildContentText(FindComponentByTestId<MudText>(_target, "AboutVersionTitle").Instance.ChildContent)
                .Should()
                .Be("qBittorrent Version WebUI (64-bit)");

            ActivateTab("Software Used");

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
            Mock.Get(_apiClient).Invocations.Clear();

            var target = RenderPage("Version");

            GetChildContentText(FindComponentByTestId<MudText>(target, "AboutVersionTitle").Instance.ChildContent)
                .Should()
                .Be("qBittorrent Version WebUI (64-bit)");

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

        private void ActivateTab(string tabText)
        {
            var tab = _target.FindAll("div.mud-tab")
                .Single(element => element.TextContent == tabText);

            tab.Click();
        }
    }
}
