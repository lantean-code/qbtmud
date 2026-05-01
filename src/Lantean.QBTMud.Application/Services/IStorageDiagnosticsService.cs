using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides diagnostics and cleanup operations for qbtmud storage data.
    /// </summary>
    public interface IStorageDiagnosticsService
    {
        /// <summary>
        /// Gets qbtmud-prefixed storage entries from all available storage types.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The qbtmud storage entries.</returns>
        Task<IReadOnlyList<AppStorageEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a qbtmud-prefixed storage entry from the specified storage type.
        /// </summary>
        /// <param name="storageType">The storage type containing the entry.</param>
        /// <param name="prefixedKey">The fully-prefixed storage key.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveEntryAsync(StorageType storageType, string prefixedKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes qbtmud-prefixed storage entries.
        /// </summary>
        /// <param name="storageType">The optional storage type to clear. When null, all available storage types are cleared.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The number of removed keys.</returns>
        Task<int> ClearEntriesAsync(StorageType? storageType = null, CancellationToken cancellationToken = default);
    }
}
