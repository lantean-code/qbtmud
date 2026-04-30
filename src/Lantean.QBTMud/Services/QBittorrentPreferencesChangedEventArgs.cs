using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides data for runtime qBittorrent preference changes.
    /// </summary>
    public sealed class QBittorrentPreferencesChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QBittorrentPreferencesChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousPreferences">The previous runtime preferences snapshot.</param>
        /// <param name="currentPreferences">The current runtime preferences snapshot.</param>
        public QBittorrentPreferencesChangedEventArgs(QBittorrentPreferences? previousPreferences, QBittorrentPreferences? currentPreferences)
        {
            PreviousPreferences = previousPreferences;
            CurrentPreferences = currentPreferences;
        }

        /// <summary>
        /// Gets the previous runtime preferences snapshot.
        /// </summary>
        public QBittorrentPreferences? PreviousPreferences { get; }

        /// <summary>
        /// Gets the current runtime preferences snapshot.
        /// </summary>
        public QBittorrentPreferences? CurrentPreferences { get; }
    }
}
