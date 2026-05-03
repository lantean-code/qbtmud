namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Represents persisted storage-routing selections for qbtmud settings data.
    /// </summary>
    public sealed class StorageRoutingSettings
    {
        /// <summary>
        /// Gets the local-only storage key used for persisted routing settings.
        /// </summary>
        public const string StorageKey = "StorageRouting.Settings.v1";

        /// <summary>
        /// Gets a default routing-settings instance.
        /// </summary>
        public static StorageRoutingSettings Default
        {
            get
            {
                return new StorageRoutingSettings
                {
                    MasterStorageType = StorageType.LocalStorage
                };
            }
        }

        /// <summary>
        /// Gets or sets the master storage type used when no group or item override exists.
        /// </summary>
        public StorageType MasterStorageType { get; set; } = StorageType.LocalStorage;

        /// <summary>
        /// Gets or sets per-group storage-type overrides keyed by group identifier.
        /// </summary>
        public Dictionary<string, StorageType> GroupStorageTypes { get; set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets per-item storage-type overrides keyed by catalog item identifier.
        /// </summary>
        public Dictionary<string, StorageType> ItemStorageTypes { get; set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Creates a deep clone of the current settings.
        /// </summary>
        /// <returns>A cloned settings instance.</returns>
        public StorageRoutingSettings Clone()
        {
            return new StorageRoutingSettings
            {
                MasterStorageType = MasterStorageType,
                GroupStorageTypes = new Dictionary<string, StorageType>(GroupStorageTypes, StringComparer.Ordinal),
                ItemStorageTypes = new Dictionary<string, StorageType>(ItemStorageTypes, StringComparer.Ordinal)
            };
        }

        /// <summary>
        /// Returns a normalized clone of the provided settings.
        /// </summary>
        /// <param name="settings">The input settings.</param>
        /// <returns>The normalized settings clone.</returns>
        public static StorageRoutingSettings Normalize(StorageRoutingSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var normalized = settings.Clone();

            if (!Enum.IsDefined(normalized.MasterStorageType))
            {
                normalized.MasterStorageType = StorageType.LocalStorage;
            }

            normalized.GroupStorageTypes = NormalizeOverrides(normalized.GroupStorageTypes);
            normalized.ItemStorageTypes = NormalizeOverrides(normalized.ItemStorageTypes);

            return normalized;
        }

        private static Dictionary<string, StorageType> NormalizeOverrides(IReadOnlyDictionary<string, StorageType>? source)
        {
            var normalized = new Dictionary<string, StorageType>(StringComparer.Ordinal);
            if (source is null)
            {
                return normalized;
            }

            foreach (var (key, value) in source)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (!Enum.IsDefined(value))
                {
                    continue;
                }

                normalized[key.Trim()] = value;
            }

            return normalized;
        }
    }
}
