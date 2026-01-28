namespace Lantean.QBTMud.Models
{
    internal sealed record TorrentCreationFormState
    {
        public string SourcePath { get; init; } = string.Empty;

        public string TorrentFilePath { get; init; } = string.Empty;

        public int? PieceSize { get; init; }

        public bool Private { get; init; }

        public bool StartSeeding { get; init; } = true;

        public string Trackers { get; init; } = string.Empty;

        public string UrlSeeds { get; init; } = string.Empty;

        public string Comment { get; init; } = string.Empty;

        public string Source { get; init; } = string.Empty;

        public string Format { get; init; } = "hybrid";

        public bool OptimizeAlignment { get; init; } = true;

        public int? PaddedFileSizeLimit { get; init; }
    }
}
