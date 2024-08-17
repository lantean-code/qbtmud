namespace Lantean.QBTMudBlade.Models
{
    public struct FilterState
    {
        public FilterState(string category, Status status, string tag, string tracker, bool useSubcategories, string? terms)
        {
            Category = category;
            Status = status;
            Tag = tag;
            Tracker = tracker;
            UseSubcategories = useSubcategories;
            Terms = terms;
        }

        public string Category { get; } = "all";
        public Status Status { get; } = Status.All;
        public string Tag { get; } = "all";
        public string Tracker { get; } = "all";
        public bool UseSubcategories { get; }
        public string? Terms { get; }
    }
}