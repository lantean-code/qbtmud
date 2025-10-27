namespace Lantean.QBTMud.Models
{
    public readonly struct FilterState
    {
        public FilterState(
            string category,
            Status status,
            string tag,
            string tracker,
            bool useSubcategories,
            string? terms,
            TorrentFilterField filterField,
            bool useRegex,
            bool isRegexValid)
        {
            Category = category;
            Status = status;
            Tag = tag;
            Tracker = tracker;
            UseSubcategories = useSubcategories;
            Terms = terms;
            FilterField = filterField;
            UseRegex = useRegex;
            IsRegexValid = isRegexValid;
        }

        public string Category { get; } = "all";
        public Status Status { get; } = Status.All;
        public string Tag { get; } = "all";
        public string Tracker { get; } = "all";
        public bool UseSubcategories { get; }
        public string? Terms { get; }
        public TorrentFilterField FilterField { get; } = TorrentFilterField.Name;
        public bool UseRegex { get; }
        public bool IsRegexValid { get; } = true;
    }
}
