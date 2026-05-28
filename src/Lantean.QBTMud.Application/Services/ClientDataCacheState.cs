using System.Text.Json;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Holds scoped in-memory ClientData cache state for the current session.
    /// </summary>
    public sealed class ClientDataCacheState
    {
        /// <summary>
        /// Gets the semaphore that synchronizes cache reads and writes.
        /// </summary>
        public SemaphoreSlim CacheSemaphore { get; } = new(1, 1);

        /// <summary>
        /// Gets or sets the cached ClientData entries for the current scope.
        /// </summary>
        public IReadOnlyDictionary<string, JsonElement>? CachedEntries { get; set; }
    }
}
