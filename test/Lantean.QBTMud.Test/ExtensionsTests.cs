using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using System.ComponentModel;
using ContentPriority = Lantean.QBTMud.Models.Priority;
using MudTorrent = Lantean.QBTMud.Models.Torrent;

namespace Lantean.QBTMud.Test
{
    public sealed class ExtensionsTests
    {
        [Fact]
        public void GIVEN_PathWithDirectoryAndFile_WHEN_GetDirectoryPath_THEN_ShouldReturnDirectoryPortion()
        {
            var result = "folder/subfolder/file.txt".GetDirectoryPath();

            result.Should().Be("folder/subfolder");
        }

        [Fact]
        public void GIVEN_ContentItemNameWithDirectoryAndFile_WHEN_GetDirectoryPath_THEN_ShouldReturnDirectoryPortion()
        {
            var contentItem = CreateContentItem("folder/subfolder/file.txt");

            var result = contentItem.GetDirectoryPath();

            result.Should().Be("folder/subfolder");
        }

        [Fact]
        public void GIVEN_PathWithDirectoryAndFile_WHEN_GetFileName_THEN_ShouldReturnFilePortion()
        {
            var result = "folder/subfolder/file.txt".GetFileName();

            result.Should().Be("file.txt");
        }

        [Fact]
        public void GIVEN_ContentItemNameWithDirectoryAndFile_WHEN_GetFileName_THEN_ShouldReturnFilePortion()
        {
            var contentItem = CreateContentItem("folder/subfolder/file.txt");

            var result = contentItem.GetFileName();

            result.Should().Be("file.txt");
        }

        [Fact]
        public void GIVEN_PathWithMultipleSegments_WHEN_GetDescendantsKeyWithDefaultLevel_THEN_ShouldReturnAllButLastWithSeparator()
        {
            var result = "folder/subfolder/file.txt".GetDescendantsKey();

            result.Should().Be("folder/subfolder/");
        }

        [Fact]
        public void GIVEN_PathWithMultipleSegments_WHEN_GetDescendantsKeyWithSpecificLevel_THEN_ShouldReturnRequestedDepthWithSeparator()
        {
            var result = "folder/subfolder/file.txt".GetDescendantsKey(level: 1);

            result.Should().Be("folder/");
        }

        [Fact]
        public void GIVEN_ContentItemNameWithMultipleSegments_WHEN_GetDescendantsKeyWithSpecificLevel_THEN_ShouldReturnRequestedDepthWithSeparator()
        {
            var contentItem = CreateContentItem("folder/subfolder/file.txt");

            var result = contentItem.GetDescendantsKey(level: 2);

            result.Should().Be("folder/subfolder/");
        }

        [Fact]
        public void GIVEN_NotDisposedCancellationTokenSource_WHEN_CancelIfNotDisposed_THEN_ShouldCancelSource()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            cancellationTokenSource.CancelIfNotDisposed();

            cancellationTokenSource.IsCancellationRequested.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_DisposedCancellationTokenSource_WHEN_CancelIfNotDisposed_THEN_ShouldNotThrow()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Dispose();

            var action = () => cancellationTokenSource.CancelIfNotDisposed();

            action.Should().NotThrow();
        }

