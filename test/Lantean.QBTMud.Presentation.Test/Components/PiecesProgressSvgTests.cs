using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Presentation.Test.Components
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
        public void GIVEN_LoadingState_WHEN_Expanded_THEN_DoesNotRenderGridCells()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 5).ToList();

            var target = RenderComponent(pieces, loading: true);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            target.WaitForAssertion(() =>
            {
                HasGridSvg(target).Should().BeFalse();
                target.FindAll("rect").Should().BeEmpty();
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesLoadingText").Instance.ChildContent).Should().Be("Loading pieces...");
            });
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
        public void GIVEN_NativeToggle_WHEN_Activated_THEN_ExpandsAndCollapses()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloading, 4).ToList();

            var target = RenderComponent(pieces);

            var toggleElement = FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Find("[data-test-id=\"PiecesToggle\"]");
            toggleElement.Click();

            target.WaitForAssertion(() =>
            {
                HasGridSvg(target).Should().BeTrue();
            });

            toggleElement.Click();

            GetToggleButton(target).GetAttribute("aria-expanded").Should().Be("false");
        }

        [Fact]
        public void GIVEN_ProgressToggle_WHEN_Rendered_THEN_UsesNativeKeyboardActivationAndTraversal()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 2).ToList();

            var target = RenderComponent(pieces);

            var button = GetToggleButton(target);

            button.TagName.Should().Be("BUTTON");
            button.GetAttribute("type").Should().Be("button");
            button.HasAttribute("disabled").Should().BeFalse();
            button.HasAttribute("tabindex").Should().BeFalse();
            button.HasAttribute("blazor:onkeydown:preventDefault").Should().BeFalse();
            button.HasAttribute("blazor:onkeyup:preventDefault").Should().BeFalse();
            button.GetAttribute("aria-expanded").Should().Be("false");
            button.GetAttribute("aria-label").Should().NotBeNullOrWhiteSpace();
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
        public void GIVEN_NoPieces_WHEN_RenderedCollapsed_THEN_UsesPendingOverlayWithoutGradient()
        {
            var target = RenderComponent(Array.Empty<PieceState>());
            var summary = FindComponentByTestId<MudText>(target, "PiecesLinearSummary");

            GetChildContentText(summary.Instance.ChildContent).Should().Be("Pieces data unavailable");
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
        public void GIVEN_LightMode_WHEN_Rendered_THEN_LinearOverlayUsesDarkPaletteTextColor()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 4).ToList();
            var theme = new MudTheme
            {
                PaletteLight = new PaletteLight
                {
                    TextPrimary = "rgba(210, 220, 230, 1)",
                },
                PaletteDark = new PaletteDark
                {
                    TextPrimary = "rgba(12, 34, 56, 1)",
                },
            };

            var target = RenderComponent(pieces, theme: theme);

            var overlay = target.Find("[data-test-id=\"PiecesLinearOverlay\"]");

            overlay.GetAttribute("style").Should().Contain("color: rgba(12,34,56,1);");
        }

        [Fact]
        public void GIVEN_DarkMode_WHEN_Rendered_THEN_LinearOverlayUsesLightPaletteTextColor()
        {
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 4).ToList();
            var theme = new MudTheme
            {
                PaletteLight = new PaletteLight
                {
                    TextPrimary = "rgba(210, 220, 230, 1)",
                },
                PaletteDark = new PaletteDark
                {
                    TextPrimary = "rgba(12, 34, 56, 1)",
                },
            };

            var target = RenderComponent(pieces, isDarkMode: true, theme: theme);

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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GIVEN_StablePieces_WHEN_ThemeColoursChange_THEN_RefreshesProgressColours(bool replaceTheme)
        {
            var pieces = new[] { PieceState.Downloaded, PieceState.Downloading, PieceState.NotDownloaded };
            var theme = new MudTheme();
            TestContext.RenderTree.Add<CascadingValue<Breakpoint>>(parameters => parameters.Add(component => component.Value, Breakpoint.Lg));
            Action<ComponentParameterCollectionBuilder<CascadingValue<MudTheme>>> configure = parameters =>
            {
                parameters.Add(component => component.Value, theme);
                parameters.AddChildContent<PiecesProgressSvg>(child => child.Add(component => component.Hash, "Hash").Add(component => component.Pieces, pieces));
            };
            var provider = TestContext.Render<CascadingValue<MudTheme>>(configure);
            var target = provider.FindComponent<PiecesProgressSvg>();
            var originalStyle = GetToggleButton(target).Children[0].GetAttribute("style");
            var updatedTheme = replaceTheme ? new MudTheme() : theme;
            updatedTheme.PaletteLight.Success = "#010203";
            updatedTheme.PaletteLight.Info = "#040506";
            updatedTheme.PaletteLight.Surface = "#070809";

            theme = updatedTheme;
            provider.Render(configure);

            provider.FindComponent<PiecesProgressSvg>().Instance.Should().BeSameAs(target.Instance);
            var style = GetToggleButton(target).Children[0].GetAttribute("style");
            style.Should().NotBe(originalStyle);
            style.Should().Contain("rgba(1,2,3,1)").And.Contain("rgba(4,5,6,1)").And.Contain("rgba(7,8,9,1)");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GIVEN_StablePieces_WHEN_DarkModeChanges_THEN_UsesCurrentPalette(bool initiallyDark)
        {
            var theme = new MudTheme();
            theme.PaletteLight.Success = "#010203";
            theme.PaletteDark.Success = "#040506";
            var pieces = new[] { PieceState.Downloaded };
            var isDark = initiallyDark;
            TestContext.RenderTree.Add<CascadingValue<MudTheme>>(parameters => parameters.Add(component => component.Value, theme));
            TestContext.RenderTree.Add<CascadingValue<Breakpoint>>(parameters => parameters.Add(component => component.Value, Breakpoint.Lg));
            Action<ComponentParameterCollectionBuilder<CascadingValue<bool>>> configure = parameters =>
            {
                parameters.Add(component => component.Name, "IsDarkMode");
                parameters.Add(component => component.Value, isDark);
                parameters.AddChildContent<PiecesProgressSvg>(child => child.Add(component => component.Hash, "Hash").Add(component => component.Pieces, pieces));
            };
            var provider = TestContext.Render<CascadingValue<bool>>(configure);
            var target = provider.FindComponent<PiecesProgressSvg>();

            isDark = !isDark;
            provider.Render(configure);

            provider.FindComponent<PiecesProgressSvg>().Instance.Should().BeSameAs(target.Instance);
            GetToggleButton(target).Children[0].GetAttribute("style").Should().Contain(initiallyDark ? "rgba(1,2,3,1)" : "rgba(4,5,6,1)");
        }

        [Fact]
        public void GIVEN_StableExpandedPieces_WHEN_BreakpointChanges_THEN_RebuildsGeometryAndHiddenMessage()
        {
            var breakpoint = Breakpoint.Lg;
            var theme = new MudTheme();
            var pieces = Enumerable.Repeat(PieceState.Downloaded, 64).ToArray();
            TestContext.RenderTree.Add<CascadingValue<MudTheme>>(parameters => parameters.Add(component => component.Value, theme));
            Action<ComponentParameterCollectionBuilder<CascadingValue<Breakpoint>>> configure = parameters =>
            {
                parameters.Add(component => component.Value, breakpoint);
                parameters.AddChildContent<PiecesProgressSvg>(child => child.Add(component => component.Hash, "Hash").Add(component => component.Pieces, pieces));
            };
            var provider = TestContext.Render<CascadingValue<Breakpoint>>(configure);
            var target = provider.FindComponent<PiecesProgressSvg>();
            GetToggleButton(target).Click();
            target.WaitForAssertion(() => GetGridViewBox(target).Should().Be("0 0 96 1"));

            breakpoint = Breakpoint.Sm;
            provider.Render(configure);

            provider.FindComponent<PiecesProgressSvg>().Instance.Should().BeSameAs(target.Instance);
            target.WaitForAssertion(() => GetGridViewBox(target).Should().Be("0 0 32 2"));

            breakpoint = Breakpoint.Xs;
            provider.Render(configure);

            target.WaitForAssertion(() => GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesHiddenText").Instance.ChildContent).Should().Be("Pieces SVG hidden on small screens."));

            breakpoint = Breakpoint.Lg;
            provider.Render(configure);

            target.WaitForAssertion(() => GetGridViewBox(target).Should().Be("0 0 96 1"));

            provider.Render(configure);

            provider.FindComponent<PiecesProgressSvg>().Instance.Should().BeSameAs(target.Instance);
            GetGridViewBox(target).Should().Be("0 0 96 1");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GIVEN_StablePieces_WHEN_LoadingOrFailureClears_THEN_RestoresProgress(bool failed)
        {
            var pieces = new[] { PieceState.Downloaded };
            var target = RenderComponent(pieces);
            GetToggleButton(target).Click();
            target.WaitForAssertion(() => HasGridSvg(target).Should().BeTrue());
            target.Render(parameters => parameters.Add(component => component.PiecesLoading, !failed).Add(component => component.PiecesFailed, failed));

            target.Render(parameters => parameters.Add(component => component.PiecesLoading, false).Add(component => component.PiecesFailed, false));

            target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesLinearSummary").Instance.ChildContent).Should().Be("100% complete — 1 downloaded, 0 in progress");
                HasGridSvg(target).Should().BeTrue();
            });
        }

        [Fact]
        public void GIVEN_StablePieces_WHEN_TorrentChanges_THEN_RefreshesAccessibleIdentity()
        {
            var target = RenderComponent(new[] { PieceState.Downloaded });
            GetToggleButton(target).Click();

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));

            target.WaitForAssertion(() =>
            {
                GetToggleButton(target).GetAttribute("aria-label").Should().Contain("OtherHash");
                GetGridElement(target).GetAttribute("aria-label").Should().Contain("OtherHash");
            });
        }

        private static IElement GetToggleButton(IRenderedComponent<PiecesProgressSvg> target)
        {
            return FindComponentByTestId<MudTooltip>(target, "PiecesToggle").Nodes.OfType<IElement>()
                .SelectMany(element => element.GetElementsByTagName("button")).Single();
        }

        private static IElement GetGridElement(IRenderedComponent<PiecesProgressSvg> target)
        {
            return target.FindComponents<MudPaper>().First().Nodes.OfType<IElement>()
                .SelectMany(element => element.GetElementsByTagName("svg"))
                .Single(element => element.ClassList.Contains("pieces-progress-svg__grid"));
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
