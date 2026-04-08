using Lantean.QBTMud.Models;
using System.Text.Json;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides persistent qbtmud-specific application settings.
    /// </summary>
    public sealed class AppSettingsService : IAppSettingsService
    {
        private const string _legacyDarkModeStorageKey = "MainLayout.IsDarkMode";

        private readonly SemaphoreSlim _initializationSemaphore = new SemaphoreSlim(1, 1);
        private readonly ISettingsStorageService _settingsStorageService;
        private AppSettings? _cachedSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppSettingsService"/> class.
        /// </summary>
        /// <param name="settingsStorageService">The local storage service.</param>
        public AppSettingsService(ISettingsStorageService settingsStorageService)
        {
            _settingsStorageService = settingsStorageService;
        }

        /// <inheritdoc />
        public async Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            return await LoadSettingsAsync(forceReload: false, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<AppSettings> RefreshSettingsAsync(CancellationToken cancellationToken = default)
        {
            return await LoadSettingsAsync(forceReload: true, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<AppSettings> SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var normalized = Normalize(settings);
            _cachedSettings = normalized.Clone();

            await _settingsStorageService.SetItemAsync(AppSettings.StorageKey, normalized, cancellationToken);

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
                ThemeModePreference = NormalizeThemeModePreference(settings.ThemeModePreference),
                DownloadFinishedNotificationsEnabled = settings.DownloadFinishedNotificationsEnabled,
                TorrentAddedNotificationsEnabled = settings.TorrentAddedNotificationsEnabled,
                TorrentAddedSnackbarsEnabledWithNotifications = settings.TorrentAddedSnackbarsEnabledWithNotifications,
                DismissedReleaseTag = string.IsNullOrWhiteSpace(settings.DismissedReleaseTag)
                    ? null
                    : settings.DismissedReleaseTag.Trim(),
                ThemeRepositoryIndexUrl = NormalizeThemeRepositoryIndexUrl(settings.ThemeRepositoryIndexUrl)
            };
        }

        private async Task<AppSettings> MigrateLegacyThemeModePreference(AppSettings settings, CancellationToken cancellationToken)
        {
            var legacyDarkMode = await _settingsStorageService.GetItemAsync<bool?>(_legacyDarkModeStorageKey, cancellationToken);
            if (!legacyDarkMode.HasValue)
            {
                return settings;
            }

            if (settings.ThemeModePreference == ThemeModePreference.System)
            {
                settings.ThemeModePreference = legacyDarkMode.Value
                    ? ThemeModePreference.Dark
                    : ThemeModePreference.Light;
                await _settingsStorageService.SetItemAsync(AppSettings.StorageKey, settings, cancellationToken);
            }

            await _settingsStorageService.RemoveItemAsync(_legacyDarkModeStorageKey, cancellationToken);
            return settings;
        }

        private static ThemeModePreference NormalizeThemeModePreference(ThemeModePreference mode)
        {
            return Enum.IsDefined(mode)
                ? mode
                : ThemeModePreference.System;
        }

        private static string NormalizeThemeRepositoryIndexUrl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                return string.Empty;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return uri.AbsoluteUri;
        }

        private async Task<AppSettings> LoadSettingsAsync(bool forceReload, CancellationToken cancellationToken)
        {
            await _initializationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (!forceReload && _cachedSettings is not null)
                {
                    return _cachedSettings.Clone();
                }

                AppSettings? loadedSettings = null;
                try
                {
                    loadedSettings = await _settingsStorageService.GetItemAsync<AppSettings>(AppSettings.StorageKey, cancellationToken);
                }
                catch (JsonException)
                {
                    loadedSettings = null;
                }

                var normalized = loadedSettings is null
                    ? AppSettings.Default.Clone()
                    : Normalize(loadedSettings);
                _cachedSettings = await MigrateLegacyThemeModePreference(normalized, cancellationToken);
                return _cachedSettings.Clone();
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }
    }
}
