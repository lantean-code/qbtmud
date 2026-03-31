namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Tracks whether the application has lost connectivity to qBittorrent.
    /// </summary>
    public interface IConnectivityStateService
    {
        /// <summary>
        /// Gets a value indicating whether connectivity is currently lost.
        /// </summary>
        bool IsLostConnection { get; }

        /// <summary>
        /// Occurs when the connectivity state changes.
        /// </summary>
        event Action<bool>? ConnectivityChanged;

        /// <summary>
        /// Marks the application as disconnected from qBittorrent.
        /// </summary>
        void MarkLostConnection();

        /// <summary>
        /// Marks the application as connected to qBittorrent.
        /// </summary>
        void MarkConnected();
    }
}
