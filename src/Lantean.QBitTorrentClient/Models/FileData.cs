using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record FileData
    {
        [JsonConstructor]
        public FileData(
            int index,
            string name,
            long size,
            float progress,
            Priority priority,
            bool isSeed,
            IReadOnlyList<int> pieceRange,
            float availability)
        {
            Index = index;
            Name = name;
            Size = size;
            Progress = progress;
            Priority = priority;
            IsSeed = isSeed;
            PieceRange = pieceRange ?? [];
            Availability = availability;
        }

        [JsonPropertyName("index")]
        public int Index { get; }

        [JsonPropertyName("name")]
        public string Name { get; }

        [JsonPropertyName("size")]
        public long Size { get; }

        [JsonPropertyName("progress")]
        public float Progress { get; }

        [JsonPropertyName("priority")]
        public Priority Priority { get; }

        [JsonPropertyName("is_seed")]
        public bool IsSeed { get; }

        [JsonPropertyName("piece_range")]
        public IReadOnlyList<int> PieceRange { get; }

        [JsonPropertyName("availability")]
        public float Availability { get; }
    }
}