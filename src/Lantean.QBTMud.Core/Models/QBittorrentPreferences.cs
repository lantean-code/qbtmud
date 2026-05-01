using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Core.Models
{
    /// <summary>
    /// Represents the qBittorrent preferences used by the running application shell.
    /// </summary>
    public sealed record QBittorrentPreferences
    {
        /// <summary>
        /// Gets the selected WebUI locale.
        /// </summary>
        public string? Locale { get; init; }

        /// <summary>
        /// Gets a value indicating whether automatic torrent management is enabled by default.
        /// </summary>
        public bool AutoTmmEnabled { get; init; }

        /// <summary>
        /// Gets the default save path.
        /// </summary>
        public string? SavePath { get; init; }

        /// <summary>
        /// Gets the default temporary path.
        /// </summary>
        public string? TempPath { get; init; }

        /// <summary>
        /// Gets a value indicating whether the default temporary path is enabled.
        /// </summary>
        public bool TempPathEnabled { get; init; }

        /// <summary>
        /// Gets a value indicating whether new torrents should be added stopped by default.
        /// </summary>
        public bool AddStoppedEnabled { get; init; }

        /// <summary>
        /// Gets a value indicating whether new torrents should be added to the top of the queue by default.
        /// </summary>
        public bool AddToTopOfQueue { get; init; }

        /// <summary>
        /// Gets the default torrent stop condition.
        /// </summary>
        public StopCondition TorrentStopCondition { get; init; }

        /// <summary>
        /// Gets the default torrent content layout.
        /// </summary>
        public TorrentContentLayout TorrentContentLayout { get; init; }

        /// <summary>
        /// Gets a value indicating whether the default share ratio limit is enabled.
        /// </summary>
        public bool MaxRatioEnabled { get; init; }

        /// <summary>
        /// Gets the default share ratio limit.
        /// </summary>
        public double MaxRatio { get; init; }

        /// <summary>
        /// Gets a value indicating whether the default seeding time limit is enabled.
        /// </summary>
        public bool MaxSeedingTimeEnabled { get; init; }

        /// <summary>
        /// Gets the default seeding time limit.
        /// </summary>
        public int MaxSeedingTime { get; init; }

        /// <summary>
        /// Gets a value indicating whether the default inactive seeding time limit is enabled.
        /// </summary>
        public bool MaxInactiveSeedingTimeEnabled { get; init; }

        /// <summary>
        /// Gets the default inactive seeding time limit.
        /// </summary>
        public int MaxInactiveSeedingTime { get; init; }

        /// <summary>
        /// Gets a value indicating whether torrent queueing is enabled.
        /// </summary>
        public bool QueueingEnabled { get; init; }

        /// <summary>
        /// Gets a value indicating whether torrent deletion requires confirmation.
        /// </summary>
        public bool ConfirmTorrentDeletion { get; init; }

        /// <summary>
        /// Gets a value indicating whether deleting a torrent should delete content files by default.
        /// </summary>
        public bool DeleteTorrentContentFiles { get; init; }

        /// <summary>
        /// Gets a value indicating whether torrent recheck requires confirmation.
        /// </summary>
        public bool ConfirmTorrentRecheck { get; init; }

        /// <summary>
        /// Gets a value indicating whether the status bar should show the external IP address.
        /// </summary>
        public bool StatusBarExternalIp { get; init; }

        /// <summary>
        /// Gets a value indicating whether RSS processing is enabled.
        /// </summary>
        public bool RssProcessingEnabled { get; init; }

        /// <summary>
        /// Gets a value indicating whether category filters should include subcategories.
        /// </summary>
        public bool UseSubcategories { get; init; }

        /// <summary>
        /// Gets a value indicating whether peer countries should be resolved.
        /// </summary>
        public bool ResolvePeerCountries { get; init; }

        /// <summary>
        /// Gets the main data refresh interval in milliseconds.
        /// </summary>
        public int RefreshInterval { get; init; }
    }
}
