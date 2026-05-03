namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Specifies how a storage value should be interpreted when migrating between storage types.
    /// </summary>
    public enum StorageItemSerializationMode
    {
        /// <summary>
        /// Treats the stored value as a raw string.
        /// </summary>
        RawString = 0,

        /// <summary>
        /// Treats the stored value as JSON.
        /// </summary>
        Json = 1
    }
}
