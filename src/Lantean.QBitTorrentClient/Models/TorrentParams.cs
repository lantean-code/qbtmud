using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record TorrentParams
    {
        public TorrentParams()
        {
            Category = "";
            DownloadPath = "";
            OperatingMode = "";
            SavePath = "";
            Tags = [];
        }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("download_limit")]
        public int? DownloadLimit { get; set; }

        [JsonPropertyName("download_path")]
        public string DownloadPath { get; set; }

        [JsonPropertyName("inactive_seeding_time_limit")]
        public int? InactiveSeedingTimeLimit { get; set; }

        [JsonPropertyName("operating_mode")]
        public string OperatingMode { get; set; }

        [JsonPropertyName("ratio_limit")]
        public int? RatioLimit { get; set; }

        [JsonPropertyName("save_path")]
        public string SavePath { get; set; }

        [JsonPropertyName("seeding_time_limit")]
        public int? SeedingTimeLimit { get; set; }

        [JsonPropertyName("skip_checking")]
        public bool? SkipChecking { get; set; }

        [JsonPropertyName("stopped")]
        public bool? Stopped { get; set; }

        [JsonPropertyName("tags")]
        public IReadOnlyList<string> Tags { get; set; }

        [JsonPropertyName("upload_limit")]
        public int? UploadLimit { get; set; }

        [JsonPropertyName("use_auto_tmm")]
        public bool UseAutoTmm { get; set; }

        [JsonPropertyName("content_layout")]
        public string? ContentLayout { get; set; }
    }
}