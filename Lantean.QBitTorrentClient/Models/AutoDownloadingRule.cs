using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record AutoDownloadingRule
    {
        [JsonConstructor]
        public AutoDownloadingRule(
            bool enabled,
            string mustContain,
            string mustNotContain,
            bool useRegex,
            string episodeFilter,
            bool smartFilter,
            IEnumerable<string> previouslyMatchedEpisodes,
            IEnumerable<string> affectedFeeds,
            int ignoreDays,
            string lastMatch,
            bool addPaused,
            string assignedCategory,
            string savePath)
        {
            Enabled = enabled;
            MustContain = mustContain;
            MustNotContain = mustNotContain;
            UseRegex = useRegex;
            EpisodeFilter = episodeFilter;
            SmartFilter = smartFilter;
            PreviouslyMatchedEpisodes = previouslyMatchedEpisodes;
            AffectedFeeds = affectedFeeds;
            IgnoreDays = ignoreDays;
            LastMatch = lastMatch;
            AddPaused = addPaused;
            AssignedCategory = assignedCategory;
            SavePath = savePath;
        }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; }

        [JsonPropertyName("mustContain")]
        public string MustContain { get; }

        [JsonPropertyName("mustNotContain")]
        public string MustNotContain { get; }

        [JsonPropertyName("useRegex")]
        public bool UseRegex { get; }

        [JsonPropertyName("episodeFilter")]
        public string EpisodeFilter { get; }

        [JsonPropertyName("smartFilter")]
        public bool SmartFilter { get; }

        [JsonPropertyName("previouslyMatchedEpisodes")]
        public IEnumerable<string> PreviouslyMatchedEpisodes { get; }

        [JsonPropertyName("affectedFeeds")]
        public IEnumerable<string> AffectedFeeds { get; }

        [JsonPropertyName("ignoreDays")]
        public int IgnoreDays { get; }

        [JsonPropertyName("lastMatch")]
        public string LastMatch { get; }

        [JsonPropertyName("addPaused")]
        public bool AddPaused { get; }

        [JsonPropertyName("assignedCategory")]
        public string AssignedCategory { get; }

        [JsonPropertyName("savePath")]
        public string SavePath { get; }
    }
}
