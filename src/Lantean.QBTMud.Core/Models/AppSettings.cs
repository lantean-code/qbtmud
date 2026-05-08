namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Stores qbtmud-specific application settings.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>
        /// Gets the local storage key used to persist qbtmud app settings.
        /// </summary>
        public const string StorageKey = "AppSettings.State.v2";

        /// <summary>
        /// Gets the legacy local storage key used to persist qbtmud app settings before v2.
        /// </summary>
        public const string LegacyStorageKey = "AppSettings.State.v1";

        /// <summary>
        /// Gets the default settings.
        /// </summary>
        public static AppSettings Default
        {
            get
            {
                return new AppSettings
                {
                    SpeedHistoryEnabled = false,
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = false,
                    ThemeModePreference = ThemeModePreference.System,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false,
                    DismissedReleaseTag = null,
                    ThemeRepositoryIndexUrl = "https://lantean-code.github.io/qbtmud-themes/index.json"
                };
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether speed history is enabled.
        /// </summary>
        public bool SpeedHistoryEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether startup update checks are enabled.
        /// </summary>
        public bool UpdateChecksEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether browser notifications are enabled.
        /// </summary>
        public bool NotificationsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the visual theme mode preference.
        /// </summary>
        public ThemeModePreference ThemeModePreference { get; set; } = ThemeModePreference.System;

        /// <summary>
        /// Gets or sets a value indicating whether notifications should be shown for completed downloads.
        /// </summary>
        public bool DownloadFinishedNotificationsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether notifications should be shown for added torrents.
        /// </summary>
        public bool TorrentAddedNotificationsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether add-torrent snackbars should still be shown when browser notifications are enabled.
        /// </summary>
        public bool TorrentAddedSnackbarsEnabledWithNotifications { get; set; }

        /// <summary>
        /// Gets or sets the latest release tag dismissed by the user.
        /// </summary>
        public string? DismissedReleaseTag { get; set; }

        /// <summary>
        /// Gets or sets the optional theme repository index URL.
        /// </summary>
        public string ThemeRepositoryIndexUrl { get; set; } = "https://lantean-code.github.io/qbtmud-themes/index.json";

        /// <summary>
        /// Determines whether two app-settings instances are equivalent.
        /// </summary>
        /// <param name="left">The first settings instance.</param>
        /// <param name="right">The second settings instance.</param>
        /// <returns><see langword="true"/> when the settings are equivalent; otherwise, <see langword="false"/>.</returns>
        public static bool AreEquivalent(AppSettings? left, AppSettings? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            return left.SpeedHistoryEnabled == right.SpeedHistoryEnabled
                && left.UpdateChecksEnabled == right.UpdateChecksEnabled
                && left.NotificationsEnabled == right.NotificationsEnabled
                && left.ThemeModePreference == right.ThemeModePreference
                && left.DownloadFinishedNotificationsEnabled == right.DownloadFinishedNotificationsEnabled
                && left.TorrentAddedNotificationsEnabled == right.TorrentAddedNotificationsEnabled
                && left.TorrentAddedSnackbarsEnabledWithNotifications == right.TorrentAddedSnackbarsEnabledWithNotifications
                && string.Equals(left.DismissedReleaseTag, right.DismissedReleaseTag, StringComparison.Ordinal)
                && string.Equals(left.ThemeRepositoryIndexUrl, right.ThemeRepositoryIndexUrl, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets or sets a value indicating whether startup update checks are enabled.
        /// </summary>
        /// Creates a deep copy of the current settings.
        /// </summary>
        /// <returns>A copied instance.</returns>
        public AppSettings Clone()
        {
            return new AppSettings
            {
                SpeedHistoryEnabled = SpeedHistoryEnabled,
                UpdateChecksEnabled = UpdateChecksEnabled,
                NotificationsEnabled = NotificationsEnabled,
                ThemeModePreference = ThemeModePreference,
                DownloadFinishedNotificationsEnabled = DownloadFinishedNotificationsEnabled,
                TorrentAddedNotificationsEnabled = TorrentAddedNotificationsEnabled,
                TorrentAddedSnackbarsEnabledWithNotifications = TorrentAddedSnackbarsEnabledWithNotifications,
                DismissedReleaseTag = DismissedReleaseTag,
                ThemeRepositoryIndexUrl = ThemeRepositoryIndexUrl
            };
        }
    }
}
