namespace Lantean.QBTMud.Services
{
    public sealed class LocalStorageService : ILocalStorageService
    {
        private readonly IBrowserStorageService _storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalStorageService"/> class.
        /// </summary>
        /// <param name="storageServiceFactory">The browser storage service factory.</param>
        public LocalStorageService(IBrowserStorageServiceFactory storageServiceFactory)
        {
            ArgumentNullException.ThrowIfNull(storageServiceFactory);
            _storage = storageServiceFactory.CreateLocalStorageService();
        }

        /// <summary>
        /// Retrieves an item from local storage by key.
        /// </summary>
        /// <param name="key">The local storage key to read.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The stored value if present; otherwise, the default for the type.</returns>
        public ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return _storage.GetItemAsync<T>(key, cancellationToken);
        }

        /// <summary>
        /// Retrieves a raw string value from local storage by key.
        /// </summary>
        /// <param name="key">The local storage key to read.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The stored string if present; otherwise, null.</returns>
        public ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
        {
            return _storage.GetItemAsStringAsync(key, cancellationToken);
        }

        /// <summary>
        /// Persists an item to local storage under the specified key.
        /// </summary>
        /// <param name="key">The local storage key to write.</param>
        /// <param name="data">The value to store.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            return _storage.SetItemAsync(key, data, cancellationToken);
        }

        /// <summary>
        /// Persists a raw string value to local storage under the specified key.
        /// </summary>
        /// <param name="key">The local storage key to write.</param>
        /// <param name="data">The string value to store.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
        {
            return _storage.SetItemAsStringAsync(key, data, cancellationToken);
        }

        /// <summary>
        /// Removes an item from local storage by key.
        /// </summary>
        /// <param name="key">The local storage key to remove.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            return _storage.RemoveItemAsync(key, cancellationToken);
        }
    }
}
