using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record TorrentTracker
    {
        [JsonConstructor]
        public TorrentTracker(
            string url,
            TrackerStatus status,
            int tier,
            int peers,
            int seeds,
            int leeches,
            int downloads,
            string message)
        {
            Url = url;
            Status = status;
            Tier = tier;
            Peers = peers;
            Seeds = seeds;
            Leeches = leeches;
            Downloads = downloads;
            Message = message;
        }

        [JsonPropertyName("url")]
        public string Url { get; }

        [JsonPropertyName("status")]
        public TrackerStatus Status { get; }

        [JsonPropertyName("tier")]
        public int Tier { get; }

        [JsonPropertyName("num_peers")]
        public int Peers { get; }

        [JsonPropertyName("num_seeds")]
        public int Seeds { get; }

        [JsonPropertyName("num_leeches")]
        public int Leeches { get; }

        [JsonPropertyName("num_downloaded")]
        public int Downloads { get; }

        [JsonPropertyName("msg")]
        public string Message { get; }
    }
}