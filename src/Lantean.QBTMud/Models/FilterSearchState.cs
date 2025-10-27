namespace Lantean.QBTMud.Models
{
    public readonly struct FilterSearchState
    {
        public FilterSearchState(string? text, TorrentFilterField field, bool useRegex, bool isRegexValid)
        {
            Text = text;
            Field = field;
            UseRegex = useRegex;
            IsRegexValid = isRegexValid;
        }

        public string? Text { get; }

        public TorrentFilterField Field { get; }

        public bool UseRegex { get; }

        public bool IsRegexValid { get; }
    }
}
