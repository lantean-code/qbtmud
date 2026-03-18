using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PiecesProgressSvgTests : RazorComponentTestBase<PiecesProgressSvg>
    {
        [Fact]
        public void GIVEN_LargePieceCount_WHEN_Expanded_THEN_ShowsSpinnerAndRendersSvg()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 50000).ToList();

            var target = RenderComponent(pieces);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            FindComponentByTestId<MudStack>(target, "PiecesSpinner");

            target.WaitForAssertion(() =>
            {
                HasGridSvg(target).Should().BeTrue();
                target.FindComponents<MudStack>().Where(c => c.FindAll("[data-test-id=\"PiecesSpinner\"]").Count > 0).Should().BeEmpty();
            });
        }

        [Fact]
        public void GIVEN_SmallPieceCount_WHEN_Expanded_THEN_RendersWithoutSpinner()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloading, 10).ToList();

            var target = RenderComponent(pieces);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            target.WaitForAssertion(() =>
            {
                HasGridSvg(target).Should().BeTrue();
                target.FindAll("[data-test-id=\"PiecesSpinner\"]").Count.Should().Be(0);
            });
        }

        [Fact]
        public void GIVEN_LoadingState_WHEN_Rendered_THEN_ShowsLoadingSummary()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 5).ToList();

            var target = RenderComponent(pieces, loading: true);

            GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesLoadingText").Instance.ChildContent).Should().Be("Loading pieces...");
        }

        [Fact]
        public void GIVEN_FailedState_WHEN_Expanded_THEN_ShowsUnavailableMessage()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 5).ToList();

            var target = RenderComponent(pieces, failed: true);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesEmptyText").Instance.ChildContent).Should().Be("Pieces data unavailable.");
            });
        }

        [Fact]
        public void GIVEN_SmallBreakpoint_WHEN_Expanded_THEN_ShowsHiddenMessage()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 20).ToList();

            var target = RenderComponent(pieces, breakpoint: Breakpoint.Xs);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesHiddenText").Instance.ChildContent).Should().Be("Pieces SVG hidden on small screens.");
                HasGridSvg(target).Should().BeFalse();
            });
        }

        [Fact]
        public void GIVEN_KeyboardToggle_WHEN_SpacePressed_THEN_Expands()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloading, 4).ToList();

            var target = RenderComponent(pieces);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.KeyDown(new KeyboardEventArgs { Key = " " });

            target.WaitForAssertion(() =>
            {
                HasGridSvg(target).Should().BeTrue();
            });
        }

        [Fact]
        public void GIVEN_KeyboardToggle_WHEN_EnterPressed_THEN_Expands()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 2).ToList();

            var target = RenderComponent(pieces);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.KeyDown(new KeyboardEventArgs { Key = "Enter" });

            toggleElement.GetAttribute("aria-expanded").Should().Be("true");
        }

        [Fact]
        public void GIVEN_NonToggleKey_WHEN_Pressed_THEN_RemainsCollapsed()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloading, 4).ToList();

            var target = RenderComponent(pieces);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.KeyDown(new KeyboardEventArgs { Key = "Escape" });

            toggleElement.GetAttribute("aria-expanded").Should().Be("false");
        }

        [Fact]
        public void GIVEN_NoPieces_WHEN_Expanded_THEN_ShowsUnavailableMessage()
        {
            var target = RenderComponent(Array.Empty<PieceState>());

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesEmptyText").Instance.ChildContent).Should().Be("Pieces data unavailable.");
            });
        }

        [Fact]
        public void GIVEN_SmallBreakpoint_WHEN_Expanded_THEN_UsesSmColumns()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 64).ToList();

            var target = RenderComponent(pieces, breakpoint: Breakpoint.Sm);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            target.WaitForAssertion(() =>
            {
                GetGridViewBox(target).Should().Contain("0 0 32");
            });
        }

        [Fact]
        public void GIVEN_MediumBreakpoint_WHEN_Expanded_THEN_UsesMdColumns()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 64).ToList();

            var target = RenderComponent(pieces, breakpoint: Breakpoint.Md);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            target.WaitForAssertion(() =>
            {
                GetGridViewBox(target).Should().Contain("0 0 64");
            });
        }

        [Fact]
        public void GIVEN_ExtraLargeBreakpoint_WHEN_Expanded_THEN_UsesXlColumns()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 128).ToList();

            var target = RenderComponent(pieces, breakpoint: Breakpoint.Xl);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            target.WaitForAssertion(() =>
            {
                GetGridViewBox(target).Should().Contain("0 0 128");
            });
        }

        [Fact]
        public void GIVEN_MixedPieces_WHEN_Rendered_THEN_SummaryShowsAllStates()
        {
            var pieces = new List<PieceState>
            {
                PieceState.Downloaded,
                PieceState.Downloading,
                PieceState.NotDownloaded
            };

            var target = RenderComponent(pieces);

            var summary = FindComponentByTestId<MudText>(target, "PiecesLinearSummary");
            GetChildContentText(summary.Instance.ChildContent).Should().Be("50% complete — 1 downloaded, 1 in progress");
        }

        [Fact]
        public void GIVEN_LightModeWithDownloadedPieces_WHEN_Rendered_THEN_LinearOverlayUsesSuccessContrastText()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 4).ToList();
            var theme = new MudTheme
            {
                PaletteLight = new PaletteLight
                {
                    SuccessContrastText = "rgba(12, 34, 56, 1)",
                    TextPrimary = "rgba(210, 220, 230, 1)",
                },
                PaletteDark = new PaletteDark
                {
                    SuccessContrastText = "rgba(101, 102, 103, 1)",
                },
            };

            var target = RenderComponent(pieces, theme: theme);

            var overlay = target.Find("[data-test-id=\"PiecesLinearOverlay\"]");

            overlay.GetAttribute("style").Should().Contain("color: rgba(12,34,56,1);");
        }

        [Fact]
        public void GIVEN_DarkModeWithDownloadedPieces_WHEN_Rendered_THEN_LinearOverlayUsesSuccessContrastText()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 4).ToList();
            var theme = new MudTheme
            {
                PaletteLight = new PaletteLight
                {
                    SuccessContrastText = "rgba(210, 220, 230, 1)",
                },
                PaletteDark = new PaletteDark
                {
                    SuccessContrastText = "rgba(12, 34, 56, 1)",
                    TextPrimary = "rgba(101, 102, 103, 1)",
                },
            };

            var target = RenderComponent(pieces, isDarkMode: true, theme: theme);

            var overlay = target.Find("[data-test-id=\"PiecesLinearOverlay\"]");

            overlay.GetAttribute("style").Should().Contain("color: rgba(12,34,56,1);");
        }

        [Fact]
        public void GIVEN_PendingPieces_WHEN_Rendered_THEN_LinearOverlayUsesTextPrimary()
        {
            var pieces = Enumerable.Repeat(PieceState.NotDownloaded, 4).ToList();
            var theme = new MudTheme
            {
                PaletteLight = new PaletteLight
                {
                    SuccessContrastText = "rgba(12, 34, 56, 1)",
                    TextPrimary = "rgba(210, 220, 230, 1)",
                },
            };

            var target = RenderComponent(pieces, theme: theme);

            var overlay = target.Find("[data-test-id=\"PiecesLinearOverlay\"]");

            overlay.GetAttribute("style").Should().Contain("color: rgba(210,220,230,1);");
        }

        [Fact]
        public void GIVEN_ExpandedState_WHEN_Toggled_THEN_UsesExpandedShapeClasses()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 4).ToList();

            var target = RenderComponent(pieces);

            var root = target.FindComponents<MudPaper>().First();
            root.Instance.Class.Should().Contain("pieces-progress-svg");
            root.Instance.Class.Should().NotContain("pieces-progress-svg--expanded");

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            target.WaitForAssertion(() =>
            {
                var papers = target.FindComponents<MudPaper>();
                papers.First().Instance.Class.Should().Contain("pieces-progress-svg--expanded");
                papers.Last().Instance.Class.Should().Contain("pieces-progress-svg__surface--expanded");
            });
        }

        [Fact]
        public void GIVEN_NotDownloadedPieces_WHEN_Expanded_THEN_PendingCellsUseSurfaceFillOverDistinctGridBackground()
        {
            var pieces = Enumerable.Repeat(PieceState.NotDownloaded, 4).ToList();

            var target = RenderComponent(pieces);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            target.WaitForAssertion(() =>
            {
                var style = target.Find("style").TextContent;
                var grid = target.FindAll("svg").Single(svg => svg.ClassList.Contains("pieces-progress-svg__grid"));
                var rects = target.FindAll("rect");
                style.Should().Contain(".pieces-progress-svg__rect--pending");
                style.Should().Contain(".pieces-progress-svg__rect { stroke-width: 0.03;");
                style.Should().Contain(".pieces-progress-svg__rect--downloaded { fill:");
                style.Should().Contain(".pieces-progress-svg__rect--downloaded { fill:");
                style.Should().Contain(".pieces-progress-svg__rect--pending { fill: transparent; stroke:");
                style.Should().NotContain("stroke-dasharray");
                style.Should().NotContain("stroke: none;");
                grid.GetAttribute("style").Should().Contain("background-color:");
                rects.Count.Should().Be(4);
            });
        }

        [Fact]
        public async Task GIVEN_BuildInProgress_WHEN_ParametersChange_THEN_DoesNotStartSecondBuild()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 60000).ToList();

            var target = RenderComponent(pieces);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            await target.InvokeAsync(() => target.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(PiecesProgressSvg.Hash), "Hash" },
                { nameof(PiecesProgressSvg.Pieces), pieces },
                { nameof(PiecesProgressSvg.PiecesLoading), false },
                { nameof(PiecesProgressSvg.PiecesFailed), false },
            })));

            target.WaitForAssertion(() =>
            {
                HasGridSvg(target).Should().BeTrue();
            });
        }

        private static bool HasGridSvg(IRenderedComponent<PiecesProgressSvg> target)
        {
            return target.FindAll("svg").Any(svg => svg.ClassList.Contains("pieces-progress-svg__grid"));
        }

        private static string? GetGridViewBox(IRenderedComponent<PiecesProgressSvg> target)
        {
            var grid = target.FindAll("svg").Single(svg => svg.ClassList.Contains("pieces-progress-svg__grid"));
            return grid.GetAttribute("viewBox");
        }

        private IRenderedComponent<PiecesProgressSvg> RenderComponent(IReadOnlyList<PieceState> pieces, bool loading = false, bool failed = false, Breakpoint breakpoint = Breakpoint.Lg, bool isDarkMode = false, MudTheme? theme = null)
        {
            return TestContext.Render<PiecesProgressSvg>(parameters =>
            {
                parameters.Add(p => p.Hash, "Hash");
                parameters.Add(p => p.Pieces, pieces);
                parameters.Add(p => p.PiecesLoading, loading);
                parameters.Add(p => p.PiecesFailed, failed);
                parameters.AddCascadingValue(theme ?? new MudTheme());
                parameters.AddCascadingValue("IsDarkMode", isDarkMode);
                parameters.AddCascadingValue(breakpoint);
            });
        }
    }
}
