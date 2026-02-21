namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Stores qbtmud-specific application settings.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>
        /// Gets the local storage key used to persist qbtmud app settings.
        /// </summary>
        public const string StorageKey = "AppSettings.State.v1";

        /// <summary>
        /// Gets the default settings.
        /// </summary>
        public static AppSettings Default
        {
            get
            {
                return new AppSettings
                {
                    UpdateChecksEnabled = true,
                    NotificationsEnabled = false,
                    DownloadFinishedNotificationsEnabled = true,
                    TorrentAddedNotificationsEnabled = false,
                    TorrentAddedSnackbarsEnabledWithNotifications = false,
                    DismissedReleaseTag = null
                };
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether startup update checks are enabled.
        /// </summary>
        public bool UpdateChecksEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether browser notifications are enabled.
        /// </summary>
        public bool NotificationsEnabled { get; set; }

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
        /// Creates a deep copy of the current settings.
        /// </summary>
        /// <returns>A copied instance.</returns>
        public AppSettings Clone()
        {
            return new AppSettings
            {
                UpdateChecksEnabled = UpdateChecksEnabled,
                NotificationsEnabled = NotificationsEnabled,
                DownloadFinishedNotificationsEnabled = DownloadFinishedNotificationsEnabled,
                TorrentAddedNotificationsEnabled = TorrentAddedNotificationsEnabled,
                TorrentAddedSnackbarsEnabledWithNotifications = TorrentAddedSnackbarsEnabledWithNotifications,
                DismissedReleaseTag = DismissedReleaseTag
            };
        }
    }
}
