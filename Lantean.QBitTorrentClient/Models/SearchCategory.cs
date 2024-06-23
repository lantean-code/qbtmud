using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record SearchCategory
    {
        [JsonConstructor]
        public SearchCategory(string id, string name)
        {
            Id = id;
            Name = name;
        }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}