using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Core.Models
{
    public record TorrentOptions
    {
        public TorrentOptions(
            bool torrentManagementMode,
            string savePath,
            string? cookie,
            string? renameTorrent,
            string? category,
            bool startTorrent,
            bool addToTopOfQueue,
            StopCondition stopCondition,
            bool skipHashCheck,
            TorrentContentLayout contentLayout,
            bool downloadInSequentialOrder,
            bool downloadFirstAndLastPiecesFirst,
            int downloadLimit,
            int uploadLimit)
        {
            TorrentManagementMode = torrentManagementMode;
            SavePath = savePath;
            Cookie = cookie;
            RenameTorrent = renameTorrent;
            Category = category;
            StartTorrent = startTorrent;
            AddToTopOfQueue = addToTopOfQueue;
            StopCondition = stopCondition;
            SkipHashCheck = skipHashCheck;
            ContentLayout = contentLayout;
            DownloadInSequentialOrder = downloadInSequentialOrder;
            DownloadFirstAndLastPiecesFirst = downloadFirstAndLastPiecesFirst;
            DownloadLimit = downloadLimit;
            UploadLimit = uploadLimit;
        }

        public bool TorrentManagementMode { get; }

        public string SavePath { get; }

        public string? Cookie { get; }

        public string? RenameTorrent { get; }

        public string? Category { get; }

        public bool StartTorrent { get; }

        public bool AddToTopOfQueue { get; }

        public StopCondition StopCondition { get; }

        public bool SkipHashCheck { get; }

        public TorrentContentLayout ContentLayout { get; }

        public bool DownloadInSequentialOrder { get; }

        public bool DownloadFirstAndLastPiecesFirst { get; }

        public int DownloadLimit { get; }

        public int UploadLimit { get; }
        public string? DownloadPath { get; internal set; }
        public int? InactiveSeedingTimeLimit { get; internal set; }
        public double? RatioLimit { get; internal set; }
        public int? SeedingTimeLimit { get; internal set; }
        public ShareLimitAction? ShareLimitAction { get; internal set; }
        public bool? UseDownloadPath { get; internal set; }
        public IEnumerable<string>? Tags { get; internal set; }
    }
}
