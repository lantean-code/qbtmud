namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents the persistence storage type used for qbtmud settings data.
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// Persists settings in browser local storage.
        /// </summary>
        LocalStorage = 0,

        /// <summary>
        /// Persists settings in qBittorrent client data.
        /// </summary>
        ClientData = 1
    }
}
