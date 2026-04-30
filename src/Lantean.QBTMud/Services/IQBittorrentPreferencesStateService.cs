using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Stores runtime preferences shared by authenticated UI components.
    /// </summary>
    public interface IQBittorrentPreferencesStateService
    {
        /// <summary>
        /// Occurs when the runtime preferences snapshot changes.
        /// </summary>
        event EventHandler<QBittorrentPreferencesChangedEventArgs>? Changed;

        /// <summary>
        /// Gets the current runtime preferences snapshot.
        /// </summary>
        QBittorrentPreferences? Current { get; }

        /// <summary>
        /// Sets the current runtime preferences snapshot.
        /// </summary>
        /// <param name="preferences">The runtime preferences snapshot.</param>
        /// <returns><see langword="true"/> when the snapshot changed; otherwise, <see langword="false"/>.</returns>
        bool SetPreferences(QBittorrentPreferences? preferences);
    }
}
