using Lantean.QBTMud.Core.Interop;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides local storage entry operations required by application services.
    /// </summary>
    public interface ILocalStorageEntryAdapter
    {
        /// <summary>
        /// Gets local storage entries that match a key prefix.
        /// </summary>
        /// <param name="prefix">The key prefix.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The matching local storage entries.</returns>
        Task<IReadOnlyList<BrowserStorageEntry>> GetEntriesByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a local storage entry by key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveEntryAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears local storage entries that match a key prefix.
        /// </summary>
        /// <param name="prefix">The key prefix.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of removed entries.</returns>
        Task<int> ClearEntriesByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
    }
}
