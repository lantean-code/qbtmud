namespace Lantean.QBTMud.Services
{
    public interface ILocalStorageService
    {
        /// <summary>
        /// Retrieves an item from local storage by key.
        /// </summary>
        /// <param name="key">The local storage key to read.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The stored value if present; otherwise, the default for the type.</returns>
        ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a raw string value from local storage by key.
        /// </summary>
        /// <param name="key">The local storage key to read.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The stored string if present; otherwise, null.</returns>
        ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists an item to local storage under the specified key.
        /// </summary>
        /// <param name="key">The local storage key to write.</param>
        /// <param name="data">The value to store.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a raw string value to local storage under the specified key.
        /// </summary>
        /// <param name="key">The local storage key to write.</param>
        /// <param name="data">The string value to store.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an item from local storage by key.
        /// </summary>
        /// <param name="key">The local storage key to remove.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default);
    }
}
