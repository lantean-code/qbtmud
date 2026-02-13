using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class LostConnectionDialogTests : RazorComponentTestBase<LostConnectionDialog>
    {
        private readonly TestNavigationManager _navigationManager;
        private readonly LostConnectionDialogTestDriver _target;

        public LostConnectionDialogTests()
        {
            _navigationManager = new TestNavigationManager();
            TestContext.Services.RemoveAll(typeof(NavigationManager));
            TestContext.Services.AddSingleton<NavigationManager>(_navigationManager);

            _target = new LostConnectionDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_DialogRendered_WHEN_Initialized_THEN_ShowsConnectionLostTexts()
        {
            var dialog = await _target.RenderDialogAsync();

            var message = FindComponentByTestId<MudText>(dialog.Component, "LostConnectionMessage");
            var subtitle = FindComponentByTestId<MudText>(dialog.Component, "LostConnectionSubtitle");
            var reconnect = FindButton(dialog.Component, "LostConnectionReconnect");

            GetChildContentText(message.Instance.ChildContent).Should().Be("qBittorrent client is not reachable");
            GetChildContentText(subtitle.Instance.ChildContent).Should().Be("Connection status: Disconnected");
            GetChildContentText(reconnect.Instance.ChildContent).Should().Be("Reconnect");
        }

        [Fact]
        public async Task GIVEN_ReconnectClicked_WHEN_Invoked_THEN_NavigatesHomeWithForceLoad()
        {
            var dialog = await _target.RenderDialogAsync();
            var reconnect = FindButton(dialog.Component, "LostConnectionReconnect");

            await dialog.Component.InvokeAsync(() => reconnect.Find("button").Click(new MouseEventArgs()));

            _navigationManager.LastNavigationUri.Should().Be("./");
            _navigationManager.LastNavigationForceLoad.Should().BeTrue();
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public string? LastNavigationUri { get; private set; }

            public bool LastNavigationForceLoad { get; private set; }

            public TestNavigationManager()
            {
                Initialize("http://localhost/qbt/", "http://localhost/qbt/");
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                LastNavigationUri = uri;
                LastNavigationForceLoad = forceLoad;
                Uri = ToAbsoluteUri(uri).ToString();
            }
        }
    }

    internal sealed class LostConnectionDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public LostConnectionDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<LostConnectionDialogRenderContext> RenderDialogAsync()
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();
            var reference = await dialogService.ShowAsync<LostConnectionDialog>(title: null, options: new DialogOptions());
            var component = provider.FindComponent<LostConnectionDialog>();

            return new LostConnectionDialogRenderContext(provider, component, reference);
        }
    }

    internal sealed class LostConnectionDialogRenderContext
    {
        public LostConnectionDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<LostConnectionDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<LostConnectionDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
