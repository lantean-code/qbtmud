using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Test.Infrastructure;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PiecesProgressNewTests : RazorComponentTestBase
    {
        [Fact]
        public void GIVEN_ToggleRendered_WHEN_Checked_THEN_PreventDefaultIsEnabled()
        {
            var target = TestContext.Render<PiecesProgressNew>(parameters =>
            {
                parameters.Add(p => p.Hash, "Hash");
                parameters.Add(p => p.Pieces, new[] { PieceState.Downloaded });
                parameters.AddCascadingValue(new MudTheme());
                parameters.AddCascadingValue("IsDarkMode", false);
            });

            target.Markup.Should().Contain("onkeydown:preventDefault");
        }
    }
}
