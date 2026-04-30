using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Coordinates authenticated shell startup and refresh operations.
    /// </summary>
    public interface IShellSessionWorkflow
    {
        /// <summary>
        /// Loads the initial shell session state.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns>The load result.</returns>
        Task<ShellSessionLoadResult> LoadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to recover shell startup after a retryable failure.
        /// </summary>
        /// <param name="requestId">The current qBittorrent request identifier.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns>The load result.</returns>
        Task<ShellSessionLoadResult> RecoverAsync(int requestId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refreshes shell state from qBittorrent.
        /// </summary>
        /// <param name="requestId">The current qBittorrent request identifier.</param>
        /// <param name="currentMainData">The current shell main-data snapshot.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns>The refresh result.</returns>
        Task<ShellSessionRefreshResult> RefreshAsync(int requestId, MainData? currentMainData, CancellationToken cancellationToken = default);
    }
}
