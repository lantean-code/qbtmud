using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record PeerLog
    {
        [JsonConstructor]
        public PeerLog(
            int id,
            string iPAddress,
            long timestamp,
            bool blocked,
            string reason)
        {
            Id = id;
            IPAddress = iPAddress;
            Timestamp = timestamp;
            Blocked = blocked;
            Reason = reason;
        }

        [JsonPropertyName("id")]
        public int Id { get; }

        [JsonPropertyName("ip")]
        public string IPAddress { get; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; }

        [JsonPropertyName("blocked")]
        public bool Blocked { get; }

        [JsonPropertyName("reason")]
        public string Reason { get; }
    }
}