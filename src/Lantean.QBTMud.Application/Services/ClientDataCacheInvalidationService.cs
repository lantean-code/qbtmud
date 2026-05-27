namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Clears the scoped in-memory ClientData cache.
    /// </summary>
    public sealed class ClientDataCacheInvalidationService : IClientDataCacheInvalidationService
    {
        private readonly ClientDataCacheState _clientDataCacheState;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientDataCacheInvalidationService"/> class.
        /// </summary>
        /// <param name="clientDataCacheState">The scoped ClientData cache state.</param>
        public ClientDataCacheInvalidationService(ClientDataCacheState clientDataCacheState)
        {
            _clientDataCacheState = clientDataCacheState;
        }

        /// <inheritdoc />
        public async Task InvalidateClientDataCacheAsync(CancellationToken cancellationToken = default)
        {
            await _clientDataCacheState.CacheSemaphore.WaitAsync(cancellationToken);
            try
            {
                _clientDataCacheState.CachedEntries = null;
            }
            finally
            {
                _clientDataCacheState.CacheSemaphore.Release();
            }
        }
    }
}
