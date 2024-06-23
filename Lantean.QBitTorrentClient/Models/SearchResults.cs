using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record SearchResults
    {
        [JsonConstructor]
        public SearchResults(IReadOnlyList<SearchResult> results, string status, int total)
        {
            Results = results;
            Status = status;
            Total = total;
        }

        [JsonPropertyName("results")]
        public IReadOnlyList<SearchResult> Results { get; }

        [JsonPropertyName("status")]
        public string Status { get; }

        [JsonPropertyName("total")]
        public int Total { get; }
    }
}