﻿namespace Lantean.QBitTorrentClient.Models
{
    public record AddTorrentParams
    {
        public IEnumerable<string>? Urls { get; set; }

        public bool? SkipChecking { get; set; }

        public bool? SequentialDownload { get; set; }

        public bool? FirstLastPiecePriority { get; set; }

        public bool? AddToTopOfQueue { get; set; }

        // v4
        public bool? Paused { get; set; }
        // v5
        public bool? Stopped { get; set; }

        public string? SavePath { get; set; }

        public string? DownloadPath { get; set; }

        public bool? UseDownloadPath { get; set; }

        public string? Category { get; set; }
        
        public IEnumerable<string>? Tags { get; set; }

        public string? RenameTorrent { get; set; }

        public long? UploadLimit { get; set; }
        
        public long? DownloadLimit { get; set; }

        public float? RatioLimit { get; set; }

        public int? SeedingTimeLimit { get; set; }

        public int? InactiveSeedingTimeLimit { get; set; }

        public ShareLimitAction? ShareLimitAction { get; set; }

        public bool? AutoTorrentManagement { get; set; }

        public StopCondition? StopCondition { get; set; }

        public TorrentContentLayout? ContentLayout { get; set; }

        public string? Cookie { get; set; }

        public Dictionary<string, Stream>? Torrents { get; set; }
    }
}