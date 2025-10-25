using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record AutoDownloadingRule
    {
        public AutoDownloadingRule()
        {
            AffectedFeeds = [];
            AssignedCategory = "";
            EpisodeFilter = "";
            LastMatch = "";
            MustContain = "";
            MustNotContain = "";
            PreviouslyMatchedEpisodes = [];
            SavePath = "";
            TorrentParams = new();
        }

        [JsonPropertyName("addPaused")]
        public bool? AddPaused { get; set; }

        [JsonPropertyName("affectedFeeds")]
        public IReadOnlyList<string> AffectedFeeds { get; set; }

        [JsonPropertyName("assignedCategory")]
        public string AssignedCategory { get; set; }

        [JsonPropertyName("enabled")]
        public bool? Enabled { get; set; }

        [JsonPropertyName("episodeFilter")]
        public string EpisodeFilter { get; set; }

        [JsonPropertyName("ignoreDays")]
        public int? IgnoreDays { get; set; }

        [JsonPropertyName("lastMatch")]
        public string LastMatch { get; set; }

        [JsonPropertyName("mustContain")]
        public string MustContain { get; set; }

        [JsonPropertyName("mustNotContain")]
        public string MustNotContain { get; set; }

        [JsonPropertyName("previouslyMatchedEpisodes")]
        public IReadOnlyList<string> PreviouslyMatchedEpisodes { get; set; }

        [JsonPropertyName("priority")]
        public int? Priority { get; set; }

        [JsonPropertyName("savePath")]
        public string SavePath { get; set; }

        [JsonPropertyName("smartFilter")]
        public bool? SmartFilter { get; set; }

        [JsonPropertyName("torrentContentLayout")]
        public string? TorrentContentLayout { get; set; }

        [JsonPropertyName("torrentParams")]
        public TorrentParams TorrentParams { get; set; }

        [JsonPropertyName("useRegex")]
        public bool? UseRegex { get; set; }
    }
}