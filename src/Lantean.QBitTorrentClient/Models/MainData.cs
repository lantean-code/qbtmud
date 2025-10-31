using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record MainData
    {
        [JsonConstructor]
        public MainData(
            int responseId,
            bool fullUpdate,
            IReadOnlyDictionary<string, Torrent>? torrents,
            IReadOnlyList<string>? torrentsRemoved,
            IReadOnlyDictionary<string, Category>? categories,
            IReadOnlyList<string>? categoriesRemoved,
            IReadOnlyList<string>? tags,
            IReadOnlyList<string>? tagsRemoved,
            IReadOnlyDictionary<string, IReadOnlyList<string>>? trackers,
            IReadOnlyList<string>? trackersRemoved,
            ServerState? serverState)
        {
            ResponseId = responseId;
            FullUpdate = fullUpdate;
            Torrents = torrents;
            TorrentsRemoved = torrentsRemoved;
            Categories = categories;
            CategoriesRemoved = categoriesRemoved;
            Tags = tags;
            TagsRemoved = tagsRemoved;
            Trackers = trackers;
            TrackersRemoved = trackersRemoved;
            ServerState = serverState;
        }

        [JsonPropertyName("rid")]
        public int ResponseId { get; }

        [JsonPropertyName("full_update")]
        public bool FullUpdate { get; }

        [JsonPropertyName("torrents")]
        public IReadOnlyDictionary<string, Torrent>? Torrents { get; }

        [JsonPropertyName("torrents_removed")]
        public IReadOnlyList<string>? TorrentsRemoved { get; }

        [JsonPropertyName("categories")]
        public IReadOnlyDictionary<string, Category>? Categories { get; }

        [JsonPropertyName("categories_removed")]
        public IReadOnlyList<string>? CategoriesRemoved { get; }

        [JsonPropertyName("tags")]
        public IReadOnlyList<string>? Tags { get; }

        [JsonPropertyName("tags_removed")]
        public IReadOnlyList<string>? TagsRemoved { get; }

        [JsonPropertyName("trackers")]
        public IReadOnlyDictionary<string, IReadOnlyList<string>>? Trackers { get; }

        [JsonPropertyName("trackers_removed")]
        public IReadOnlyList<string>? TrackersRemoved { get; }

        [JsonPropertyName("server_state")]
        public ServerState? ServerState { get; }
    }
}