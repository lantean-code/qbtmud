using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record ApplicationCookie
    {
        [JsonConstructor]
        public ApplicationCookie(string name, string? domain, string? path, string? value, long? expirationDate)
        {
            Name = name;
            Domain = domain;
            Path = path;
            Value = value;
            ExpirationDate = expirationDate;
        }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("domain")]
        public string? Domain { get; }

        [JsonPropertyName("path")]
        public string? Path { get; }

        [JsonPropertyName("value")]
        public string? Value { get; }

        [JsonPropertyName("expirationDate")]
        public long? ExpirationDate { get; }
    }
}