namespace Lantean.QBitTorrentClient.Models
{
    public class TorrentCreationTaskRequest
    {
        public string SourcePath { get; set; } = string.Empty;

        public string? TorrentFilePath { get; set; }

        public int? PieceSize { get; set; }

        public bool? Private { get; set; }

        public bool? StartSeeding { get; set; }

        public string? Comment { get; set; }

        public string? Source { get; set; }

        public IEnumerable<string>? Trackers { get; set; }

        public IEnumerable<string>? UrlSeeds { get; set; }

        public string? Format { get; set; }

        public bool? OptimizeAlignment { get; set; }

        public int? PaddedFileSizeLimit { get; set; }
    }
}
