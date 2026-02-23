using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PieceProgressTests : RazorComponentTestBase<PieceProgress>
    {
        private static readonly Guid TestBrowserViewportId = new("10000000-0000-0000-0000-000000000001");

        private readonly Mock<IBrowserViewportService> _viewportServiceMock;
        private IBrowserViewportObserver? _observer;
        private readonly MudTheme _theme;
        private readonly IRenderedComponent<PieceProgress> _target;

        public PieceProgressTests()
        {
            _theme = CreateTheme();
            _viewportServiceMock = new Mock<IBrowserViewportService>(MockBehavior.Strict);
            _viewportServiceMock
                .Setup(service => service.SubscribeAsync(It.IsAny<IBrowserViewportObserver>(), It.IsAny<bool>()))
                .Callback<IBrowserViewportObserver, bool>((value, _) => _observer = value)
                .Returns(Task.CompletedTask);
            _viewportServiceMock
                .Setup(service => service.UnsubscribeAsync(It.IsAny<IBrowserViewportObserver>()))
                .Returns(Task.CompletedTask);
            TestContext.Services.RemoveAll<IBrowserViewportService>();
            TestContext.Services.AddSingleton(_viewportServiceMock.Object);

            _target = RenderPieceProgress(Array.Empty<PieceState>(), false, "Hash");
        }

        [Fact]
        public void GIVEN_LightMode_WHEN_Rendered_THEN_RenderPiecesBarUsesLightPaletteColorsAndStates()
        {
            var expectedDownloadingColor = _theme.PaletteLight.Success.ToString(MudColorOutputFormats.RGBA);
            var expectedHaveColor = _theme.PaletteLight.Info.ToString(MudColorOutputFormats.RGBA);
            var expectedBorderColor = _theme.PaletteLight.Black.ToString(MudColorOutputFormats.RGBA);

            var renderInvocation = TestContext.JSInterop.SetupVoid(
                "qbt.renderPiecesBar",
                invocation =>
                    invocation.Arguments.Count == 6
                    && invocation.Arguments[0] is string elementId
                    && elementId == "progress"
                    && invocation.Arguments[1] is string hash
                    && hash == "Hash"
                    && invocation.Arguments[2] is int[] states
                    && states.SequenceEqual(new[] { 2, 1, 0 })
                    && invocation.Arguments[3] is string downloadingColor
                    && downloadingColor == expectedDownloadingColor
                    && invocation.Arguments[4] is string haveColor
                    && haveColor == expectedHaveColor
                    && invocation.Arguments[5] is string borderColor
                    && borderColor == expectedBorderColor);
            renderInvocation.SetVoidResult();

            var target = RenderPieceProgress(new[] { PieceState.Downloaded, PieceState.Downloading, PieceState.NotDownloaded }, false, "Hash");

            target.WaitForAssertion(() =>
            {
                renderInvocation.Invocations.Should().ContainSingle();
            });
        }

        [Fact]
        public void GIVEN_DarkMode_WHEN_Rendered_THEN_RenderPiecesBarUsesDarkPaletteColorsAndStates()
        {
            var expectedDownloadingColor = _theme.PaletteDark.Success.ToString(MudColorOutputFormats.RGBA);
            var expectedHaveColor = _theme.PaletteDark.Info.ToString(MudColorOutputFormats.RGBA);
            var expectedBorderColor = _theme.PaletteDark.White.ToString(MudColorOutputFormats.RGBA);

            var renderInvocation = TestContext.JSInterop.SetupVoid(
                "qbt.renderPiecesBar",
                invocation =>
                    invocation.Arguments.Count == 6
                    && invocation.Arguments[0] is string elementId
                    && elementId == "progress"
                    && invocation.Arguments[1] is string hash
                    && hash == "DarkHash"
                    && invocation.Arguments[2] is int[] states
                    && states.SequenceEqual(new[] { 0, 2 })
                    && invocation.Arguments[3] is string downloadingColor
                    && downloadingColor == expectedDownloadingColor
                    && invocation.Arguments[4] is string haveColor
                    && haveColor == expectedHaveColor
                    && invocation.Arguments[5] is string borderColor
                    && borderColor == expectedBorderColor);
            renderInvocation.SetVoidResult();

            var target = RenderPieceProgress(new[] { PieceState.NotDownloaded, PieceState.Downloaded }, true, "DarkHash");

            target.WaitForAssertion(() =>
            {
                renderInvocation.Invocations.Should().ContainSingle();
            });
        }

        [Fact]
        public void GIVEN_FirstRender_WHEN_ComponentRerenders_THEN_ViewportSubscriptionOccursOnlyOnce()
        {
            _viewportServiceMock.ClearInvocations();
            _observer = null;

            var target = RenderPieceProgress(new[] { PieceState.Downloaded }, false, "Hash");

            target.WaitForAssertion(() =>
            {
                _viewportServiceMock.Verify(service => service.SubscribeAsync(It.IsAny<IBrowserViewportObserver>(), true), Times.Once);
            });

            target.Render();

            _viewportServiceMock.Verify(service => service.SubscribeAsync(It.IsAny<IBrowserViewportObserver>(), true), Times.Once);
            _observer.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_ViewportChangeNotification_WHEN_Notified_THEN_RendersPiecesBarAgainAndRequestsRerender()
        {
            var renderInvocation = TestContext.JSInterop.SetupVoid("qbt.renderPiecesBar", invocation => invocation.Arguments.Count == 6);
            renderInvocation.SetVoidResult();

            var target = RenderPieceProgress(new[] { PieceState.Downloaded, PieceState.Downloading }, false, "Hash");

            target.WaitForAssertion(() =>
            {
                renderInvocation.Invocations.Should().ContainSingle();
            });

            var initialRenderCount = target.RenderCount;

            await target.InvokeAsync(() => target.Instance.NotifyBrowserViewportChangeAsync(CreateViewportEventArgs(Breakpoint.Lg)));

            target.WaitForAssertion(() =>
            {
                renderInvocation.Invocations.Should().HaveCount(2);
                target.RenderCount.Should().BeGreaterThan(initialRenderCount);
            });
        }

        [Fact]
        public async Task GIVEN_ParametersChange_WHEN_SetParametersAsyncCalled_THEN_RendersPiecesBarWithUpdatedValues()
        {
            var updatedInvocation = TestContext.JSInterop.SetupVoid(
                "qbt.renderPiecesBar",
                invocation =>
                    invocation.Arguments.Count == 6
                    && invocation.Arguments[1] is string hash
                    && hash == "UpdatedHash"
                    && invocation.Arguments[2] is int[] states
                    && states.SequenceEqual(new[] { 1, 1 }));
            updatedInvocation.SetVoidResult();

            var target = RenderPieceProgress(new[] { PieceState.Downloaded }, false, "Hash");

            await target.InvokeAsync(() => target.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(PieceProgress.Hash), "UpdatedHash" },
                { nameof(PieceProgress.Pieces), new[] { PieceState.Downloading, PieceState.Downloading } },
            })));

            target.WaitForAssertion(() =>
            {
                updatedInvocation.Invocations.Should().ContainSingle();
            });
        }

        [Fact]
        public void GIVEN_ObserverInterface_WHEN_ReadingProperties_THEN_IdAndResizeOptionsAreValid()
        {
            var observer = (IBrowserViewportObserver)_target.Instance;

            observer.Id.Should().NotBe(Guid.Empty);
            observer.ResizeOptions.Should().NotBeNull();
            var resizeOptions = observer.ResizeOptions!;
            resizeOptions.ReportRate.Should().Be(50);
            resizeOptions.NotifyOnBreakpointOnly.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ComponentDisposedTwice_WHEN_DisposeInvoked_THEN_UnsubscribesOnlyOnce()
        {
            _viewportServiceMock.ClearInvocations();
            _observer = null;

            var target = RenderPieceProgress(new[] { PieceState.Downloaded }, false, "Hash");

            target.WaitForAssertion(() =>
            {
                _viewportServiceMock.Verify(service => service.SubscribeAsync(It.IsAny<IBrowserViewportObserver>(), true), Times.Once);
            });

            _observer.Should().NotBeNull();

            await target.Instance.DisposeAsync();
            await target.Instance.DisposeAsync();

            _viewportServiceMock.Verify(service => service.UnsubscribeAsync(_observer!), Times.Once);
        }

        private IRenderedComponent<PieceProgress> RenderPieceProgress(IReadOnlyList<PieceState> pieces, bool isDarkMode, string hash)
        {
            return TestContext.Render<PieceProgress>(parameters =>
            {
                parameters.Add(p => p.Hash, hash);
                parameters.Add(p => p.Pieces, pieces);
                parameters.AddCascadingValue("IsDarkMode", isDarkMode);
                parameters.AddCascadingValue(_theme);
            });
        }

        private static BrowserViewportEventArgs CreateViewportEventArgs(Breakpoint breakpoint)
        {
            return new BrowserViewportEventArgs(
                TestBrowserViewportId,
                new BrowserWindowSize
                {
                    Width = 1280,
                    Height = 720,
                },
                breakpoint,
                false);
        }

        private static MudTheme CreateTheme()
        {
            return new MudTheme
            {
                PaletteLight = new PaletteLight
                {
                    Success = new MudColor("#11AA33"),
                    Info = new MudColor("#2266CC"),
                    Black = new MudColor("#101010"),
                },
                PaletteDark = new PaletteDark
                {
                    Success = new MudColor("#66CC99"),
                    Info = new MudColor("#55AADD"),
                    White = new MudColor("#FAFAFA"),
                },
            };
        }
    }
}
