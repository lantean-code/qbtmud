using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record TorrentMetadata
    {
        [JsonConstructor]
        public TorrentMetadata(
            string? infoHashV1,
            string? infoHashV2,
            string? hash,
            TorrentMetadataInfo? info,
            IReadOnlyList<TorrentMetadataTracker>? trackers,
            IReadOnlyList<string>? webSeeds,
            string? createdBy,
            long? creationDate,
            string? comment)
        {
            InfoHashV1 = infoHashV1;
            InfoHashV2 = infoHashV2;
            Hash = hash;
            Info = info;
            Trackers = trackers ?? Array.Empty<TorrentMetadataTracker>();
            WebSeeds = webSeeds ?? Array.Empty<string>();
            CreatedBy = createdBy;
            CreationDate = creationDate;
            Comment = comment;
        }

        [JsonPropertyName("infohash_v1")]
        public string? InfoHashV1 { get; }

        [JsonPropertyName("infohash_v2")]
        public string? InfoHashV2 { get; }

        [JsonPropertyName("hash")]
        public string? Hash { get; }

        [JsonPropertyName("info")]
        public TorrentMetadataInfo? Info { get; }

        [JsonPropertyName("trackers")]
        public IReadOnlyList<TorrentMetadataTracker> Trackers { get; }

        [JsonPropertyName("webseeds")]
        public IReadOnlyList<string> WebSeeds { get; }

        [JsonPropertyName("created_by")]
        public string? CreatedBy { get; }

        [JsonPropertyName("creation_date")]
        public long? CreationDate { get; }

        [JsonPropertyName("comment")]
        public string? Comment { get; }
    }

    public record TorrentMetadataInfo
    {
        [JsonConstructor]
        public TorrentMetadataInfo(
            IReadOnlyList<TorrentMetadataFile>? files,
            long? length,
            string? name,
            long? pieceLength,
            int? piecesCount,
            bool? @private)
        {
            Files = files ?? Array.Empty<TorrentMetadataFile>();
            Length = length;
            Name = name;
            PieceLength = pieceLength;
            PiecesCount = piecesCount;
            Private = @private;
        }

        [JsonPropertyName("files")]
        public IReadOnlyList<TorrentMetadataFile> Files { get; }

        [JsonPropertyName("length")]
        public long? Length { get; }

        [JsonPropertyName("name")]
        public string? Name { get; }

        [JsonPropertyName("piece_length")]
        public long? PieceLength { get; }

        [JsonPropertyName("pieces_num")]
        public int? PiecesCount { get; }

        [JsonPropertyName("private")]
        public bool? Private { get; }
    }

    public record TorrentMetadataFile(
        [property: JsonPropertyName("path")] string? Path,
        [property: JsonPropertyName("length")] long? Length);

    public record TorrentMetadataTracker(
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("tier")] int? Tier);
}
