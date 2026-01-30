using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PiecesProgressCanvasTests : RazorComponentTestBase
    {
        [Fact]
        public void GIVEN_ToggleRendered_WHEN_EnterPressed_THEN_Expands()
        {
            var target = TestContext.Render<PiecesProgressCanvas>(parameters =>
            {
                parameters.Add(p => p.Hash, "Hash");
                parameters.Add(p => p.Pieces, new[] { PieceState.Downloaded });
                parameters.AddCascadingValue(new MudTheme());
                parameters.AddCascadingValue("IsDarkMode", false);
                parameters.AddCascadingValue(Breakpoint.Lg);
            });

            var collapse = target.FindComponent<MudCollapse>();
            collapse.Instance.Expanded.Should().BeTrue();

            var toggle = target.Find("div.pieces-progress-canvas__linear");
            toggle.KeyDown(new KeyboardEventArgs { Key = "Enter" });

            collapse.Instance.Expanded.Should().BeFalse();
        }
    }
}
