using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.TestSupport.Infrastructure;
using Moq;

namespace Lantean.QBTMud.Application.Test.Services
{
    public sealed class AppSettingsServiceTests
    {
        private readonly TestLocalStorageService _settingsStorageService;
        private readonly AppSettingsService _target;

        public AppSettingsServiceTests()
        {
            _settingsStorageService = new TestLocalStorageService();
            _target = new AppSettingsService(_settingsStorageService);
        }

        [Fact]
        public async Task GIVEN_NoPersistedSettings_WHEN_GetSettingsInvoked_THEN_ReturnsDefaults()
        {
            var result = await _target.GetSettingsAsync(TestContext.Current.CancellationToken);

            result.UpdateChecksEnabled.Should().BeTrue();
            result.NotificationsEnabled.Should().BeFalse();
            result.ThemeModePreference.Should().Be(ThemeModePreference.System);
            result.DownloadFinishedNotificationsEnabled.Should().BeTrue();
            result.TorrentAddedNotificationsEnabled.Should().BeFalse();
            result.TorrentAddedSnackbarsEnabledWithNotifications.Should().BeFalse();
            result.DismissedReleaseTag.Should().BeNull();
            result.ThemeRepositoryIndexUrl.Should().Be("https://lantean-code.github.io/qbtmud-themes/index.json");
        }

