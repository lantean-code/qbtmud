using Lantean.QBTMud.Models;
using System.Text.Json;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides persistent qbtmud-specific application settings.
    /// </summary>
    public sealed class AppSettingsService : IAppSettingsService
    {
        private readonly SemaphoreSlim _initializationSemaphore = new SemaphoreSlim(1, 1);
        private readonly ILocalStorageService _localStorageService;
        private AppSettings? _cachedSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsService"/> class.
        /// </summary>
        /// <param name="localStorageService">The local storage service.</param>
        public AppSettingsService(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        /// <inheritdoc />
        public async Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            if (_cachedSettings is not null)
            {
                return _cachedSettings.Clone();
            }

            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_cachedSettings is not null)
                {
                    return _cachedSettings.Clone();
                }

                AppSettings? loadedSettings = null;
                try
                {
                    loadedSettings = await _localStorageService.GetItemAsync<AppSettings>(AppSettings.StorageKey, cancellationToken);
                }
                catch (JsonException)
                {
                    loadedSettings = null;
                }

                _cachedSettings = loadedSettings is null
                    ? AppSettings.Default.Clone()
                    : Normalize(loadedSettings);
                return _cachedSettings.Clone();
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task<AppSettings> SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var normalized = Normalize(settings);
            _cachedSettings = normalized.Clone();

            await _localStorageService.SetItemAsync(AppSettings.StorageKey, normalized, cancellationToken);
            return _cachedSettings.Clone();
        }

        /// <inheritdoc />
        public async Task<AppSettings> SaveDismissedReleaseTagAsync(string? tagName, CancellationToken cancellationToken = default)
        {
            var settings = await GetSettingsAsync(cancellationToken);
            settings.DismissedReleaseTag = string.IsNullOrWhiteSpace(tagName) ? null : tagName.Trim();
            return await SaveSettingsAsync(settings, cancellationToken);
        }

        private static AppSettings Normalize(AppSettings settings)
        {
            return new AppSettings
            {
                UpdateChecksEnabled = settings.UpdateChecksEnabled,
                NotificationsEnabled = settings.NotificationsEnabled,
                DownloadFinishedNotificationsEnabled = settings.DownloadFinishedNotificationsEnabled,
                TorrentAddedNotificationsEnabled = settings.TorrentAddedNotificationsEnabled,
                TorrentAddedSnackbarsEnabledWithNotifications = settings.TorrentAddedSnackbarsEnabledWithNotifications,
                DismissedReleaseTag = string.IsNullOrWhiteSpace(settings.DismissedReleaseTag)
                    ? null
                    : settings.DismissedReleaseTag.Trim()
            };
        }
    }
}
