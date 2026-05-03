using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Presentation.Test.Components
{
    public sealed class MenuTests : RazorComponentTestBase
    {
        public MenuTests()
        {
            TestContext.UseApiClientMock();
            TestContext.UseSnackbarMock();
        }

        [Fact]
        public void GIVEN_MenuHidden_WHEN_Rendered_THEN_NoMenuShown()
        {
            var target = TestContext.Render<Menu>();

            target.FindComponents<MudMenu>().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ShowMenuCalled_WHEN_Rendered_THEN_MenuVisibleWithPreferences()
        {
            var preferences = PreferencesFactory.CreateQBittorrentPreferences(spec =>
            {
                spec.RssProcessingEnabled = true;
            });
            var popoverProvider = TestContext.Render<MudPopoverProvider>();
            var target = TestContext.Render<Menu>();

            await target.InvokeAsync(() => target.Instance.ShowMenu(preferences));

            target.WaitForState(() => target.FindComponents<MudMenu>().Count == 1);

            var menu = target.FindComponent<MudMenu>();
            menu.Should().NotBeNull();
            menu.Instance.Icon.Should().Be(Icons.Material.Filled.MoreVert);
            menu.Instance.Disabled.Should().BeFalse();
            menu.Instance.PopoverClass.Should().Be("app-menu-popover");
            menu.Instance.ListClass.Should().Be("app-menu-list");

            await target.InvokeAsync(() => menu.Instance.OpenMenuAsync(new MouseEventArgs(), false));

            popoverProvider.WaitForAssertion(() =>
            {
                popoverProvider.FindComponents<MudMenuItem>()
                    .Any(item => HasTestId(item, "Action-rss"))
                    .Should().BeTrue();
            });
        }

        [Fact]
        public async Task GIVEN_ShowMenuCalledWithoutPreferences_WHEN_Rendered_THEN_ApplicationActionsReceivesNullPreferences()
        {
            var popoverProvider = TestContext.Render<MudPopoverProvider>();
            var target = TestContext.Render<Menu>();

            await target.InvokeAsync(() => target.Instance.ShowMenu());

            target.WaitForState(() => target.FindComponents<MudMenu>().Count == 1);

            var menu = target.FindComponent<MudMenu>();

            await target.InvokeAsync(() => menu.Instance.OpenMenuAsync(new MouseEventArgs(), false));

            popoverProvider.WaitForAssertion(() =>
            {
                popoverProvider.FindComponents<MudMenuItem>()
                    .Any(item => HasTestId(item, "Action-rss"))
                    .Should().BeFalse();
            });
        }
    }
}
