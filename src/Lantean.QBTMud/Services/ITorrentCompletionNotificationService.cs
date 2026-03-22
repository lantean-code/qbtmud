using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Tracks torrent state transitions and shows browser notifications based on user preferences.
    /// </summary>
    public interface ITorrentCompletionNotificationService
    {
        /// <summary>
        /// Initializes internal state from the current torrent snapshot without emitting notifications.
        /// </summary>
        /// <param name="torrents">The current torrent snapshot.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task InitializeAsync(IReadOnlyDictionary<string, Torrent> torrents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes torrent changes and emits notifications when configured transitions are detected.
        /// </summary>
        /// <param name="torrents">The current torrent snapshot.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ProcessAsync(IReadOnlyDictionary<string, Torrent> torrents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes transition batches and emits notifications when configured transitions are detected.
        /// </summary>
        /// <param name="transitions">The torrent transition batch.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ProcessTransitionsAsync(IReadOnlyList<TorrentTransition> transitions, CancellationToken cancellationToken = default);
    }
}
