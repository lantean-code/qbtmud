using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Default implementation of <see cref="IQBittorrentPreferencesStateService"/>.
    /// </summary>
    public sealed class QBittorrentPreferencesStateService : IQBittorrentPreferencesStateService
    {
        /// <inheritdoc />
        public event EventHandler<QBittorrentPreferencesChangedEventArgs>? Changed;

        /// <inheritdoc />
        public QBittorrentPreferences? Current { get; private set; }

        /// <inheritdoc />
        public bool SetPreferences(QBittorrentPreferences? preferences)
        {
            if (Equals(Current, preferences))
            {
                return false;
            }

            var previousPreferences = Current;
            Current = preferences;
            Changed?.Invoke(this, new QBittorrentPreferencesChangedEventArgs(previousPreferences, Current));
            return true;
        }
    }
}