        [Fact]
        public void GIVEN_TorrentWithEqualTotalSizeAndDownloaded_WHEN_IsFinished_THEN_ShouldReturnTrue()
        {
            var torrent = CreateTorrent(totalSize: 100, downloaded: 100);

            var result = torrent.IsFinished();

            result.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_TorrentWithDifferentTotalSizeAndDownloaded_WHEN_IsFinished_THEN_ShouldReturnFalse()
        {
            var torrent = CreateTorrent(totalSize: 100, downloaded: 99);

            var result = torrent.IsFinished();

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NullTorrent_WHEN_MetaDownloaded_THEN_ShouldReturnFalse()
        {
            var result = Extensions.MetaDownloaded(null!);

            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("metaDL")]
        [InlineData("forcedMetaDL")]
        public void GIVEN_TorrentInMetadataDownloadState_WHEN_MetaDownloaded_THEN_ShouldReturnFalse(string state)
        {
            var torrent = CreateTorrent(state: state, totalSize: 100);

            var result = torrent.MetaDownloaded();

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_TorrentWithUnknownTotalSize_WHEN_MetaDownloaded_THEN_ShouldReturnFalse()
        {
            var torrent = CreateTorrent(state: "downloading", totalSize: -1);

            var result = torrent.MetaDownloaded();

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_TorrentWithKnownTotalSizeAndNormalState_WHEN_MetaDownloaded_THEN_ShouldReturnTrue()
        {
            var torrent = CreateTorrent(state: "downloading", totalSize: 100);

            var result = torrent.MetaDownloaded();

            result.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_EnumValueWithDescriptionAttribute_WHEN_GetDescriptionAttributeOrDefault_THEN_ShouldReturnAttributeDescription()
        {
            var result = DescribedEnum.ValueWithDescription.GetDescriptionAttributeOrDefault();

            result.Should().Be("Description");
        }

        [Fact]
        public void GIVEN_EnumValueWithoutDescriptionAttribute_WHEN_GetDescriptionAttributeOrDefault_THEN_ShouldReturnValueName()
        {
            var result = DescribedEnum.ValueWithoutDescription.GetDescriptionAttributeOrDefault();

            result.Should().Be("ValueWithoutDescription");
        }

        [Fact]
        public void GIVEN_UndefinedEnumValue_WHEN_GetDescriptionAttributeOrDefault_THEN_ShouldReturnNumericValueAsString()
        {
            var result = ((DescribedEnum)999).GetDescriptionAttributeOrDefault();

            result.Should().Be("999");
        }

        [Fact]
        public void GIVEN_NavigationManager_WHEN_NavigateToHomeWithDefaultArguments_THEN_ShouldNavigateToRootWithoutForceLoad()
        {
            var navigationManager = new TestNavigationManager();

            navigationManager.NavigateToHome();

            navigationManager.Uri.Should().Be("http://localhost/");
            navigationManager.LastForceLoad.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_NavigationManager_WHEN_NavigateToHomeWithForceLoadTrue_THEN_ShouldNavigateToRootWithForceLoad()
        {
            var navigationManager = new TestNavigationManager();

            navigationManager.NavigateToHome(forceLoad: true);

            navigationManager.Uri.Should().Be("http://localhost/");
            navigationManager.LastForceLoad.Should().BeTrue();
        }

        private static ContentItem CreateContentItem(string name)
        {
            return new ContentItem(
                name: name,
                displayName: "DisplayName",
                index: 0,
                priority: ContentPriority.Normal,
                progress: 0.5f,
                size: 100,
                availability: 1.0f);
        }

        private static MudTorrent CreateTorrent(string state = "downloading", long totalSize = 0, long downloaded = 0)
        {
            return new MudTorrent(
                hash: "Hash",
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                aavailability: 1,
                category: string.Empty,
                completed: 0,
                completionOn: 0,
                contentPath: string.Empty,
                downloadLimit: 0,
                downloadSpeed: 0,
                downloaded: downloaded,
                downloadedSession: 0,
                estimatedTimeOfArrival: 0,
                firstLastPiecePriority: false,
                forceStart: false,
                infoHashV1: string.Empty,
                infoHashV2: string.Empty,
                lastActivity: 0,
                magnetUri: string.Empty,
                maxRatio: 0,
                maxSeedingTime: 0,
                name: "Name",
                numberComplete: 0,
                numberIncomplete: 0,
                numberLeeches: 0,
                numberSeeds: 0,
                priority: 0,
                progress: 0,
                ratio: 0,
                ratioLimit: 0,
                savePath: string.Empty,
                seedingTime: 0,
                seedingTimeLimit: 0,
                seenComplete: 0,
                sequentialDownload: false,
                size: 0,
                state: state,
                superSeeding: false,
                tags: Array.Empty<string>(),
                timeActive: 0,
                totalSize: totalSize,
                tracker: string.Empty,
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
                downloadPath: string.Empty,
                rootPath: string.Empty,
                isPrivate: false,
                Lantean.QBitTorrentClient.Models.ShareLimitAction.Default,
                comment: string.Empty);
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/test");
            }

            public bool LastForceLoad { get; private set; }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                LastForceLoad = forceLoad;
                Uri = ToAbsoluteUri(uri).ToString();
                NotifyLocationChanged(false);
            }
        }

        private enum DescribedEnum
        {
            [Description("Description")]
            ValueWithDescription,
            ValueWithoutDescription,
        }
    }
}
