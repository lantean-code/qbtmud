using AwesomeAssertions;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class TorrentCompletionNotificationServiceTests
    {
        private readonly Mock<IJSRuntime> _jsRuntime;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ILanguageLocalizer _languageLocalizer;
        private readonly TorrentCompletionNotificationService _target;

        public TorrentCompletionNotificationServiceTests()
        {
            _jsRuntime = new Mock<IJSRuntime>(MockBehavior.Loose);
            _appSettingsService = Mock.Of<IAppSettingsService>();
            _languageLocalizer = Mock.Of<ILanguageLocalizer>();

            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<bool>("qbt.isNotificationSupported", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .ReturnsAsync(true);
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .ReturnsAsync("granted");
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.requestNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .ReturnsAsync("granted");

            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false
                });

            Mock.Get(_languageLocalizer)
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] arguments) =>
                {
                    var result = source;
                    for (var i = 0; i < arguments.Length; i++)
                    {
                        result = result.Replace($"%{i + 1}", arguments[i]?.ToString(), StringComparison.Ordinal);
                    }

                    return result;
                });

            _target = new TorrentCompletionNotificationService(_jsRuntime.Object, _appSettingsService, _languageLocalizer);
        }

        [Fact]
        public async Task GIVEN_FinishedTransitionBatch_WHEN_Processed_THEN_ShowsDesktopCompletionNotification()
        {
            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", false, false, true)
                ],
                TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.Is<object?[]?>(arguments => HasNotificationPayload(arguments, "Download completed", "'Name' has finished downloading."))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_AddedTransitionAndOptionEnabled_WHEN_Processed_THEN_ShowsDesktopAddedNotification()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = true
                });

            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", true, false, false)
                ],
                TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.Is<object?[]?>(arguments => HasNotificationPayload(arguments, "Torrent added", "'Name' was added."))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_AddedTransitionAndOptionDisabled_WHEN_Processed_THEN_DoesNotShowNotification()
        {
            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", true, false, false)
                ],
                TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_FinishedTransitionAlreadyFinished_WHEN_Processed_THEN_DoesNotShowNotification()
        {
            await _target.ProcessTransitionsAsync(
            [
                new TorrentTransition("Hash", "Name", false, true, true)
                ],
                TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_DownloadCompletedTypeDisabled_WHEN_FinishedTransitionProcessed_THEN_DoesNotShowNotification()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = false,
                    TorrentAddedNotificationsEnabled = true
                });

            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", false, false, true)
                ],
                TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NotificationsDisabledOrPermissionDenied_WHEN_Processed_THEN_DoesNotShowNotification()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = false,
                    TorrentAddedNotificationsEnabled = true
                });

            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", false, false, true)
                ],
                TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);

            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = true
                });
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<string?>("qbt.getNotificationPermission", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .ReturnsAsync("denied");

            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", false, false, true)
                ],
                TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NewTorrentBetweenSnapshots_WHEN_AddedOptionEnabled_THEN_ShowsAddedNotification()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = true
                });

            await _target.InitializeAsync(new Dictionary<string, Torrent>(), TestContext.Current.CancellationToken);
            await _target.ProcessAsync(CreateSnapshot(state: "downloading", downloaded: 10, totalSize: 100), TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.Is<object?[]?>(arguments => HasNotificationPayload(arguments, "Torrent added", "'Name' was added."))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_DownloadFinishedNotificationsDisabled_WHEN_ProcessAsyncDetectsFinishedTransition_THEN_DoesNotShowNotification()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    DownloadFinishedNotificationsEnabled = false,
                    TorrentAddedNotificationsEnabled = false
                });

            await _target.InitializeAsync(CreateSnapshot(state: "downloading", downloaded: 10, totalSize: 100), TestContext.Current.CancellationToken);
            await _target.ProcessAsync(CreateSnapshot(state: "downloading", downloaded: 100, totalSize: 100), TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_FinishedTransitionAlreadyProcessed_WHEN_ProcessInvokedAgain_THEN_DoesNotNotifyTwice()
        {
            await _target.InitializeAsync(CreateSnapshot(state: "downloading", downloaded: 10, totalSize: 100), TestContext.Current.CancellationToken);
            await _target.ProcessAsync(CreateSnapshot(state: "downloading", downloaded: 100, totalSize: 100), TestContext.Current.CancellationToken);
            await _target.ProcessAsync(CreateSnapshot(state: "downloading", downloaded: 100, totalSize: 100), TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RequestPermissionInvoked_WHEN_RequestPermissionAsyncCalled_THEN_ReturnsGranted()
        {
            var permission = await _target.RequestPermissionAsync(TestContext.Current.CancellationToken);

            permission.Should().Be(BrowserNotificationPermission.Granted);
            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<string?>(
                    "qbt.requestNotificationPermission",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_UnsupportedNotifications_WHEN_TransitionsProcessed_THEN_DoesNotRequestPermissionOrShow()
        {
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<bool>("qbt.isNotificationSupported", It.IsAny<CancellationToken>(), It.IsAny<object?[]?>()))
                .ReturnsAsync(false);

            await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", false, false, true)
                ],
                TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<string?>(
                    "qbt.getNotificationPermission",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_ProcessInvokedBeforeInitialize_WHEN_FirstProcessRuns_THEN_InitializesWithoutNotifications()
        {
            await _target.ProcessAsync(CreateSnapshot(state: "downloading", downloaded: 100, totalSize: 100), TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_SnapshotLookupMissDuringProcessing_WHEN_ProcessAsyncRuns_THEN_SkipsNotificationAndDoesNotThrow()
        {
            var initialTorrent = CreateSnapshot(state: "downloading", downloaded: 10, totalSize: 100).Values.Single();
            await _target.InitializeAsync(
                new Dictionary<string, Torrent>
                {
                    ["TargetHash"] = initialTorrent
                },
                TestContext.Current.CancellationToken);

            var currentTorrent = CreateSnapshot(state: "downloading", downloaded: 100, totalSize: 100).Values.Single();
            var changingDictionary = new Mock<IReadOnlyDictionary<string, Torrent>>(MockBehavior.Strict);
            changingDictionary
                .SetupSequence(dictionary => dictionary.GetEnumerator())
                .Returns(new Dictionary<string, Torrent>
                {
                    ["OtherHash"] = currentTorrent
                }.GetEnumerator())
                .Returns(new Dictionary<string, Torrent>
                {
                    ["TargetHash"] = currentTorrent
                }.GetEnumerator());

            var act = async () => await _target.ProcessAsync(changingDictionary.Object, TestContext.Current.CancellationToken);

            await act.Should().NotThrowAsync();
            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_NewTorrentWithWhitespaceName_WHEN_AddedNotificationShown_THEN_UsesHashAsDisplayName()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = true
                });

            await _target.InitializeAsync(new Dictionary<string, Torrent>(), TestContext.Current.CancellationToken);
            await _target.ProcessAsync(CreateSnapshot(state: "downloading", downloaded: 10, totalSize: 100, name: "  "), TestContext.Current.CancellationToken);

            _jsRuntime.Verify(
                runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.Is<object?[]?>(arguments => HasNotificationPayload(arguments, "Torrent added", "'Hash' was added."))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ShowNotificationThrowsJsException_WHEN_TransitionsProcessed_THEN_DoesNotThrow()
        {
            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ThrowsAsync(new JSException("boom"));

            var act = async () => await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", false, false, true),
                    new TorrentTransition("Hash2", "Name2", true, false, false)
                ],
                TestContext.Current.CancellationToken);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GIVEN_AddedNotificationThrowsJsException_WHEN_AddedTransitionProcessed_THEN_DoesNotThrow()
        {
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AppSettings
                {
                    NotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = true
                });

            _jsRuntime
                .Setup(runtime => runtime.InvokeAsync<IJSVoidResult>(
                    "qbt.showNotification",
                    It.IsAny<CancellationToken>(),
                    It.IsAny<object?[]?>()))
                .ThrowsAsync(new JSException("boom"));

            var act = async () => await _target.ProcessTransitionsAsync(
                [
                    new TorrentTransition("Hash", "Name", true, false, false)
                ],
                TestContext.Current.CancellationToken);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task GIVEN_NoTransitions_WHEN_ProcessTransitionsInvoked_THEN_DoesNotResolveSettings()
        {
            await _target.ProcessTransitionsAsync(Array.Empty<TorrentTransition>(), TestContext.Current.CancellationToken);

            Mock.Get(_appSettingsService).Verify(
                service => service.GetSettingsAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static IReadOnlyDictionary<string, Torrent> CreateSnapshot(string state, long downloaded, long totalSize, string name = "Name")
        {
            var torrent = new Torrent(
                "Hash",
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
                downloaded: downloaded,
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
                progress: 0,
                ratio: 0,
                ratioLimit: 0,
                savePath: "SavePath",
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
                shareLimitAction: Lantean.QBitTorrentClient.Models.ShareLimitAction.Default,
                comment: "Comment");

            return new Dictionary<string, Torrent>
            {
                [torrent.Hash] = torrent
            };
        }

        private static bool HasNotificationPayload(object?[]? arguments, string title, string body)
        {
            return (arguments is [string actualTitle, string actualBody])
                   && string.Equals(actualTitle, title, StringComparison.Ordinal)
                   && string.Equals(actualBody, body, StringComparison.Ordinal);
        }
    }
}
