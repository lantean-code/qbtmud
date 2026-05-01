using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Core.Models;
using Moq;
using QBittorrent.ApiClient.Models;
using AppSettingsModel = Lantean.QBTMud.Core.Models.AppSettings;
using MudTorrent = Lantean.QBTMud.Core.Models.Torrent;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class TorrentCompletionNotificationServiceTests
    {
        private readonly IBrowserNotificationService _browserNotificationService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ILanguageLocalizer _languageLocalizer;
        private readonly TorrentCompletionNotificationService _target;

        public TorrentCompletionNotificationServiceTests()
        {
            _browserNotificationService = Mock.Of<IBrowserNotificationService>();
            _appSettingsService = Mock.Of<IAppSettingsService>();
            _languageLocalizer = Mock.Of<ILanguageLocalizer>();
            _target = new TorrentCompletionNotificationService(_browserNotificationService, _appSettingsService, _languageLocalizer);

            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateSettings());
            Mock.Get(_browserNotificationService)
                .Setup(service => service.IsSupportedAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Granted);
            Mock.Get(_browserNotificationService)
                .Setup(service => service.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Mock.Get(_languageLocalizer)
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string context, string source, object[] arguments) => $"{context}|{source}|{string.Join("|", arguments.Select(argument => argument?.ToString()))}");
        }

        [Fact]
        public async Task GIVEN_ServiceNotInitialized_WHEN_ProcessAsync_THEN_ShouldInitializeWithoutShowingNotifications()
        {
            var torrents = new Dictionary<string, MudTorrent>
            {
                ["Hash"] = CreateTorrent("Hash", "Name", false)
            };

            await _target.ProcessAsync(torrents, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_appSettingsService).Verify(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()), Times.Never);
            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NotificationsDisabled_WHEN_ProcessAsync_THEN_ShouldUpdateSnapshotWithoutShowingNotifications()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateSettings(notificationsEnabled: false));

            await _target.InitializeAsync(new Dictionary<string, MudTorrent>
            {
                ["Hash"] = CreateTorrent("Hash", "Name", false)
            }, Xunit.TestContext.Current.CancellationToken);

            await _target.ProcessAsync(new Dictionary<string, MudTorrent>
            {
                ["Hash"] = CreateTorrent("Hash", "Name", true)
            }, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_browserNotificationService).Verify(service => service.IsSupportedAsync(It.IsAny<CancellationToken>()), Times.Never);
            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_BrowserNotificationsUnsupported_WHEN_ProcessAsync_THEN_ShouldNotShowNotifications()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.IsSupportedAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            await _target.InitializeAsync(new Dictionary<string, MudTorrent>
            {
                ["Hash"] = CreateTorrent("Hash", "Name", false)
            }, Xunit.TestContext.Current.CancellationToken);

            await _target.ProcessAsync(new Dictionary<string, MudTorrent>
            {
                ["Hash"] = CreateTorrent("Hash", "Name", true)
            }, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_browserNotificationService).Verify(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()), Times.Never);
            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_BrowserNotificationPermissionDenied_WHEN_ProcessTransitionsAsync_THEN_ShouldNotShowNotifications()
        {
            Mock.Get(_browserNotificationService)
                .Setup(service => service.GetPermissionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(BrowserNotificationPermission.Denied);

            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", false, false, true)
                ],
                Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NewTorrentWithBlankName_WHEN_ProcessAsync_THEN_ShouldShowAddedNotificationUsingHash()
        {
            await _target.InitializeAsync(new Dictionary<string, MudTorrent>(), Xunit.TestContext.Current.CancellationToken);

            await _target.ProcessAsync(new Dictionary<string, MudTorrent>
            {
                ["Hash"] = CreateTorrent("Hash", " ", false)
            }, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(
                    "AppNotifications|Torrent added|",
                    "AppNotifications|'%1' was added.|Hash",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NewTorrentNotificationsDisabled_WHEN_ProcessAsync_THEN_ShouldNotShowAddedNotification()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateSettings(torrentAddedNotificationsEnabled: false));

            await _target.InitializeAsync(new Dictionary<string, MudTorrent>(), Xunit.TestContext.Current.CancellationToken);

            await _target.ProcessAsync(new Dictionary<string, MudTorrent>
            {
                ["Hash"] = CreateTorrent("Hash", "Name", false)
            }, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_TorrentFinishesDownloading_WHEN_ProcessAsync_THEN_ShouldShowFinishedNotification()
        {
            await _target.InitializeAsync(new Dictionary<string, MudTorrent>
            {
                ["Hash"] = CreateTorrent("Hash", "Name", false)
            }, Xunit.TestContext.Current.CancellationToken);

            await _target.ProcessAsync(new Dictionary<string, MudTorrent>
            {
                ["Hash"] = CreateTorrent("Hash", "Name", true)
            }, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(
                    "AppNotifications|Download completed|",
                    "AppNotifications|'%1' has finished downloading.|Name",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_TorrentDoesNotFinishDownloading_WHEN_ProcessAsync_THEN_ShouldNotShowFinishedNotification()
        {
            await _target.InitializeAsync(new Dictionary<string, MudTorrent>
            {
                ["Hash"] = CreateTorrent("Hash", "Name", false)
            }, Xunit.TestContext.Current.CancellationToken);

            await _target.ProcessAsync(new Dictionary<string, MudTorrent>
            {
                ["Hash"] = CreateTorrent("Hash", "Name", false)
            }, Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NoTransitions_WHEN_ProcessTransitionsAsync_THEN_ShouldReturnWithoutLoadingSettings()
        {
            await _target.ProcessTransitionsAsync([], Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_appSettingsService).Verify(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AddedTransitionAndNotificationsEnabled_WHEN_ProcessTransitionsAsync_THEN_ShouldShowAddedNotification()
        {
            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", true, false, false)
                ],
                Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(
                    "AppNotifications|Torrent added|",
                    "AppNotifications|'%1' was added.|Name",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_AddedTransitionAndAddedNotificationsDisabled_WHEN_ProcessTransitionsAsync_THEN_ShouldNotShowAddedNotification()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateSettings(torrentAddedNotificationsEnabled: false));

            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", true, false, false)
                ],
                Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_FinishedTransitionAndDownloadFinishedNotificationsEnabled_WHEN_ProcessTransitionsAsync_THEN_ShouldShowFinishedNotification()
        {
            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", false, false, true)
                ],
                Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(
                    "AppNotifications|Download completed|",
                    "AppNotifications|'%1' has finished downloading.|Name",
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_FinishedTransitionAndDownloadFinishedNotificationsDisabled_WHEN_ProcessTransitionsAsync_THEN_ShouldNotShowFinishedNotification()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateSettings(downloadFinishedNotificationsEnabled: false));

            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", false, false, true)
                ],
                Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NonFinishingTransition_WHEN_ProcessTransitionsAsync_THEN_ShouldNotShowFinishedNotification()
        {
            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", false, true, true)
                ],
                Xunit.TestContext.Current.CancellationToken);

            Mock.Get(_browserNotificationService).Verify(
                service => service.ShowNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static AppSettingsModel CreateSettings(
            bool notificationsEnabled = true,
            bool downloadFinishedNotificationsEnabled = true,
            bool torrentAddedNotificationsEnabled = true)
        {
            return new AppSettingsModel
            {
                NotificationsEnabled = notificationsEnabled,
                DownloadFinishedNotificationsEnabled = downloadFinishedNotificationsEnabled,
                TorrentAddedNotificationsEnabled = torrentAddedNotificationsEnabled
            };
        }

        private static MudTorrent CreateTorrent(string hash, string name, bool isFinished)
        {
            var downloaded = isFinished ? 100L : 50L;

            return new MudTorrent(
                hash,
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                availability: 1,
                category: string.Empty,
                completed: 0,
                completionOn: 0,
                contentPath: string.Empty,
                downloadLimit: 0,
                downloadSpeed: 0,
                downloaded,
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
                name,
                numberComplete: 0,
                numberIncomplete: 0,
                numberLeeches: 0,
                numberSeeds: 0,
                priority: 0,
                progress: isFinished ? 1 : 0.5f,
                ratio: 0,
                ratioLimit: 0,
                savePath: string.Empty,
                seedingTime: 0,
                seedingTimeLimit: 0,
                seenComplete: 0,
                sequentialDownload: false,
                size: 100,
                state: isFinished ? TorrentState.Uploading : TorrentState.Downloading,
                superSeeding: false,
                tags: Array.Empty<string>(),
                timeActive: 0,
                totalSize: 100,
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
                ShareLimitAction.Default,
                comment: string.Empty);
        }
    }
}
