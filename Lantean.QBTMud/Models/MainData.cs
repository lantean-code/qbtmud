namespace Lantean.QBTMud.Models
{
    public record MainData
    {
        public MainData(
            IDictionary<string, Torrent> torrents,
            IEnumerable<string> tags,
            IDictionary<string, Category> categories,
            IDictionary<string, IReadOnlyList<string>> trackers,
            ServerState serverState,
            Dictionary<string, HashSet<string>> tagState,
            Dictionary<string, HashSet<string>> categoriesState,
            Dictionary<string, HashSet<string>> statusState,
            Dictionary<string, HashSet<string>> trackersState)
        {
            Torrents = torrents.ToDictionary();
            Tags = tags.ToHashSet();
            Categories = categories.ToDictionary();
            Trackers = trackers.ToDictionary();
            ServerState = serverState;
            TagState = tagState;
            CategoriesState = categoriesState;
            StatusState = statusState;
            TrackersState = trackersState;
        }

        public Dictionary<string, Torrent> Torrents { get; }
        public HashSet<string> Tags { get; }
        public Dictionary<string, Category> Categories { get; }
        public Dictionary<string, IReadOnlyList<string>> Trackers { get; }
        public ServerState ServerState { get; }

        public Dictionary<string, HashSet<string>> TagState { get; }
        public Dictionary<string, HashSet<string>> CategoriesState { get; }
        public Dictionary<string, HashSet<string>> StatusState { get; }
        public Dictionary<string, HashSet<string>> TrackersState { get; }
        public string? SelectedTorrentHash { get; set; }
        public bool LostConnection { get; set; }
    }
}