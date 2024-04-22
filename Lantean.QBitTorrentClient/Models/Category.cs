using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record Category
    {
        [JsonConstructor]
        public Category(
            string name,
            string? savePath)
        {
            Name = name;
            SavePath = savePath;
        }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("savePath")]
        public string? SavePath { get; }
    }
}