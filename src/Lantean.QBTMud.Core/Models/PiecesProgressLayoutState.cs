namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Retains piece-map layout information while torrent-specific content is replaced.
    /// </summary>
    public sealed record PiecesProgressLayoutState
    {
        /// <summary>
        /// Gets or sets the piece count used by the last rendered map.
        /// </summary>
        public int PieceCount { get; set; }
    }
}
