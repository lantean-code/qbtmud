using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record SearchStatus
    {
        [JsonConstructor]
        public SearchStatus(int id, string status, int total)
        {
            Id = id;
            Status = status;
            Total = total;
        }

        [JsonPropertyName("id")]
        public int Id { get; }

        [JsonPropertyName("status")]
        public string Status { get; }

        [JsonPropertyName("total")]
        public int Total { get; }
    }
}