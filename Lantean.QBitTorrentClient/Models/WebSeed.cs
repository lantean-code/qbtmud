using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record WebSeed
    {
        [JsonConstructor]
        public WebSeed(string url)
        {
            Url = url;
        }

        [JsonPropertyName("url")]
        public string Url { get; }
    }
}