using System;
using System.Collections.Generic;
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
            string message,
            long? nextAnnounce,
            long? minAnnounce,
            IReadOnlyList<TrackerEndpoint>? endpoints)
        {
            Url = url;
            Status = status;
            Tier = tier;
            Peers = peers;
            Seeds = seeds;
            Leeches = leeches;
            Downloads = downloads;
            Message = message;
            NextAnnounce = nextAnnounce;
            MinAnnounce = minAnnounce;
            Endpoints = endpoints ?? Array.Empty<TrackerEndpoint>();
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

        [JsonPropertyName("next_announce")]
        public long? NextAnnounce { get; }

        [JsonPropertyName("min_announce")]
        public long? MinAnnounce { get; }

        [JsonPropertyName("endpoints")]
        public IReadOnlyList<TrackerEndpoint> Endpoints { get; }
    }

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
