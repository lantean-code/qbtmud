namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides raw access to qBittorrent ClientData storage for qbtmud-prefixed keys.
    /// </summary>
    public interface IClientDataStorageAdapter
    {
        /// <summary>
        /// Loads qbtmud-prefixed entries for specific prefixed keys.
        /// </summary>
        /// <param name="prefixedKeys">The full prefixed keys to load.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The matching prefixed entries result.</returns>
        Task<ClientDataLoadResult> LoadPrefixedEntriesAsync(IEnumerable<string> prefixedKeys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads all qbtmud-prefixed entries.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>All qbtmud-prefixed entries result.</returns>
        Task<ClientDataLoadResult> LoadPrefixedEntriesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores prefixed values in ClientData.
        /// </summary>
        /// <param name="prefixedValues">The map of prefixed keys to values. A null value removes the key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The storage operation result.</returns>
        Task<ClientDataStorageResult> StorePrefixedEntriesAsync(IReadOnlyDictionary<string, object?> prefixedValues, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes prefixed keys from ClientData.
        /// </summary>
        /// <param name="prefixedKeys">The full prefixed keys to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The removal operation result.</returns>
        Task<ClientDataStorageResult> RemovePrefixedEntriesAsync(IEnumerable<string> prefixedKeys, CancellationToken cancellationToken = default);
    }
}
