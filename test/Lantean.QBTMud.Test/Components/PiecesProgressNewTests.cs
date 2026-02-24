using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PiecesProgressNewTests : RazorComponentTestBase
    {
        [Fact]
        public void GIVEN_ToggleRendered_WHEN_EnterPressed_THEN_Expands()
        {
            var target = RenderTarget(new[] { PieceState.Downloaded });

            var collapse = target.FindComponent<MudCollapse>();
            collapse.Instance.Expanded.Should().BeFalse();

            var toggle = target.Find("div.pieces-progress-new__linear");
            toggle.KeyDown(new KeyboardEventArgs { Key = "Enter" });

            collapse.Instance.Expanded.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_ToggleRendered_WHEN_ClickedTwice_THEN_ExpandsThenCollapses()
        {
            var target = RenderTarget(new[] { PieceState.Downloaded });

            var collapse = target.FindComponent<MudCollapse>();
            collapse.Instance.Expanded.Should().BeFalse();

            var toggle = target.Find("div.pieces-progress-new__linear");
            toggle.Click();
            collapse.Instance.Expanded.Should().BeTrue();

            toggle.Click();
            collapse.Instance.Expanded.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ToggleRendered_WHEN_NonToggleKeyPressed_THEN_DoesNotExpand()
        {
            var target = RenderTarget(new[] { PieceState.Downloaded });

            var collapse = target.FindComponent<MudCollapse>();
            collapse.Instance.Expanded.Should().BeFalse();

            var toggle = target.Find("div.pieces-progress-new__linear");
            toggle.KeyDown(new KeyboardEventArgs { Key = "Escape" });

            collapse.Instance.Expanded.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NoPieces_WHEN_Expanded_THEN_ShowsHeatmapUnavailableText()
        {
            var target = RenderTarget(Array.Empty<PieceState>());

            var toggle = target.Find("div.pieces-progress-new__linear");
            toggle.Click();

            var textValues = target.FindComponents<MudText>()
                .Select(component => GetChildContentText(component.Instance.ChildContent))
                .OfType<string>()
                .ToList();

            textValues.Should().Contain("Heatmap unavailable without piece data.");
        }

        [Fact]
        public void GIVEN_ExpandedHeatmap_WHEN_SpacePressed_THEN_Collapses()
        {
            var target = RenderTarget(new[] { PieceState.Downloaded, PieceState.Downloading, PieceState.NotDownloaded });

            var toggle = target.Find("div.pieces-progress-new__linear");
            toggle.Click();

            var collapse = target.FindComponent<MudCollapse>();
            collapse.Instance.Expanded.Should().BeTrue();

            toggle.KeyDown(new KeyboardEventArgs { Key = "Space" });

            collapse.Instance.Expanded.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_UnchangedParameters_WHEN_SetAgain_THEN_RemainsCollapsed()
        {
            var pieces = new[] { PieceState.Downloaded, PieceState.NotDownloaded };
            var target = RenderTarget(pieces);

            await target.InvokeAsync(() => target.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(PiecesProgressNew.Hash), "Hash" },
                { nameof(PiecesProgressNew.Pieces), pieces },
            })));

            var collapse = target.FindComponent<MudCollapse>();
            collapse.Instance.Expanded.Should().BeFalse();
            GetHeatmapTooltips(target).Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ExpandedHeatmap_WHEN_ParametersChanged_THEN_RerendersHeatmapAndStaysExpanded()
        {
            var target = RenderTarget(new[] { PieceState.Downloaded, PieceState.Downloaded });

            var toggle = target.Find("div.pieces-progress-new__linear");
            toggle.Click();

            await target.InvokeAsync(() => target.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(PiecesProgressNew.Hash), "UpdatedHash" },
                { nameof(PiecesProgressNew.Pieces), new[] { PieceState.Downloaded, PieceState.Downloading, PieceState.NotDownloaded, PieceState.Downloaded } },
            })));

            var collapse = target.FindComponent<MudCollapse>();
            collapse.Instance.Expanded.Should().BeTrue();
            GetHeatmapTooltips(target).Should().Contain(tooltip => tooltip.Contains("Piece #4:", StringComparison.Ordinal));
        }

        [Fact]
        public void GIVEN_NullHashAndMixedPiecesInDarkMode_WHEN_Rendered_THEN_ShowsExpectedSummaryAndTooltip()
        {
            var target = RenderTarget(
                new[]
                {
                    PieceState.Downloaded,
                    PieceState.Downloaded,
                    PieceState.Downloading,
                    PieceState.NotDownloaded,
                    PieceState.NotDownloaded,
                },
                isDarkMode: true,
                hash: (string)null!,
                theme: CreateTheme());

            var textValues = GetMudTextValues(target);
            textValues.Should().Contain("50% complete — 2 downloaded, 1 in progress");

            var linearTooltip = target.FindComponents<Tooltip>()
                .Select(tooltip => tooltip.Instance.Text)
                .First(text => text is not null && text.StartsWith("Downloaded:", StringComparison.Ordinal));

            linearTooltip.Should().Be("Downloaded: 2\nDownloading: 1\nPending: 2");
        }

        [Fact]
        public void GIVEN_TwoThousandFiftyPieces_WHEN_Expanded_THEN_GeneratesDualPieceHeatmapTooltips()
        {
            var target = RenderTarget(CreatePatternedPieces(2050));

            var toggle = target.Find("div.pieces-progress-new__linear");
            toggle.Click();

            var collapse = target.FindComponent<MudCollapse>();
            collapse.Instance.Expanded.Should().BeTrue();

            var heatmapTooltips = GetHeatmapTooltips(target);
            heatmapTooltips.Should().Contain(tooltip => tooltip.Contains('\n'));
            heatmapTooltips.Should().Contain(tooltip => tooltip.Contains("Piece #2049:", StringComparison.Ordinal) && tooltip.Contains("Piece #2050:", StringComparison.Ordinal));
        }

        [Fact]
        public void GIVEN_FourThousandNinetyNinePieces_WHEN_Expanded_THEN_GeneratesTrailingThreePieceTooltip()
        {
            var target = RenderTarget(CreatePatternedPieces(4099));

            var toggle = target.Find("div.pieces-progress-new__linear");
            toggle.Click();

            var collapse = target.FindComponent<MudCollapse>();
            collapse.Instance.Expanded.Should().BeTrue();

            var heatmapTooltips = GetHeatmapTooltips(target);
            heatmapTooltips.Should().Contain(tooltip => tooltip.Contains("Piece #4097:", StringComparison.Ordinal) && tooltip.Contains("Piece #4099:", StringComparison.Ordinal));
            heatmapTooltips.Should().NotContain(tooltip => tooltip.Contains("Piece #4100:", StringComparison.Ordinal));
        }

        private IRenderedComponent<PiecesProgressNew> RenderTarget(IReadOnlyList<PieceState> pieces, bool isDarkMode = false, string hash = "Hash", MudTheme? theme = null)
        {
            return TestContext.Render<PiecesProgressNew>(parameters =>
            {
                parameters.Add(p => p.Hash, hash);
                parameters.Add(p => p.Pieces, pieces);
                parameters.AddCascadingValue(theme ?? new MudTheme());
                parameters.AddCascadingValue("IsDarkMode", isDarkMode);
            });
        }

        private static List<string> GetHeatmapTooltips(IRenderedComponent<PiecesProgressNew> target)
        {
            return target.FindComponents<Tooltip>()
                .Select(tooltip => tooltip.Instance.Text)
                .OfType<string>()
                .Where(text => text.StartsWith("Piece #", StringComparison.Ordinal))
                .ToList();
        }

        private List<string> GetMudTextValues(IRenderedComponent<PiecesProgressNew> target)
        {
            return target.FindComponents<MudText>()
                .Select(component => GetChildContentText(component.Instance.ChildContent))
                .OfType<string>()
                .ToList();
        }

        private static PieceState[] CreatePatternedPieces(int count)
        {
            var pieces = new PieceState[count];
            for (var index = 0; index < count; index++)
            {
                pieces[index] = (index % 3) switch
                {
                    0 => PieceState.Downloaded,
                    1 => PieceState.Downloading,
                    _ => PieceState.NotDownloaded,
                };
            }

            return pieces;
        }

        private static MudTheme CreateTheme()
        {
            return new MudTheme
            {
                PaletteLight = new PaletteLight
                {
                    Success = new MudColor("#11AA33"),
                    Info = new MudColor("#2266CC"),
                    Surface = new MudColor("#EFEFEF"),
                },
                PaletteDark = new PaletteDark
                {
                    Success = new MudColor("#66CC99"),
                    Info = new MudColor("#55AADD"),
                    Surface = new MudColor("#101010"),
                },
            };
        }
    }
}
