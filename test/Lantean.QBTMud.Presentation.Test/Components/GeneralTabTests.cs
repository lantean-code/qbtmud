using System.Net;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Presentation.Test.Components
{
    public sealed class GeneralTabTests : RazorComponentTestBase<GeneralTab>
    {
        private readonly IApiClient _apiClient = Mock.Of<IApiClient>();
        private readonly IManagedTimer _timer;
        private readonly IManagedTimerFactory _timerFactory;
        private Func<CancellationToken, Task<ManagedTimerTickResult>>? _tickHandler;

        public GeneralTabTests()
        {
            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(_apiClient);

            _timer = Mock.Of<IManagedTimer>();
            _timerFactory = Mock.Of<IManagedTimerFactory>();
            Mock.Get(_timerFactory)
                .Setup(factory => factory.Create(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(_timer);
            Mock.Get(_timer)
                .Setup(timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((handler, _) => _tickHandler = handler)
                .ReturnsAsync(true);
            TestContext.Services.RemoveAll<IManagedTimerFactory>();
            TestContext.Services.AddSingleton(_timerFactory);
        }

        [Fact]
        public async Task GIVEN_InactiveTab_WHEN_TimerTicks_THEN_DoesNotRender()
        {
            var target = RenderGeneralTab(false, "Hash");
            var initialRenderCount = target.RenderCount;

            await TriggerTimerTickAsync(target);

            target.RenderCount.Should().Be(initialRenderCount);
        }

        [Fact]
        public void GIVEN_NullHash_WHEN_Rendered_THEN_DoesNotCallApi()
        {
            RenderGeneralTab(true, null);

            Mock.Get(_apiClient).Verify(c => c.GetTorrentPropertiesAsync(It.IsAny<string>()), Times.Never);
            Mock.Get(_apiClient).Verify(c => c.GetTorrentPieceStatesAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_ActiveTab_WHEN_PropertiesNotFound_THEN_ShowsPiecesUnavailable()
        {
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPropertiesAsync("Hash"))
                .ReturnsFailure(ApiFailureKind.NotFound, "Message", HttpStatusCode.NotFound);

            var target = RenderGeneralTab(true, "Hash");

            target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesLinearSummary").Instance.ChildContent).Should().Be("Pieces data unavailable");
            });
        }

        [Fact]
        public void GIVEN_ActiveTab_WHEN_PiecesRequestFails_THEN_RoutesFailureThroughWorkflow()
        {
            var apiFeedbackWorkflow = new Mock<IApiFeedbackWorkflow>(MockBehavior.Strict);
            apiFeedbackWorkflow
                .Setup(workflow => workflow.HandleFailureAsync(
                    It.IsAny<ApiResult<IReadOnlyList<PieceState>>>(),
                    It.IsAny<Func<string?, string>?>(),
                    It.IsAny<Severity>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            TestContext.Services.RemoveAll<IApiFeedbackWorkflow>();
            TestContext.Services.AddSingleton(apiFeedbackWorkflow.Object);

            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPropertiesAsync("Hash"))
                .ReturnsSuccessAsync(CreateProperties());
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPieceStatesAsync("Hash"))
                .ReturnsFailure(ApiFailureKind.ServerError, "Message", HttpStatusCode.InternalServerError);

            _ = RenderGeneralTab(true, "Hash");

            apiFeedbackWorkflow.Verify(workflow => workflow.HandleFailureAsync(
                It.IsAny<ApiResult<IReadOnlyList<PieceState>>>(),
                It.IsAny<Func<string?, string>?>(),
                It.IsAny<Severity>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void GIVEN_ActiveTab_WHEN_PropertiesRequestFails_THEN_ShowsPiecesUnavailable()
        {
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPropertiesAsync("Hash"))
                .ReturnsFailure(ApiFailureKind.ServerError, "Message", HttpStatusCode.InternalServerError);

            var target = RenderGeneralTab(true, "Hash");

            target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesLinearSummary").Instance.ChildContent).Should().Be("Pieces data unavailable");
            });
        }

        [Fact]
        public void GIVEN_ActiveTab_WHEN_PiecesNotFound_THEN_ShowsPiecesUnavailable()
        {
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPropertiesAsync("Hash"))
                .ReturnsSuccessAsync(CreateProperties());
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPieceStatesAsync("Hash"))
                .ReturnsFailure(ApiFailureKind.NotFound, "Message", HttpStatusCode.NotFound);

            var target = RenderGeneralTab(true, "Hash");

            target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesLinearSummary").Instance.ChildContent).Should().Be("Pieces data unavailable");
            });
        }

        [Fact]
        public void GIVEN_ActiveTab_WHEN_PiecesLoaded_THEN_ShowsPiecesProgressSummary()
        {
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPropertiesAsync("Hash"))
                .ReturnsSuccessAsync(CreateProperties());
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPieceStatesAsync("Hash"))
                .ReturnsSuccessAsync(new[] { PieceState.Downloaded, PieceState.Downloading });

            var target = RenderGeneralTab(true, "Hash");

            target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesLinearSummary").Instance.ChildContent).Should().Be("75% complete — 1 downloaded, 1 in progress");
            });
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTicksAndPropertiesNotFound_THEN_ShowsPiecesUnavailable()
        {
            Mock.Get(_apiClient)
                .SetupSequence(c => c.GetTorrentPropertiesAsync("Hash"))
                .ReturnsAsync(CreateProperties())
                .ReturnsFailure(ApiFailureKind.NotFound, "Message", HttpStatusCode.NotFound);
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPieceStatesAsync("Hash"))
                .ReturnsSuccessAsync(new[] { PieceState.Downloaded });

            var target = RenderGeneralTab(true, "Hash");
            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesLinearSummary").Instance.ChildContent).Should().Be("Pieces data unavailable");
            });
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTicksAndPropertiesForbidden_THEN_ShowsPiecesUnavailable()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            Mock.Get(_apiClient)
                .SetupSequence(c => c.GetTorrentPropertiesAsync("Hash"))
                .ReturnsAsync(CreateProperties())
                .ReturnsFailure(ApiFailureKind.AuthenticationRequired, "Message", HttpStatusCode.Forbidden);
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPieceStatesAsync("Hash"))
                .ReturnsSuccessAsync(new[] { PieceState.Downloaded });

            var target = RenderGeneralTab(true, "Hash");
            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesLinearSummary").Instance.ChildContent).Should().Be("Pieces data unavailable");
            });
            navigationManager.Uri.Should().EndWith("/login");
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTicksAndPiecesNotFound_THEN_ShowsPiecesUnavailable()
        {
            Mock.Get(_apiClient)
                .SetupSequence(c => c.GetTorrentPropertiesAsync("Hash"))
                .ReturnsAsync(CreateProperties())
                .ReturnsAsync(CreateProperties());
            Mock.Get(_apiClient)
                .SetupSequence(c => c.GetTorrentPieceStatesAsync("Hash"))
                .ReturnsAsync(new[] { PieceState.Downloaded })
                .ReturnsFailure(ApiFailureKind.NotFound, "Message", HttpStatusCode.NotFound);

            var target = RenderGeneralTab(true, "Hash");
            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesLinearSummary").Instance.ChildContent).Should().Be("Pieces data unavailable");
            });
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTicksAndPiecesLoaded_THEN_ShowsUpdatedProgressSummary()
        {
            Mock.Get(_apiClient)
                .SetupSequence(c => c.GetTorrentPropertiesAsync("Hash"))
                .ReturnsAsync(CreateProperties())
                .ReturnsAsync(CreateProperties());
            Mock.Get(_apiClient)
                .SetupSequence(c => c.GetTorrentPieceStatesAsync("Hash"))
                .ReturnsAsync(new[] { PieceState.Downloading })
                .ReturnsAsync(new[] { PieceState.Downloaded, PieceState.Downloaded });

            var target = RenderGeneralTab(true, "Hash");
            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() =>
            {
                GetChildContentText(FindComponentByTestId<MudText>(target, "PiecesLinearSummary").Instance.ChildContent).Should().Be("100% complete — 2 downloaded, 0 in progress");
            });
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTicksAndPropertiesServerError_THEN_ReturnsContinue()
        {
            Mock.Get(_apiClient)
                .SetupSequence(c => c.GetTorrentPropertiesAsync("Hash"))
                .ReturnsAsync(CreateProperties())
                .ReturnsFailure(ApiFailureKind.ServerError, "Message", HttpStatusCode.InternalServerError);
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPieceStatesAsync("Hash"))
                .ReturnsSuccessAsync(new[] { PieceState.Downloaded });

            var target = RenderGeneralTab(true, "Hash");

            var result = await TriggerTimerTickAsync(target);

            result.Should().Be(ManagedTimerTickResult.Continue);
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTicksAndPiecesServerError_THEN_ReturnsContinue()
        {
            Mock.Get(_apiClient)
                .SetupSequence(c => c.GetTorrentPropertiesAsync("Hash"))
                .ReturnsAsync(CreateProperties())
                .ReturnsAsync(CreateProperties());
            Mock.Get(_apiClient)
                .SetupSequence(c => c.GetTorrentPieceStatesAsync("Hash"))
                .ReturnsAsync(new[] { PieceState.Downloaded })
                .ReturnsFailure(ApiFailureKind.ServerError, "Message", HttpStatusCode.InternalServerError);

            var target = RenderGeneralTab(true, "Hash");

            var result = await TriggerTimerTickAsync(target);

            result.Should().Be(ManagedTimerTickResult.Continue);
        }

        [Fact]
        public async Task GIVEN_RenderedComponent_WHEN_Disposed_THEN_DoesNotThrow()
        {
            var target = RenderGeneralTab(false, "Hash");

            await target.Instance.DisposeAsync();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_LoadedTorrent_WHEN_NextSelectionFails_THEN_KeepsPreviousDisplayAndRetriesOnTick(bool failPieces)
        {
            var api = Mock.Get(_apiClient);
            api.Setup(client => client.GetTorrentPropertiesAsync("Hash")).ReturnsSuccessAsync(CreateProperties());
            api.Setup(client => client.GetTorrentPieceStatesAsync("Hash")).ReturnsSuccessAsync(new[] { PieceState.Downloaded });
            if (failPieces)
            {
                api.Setup(client => client.GetTorrentPropertiesAsync("OtherHash")).ReturnsSuccessAsync(CreateProperties());
                api.Setup(client => client.GetTorrentPieceStatesAsync("OtherHash")).ReturnsFailure(ApiFailureKind.ServerError, "Message", HttpStatusCode.InternalServerError);
            }
            else
            {
                api.Setup(client => client.GetTorrentPropertiesAsync("OtherHash")).ReturnsFailure(ApiFailureKind.ServerError, "Message", HttpStatusCode.InternalServerError);
            }
            var target = RenderGeneralTab(true, "Hash");

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));

            var progress = target.FindComponent<PiecesProgressSvg>();
            progress.Instance.Hash.Should().Be("Hash");
            progress.Instance.Pieces.Should().Equal(PieceState.Downloaded);
            progress.Instance.PiecesLoading.Should().BeFalse();
            api.Setup(client => client.GetTorrentPropertiesAsync("OtherHash")).ReturnsSuccessAsync(CreateProperties());
            api.Setup(client => client.GetTorrentPieceStatesAsync("OtherHash")).ReturnsSuccessAsync(new[] { PieceState.NotDownloaded });

            await TriggerTimerTickAsync(target);

            progress.Instance.Hash.Should().Be("OtherHash");
            progress.Instance.Pieces.Should().Equal(PieceState.NotDownloaded);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_PendingSelection_WHEN_Disposed_THEN_IgnoresLateResponse(bool delayPieces)
        {
            var properties = new TaskCompletionSource<ApiResult<TorrentProperties>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pieces = new TaskCompletionSource<ApiResult<IReadOnlyList<PieceState>>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var api = Mock.Get(_apiClient);
            api.Setup(client => client.GetTorrentPropertiesAsync("Hash")).Returns(delayPieces ? Task.FromResult(ApiResult.CreateSuccess(CreateProperties())) : properties.Task);
            api.Setup(client => client.GetTorrentPieceStatesAsync("Hash")).Returns(pieces.Task);
            var displayed = new Mock<Func<string, Task>>();
            displayed.Setup(callback => callback(It.IsAny<string>())).Returns(Task.CompletedTask);
            var target = TestContext.Render<GeneralTab>(parameters => parameters
                .Add(component => component.Hash, "Hash")
                .Add(component => component.Active, true)
                .Add(component => component.DisplayedHashChanged, displayed.Object)
                .AddCascadingValue(new MudTheme()));

            await target.Instance.DisposeAsync();
            await target.InvokeAsync(() => properties.SetResult(ApiResult.CreateSuccess(CreateProperties())));
            await target.InvokeAsync(() => pieces.SetResult(ApiResult.CreateSuccess<IReadOnlyList<PieceState>>(new[] { PieceState.Downloaded })));

            displayed.Verify(callback => callback(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_PendingSelection_WHEN_RenderedAgainOrTimerTicks_THEN_KeepsDisplayWithoutDuplicateRequests()
        {
            var api = Mock.Get(_apiClient);
            api.Setup(client => client.GetTorrentPropertiesAsync("Hash")).ReturnsSuccessAsync(CreateProperties());
            api.Setup(client => client.GetTorrentPieceStatesAsync("Hash")).ReturnsSuccessAsync(new[] { PieceState.Downloaded });
            var pending = new TaskCompletionSource<ApiResult<TorrentProperties>>(TaskCreationOptions.RunContinuationsAsynchronously);
            api.Setup(client => client.GetTorrentPropertiesAsync("OtherHash")).Returns(pending.Task);
            api.Setup(client => client.GetTorrentPieceStatesAsync("OtherHash")).ReturnsSuccessAsync(new[] { PieceState.NotDownloaded });
            var target = RenderGeneralTab(true, "Hash");

            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));
            target.Render(parameters => parameters.Add(component => component.PiecesExpanded, true));
            var result = await TriggerTimerTickAsync(target);

            result.Should().Be(ManagedTimerTickResult.Continue);
            target.FindComponent<PiecesProgressSvg>().Instance.Hash.Should().Be("Hash");
            api.Verify(client => client.GetTorrentPropertiesAsync("OtherHash"), Times.Once);
            await target.InvokeAsync(() => pending.SetResult(ApiResult.CreateSuccess(CreateProperties())));
            target.WaitForAssertion(() => target.FindComponent<PiecesProgressSvg>().Instance.Hash.Should().Be("OtherHash"));
        }

        [Fact]
        public async Task GIVEN_DisposedTab_WHEN_RenderedOrTimerTicks_THEN_DoesNotLoadAgain()
        {
            var target = RenderGeneralTab(false, "Hash");
            await target.Instance.DisposeAsync();

            target.Render(parameters => parameters.Add(component => component.Active, true));
            var result = await TriggerTimerTickAsync(target);

            result.Should().Be(ManagedTimerTickResult.Stop);
            Mock.Get(_apiClient).Verify(client => client.GetTorrentPropertiesAsync(It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_RefreshInFlight_WHEN_TorrentChanges_THEN_RefreshCannotReplaceNewDetails(bool delayPieces)
        {
            var api = Mock.Get(_apiClient);
            api.Setup(client => client.GetTorrentPropertiesAsync("Hash")).ReturnsSuccessAsync(CreateProperties());
            api.Setup(client => client.GetTorrentPieceStatesAsync("Hash")).ReturnsSuccessAsync(new[] { PieceState.Downloaded });
            api.Setup(client => client.GetTorrentPropertiesAsync("OtherHash")).ReturnsSuccessAsync(CreateProperties());
            api.Setup(client => client.GetTorrentPieceStatesAsync("OtherHash")).ReturnsSuccessAsync(new[] { PieceState.Downloading });
            var target = RenderGeneralTab(true, "Hash");
            var properties = new TaskCompletionSource<ApiResult<TorrentProperties>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pieces = new TaskCompletionSource<ApiResult<IReadOnlyList<PieceState>>>(TaskCreationOptions.RunContinuationsAsynchronously);
            api.Setup(client => client.GetTorrentPropertiesAsync("Hash")).Returns(delayPieces ? Task.FromResult(ApiResult.CreateSuccess(CreateProperties())) : properties.Task);
            api.Setup(client => client.GetTorrentPieceStatesAsync("Hash")).Returns(pieces.Task);

            var tick = TriggerTimerTickAsync(target);
            target.Render(parameters => parameters.Add(component => component.Hash, "OtherHash"));
            await target.InvokeAsync(() => properties.SetResult(ApiResult.CreateSuccess(CreateProperties())));
            await target.InvokeAsync(() => pieces.SetResult(ApiResult.CreateSuccess<IReadOnlyList<PieceState>>(new[] { PieceState.NotDownloaded })));
            await tick;

            target.FindComponent<PiecesProgressSvg>().Instance.Hash.Should().Be("OtherHash");
            target.FindComponent<PiecesProgressSvg>().Instance.Pieces.Should().Equal(PieceState.Downloading);
        }

        [Fact]
        public async Task GIVEN_CancelledTickToken_WHEN_TimerHandlerRuns_THEN_ReturnsStop()
        {
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPropertiesAsync("Hash"))
                .ReturnsSuccessAsync(CreateProperties());
            Mock.Get(_apiClient)
                .Setup(c => c.GetTorrentPieceStatesAsync("Hash"))
                .ReturnsSuccessAsync(new[] { PieceState.Downloaded });

            var target = RenderGeneralTab(true, "Hash");
            var handler = GetTickHandler(target);
            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();

            ManagedTimerTickResult result = ManagedTimerTickResult.Continue;
            await target.InvokeAsync(async () =>
            {
                result = await handler(cancellationSource.Token);
            });

            result.Should().Be(ManagedTimerTickResult.Stop);
        }

        private IRenderedComponent<GeneralTab> RenderGeneralTab(bool active, string? hash)
        {
            return TestContext.Render<GeneralTab>(parameters =>
            {
                parameters.Add(p => p.Active, active);
                parameters.Add(p => p.Hash, hash);
                parameters.AddCascadingValue("RefreshInterval", 10);
                parameters.AddCascadingValue(new MudTheme());
                parameters.AddCascadingValue("IsDarkMode", false);
                parameters.AddCascadingValue(Breakpoint.Lg);
            });
        }

        private async Task<ManagedTimerTickResult> TriggerTimerTickAsync(IRenderedComponent<GeneralTab> target)
        {
            var handler = GetTickHandler(target);
            return await target.InvokeAsync(() => handler(CancellationToken.None));
        }

        private Func<CancellationToken, Task<ManagedTimerTickResult>> GetTickHandler(IRenderedComponent<GeneralTab> target)
        {
            target.WaitForAssertion(() =>
            {
                Mock.Get(_timer).Verify(
                    timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            _tickHandler.Should().NotBeNull();
            return _tickHandler!;
        }

        private static TorrentProperties CreateProperties()
        {
            return new TorrentProperties(
                additionDate: 1,
                comment: "Comment",
                completionDate: 2,
                createdBy: "CreatedBy",
                creationDate: 3,
                downloadLimit: 4,
                downloadSpeed: 5,
                downloadSpeedAverage: 6,
                estimatedTimeOfArrival: 7,
                lastSeen: 8,
                connections: 9,
                connectionsLimit: 10,
                peers: 11,
                peersTotal: 12,
                pieceSize: 13,
                piecesHave: 14,
                piecesNum: 15,
                reannounce: 16,
                savePath: "SavePath",
                seedingTime: 17,
                seeds: 18,
                seedsTotal: 19,
                shareRatio: 20,
                timeElapsed: 21,
                totalDownloaded: 22,
                totalDownloadedSession: 23,
                totalSize: 24,
                totalUploaded: 25,
                totalUploadedSession: 26,
                totalWasted: 27,
                uploadLimit: 28,
                uploadSpeed: 29,
                uploadSpeedAverage: 30,
                infoHashV1: "InfoHashV1",
                infoHashV2: "InfoHashV2");
        }
    }
}
