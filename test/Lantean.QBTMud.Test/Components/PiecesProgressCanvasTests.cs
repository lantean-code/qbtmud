using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Test.Infrastructure;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PiecesProgressCanvasTests : RazorComponentTestBase
    {
        [Fact]
        public void GIVEN_ToggleRendered_WHEN_Checked_THEN_PreventDefaultIsEnabled()
        {
            var target = TestContext.Render<PiecesProgressCanvas>(parameters =>
            {
                parameters.Add(p => p.Hash, "Hash");
                parameters.Add(p => p.Pieces, new[] { PieceState.Downloaded });
                parameters.AddCascadingValue(new MudTheme());
                parameters.AddCascadingValue("IsDarkMode", false);
                parameters.AddCascadingValue(Breakpoint.Lg);
            });

            target.Markup.Should().Contain("onkeydown:preventDefault");
        }
    }
}
