namespace Lantean.QBTMud.Services
{
    public sealed class SessionStorageService : ISessionStorageService
    {
        private readonly IBrowserStorageService _storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionStorageService"/> class.
        /// </summary>
        /// <param name="storageServiceFactory">The browser storage service factory.</param>
        public SessionStorageService(IBrowserStorageServiceFactory storageServiceFactory)
        {
            ArgumentNullException.ThrowIfNull(storageServiceFactory);
            _storage = storageServiceFactory.CreateSessionStorageService();
        }

        /// <summary>
        /// Retrieves an item from session storage by key.
        /// </summary>
        /// <param name="key">The session storage key to read.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The stored value if present; otherwise, the default for the type.</returns>
        public ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return _storage.GetItemAsync<T>(key, cancellationToken);
        }

        /// <summary>
        /// Persists an item to session storage under the specified key.
        /// </summary>
        /// <param name="key">The session storage key to write.</param>
        /// <param name="data">The value to store.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            return _storage.SetItemAsync(key, data, cancellationToken);
        }

        /// <summary>
        /// Removes an item from session storage by key.
        /// </summary>
        /// <param name="key">The session storage key to remove.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            return _storage.RemoveItemAsync(key, cancellationToken);
        }
    }
}
