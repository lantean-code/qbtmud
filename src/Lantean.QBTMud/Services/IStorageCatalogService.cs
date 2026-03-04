using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides definitions for all routed qbtmud storage items.
    /// </summary>
    public interface IStorageCatalogService
    {
        /// <summary>
        /// Gets the catalog groups.
        /// </summary>
        IReadOnlyList<StorageCatalogGroupDefinition> Groups { get; }

        /// <summary>
        /// Gets all catalog items.
        /// </summary>
        IReadOnlyList<StorageCatalogItemDefinition> Items { get; }

        /// <summary>
        /// Matches a runtime storage key to a catalog item.
        /// </summary>
        /// <param name="key">The runtime storage key without qbtmud prefix.</param>
        /// <returns>The matching catalog item, or <see langword="null"/> when unmatched.</returns>
        StorageCatalogItemDefinition? MatchItemByKey(string key);
    }
}
