using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Net;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class GeneralTabTests : RazorComponentTestBase<GeneralTab>
    {
        private readonly Mock<IApiClient> _apiClientMock;
        private readonly FakePeriodicTimer _timer;

        public GeneralTabTests()
        {
            _apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);

            _timer = new FakePeriodicTimer();
            TestContext.Services.RemoveAll(typeof(IPeriodicTimerFactory));
            TestContext.Services.AddSingleton<IPeriodicTimerFactory>(new FakePeriodicTimerFactory(_timer));
        }

        [Fact]
        public async Task GIVEN_InactiveTab_WHEN_TimerTicks_THEN_DoesNotRender()
        {
            var target = RenderGeneralTab(false, "Hash");
            var initialRenderCount = target.RenderCount;

            await _timer.TriggerTickAsync();
            await target.InvokeAsync(() => Task.CompletedTask);

            target.RenderCount.Should().Be(initialRenderCount);
        }

        [Fact]
        public void GIVEN_NullHash_WHEN_Rendered_THEN_DoesNotCallApi()
        {
            RenderGeneralTab(true, null);

            _apiClientMock.Verify(c => c.GetTorrentProperties(It.IsAny<string>()), Times.Never);
            _apiClientMock.Verify(c => c.GetTorrentPieceStates(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_ActiveTab_WHEN_PropertiesNotFound_THEN_ShowsPiecesUnavailable()
        {
            _apiClientMock
                .Setup(c => c.GetTorrentProperties("Hash"))
                .ThrowsAsync(new HttpRequestException("Message", null, HttpStatusCode.NotFound));

            var target = RenderGeneralTab(true, "Hash");

            target.WaitForAssertion(() => target.Markup.Should().Contain("Pieces data unavailable"));
        }

        [Fact]
        public void GIVEN_ActiveTab_WHEN_PropertiesRequestFails_THEN_ShowsPiecesUnavailable()
        {
            _apiClientMock
                .Setup(c => c.GetTorrentProperties("Hash"))
                .ThrowsAsync(new HttpRequestException("Message"));

            var target = RenderGeneralTab(true, "Hash");

            target.WaitForAssertion(() => target.Markup.Should().Contain("Pieces data unavailable"));
        }

        [Fact]
        public void GIVEN_ActiveTab_WHEN_PiecesNotFound_THEN_ShowsPiecesUnavailable()
        {
            _apiClientMock
                .Setup(c => c.GetTorrentProperties("Hash"))
                .ReturnsAsync(CreateProperties());
            _apiClientMock
                .Setup(c => c.GetTorrentPieceStates("Hash"))
                .ThrowsAsync(new HttpRequestException("Message", null, HttpStatusCode.NotFound));

            var target = RenderGeneralTab(true, "Hash");

            target.WaitForAssertion(() => target.Markup.Should().Contain("Pieces data unavailable"));
        }

        [Fact]
        public void GIVEN_ActiveTab_WHEN_PiecesLoaded_THEN_ShowsPiecesProgressSummary()
        {
            _apiClientMock
                .Setup(c => c.GetTorrentProperties("Hash"))
                .ReturnsAsync(CreateProperties());
            _apiClientMock
                .Setup(c => c.GetTorrentPieceStates("Hash"))
                .ReturnsAsync(new[] { PieceState.Downloaded, PieceState.Downloading });

            var target = RenderGeneralTab(true, "Hash");

            target.WaitForAssertion(() => target.Markup.Should().Contain("downloaded, 1 in progress"));
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTicksAndPropertiesNotFound_THEN_ShowsPiecesUnavailable()
        {
            _apiClientMock
                .SetupSequence(c => c.GetTorrentProperties("Hash"))
                .ReturnsAsync(CreateProperties())
                .ThrowsAsync(new HttpRequestException("Message", null, HttpStatusCode.NotFound));
            _apiClientMock
                .Setup(c => c.GetTorrentPieceStates("Hash"))
                .ReturnsAsync(new[] { PieceState.Downloaded });

            var target = RenderGeneralTab(true, "Hash");
            await target.InvokeAsync(async () => await _timer.TriggerTickAsync());

            target.WaitForAssertion(() => target.Markup.Should().Contain("Pieces data unavailable"));
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTicksAndPropertiesForbidden_THEN_ShowsPiecesUnavailable()
        {
            _apiClientMock
                .SetupSequence(c => c.GetTorrentProperties("Hash"))
                .ReturnsAsync(CreateProperties())
                .ThrowsAsync(new HttpRequestException("Message", null, HttpStatusCode.Forbidden));
            _apiClientMock
                .Setup(c => c.GetTorrentPieceStates("Hash"))
                .ReturnsAsync(new[] { PieceState.Downloaded });

            var target = RenderGeneralTab(true, "Hash");
            await target.InvokeAsync(async () => await _timer.TriggerTickAsync());

            target.WaitForAssertion(() => target.Markup.Should().Contain("Pieces data unavailable"));
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTicksAndPiecesNotFound_THEN_ShowsPiecesUnavailable()
        {
            _apiClientMock
                .SetupSequence(c => c.GetTorrentProperties("Hash"))
                .ReturnsAsync(CreateProperties())
                .ReturnsAsync(CreateProperties());
            _apiClientMock
                .SetupSequence(c => c.GetTorrentPieceStates("Hash"))
                .ReturnsAsync(new[] { PieceState.Downloaded })
                .ThrowsAsync(new HttpRequestException("Message", null, HttpStatusCode.NotFound));

            var target = RenderGeneralTab(true, "Hash");
            await target.InvokeAsync(async () => await _timer.TriggerTickAsync());

            target.WaitForAssertion(() => target.Markup.Should().Contain("Pieces data unavailable"));
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTicksAndPiecesLoaded_THEN_ShowsUpdatedProgressSummary()
        {
            _apiClientMock
                .SetupSequence(c => c.GetTorrentProperties("Hash"))
                .ReturnsAsync(CreateProperties())
                .ReturnsAsync(CreateProperties());
            _apiClientMock
                .SetupSequence(c => c.GetTorrentPieceStates("Hash"))
                .ReturnsAsync(new[] { PieceState.Downloading })
                .ReturnsAsync(new[] { PieceState.Downloaded, PieceState.Downloaded });

            var target = RenderGeneralTab(true, "Hash");
            await target.InvokeAsync(async () => await _timer.TriggerTickAsync());

            target.WaitForAssertion(() => target.Markup.Should().Contain("2 downloaded, 0 in progress"));
        }

        [Fact]
        public async Task GIVEN_RenderedComponent_WHEN_Disposed_THEN_DoesNotThrow()
        {
            var target = RenderGeneralTab(false, "Hash");

            await target.Instance.DisposeAsync();
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
