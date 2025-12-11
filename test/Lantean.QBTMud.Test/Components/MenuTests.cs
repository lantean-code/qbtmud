using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class MenuTests : RazorComponentTestBase
    {
        private readonly IRenderedComponent<Menu> _target;

        public MenuTests()
        {
            TestContext.UseApiClientMock();
            TestContext.UseSnackbarMock();
            _target = TestContext.Render<Menu>();
        }

        [Fact]
        public void GIVEN_MenuHidden_WHEN_Rendered_THEN_NoMenuShown()
        {
            _target.FindComponents<MudMenu>().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ShowMenuCalled_WHEN_Rendered_THEN_MenuVisibleWithPreferences()
        {
            var preferences = JsonSerializer.Deserialize<Preferences>("{\"rss_processing_enabled\":true}")!;

            await _target.InvokeAsync(() => _target.Instance.ShowMenu(preferences));

            _target.WaitForState(() => _target.FindComponents<MudMenu>().Count == 1);

            var menu = _target.FindComponent<MudMenu>();
            menu.Should().NotBeNull();
            menu.Instance.Icon.Should().Be(Icons.Material.Filled.MoreVert);
            menu.Instance.Disabled.Should().BeFalse();
        }
    }
}
