using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Test.Infrastructure;
using MudBlazor;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class MenuTests : RazorComponentTestBase
    {
        [Fact]
        public void GIVEN_MenuHidden_WHEN_Rendered_THEN_NoMenuShown()
        {
            var target = TestContext.Render<Menu>();

            target.FindComponents<MudMenu>().Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ShowMenuCalled_WHEN_Rendered_THEN_MenuVisibleWithPreferences()
        {
            TestContext.UseApiClientMock();
            TestContext.UseSnackbarMock();
            var preferences = JsonSerializer.Deserialize<Preferences>("{\"rss_processing_enabled\":true}")!;

            var target = TestContext.Render<Menu>();

            await target.InvokeAsync(() => target.Instance.ShowMenu(preferences));

            target.WaitForAssertion(() =>
            {
                target.FindComponents<MudMenu>().Should().HaveCount(1);
            });
        }
    }
}
