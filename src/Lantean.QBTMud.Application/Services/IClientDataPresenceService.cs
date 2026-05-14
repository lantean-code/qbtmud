namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Detects whether qbtmud-owned ClientData already exists for the current qBittorrent instance.
    /// </summary>
    public interface IClientDataPresenceService
    {
        /// <summary>
        /// Determines whether any qbtmud-owned ClientData entries are already stored.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns><see langword="true"/> when qbtmud ClientData entries exist; otherwise, <see langword="false"/>.</returns>
        Task<bool> HasStoredClientDataAsync(CancellationToken cancellationToken = default);
    }
}
