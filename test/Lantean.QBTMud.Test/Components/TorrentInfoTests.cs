using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using ClientModels = Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class TorrentInfoTests : RazorComponentTestBase<TorrentInfo>
    {
        private readonly Mock<ILanguageLocalizer> _languageLocalizerMock;

        public TorrentInfoTests()
        {
            _languageLocalizerMock = new Mock<ILanguageLocalizer>();
            _languageLocalizerMock
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] __) => source);

            TestContext.Services.RemoveAll<ILanguageLocalizer>();
            TestContext.Services.AddSingleton(_languageLocalizerMock.Object);
        }

        [Fact]
        public void GIVEN_NullHash_WHEN_Rendered_THEN_ShouldNotRenderToolbar()
        {
            var mainData = CreateMainData(CreateTorrent("Hash", "Name", 0.5f, 1024, "downloading"));

            var target = TestContext.Render<TorrentInfo>(parameters =>
            {
                parameters.Add(parameter => parameter.Hash, (string)null!);
                parameters.AddCascadingValue(mainData);
            });

            target.FindComponents<MudToolBar>().Should().BeEmpty();
            _languageLocalizerMock.Verify(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }

        [Fact]
        public void GIVEN_UnknownHash_WHEN_Rendered_THEN_ShouldNotRenderToolbar()
        {
            var mainData = CreateMainData(CreateTorrent("Hash", "Name", 0.5f, 1024, "downloading"));

            var target = TestContext.Render<TorrentInfo>(parameters =>
            {
                parameters.Add(parameter => parameter.Hash, "MissingHash");
                parameters.AddCascadingValue(mainData);
            });

            target.FindComponents<MudToolBar>().Should().BeEmpty();
            _languageLocalizerMock.Verify(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }

        [Fact]
        public void GIVEN_ExistingHashAndIncompleteProgress_WHEN_Rendered_THEN_ShouldRenderProgressWithSuccessColor()
        {
            var torrent = CreateTorrent("Hash", "Name", 0.5f, 1024, "downloading");
            var mainData = CreateMainData(torrent);

            var target = TestContext.Render<TorrentInfo>(parameters =>
            {
                parameters.Add(parameter => parameter.Hash, "Hash");
                parameters.AddCascadingValue(mainData);
            });

            target.FindComponents<MudToolBar>().Should().ContainSingle();

            var textValues = target.FindComponents<MudText>()
                .Select(component => GetChildContentText(component.Instance.ChildContent))
                .OfType<string>()
                .ToList();

            textValues.Should().Contain("Name");
            textValues.Should().Contain(DisplayHelpers.Size(1024));

            var progress = target.FindComponents<MudProgressLinear>().Should().ContainSingle().Subject;
            progress.Instance.Color.Should().Be(Color.Success);
            progress.Instance.GetState(x => x.Value).Should().Be(50);
            GetChildContentText(progress.Instance.ChildContent).Should().Be(DisplayHelpers.Percentage(0.5f));

            _languageLocalizerMock.Verify(localizer => localizer.Translate("TransferListModel", "Progress", It.IsAny<object[]>()), Times.Once);
        }

        [Fact]
        public void GIVEN_ExistingHashAndCompletedProgress_WHEN_Rendered_THEN_ShouldRenderProgressWithInfoColor()
        {
            var torrent = CreateTorrent("Hash", "Name", 1f, 2048, "uploading");
            var mainData = CreateMainData(torrent);

            var target = TestContext.Render<TorrentInfo>(parameters =>
            {
                parameters.Add(parameter => parameter.Hash, "Hash");
                parameters.AddCascadingValue(mainData);
            });

            var progress = target.FindComponents<MudProgressLinear>().Should().ContainSingle().Subject;
            progress.Instance.Color.Should().Be(Color.Info);
            progress.Instance.GetState(x => x.Value).Should().Be(100);
            GetChildContentText(progress.Instance.ChildContent).Should().Be(DisplayHelpers.Percentage(1f));

            _languageLocalizerMock.Verify(localizer => localizer.Translate("TransferListModel", "Progress", It.IsAny<object[]>()), Times.Once);
        }

        private static MainData CreateMainData(Torrent torrent)
        {
            return new MainData(
                new Dictionary<string, Torrent> { [torrent.Hash] = torrent },
                Array.Empty<string>(),
                new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new ServerState(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());
        }

        private static Torrent CreateTorrent(string hash, string name, float progress, long size, string state)
        {
            return new Torrent(
                hash,
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                aavailability: 0,
                category: "Category",
                completed: 0,
                completionOn: 0,
                contentPath: "ContentPath",
                downloadLimit: 0,
                downloadSpeed: 0,
                downloaded: 0,
                downloadedSession: 0,
                estimatedTimeOfArrival: 0,
                firstLastPiecePriority: false,
                forceStart: false,
                infoHashV1: "InfoHashV1",
                infoHashV2: "InfoHashV2",
                lastActivity: 0,
                magnetUri: "MagnetUri",
                maxRatio: 0,
                maxSeedingTime: 0,
                name: name,
                numberComplete: 0,
                numberIncomplete: 0,
                numberLeeches: 0,
                numberSeeds: 0,
                priority: 0,
                progress: progress,
                ratio: 0,
                ratioLimit: 0,
                savePath: "SavePath",
                seedingTime: 0,
                seedingTimeLimit: 0,
                seenComplete: 0,
                sequentialDownload: false,
                size: size,
                state: state,
                superSeeding: false,
                tags: Array.Empty<string>(),
                timeActive: 0,
                totalSize: 0,
                tracker: "Tracker",
                trackersCount: 0,
                hasTrackerError: false,
                hasTrackerWarning: false,
                hasOtherAnnounceError: false,
                uploadLimit: 0,
                uploaded: 0,
                uploadedSession: 0,
                uploadSpeed: 0,
                reannounce: 0,
                inactiveSeedingTimeLimit: 0,
                maxInactiveSeedingTime: 0,
                popularity: 0,
                downloadPath: "DownloadPath",
                rootPath: "RootPath",
                isPrivate: false,
                shareLimitAction: ClientModels.ShareLimitAction.Default,
                comment: "Comment");
        }
    }
}
