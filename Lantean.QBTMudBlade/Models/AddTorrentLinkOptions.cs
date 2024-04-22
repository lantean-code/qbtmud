namespace Lantean.QBTMudBlade.Models
{
    public record AddTorrentLinkOptions : TorrentOptions
    {
        public AddTorrentLinkOptions(string urls, TorrentOptions options) : base(options)
        {
            Urls = urls.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }

        public IReadOnlyList<string> Urls { get; }
    }
}