using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record Log
    {
        [JsonConstructor]
        public Log(
            int id,
            string message,
            long timestamp,
            LogType type)
        {
            Id = id;
            Message = message;
            Timestamp = timestamp;
            Type = type;
        }

        [JsonPropertyName("id")]
        public int Id { get; }

        [JsonPropertyName("message")]
        public string Message { get; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; }

        [JsonPropertyName("type")]
        public LogType Type { get; }
    }
}