namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Represents a routable storage item definition.
    /// </summary>
    public sealed class StorageCatalogItemDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCatalogItemDefinition"/> class.
        /// </summary>
        /// <param name="id">The unique catalog item identifier.</param>
        /// <param name="groupId">The owning group identifier.</param>
        /// <param name="displayNameSource">The localized display name source string.</param>
        /// <param name="matchMode">The key match mode.</param>
        /// <param name="matchPattern">The key or key prefix pattern.</param>
        /// <param name="serializationMode">The value serialization mode used for migration.</param>
        public StorageCatalogItemDefinition(
            string id,
            string groupId,
            string displayNameSource,
            StorageCatalogItemMatchMode matchMode,
            string matchPattern,
            StorageItemSerializationMode serializationMode)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
            ArgumentException.ThrowIfNullOrWhiteSpace(displayNameSource);
            ArgumentException.ThrowIfNullOrWhiteSpace(matchPattern);

            Id = id.Trim();
            GroupId = groupId.Trim();
            DisplayNameSource = displayNameSource.Trim();
            MatchMode = matchMode;
            MatchPattern = matchPattern.Trim();
            SerializationMode = serializationMode;
        }

        /// <summary>
        /// Gets the unique catalog item identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the owning group identifier.
        /// </summary>
        public string GroupId { get; }

        /// <summary>
        /// Gets the localized display name source string.
        /// </summary>
        public string DisplayNameSource { get; }

        /// <summary>
        /// Gets the key match mode.
        /// </summary>
        public StorageCatalogItemMatchMode MatchMode { get; }

        /// <summary>
        /// Gets the key or key-prefix pattern.
        /// </summary>
        public string MatchPattern { get; }

        /// <summary>
        /// Gets the serialization mode used for migration.
        /// </summary>
        public StorageItemSerializationMode SerializationMode { get; }
    }
}
