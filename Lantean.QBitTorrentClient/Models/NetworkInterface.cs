using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record NetworkInterface
    {
        [JsonConstructor]
        public NetworkInterface(
            string name,
            string value)
        {
            Name = name;
            Value = value;
        }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("value")]
        public string Value { get; }
    }
}