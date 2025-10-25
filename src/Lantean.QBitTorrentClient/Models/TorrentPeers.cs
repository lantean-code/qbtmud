using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record TorrentPeers
    {
        [JsonConstructor]
        public TorrentPeers(
            bool fullUpdate,
            IReadOnlyDictionary<string, Peer>? peers,
            IReadOnlyList<string>? peersRemoved,
            int requestId,
            bool? showFlags)
        {
            FullUpdate = fullUpdate;
            Peers = peers;
            PeersRemoved = peersRemoved;
            RequestId = requestId;
            ShowFlags = showFlags;
        }

        [JsonPropertyName("full_update")]
        public bool FullUpdate { get; }

        [JsonPropertyName("peers")]
        public IReadOnlyDictionary<string, Peer>? Peers { get; }

        [JsonPropertyName("peers_removed")]
        public IReadOnlyList<string>? PeersRemoved { get; }

        [JsonPropertyName("rid")]
        public int RequestId { get; }

        [JsonPropertyName("show_flags")]
        public bool? ShowFlags { get; }
    }
}