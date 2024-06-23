using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record SearchPlugin
    {
        [JsonConstructor]
        public SearchPlugin(
            bool enabled,
            string fullName,
            string name,
            IReadOnlyList<SearchCategory> supportedCategories,
            string url,
            string version)
        {
            Enabled = enabled;
            FullName = fullName;
            Name = name;
            SupportedCategories = supportedCategories;
            Url = url;
            Version = version;
        }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("fullName")]
        public string FullName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("supportedCategories")]
        public IReadOnlyList<SearchCategory> SupportedCategories { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }
}