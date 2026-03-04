namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents a group of routable storage items.
    /// </summary>
    public sealed class StorageCatalogGroupDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCatalogGroupDefinition"/> class.
        /// </summary>
        /// <param name="id">The unique group identifier.</param>
        /// <param name="displayNameSource">The localized display name source string.</param>
        /// <param name="items">The group item definitions.</param>
        public StorageCatalogGroupDefinition(string id, string displayNameSource, IReadOnlyList<StorageCatalogItemDefinition> items)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayNameSource);
            ArgumentNullException.ThrowIfNull(items);

            Id = id.Trim();
            DisplayNameSource = displayNameSource.Trim();
            Items = items;
        }

        /// <summary>
        /// Gets the unique group identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the localized display name source string.
        /// </summary>
        public string DisplayNameSource { get; }

        /// <summary>
        /// Gets the grouped storage items.
        /// </summary>
        public IReadOnlyList<StorageCatalogItemDefinition> Items { get; }
    }
}
