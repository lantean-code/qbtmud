namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Describes the kind of query-state change that occurred.
    /// </summary>
    public enum TorrentQueryStateChangeKind
    {
        /// <summary>
        /// The filter or search state changed.
        /// </summary>
        Filter = 0,

        /// <summary>
        /// The sort state changed.
        /// </summary>
        Sort = 1
    }
}
