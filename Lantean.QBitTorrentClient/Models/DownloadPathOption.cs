namespace Lantean.QBitTorrentClient.Models
{
    public record DownloadPathOption
    {
        public DownloadPathOption(bool enabled, string? path)
        {
            Enabled = enabled;
            Path = path;
        }

        public bool Enabled { get; }

        public string? Path { get; }
    }
}