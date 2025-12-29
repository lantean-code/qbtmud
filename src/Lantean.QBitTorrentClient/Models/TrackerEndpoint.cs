using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record TrackerEndpoint(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("updating")] bool? Updating,
        [property: JsonPropertyName("status")] TrackerStatus Status,
        [property: JsonPropertyName("msg")] string? Message,
        [property: JsonPropertyName("bt_version")] int? BitTorrentVersion,
        [property: JsonPropertyName("num_peers")] int? Peers,
        [property: JsonPropertyName("num_seeds")] int? Seeds,
        [property: JsonPropertyName("num_leeches")] int? Leeches,
        [property: JsonPropertyName("num_downloaded")] int? Downloads,
        [property: JsonPropertyName("next_announce")] long? NextAnnounce,
        [property: JsonPropertyName("min_announce")] long? MinAnnounce);
}
