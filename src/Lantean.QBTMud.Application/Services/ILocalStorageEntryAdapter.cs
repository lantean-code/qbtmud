using Lantean.QBTMud.Core.Interop;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides local storage entry operations required by application services.
    /// </summary>
    public interface ILocalStorageEntryAdapter
    {
        /// <summary>
        /// Gets qbtmud-owned local storage entries.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The qbtmud-owned local storage entries.</returns>
        Task<IReadOnlyList<BrowserStorageEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a local storage entry by key.
        /// </summary>
        /// <param name="key">The storage key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RemoveEntryAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears qbtmud-owned local storage entries.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The number of removed entries.</returns>
        Task<int> ClearEntriesAsync(CancellationToken cancellationToken = default);
    }
}
