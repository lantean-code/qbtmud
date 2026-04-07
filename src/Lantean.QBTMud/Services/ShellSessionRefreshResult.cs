using Lantean.QBTMud.Models;
using MudMainData = Lantean.QBTMud.Models.MainData;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Represents the result of a shell session refresh attempt.
    /// </summary>
    public sealed record ShellSessionRefreshResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShellSessionRefreshResult"/> class.
        /// </summary>
        /// <param name="outcome">The refresh outcome.</param>
        /// <param name="mainData">The resulting main-data snapshot.</param>
        /// <param name="requestId">The next qBittorrent request identifier.</param>
        /// <param name="shouldRender">A value indicating whether the caller should request a render.</param>
        /// <param name="torrentsDirty">A value indicating whether the torrent filter output should be recomputed.</param>
        public ShellSessionRefreshResult(
            ShellSessionRefreshOutcome outcome,
            MudMainData? mainData = null,
            int requestId = 0,
            bool shouldRender = false,
            bool torrentsDirty = false)
        {
            Outcome = outcome;
            MainData = mainData;
            RequestId = requestId;
            ShouldRender = shouldRender;
            TorrentsDirty = torrentsDirty;
        }

        /// <summary>
        /// Gets the refresh outcome.
        /// </summary>
        public ShellSessionRefreshOutcome Outcome { get; init; }

        /// <summary>
        /// Gets the resulting main-data snapshot.
        /// </summary>
        public MudMainData? MainData { get; init; }

        /// <summary>
        /// Gets the next qBittorrent request identifier.
        /// </summary>
        public int RequestId { get; init; }

        /// <summary>
        /// Gets a value indicating whether the caller should request a render.
        /// </summary>
        public bool ShouldRender { get; init; }

        /// <summary>
        /// Gets a value indicating whether the torrent filter output should be recomputed.
        /// </summary>
        public bool TorrentsDirty { get; init; }
    }
}
