using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides diagnostics and cleanup operations for qbtmud local storage data.
    /// </summary>
    public interface IStorageDiagnosticsService
    {
        /// <summary>
        /// Gets qbtmud-prefixed local storage entries.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The qbtmud storage entries.</returns>
        Task<IReadOnlyList<AppStorageEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a qbtmud-prefixed local storage entry.
        /// </summary>
        /// <param name="prefixedKey">The fully-prefixed storage key.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveEntryAsync(string prefixedKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes all qbtmud-prefixed local storage entries.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The number of removed keys.</returns>
        Task<int> ClearEntriesAsync(CancellationToken cancellationToken = default);
    }
}
