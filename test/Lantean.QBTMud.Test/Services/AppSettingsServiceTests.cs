using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Moq;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class AppSettingsServiceTests
    {
        private readonly TestLocalStorageService _localStorageService;
        private readonly AppSettingsService _target;

        public AppSettingsServiceTests()
        {
            _localStorageService = new TestLocalStorageService();
            _target = new AppSettingsService(_localStorageService);
        }

        [Fact]
        public async Task GIVEN_NoPersistedSettings_WHEN_GetSettingsInvoked_THEN_ReturnsDefaults()
        {
            var result = await _target.GetSettingsAsync(TestContext.Current.CancellationToken);

            result.UpdateChecksEnabled.Should().BeTrue();
            result.NotificationsEnabled.Should().BeFalse();
            result.DownloadFinishedNotificationsEnabled.Should().BeTrue();
            result.TorrentAddedNotificationsEnabled.Should().BeFalse();
            result.TorrentAddedSnackbarsEnabledWithNotifications.Should().BeFalse();
            result.DismissedReleaseTag.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_CustomSettings_WHEN_SaveSettingsInvoked_THEN_PersistsAndReturnsNormalizedCopy()
        {
            var settings = new AppSettings
            {
                UpdateChecksEnabled = false,
                NotificationsEnabled = true,
                DownloadFinishedNotificationsEnabled = false,
                TorrentAddedNotificationsEnabled = true,
                TorrentAddedSnackbarsEnabledWithNotifications = true,
                DismissedReleaseTag = " v1.2.3 "
            };

            var saved = await _target.SaveSettingsAsync(settings, TestContext.Current.CancellationToken);
            var reloaded = await _target.GetSettingsAsync(TestContext.Current.CancellationToken);

            saved.UpdateChecksEnabled.Should().BeFalse();
            saved.NotificationsEnabled.Should().BeTrue();
            saved.DownloadFinishedNotificationsEnabled.Should().BeFalse();
            saved.TorrentAddedNotificationsEnabled.Should().BeTrue();
            saved.TorrentAddedSnackbarsEnabledWithNotifications.Should().BeTrue();
            saved.DismissedReleaseTag.Should().Be("v1.2.3");

            reloaded.Should().BeEquivalentTo(saved);
        }

        [Fact]
        public async Task GIVEN_DismissedReleaseTag_WHEN_SaveDismissedReleaseTagInvoked_THEN_UpdatesSettings()
        {
            var result = await _target.SaveDismissedReleaseTagAsync("  v9.9.9  ", TestContext.Current.CancellationToken);

            result.DismissedReleaseTag.Should().Be("v9.9.9");
        }

        [Fact]
        public async Task GIVEN_EmptyDismissedReleaseTag_WHEN_Saved_THEN_NormalizesToNull()
        {
            var settings = new AppSettings
            {
                UpdateChecksEnabled = true,
                NotificationsEnabled = true,
                DownloadFinishedNotificationsEnabled = true,
                TorrentAddedNotificationsEnabled = true,
                TorrentAddedSnackbarsEnabledWithNotifications = true,
                DismissedReleaseTag = "   "
            };
            var saved = await _target.SaveSettingsAsync(settings, TestContext.Current.CancellationToken);

            saved.DismissedReleaseTag.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_WhitespaceDismissedReleaseTag_WHEN_SaveDismissedReleaseTagInvoked_THEN_ClearsTag()
        {
            await _target.SaveDismissedReleaseTagAsync("v1.0.0", TestContext.Current.CancellationToken);

            var result = await _target.SaveDismissedReleaseTagAsync("   ", TestContext.Current.CancellationToken);

            result.DismissedReleaseTag.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_CachedSettings_WHEN_GetSettingsInvokedTwice_THEN_ReadsStorageOnce()
        {
            var localStorageService = new Mock<ILocalStorageService>(MockBehavior.Strict);
            localStorageService
                .Setup(service => service.GetItemAsync<AppSettings>(AppSettings.StorageKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());

            var target = new AppSettingsService(localStorageService.Object);

            _ = await target.GetSettingsAsync(TestContext.Current.CancellationToken);
            _ = await target.GetSettingsAsync(TestContext.Current.CancellationToken);

            localStorageService.Verify(
                service => service.GetItemAsync<AppSettings>(AppSettings.StorageKey, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_LocalStorageJsonException_WHEN_GetSettingsInvoked_THEN_ReturnsDefaults()
        {
            var localStorageService = new Mock<ILocalStorageService>(MockBehavior.Strict);
            localStorageService
                .Setup(service => service.GetItemAsync<AppSettings>(AppSettings.StorageKey, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException("invalid"));

            var target = new AppSettingsService(localStorageService.Object);

            var result = await target.GetSettingsAsync(TestContext.Current.CancellationToken);

            result.Should().BeEquivalentTo(AppSettings.Default);
        }

        [Fact]
        public async Task GIVEN_ConcurrentInitialReads_WHEN_FirstReadInitializesCache_THEN_SecondReadReturnsFromSemaphoreCache()
        {
            var localStorageService = new Mock<ILocalStorageService>(MockBehavior.Strict);
            var readStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseRead = new TaskCompletionSource<AppSettings>(TaskCreationOptions.RunContinuationsAsynchronously);

            localStorageService
                .Setup(service => service.GetItemAsync<AppSettings>(AppSettings.StorageKey, It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    readStarted.TrySetResult();
                    return await releaseRead.Task;
                });

            var target = new AppSettingsService(localStorageService.Object);
            var firstReadTask = target.GetSettingsAsync(TestContext.Current.CancellationToken);
            await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            var secondReadTask = target.GetSettingsAsync(TestContext.Current.CancellationToken);

            releaseRead.TrySetResult(new AppSettings
            {
                UpdateChecksEnabled = false,
                NotificationsEnabled = true
            });

            var first = await firstReadTask;
            var second = await secondReadTask;

            first.NotificationsEnabled.Should().BeTrue();
            second.NotificationsEnabled.Should().BeTrue();
            localStorageService.Verify(
                service => service.GetItemAsync<AppSettings>(AppSettings.StorageKey, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NullSettings_WHEN_SaveSettingsInvoked_THEN_ThrowsArgumentNullException()
        {
            AppSettings? settings = null;
            var act = async () => await _target.SaveSettingsAsync(settings!, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }
    }
}
