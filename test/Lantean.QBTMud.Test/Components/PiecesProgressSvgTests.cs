using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Test.Infrastructure;
using MudBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

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
                target.Markup.Should().Contain("<svg");
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
                target.Markup.Should().Contain("<svg");
                target.Markup.Should().NotContain("PiecesSpinner");
            });
        }

        [Fact]
        public void GIVEN_LoadingState_WHEN_Rendered_THEN_ShowsLoadingSummary()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 5).ToList();

            var target = RenderComponent(pieces, loading: true);

            target.Markup.Should().Contain("Loading pieces...");
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
                target.Markup.Should().Contain("Pieces data unavailable");
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
                target.Markup.Should().Contain("Pieces SVG hidden on small screens.");
                target.Markup.Should().NotContain("pieces-progress-svg__grid");
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
                target.Markup.Should().Contain("<svg");
            });
        }

        [Fact]
        public void GIVEN_NonToggleKey_WHEN_Pressed_THEN_RemainsCollapsed()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloading, 4).ToList();

            var target = RenderComponent(pieces);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.KeyDown(new KeyboardEventArgs { Key = "Escape" });

            target.Markup.Should().Contain("aria-expanded=\"false\"");
        }

        [Fact]
        public void GIVEN_NoPieces_WHEN_Expanded_THEN_ShowsUnavailableMessage()
        {
            var target = RenderComponent(Array.Empty<PieceState>());

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            target.WaitForAssertion(() =>
            {
                target.Markup.Should().Contain("Pieces data unavailable");
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
                target.Markup.Should().Contain("viewBox=\"0 0 32");
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
                target.Markup.Should().Contain("viewBox=\"0 0 64");
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
                target.Markup.Should().Contain("viewBox=\"0 0 128");
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

            target.Markup.Should().Contain("50% complete");
            target.Markup.Should().Contain("1 downloaded, 1 in progress");
            target.Markup.Should().Contain("1 pending");
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
                target.Markup.Should().Contain("<svg");
            });
        }

        private IRenderedComponent<PiecesProgressSvg> RenderComponent(IReadOnlyList<PieceState> pieces, bool loading = false, bool failed = false, Breakpoint breakpoint = Breakpoint.Lg)
        {
            return TestContext.Render<PiecesProgressSvg>(parameters =>
            {
                parameters.Add(p => p.Hash, "Hash");
                parameters.Add(p => p.Pieces, pieces);
                parameters.Add(p => p.PiecesLoading, loading);
                parameters.Add(p => p.PiecesFailed, failed);
                parameters.AddCascadingValue(new MudTheme());
                parameters.AddCascadingValue("IsDarkMode", false);
                parameters.AddCascadingValue(breakpoint);
            });
        }
    }
}
