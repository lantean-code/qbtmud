namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides access to browser-backed key/value storage.
    /// </summary>
    public interface IBrowserStorageService
    {
        /// <summary>
        /// Retrieves an item from browser storage by key.
        /// </summary>
        /// <typeparam name="T">The type to deserialize from storage.</typeparam>
        /// <param name="key">The storage key to read.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The stored value if present; otherwise, the default for the type.</returns>
        ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves a raw string value from browser storage by key.
        /// </summary>
        /// <param name="key">The storage key to read.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The stored string if present; otherwise, null.</returns>
        ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken);

        /// <summary>
        /// Persists an item to browser storage under the specified key.
        /// </summary>
        /// <typeparam name="T">The type of data being stored.</typeparam>
        /// <param name="key">The storage key to write.</param>
        /// <param name="data">The value to store.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken);

        /// <summary>
        /// Persists a raw string value to browser storage under the specified key.
        /// </summary>
        /// <param name="key">The storage key to write.</param>
        /// <param name="data">The string value to store.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken);

        /// <summary>
        /// Removes an item from browser storage by key.
        /// </summary>
        /// <param name="key">The storage key to remove.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken);
    }
}
