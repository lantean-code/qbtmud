namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Specifies how a storage catalog item matches runtime storage keys.
    /// </summary>
    public enum StorageCatalogItemMatchMode
    {
        /// <summary>
        /// Matches only an exact storage key.
        /// </summary>
        ExactKey = 0,

        /// <summary>
        /// Matches storage keys by prefix.
        /// </summary>
        PrefixPattern = 1
    }
}
