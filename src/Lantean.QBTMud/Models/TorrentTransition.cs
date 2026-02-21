namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents a torrent state transition between two sync cycles.
    /// </summary>
    public sealed record TorrentTransition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TorrentTransition"/> class.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        /// <param name="name">The torrent display name.</param>
        /// <param name="isAdded">Indicates whether this transition represents a newly-added torrent.</param>
        /// <param name="previousIsFinished">The previous finished flag.</param>
        /// <param name="currentIsFinished">The current finished flag.</param>
        public TorrentTransition(string hash, string name, bool isAdded, bool previousIsFinished, bool currentIsFinished)
        {
            Hash = hash;
            Name = name;
            IsAdded = isAdded;
            PreviousIsFinished = previousIsFinished;
            CurrentIsFinished = currentIsFinished;
        }

        /// <summary>
        /// Gets the torrent hash.
        /// </summary>
        public string Hash { get; init; }

        /// <summary>
        /// Gets the torrent display name.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Gets a value indicating whether this transition represents a newly-added torrent.
        /// </summary>
        public bool IsAdded { get; init; }

        /// <summary>
        /// Gets the previous finished flag.
        /// </summary>
        public bool PreviousIsFinished { get; init; }

        /// <summary>
        /// Gets the current finished flag.
        /// </summary>
        public bool CurrentIsFinished { get; init; }
    }
}
