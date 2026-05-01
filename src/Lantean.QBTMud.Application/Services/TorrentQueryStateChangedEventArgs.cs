namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides data for torrent query-state change notifications.
    /// </summary>
    public sealed class TorrentQueryStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TorrentQueryStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="changeKind">The kind of change that occurred.</param>
        public TorrentQueryStateChangedEventArgs(TorrentQueryStateChangeKind changeKind)
        {
            ChangeKind = changeKind;
        }

        /// <summary>
        /// Gets the kind of change that occurred.
        /// </summary>
        public TorrentQueryStateChangeKind ChangeKind { get; }
    }
}
