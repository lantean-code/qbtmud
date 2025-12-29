using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record TorrentCreationTaskStatus
    {
        [JsonConstructor]
        public TorrentCreationTaskStatus(
            string taskID,
            string? sourcePath,
            int? pieceSize,
            bool? @private,
            string? timeAdded,
            string? format,
            bool? optimizeAlignment,
            int? paddedFileSizeLimit,
            string? status,
            string? comment,
            string? torrentFilePath,
            string? source,
            IReadOnlyList<string>? trackers,
            IReadOnlyList<string>? urlSeeds,
            string? timeStarted,
            string? timeFinished,
            string? errorMessage,
            double? progress)
        {
            TaskId = taskID;
            SourcePath = sourcePath;
            PieceSize = pieceSize;
            Private = @private;
            TimeAdded = timeAdded;
            Format = format;
            OptimizeAlignment = optimizeAlignment;
            PaddedFileSizeLimit = paddedFileSizeLimit;
            Status = status;
            Comment = comment;
            TorrentFilePath = torrentFilePath;
            Source = source;
            Trackers = trackers ?? Array.Empty<string>();
            UrlSeeds = urlSeeds ?? Array.Empty<string>();
            TimeStarted = timeStarted;
            TimeFinished = timeFinished;
            ErrorMessage = errorMessage;
            Progress = progress;
        }

        [JsonPropertyName("taskID")]
        public string TaskId { get; }

        [JsonPropertyName("sourcePath")]
        public string? SourcePath { get; }

        [JsonPropertyName("pieceSize")]
        public int? PieceSize { get; }

        [JsonPropertyName("private")]
        public bool? Private { get; }

        [JsonPropertyName("timeAdded")]
        public string? TimeAdded { get; }

        [JsonPropertyName("format")]
        public string? Format { get; }

        [JsonPropertyName("optimizeAlignment")]
        public bool? OptimizeAlignment { get; }

        [JsonPropertyName("paddedFileSizeLimit")]
        public int? PaddedFileSizeLimit { get; }

        [JsonPropertyName("status")]
        public string? Status { get; }

        [JsonPropertyName("comment")]
        public string? Comment { get; }

        [JsonPropertyName("torrentFilePath")]
        public string? TorrentFilePath { get; }

        [JsonPropertyName("source")]
        public string? Source { get; }

        [JsonPropertyName("trackers")]
        public IReadOnlyList<string> Trackers { get; }

        [JsonPropertyName("urlSeeds")]
        public IReadOnlyList<string> UrlSeeds { get; }

        [JsonPropertyName("timeStarted")]
        public string? TimeStarted { get; }

        [JsonPropertyName("timeFinished")]
        public string? TimeFinished { get; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; }

        [JsonPropertyName("progress")]
        public double? Progress { get; }
    }
}
