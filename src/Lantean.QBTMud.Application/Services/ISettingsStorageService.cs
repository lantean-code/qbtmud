namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides routed persistence for qbtmud settings data across supported storage types.
    /// </summary>
    public interface ISettingsStorageService
    {
        /// <summary>
        /// Retrieves an item by key.
        /// </summary>
        /// <typeparam name="T">The type to deserialize from storage.</typeparam>
        /// <param name="key">The storage key to read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The stored value if present; otherwise the default for the type.</returns>
        ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a raw string item by key.
        /// </summary>
        /// <param name="key">The storage key to read.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The stored string if present; otherwise <see langword="null"/>.</returns>
        ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a typed item under the specified key.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="key">The storage key to write.</param>
        /// <param name="data">The value to persist.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a raw string item under the specified key.
        /// </summary>
        /// <param name="key">The storage key to write.</param>
        /// <param name="data">The string value to persist.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an item by key.
        /// </summary>
        /// <param name="key">The storage key to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default);
    }
}
