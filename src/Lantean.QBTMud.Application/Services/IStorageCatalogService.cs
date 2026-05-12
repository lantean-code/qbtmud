using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides definitions for qbtmud storage items.
    /// </summary>
    public interface IStorageCatalogService
    {
        /// <summary>
        /// Gets the user-configurable routed storage groups.
        /// </summary>
        IReadOnlyList<StorageCatalogGroupDefinition> Groups { get; }

        /// <summary>
        /// Gets all user-configurable routed storage items.
        /// </summary>
        IReadOnlyList<StorageCatalogItemDefinition> Items { get; }

        /// <summary>
        /// Matches a runtime storage key to a user-configurable routed storage item.
        /// </summary>
        /// <param name="key">The runtime storage key without qbtmud prefix.</param>
        /// <returns>The matching catalog item, or <see langword="null"/> when unmatched.</returns>
        StorageCatalogItemDefinition? MatchItemByKey(string key);

        /// <summary>
        /// Determines whether a runtime storage key is fixed to local storage and excluded from routed storage settings.
        /// </summary>
        /// <param name="key">The runtime storage key without qbtmud prefix.</param>
        /// <returns><see langword="true"/> when the key is fixed to local storage; otherwise <see langword="false"/>.</returns>
        bool IsLocalStorageOnlyKey(string key);
    }
}
