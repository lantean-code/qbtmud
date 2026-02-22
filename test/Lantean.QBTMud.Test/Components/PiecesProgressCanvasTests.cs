using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Utilities;
using System.Collections;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PiecesProgressCanvasTests : RazorComponentTestBase<PiecesProgressCanvas>
    {
        private readonly IRenderedComponent<PiecesProgressCanvas> _target;
        private readonly MudTheme _theme;

        public PiecesProgressCanvasTests()
        {
            _theme = CreateTheme();
            _target = RenderComponent(Array.Empty<PieceState>(), Breakpoint.Lg);
        }

        [Fact]
        public void GIVEN_NoPieces_WHEN_InitiallyRendered_THEN_ShowsUnavailableSummaryAndExpandedCanvas()
        {
            var collapse = _target.FindComponent<MudCollapse>();
            var tooltip = _target.FindComponent<MudTooltip>();
            var icon = _target.FindComponent<MudIcon>();
            var textValues = GetMudTextContent(_target);

            collapse.Instance.Expanded.Should().BeTrue();
            tooltip.Instance.Text.Should().Be("Pieces data unavailable");
            textValues.Should().Contain("Pieces data unavailable");
            textValues.Should().Contain("Pieces data unavailable.");
            icon.Instance.Icon.Should().Be(Icons.Material.Filled.ExpandLess);
        }

        [Fact]
        public void GIVEN_ToggleRendered_WHEN_EnterPressed_THEN_Collapses()
        {
            var target = RenderComponent(new[] { PieceState.Downloaded }, Breakpoint.Xs);
            var toggle = FindToggleElement(target);

            toggle.KeyDown(new KeyboardEventArgs { Key = "Enter" });

            var collapse = target.FindComponent<MudCollapse>();
            var icon = target.FindComponent<MudIcon>();
            collapse.Instance.Expanded.Should().BeFalse();
            icon.Instance.Icon.Should().Be(Icons.Material.Filled.ExpandMore);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("Space")]
        [InlineData("Spacebar")]
        public void GIVEN_ToggleRendered_WHEN_SpaceVariantPressed_THEN_Collapses(string key)
        {
            var target = RenderComponent(new[] { PieceState.Downloaded }, Breakpoint.Xs);
            var toggle = FindToggleElement(target);

            toggle.KeyDown(new KeyboardEventArgs { Key = key });

            var collapse = target.FindComponent<MudCollapse>();
            collapse.Instance.Expanded.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ToggleRendered_WHEN_NonToggleKeyPressed_THEN_RemainsExpanded()
        {
            var target = RenderComponent(new[] { PieceState.Downloaded }, Breakpoint.Xs);
            var toggle = FindToggleElement(target);

            toggle.KeyDown(new KeyboardEventArgs { Key = "Escape" });

            var collapse = target.FindComponent<MudCollapse>();
            collapse.Instance.Expanded.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_CollapsedCanvas_WHEN_ParametersUpdated_THEN_RemainsCollapsedWithoutPreparingCanvas()
        {
            var target = RenderComponent(
                new[] { PieceState.Downloaded, PieceState.Downloading, PieceState.NotDownloaded },
                Breakpoint.Xs);
            var toggle = FindToggleElement(target);

            toggle.Click();

            await target.InvokeAsync(() => target.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(PiecesProgressCanvas.Hash), "Hash" },
                { nameof(PiecesProgressCanvas.Pieces), new[] { PieceState.Downloaded, PieceState.NotDownloaded } },
            })));

            var collapse = target.FindComponent<MudCollapse>();
            var icon = target.FindComponent<MudIcon>();
            collapse.Instance.Expanded.Should().BeFalse();
            icon.Instance.Icon.Should().Be(Icons.Material.Filled.ExpandMore);
        }

        [Fact]
        public void GIVEN_CollapsedCanvas_WHEN_ToggledAgainOnSmallBreakpoint_THEN_ExpandsAndShowsHiddenMessage()
        {
            var target = RenderComponent(new[] { PieceState.Downloaded, PieceState.Downloading }, Breakpoint.Xs);
            var toggle = FindToggleElement(target);

            toggle.Click();
            toggle.Click();

            var collapse = target.FindComponent<MudCollapse>();
            var textValues = GetMudTextContent(target);

            collapse.Instance.Expanded.Should().BeTrue();
            textValues.Should().Contain("Pieces canvas hidden on small screens.");
        }

        [Fact]
        public void GIVEN_MixedPiecesOnMediumBreakpoint_WHEN_Rendered_THEN_InvokesCanvasRendererWithExpectedColumnsAndStates()
        {
            var pieces = new[]
            {
                PieceState.Downloaded,
                PieceState.Downloaded,
                PieceState.Downloading,
                PieceState.NotDownloaded,
            };
            var expectedPieceStates = new[] { 2, 2, 1, 0 };
            var getRectInvocation = TestContext.JSInterop.Setup<BoundingClientRect?>(
                "qbt.getBoundingClientRect",
                invocation => invocation.Arguments.Count == 1 && invocation.Arguments[0] is string selector && selector.StartsWith("#pieces-progress-canvas-", StringComparison.Ordinal));
            getRectInvocation.SetResult(new BoundingClientRect
            {
                Width = 320,
                Height = 12,
            });
            var renderInvocation = TestContext.JSInterop.SetupVoid(
                "qbt.renderPiecesCanvas",
                invocation =>
                    invocation.Arguments.Count == 9
                    && invocation.Arguments[3] is int columns
                    && columns == 32
                    && invocation.Arguments[5] is int[] pieceStates
                    && pieceStates.SequenceEqual(expectedPieceStates));
            renderInvocation.SetVoidResult();

            var target = RenderComponent(pieces, Breakpoint.Md);

            target.WaitForAssertion(() =>
            {
                getRectInvocation.Invocations.Should().NotBeEmpty();
                renderInvocation.Invocations.Should().NotBeEmpty();
            });
        }

        [Fact]
        public void GIVEN_CanvasAlreadyRendered_WHEN_NonToggleKeyPressed_THEN_SubsequentRenderSkipsRedrawPath()
        {
            var getRectInvocation = TestContext.JSInterop.Setup<BoundingClientRect?>(
                "qbt.getBoundingClientRect",
                invocation => invocation.Arguments.Count == 1);
            getRectInvocation.SetResult(new BoundingClientRect
            {
                Width = 320,
                Height = 12,
            });
            var renderInvocation = TestContext.JSInterop.SetupVoid("qbt.renderPiecesCanvas", _ => true);
            renderInvocation.SetVoidResult();

            var target = RenderComponent(new[] { PieceState.Downloaded, PieceState.Downloading }, Breakpoint.Lg);

            target.WaitForAssertion(() =>
            {
                getRectInvocation.Invocations.Should().NotBeEmpty();
                renderInvocation.Invocations.Should().NotBeEmpty();
            });

            var toggle = FindToggleElement(target);
            toggle.KeyDown(new KeyboardEventArgs { Key = "Escape" });

            target.WaitForAssertion(() =>
            {
                getRectInvocation.Invocations.Should().NotBeEmpty();
                renderInvocation.Invocations.Should().NotBeEmpty();
            });
        }

        [Fact]
        public void GIVEN_DarkModeOnLargeBreakpoint_WHEN_Rendered_THEN_UsesDarkPaletteForCanvasColors()
        {
            var pieces = new[]
            {
                PieceState.Downloaded,
                PieceState.Downloading,
                PieceState.NotDownloaded,
            };
            var expectedDownloadedColor = _theme.PaletteDark.Success.ToString(MudColorOutputFormats.RGBA);
            var expectedDownloadingColor = _theme.PaletteDark.Info.ToString(MudColorOutputFormats.RGBA);
            var expectedPendingColor = _theme.PaletteDark.Surface.ToString(MudColorOutputFormats.RGBA);

            var getRectInvocation = TestContext.JSInterop.Setup<BoundingClientRect?>(
                "qbt.getBoundingClientRect",
                invocation => invocation.Arguments.Count == 1);
            getRectInvocation.SetResult(new BoundingClientRect
            {
                Width = 256,
                Height = 8,
            });
            var renderInvocation = TestContext.JSInterop.SetupVoid(
                "qbt.renderPiecesCanvas",
                invocation =>
                    invocation.Arguments.Count == 9
                    && invocation.Arguments[3] is int columns
                    && columns == 64
                    && invocation.Arguments[6] is string downloadedColor
                    && downloadedColor == expectedDownloadedColor
                    && invocation.Arguments[7] is string downloadingColor
                    && downloadingColor == expectedDownloadingColor
                    && invocation.Arguments[8] is string pendingColor
                    && pendingColor == expectedPendingColor);
            renderInvocation.SetVoidResult();

            var target = RenderComponent(pieces, Breakpoint.Lg, true, theme: _theme);

            target.WaitForAssertion(() =>
            {
                getRectInvocation.Invocations.Should().ContainSingle();
                renderInvocation.Invocations.Should().ContainSingle();
            });
        }

        [Fact]
        public void GIVEN_BoundingRectIsNull_WHEN_Rendered_THEN_DoesNotInvokeCanvasRenderer()
        {
            var getRectInvocation = TestContext.JSInterop.Setup<BoundingClientRect?>(
                "qbt.getBoundingClientRect",
                invocation => invocation.Arguments.Count == 1);
            getRectInvocation.SetResult(null);
            var renderInvocation = TestContext.JSInterop.SetupVoid("qbt.renderPiecesCanvas", _ => true);
            renderInvocation.SetVoidResult();

            var target = RenderComponent(new[] { PieceState.Downloaded, PieceState.Downloading }, Breakpoint.Lg);

            target.WaitForAssertion(() =>
            {
                getRectInvocation.Invocations.Should().ContainSingle();
                renderInvocation.Invocations.Should().BeEmpty();
            });
        }

        [Fact]
        public void GIVEN_BoundingRectWidthIsZero_WHEN_Rendered_THEN_DoesNotInvokeCanvasRenderer()
        {
            var getRectInvocation = TestContext.JSInterop.Setup<BoundingClientRect?>(
                "qbt.getBoundingClientRect",
                invocation => invocation.Arguments.Count == 1);
            getRectInvocation.SetResult(new BoundingClientRect
            {
                Width = 0,
                Height = 5,
            });
            var renderInvocation = TestContext.JSInterop.SetupVoid("qbt.renderPiecesCanvas", _ => true);
            renderInvocation.SetVoidResult();

            var target = RenderComponent(new[] { PieceState.Downloaded, PieceState.Downloading }, Breakpoint.Lg);

            target.WaitForAssertion(() =>
            {
                getRectInvocation.Invocations.Should().ContainSingle();
                renderInvocation.Invocations.Should().BeEmpty();
            });
        }

        [Fact]
        public void GIVEN_CountDropsToZeroForGradientGuard_WHEN_Rendered_THEN_UsesZeroPercentSummary()
        {
            var pieces = new SequencedCountPieces(
                new[] { PieceState.Downloaded },
                1,
                0,
                0,
                0);

            var target = RenderComponent(pieces, Breakpoint.Xs);
            var tooltip = target.FindComponent<MudTooltip>();
            var textValues = GetMudTextContent(target);

            tooltip.Instance.Text.Should().Be("Downloaded: 1\nDownloading: 0\nPending: 0");
            textValues.Should().Contain("0% complete — 1 downloaded, 0 in progress");
        }

        [Fact]
        public void GIVEN_TotalPiecesResolvesToZeroInSegmentMath_WHEN_Rendered_THEN_CompletesWithoutErrors()
        {
            var pieces = new SequencedCountPieces(
                new[] { PieceState.Downloaded },
                1,
                1,
                0,
                1,
                1);

            var target = RenderComponent(pieces, Breakpoint.Xs);
            var textValues = GetMudTextContent(target);

            textValues.Should().Contain("100% complete — 1 downloaded, 0 in progress");
        }

        private IRenderedComponent<PiecesProgressCanvas> RenderComponent(
            IReadOnlyList<PieceState> pieces,
            Breakpoint breakpoint,
            bool isDarkMode = false,
            string hash = "Hash",
            MudTheme? theme = null)
        {
            return TestContext.Render<PiecesProgressCanvas>(parameters =>
            {
                parameters.Add(p => p.Hash, hash);
                parameters.Add(p => p.Pieces, pieces);
                parameters.AddCascadingValue(theme ?? _theme);
                parameters.AddCascadingValue("IsDarkMode", isDarkMode);
                parameters.AddCascadingValue(breakpoint);
            });
        }

        private static IElement FindToggleElement(IRenderedComponent<PiecesProgressCanvas> target)
        {
            return target.Find("div.pieces-progress-canvas__linear");
        }

        private List<string?> GetMudTextContent(IRenderedComponent<PiecesProgressCanvas> target)
        {
            var values = new List<string?>();
            foreach (var text in target.FindComponents<MudText>())
            {
                values.Add(GetChildContentText(text.Instance.ChildContent));
            }

            return values;
        }

        private static MudTheme CreateTheme()
        {
            return new MudTheme
            {
                PaletteLight = new PaletteLight
                {
                    Success = new MudColor("#11AA33"),
                    Info = new MudColor("#2266CC"),
                    Surface = new MudColor("#E1E5EA"),
                },
                PaletteDark = new PaletteDark
                {
                    Success = new MudColor("#66CC99"),
                    Info = new MudColor("#55AADD"),
                    Surface = new MudColor("#1B2230"),
                },
            };
        }

        private sealed class SequencedCountPieces : IReadOnlyList<PieceState>
        {
            private readonly IReadOnlyList<PieceState> _items;
            private readonly Queue<int> _countSequence;

            public SequencedCountPieces(IReadOnlyList<PieceState> items, params int[] countSequence)
            {
                _items = items;
                _countSequence = new Queue<int>(countSequence);
            }

            public PieceState this[int index]
            {
                get
                {
                    if (_items.Count == 0)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    if (index < 0)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    if (index >= _items.Count)
                    {
                        return _items[_items.Count - 1];
                    }

                    return _items[index];
                }
            }

            public int Count
            {
                get
                {
                    if (_countSequence.Count > 0)
                    {
                        return _countSequence.Dequeue();
                    }

                    return _items.Count;
                }
            }

            public IEnumerator<PieceState> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
