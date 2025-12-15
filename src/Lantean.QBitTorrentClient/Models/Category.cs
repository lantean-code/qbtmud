using Lantean.QBitTorrentClient.Converters;
using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record Category
    {
        [JsonConstructor]
        public Category(
            string name,
            string? savePath,
            DownloadPathOption? downloadPath)
        {
            Name = name;
            SavePath = savePath;
            DownloadPath = downloadPath;
        }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("savePath")]
        public string? SavePath { get; }

        [JsonPropertyName("download_path")]
        [JsonConverter(typeof(DownloadPathOptionJsonConverter))]
        public DownloadPathOption? DownloadPath { get; }
    }
}