        [Fact]
        public async Task GIVEN_CustomSettings_WHEN_SaveSettingsInvoked_THEN_PersistsAndReturnsNormalizedCopy()
        {
            var settings = new AppSettings
            {
                UpdateChecksEnabled = false,
                NotificationsEnabled = true,
                ThemeModePreference = ThemeModePreference.Dark,
                DownloadFinishedNotificationsEnabled = false,
                TorrentAddedNotificationsEnabled = true,
                TorrentAddedSnackbarsEnabledWithNotifications = true,
                DismissedReleaseTag = " v1.2.3 ",
                ThemeRepositoryIndexUrl = " https://lantean-code.github.io/qbtmud-themes/index.json "
            };

            var saved = await _target.SaveSettingsAsync(settings, TestContext.Current.CancellationToken);
            var reloaded = await _target.GetSettingsAsync(TestContext.Current.CancellationToken);

            saved.UpdateChecksEnabled.Should().BeFalse();
            saved.NotificationsEnabled.Should().BeTrue();
            saved.ThemeModePreference.Should().Be(ThemeModePreference.Dark);
            saved.DownloadFinishedNotificationsEnabled.Should().BeFalse();
            saved.TorrentAddedNotificationsEnabled.Should().BeTrue();
            saved.TorrentAddedSnackbarsEnabledWithNotifications.Should().BeTrue();
            saved.DismissedReleaseTag.Should().Be("v1.2.3");
            saved.ThemeRepositoryIndexUrl.Should().Be("https://lantean-code.github.io/qbtmud-themes/index.json");

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

        [Theory]
        [InlineData("http://example.com/index.json")]
        [InlineData("notaurl")]
        public async Task GIVEN_InvalidThemeRepositoryIndexUrl_WHEN_SaveSettingsInvoked_THEN_NormalizesToEmpty(string value)
        {
            var settings = AppSettings.Default.Clone();
            settings.ThemeRepositoryIndexUrl = value;

            var saved = await _target.SaveSettingsAsync(settings, TestContext.Current.CancellationToken);

            saved.ThemeRepositoryIndexUrl.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_BlankThemeRepositoryIndexUrl_WHEN_SaveSettingsInvoked_THEN_PreservesBlank()
        {
            var settings = AppSettings.Default.Clone();
            settings.ThemeRepositoryIndexUrl = "   ";

            var saved = await _target.SaveSettingsAsync(settings, TestContext.Current.CancellationToken);

            saved.ThemeRepositoryIndexUrl.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_CachedSettings_WHEN_GetSettingsInvokedTwice_THEN_ReadsStorageOnce()
        {
            var settingsStorageService = new Mock<ISettingsStorageService>(MockBehavior.Strict);
            settingsStorageService
                .Setup(service => service.GetItemAsync<AppSettings>(AppSettings.StorageKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(AppSettings.Default.Clone());
            settingsStorageService
                .Setup(service => service.GetItemAsync<bool?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((bool?)null);

            var target = new AppSettingsService(settingsStorageService.Object);

            _ = await target.GetSettingsAsync(TestContext.Current.CancellationToken);
            _ = await target.GetSettingsAsync(TestContext.Current.CancellationToken);

            settingsStorageService.Verify(
                service => service.GetItemAsync<AppSettings>(AppSettings.StorageKey, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_CachedSettings_WHEN_RefreshSettingsInvoked_THEN_ReloadsFromStorage()
        {
            var settingsStorageService = new Mock<ISettingsStorageService>(MockBehavior.Strict);
            var loadQueue = new Queue<AppSettings?>(
            [
                new AppSettings
                {
                    UpdateChecksEnabled = false,
                    NotificationsEnabled = false
                },
                new AppSettings
                {
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = true
                }
            ]);
            settingsStorageService
                .Setup(service => service.GetItemAsync<AppSettings>(AppSettings.StorageKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => loadQueue.Dequeue());
            settingsStorageService
                .Setup(service => service.GetItemAsync<bool?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((bool?)null);

            var target = new AppSettingsService(settingsStorageService.Object);

            var first = await target.GetSettingsAsync(TestContext.Current.CancellationToken);
            var refreshed = await target.RefreshSettingsAsync(TestContext.Current.CancellationToken);

            first.UpdateChecksEnabled.Should().BeFalse();
            refreshed.UpdateChecksEnabled.Should().BeTrue();
            refreshed.NotificationsEnabled.Should().BeTrue();
            settingsStorageService.Verify(
                service => service.GetItemAsync<AppSettings>(AppSettings.StorageKey, It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_LocalStorageJsonException_WHEN_GetSettingsInvoked_THEN_ReturnsDefaults()
        {
            var settingsStorageService = new Mock<ISettingsStorageService>(MockBehavior.Strict);
            settingsStorageService
                .Setup(service => service.GetItemAsync<AppSettings>(AppSettings.StorageKey, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new JsonException("invalid"));
            settingsStorageService
                .Setup(service => service.GetItemAsync<bool?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((bool?)null);

            var target = new AppSettingsService(settingsStorageService.Object);

            var result = await target.GetSettingsAsync(TestContext.Current.CancellationToken);

            result.Should().BeEquivalentTo(AppSettings.Default);
        }

        [Fact]
        public async Task GIVEN_ConcurrentInitialReads_WHEN_FirstReadInitializesCache_THEN_SecondReadReturnsFromSemaphoreCache()
        {
            var settingsStorageService = new Mock<ISettingsStorageService>(MockBehavior.Strict);
            var readStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseRead = new TaskCompletionSource<AppSettings>(TaskCreationOptions.RunContinuationsAsynchronously);

            settingsStorageService
                .Setup(service => service.GetItemAsync<AppSettings>(AppSettings.StorageKey, It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    readStarted.TrySetResult();
                    return await releaseRead.Task;
                });
            settingsStorageService
                .Setup(service => service.GetItemAsync<bool?>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((bool?)null);

            var target = new AppSettingsService(settingsStorageService.Object);
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
            settingsStorageService.Verify(
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

        [Fact]
        public async Task GIVEN_LegacyDarkModeSettingAndSystemThemeMode_WHEN_GetSettingsInvoked_THEN_MigratesToDarkAndRemovesLegacyKey()
        {
            await _settingsStorageService.SetItemAsync("MainLayout.IsDarkMode", true, TestContext.Current.CancellationToken);

            var result = await _target.GetSettingsAsync(TestContext.Current.CancellationToken);
            var legacyValue = await _settingsStorageService.GetItemAsync<bool?>("MainLayout.IsDarkMode", TestContext.Current.CancellationToken);
            var persisted = await _settingsStorageService.GetItemAsync<AppSettings>(AppSettings.StorageKey, TestContext.Current.CancellationToken);

            result.ThemeModePreference.Should().Be(ThemeModePreference.Dark);
            legacyValue.Should().BeNull();
            persisted.Should().NotBeNull();
            persisted!.ThemeModePreference.Should().Be(ThemeModePreference.Dark);
        }

        [Fact]
        public async Task GIVEN_LegacyLightModeSettingAndSystemThemeMode_WHEN_GetSettingsInvoked_THEN_MigratesToLightAndRemovesLegacyKey()
        {
            await _settingsStorageService.SetItemAsync("MainLayout.IsDarkMode", false, TestContext.Current.CancellationToken);

            var result = await _target.GetSettingsAsync(TestContext.Current.CancellationToken);
            var legacyValue = await _settingsStorageService.GetItemAsync<bool?>("MainLayout.IsDarkMode", TestContext.Current.CancellationToken);
            var persisted = await _settingsStorageService.GetItemAsync<AppSettings>(AppSettings.StorageKey, TestContext.Current.CancellationToken);

            result.ThemeModePreference.Should().Be(ThemeModePreference.Light);
            legacyValue.Should().BeNull();
            persisted.Should().NotBeNull();
            persisted!.ThemeModePreference.Should().Be(ThemeModePreference.Light);
        }

        [Fact]
        public async Task GIVEN_ExplicitThemeModePreferenceAndLegacyDarkModeSetting_WHEN_GetSettingsInvoked_THEN_ExplicitThemeModeIsNotOverridden()
        {
            await _settingsStorageService.SetItemAsync(AppSettings.StorageKey, new AppSettings
            {
                ThemeModePreference = ThemeModePreference.Dark
            }, TestContext.Current.CancellationToken);
            await _settingsStorageService.SetItemAsync("MainLayout.IsDarkMode", false, TestContext.Current.CancellationToken);

            var result = await _target.GetSettingsAsync(TestContext.Current.CancellationToken);
            var legacyValue = await _settingsStorageService.GetItemAsync<bool?>("MainLayout.IsDarkMode", TestContext.Current.CancellationToken);

            result.ThemeModePreference.Should().Be(ThemeModePreference.Dark);
            legacyValue.Should().BeNull();
        }
    }
}
