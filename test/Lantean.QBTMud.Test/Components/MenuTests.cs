using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Test.Infrastructure;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components
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
            var target = TestContext.Render<Menu>();

            await target.InvokeAsync(() => target.Instance.ShowMenu(preferences));

            target.WaitForState(() => target.FindComponents<MudMenu>().Count == 1);

            var menu = target.FindComponent<MudMenu>();
            menu.Should().NotBeNull();
            menu.Instance.Icon.Should().Be(Icons.Material.Filled.MoreVert);
            menu.Instance.Disabled.Should().BeFalse();
            menu.Instance.PopoverClass.Should().Be("app-menu-popover");
            menu.Instance.ListClass.Should().Be("app-menu-list");
        }
    }
}
