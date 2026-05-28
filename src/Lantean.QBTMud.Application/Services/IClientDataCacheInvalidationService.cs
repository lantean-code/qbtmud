namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides invalidation for the in-memory ClientData cache used by settings storage.
    /// </summary>
    public interface IClientDataCacheInvalidationService
    {
        /// <summary>
        /// Invalidates any cached qbtmud ClientData entries.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the cache has been invalidated.</returns>
        Task InvalidateClientDataCacheAsync(CancellationToken cancellationToken = default);
    }
}
