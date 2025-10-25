using Lantean.QBitTorrentClient.Converters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record Torrent
    {
        [JsonPropertyName("hash")]
        public string Hash { get; init; } = string.Empty;

        [JsonPropertyName("infohash_v1")]
        public string? InfoHashV1 { get; init; }

        [JsonPropertyName("infohash_v2")]
        public string? InfoHashV2 { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("magnet_uri")]
        public string? MagnetUri { get; init; }

        [JsonPropertyName("size")]
        public long? Size { get; init; }

        [JsonPropertyName("progress")]
        public float? Progress { get; init; }

        [JsonPropertyName("dlspeed")]
        public long? DownloadSpeed { get; init; }

        [JsonPropertyName("upspeed")]
        public long? UploadSpeed { get; init; }

        [JsonPropertyName("priority")]
        public int? Priority { get; init; }

        [JsonPropertyName("num_seeds")]
        public int? NumberSeeds { get; init; }

        [JsonPropertyName("num_complete")]
        public int? NumberComplete { get; init; }

        [JsonPropertyName("num_leechs")]
        public int? NumberLeeches { get; init; }

        [JsonPropertyName("num_incomplete")]
        public int? NumberIncomplete { get; init; }

        [JsonPropertyName("ratio")]
        public float? Ratio { get; init; }

        [JsonPropertyName("popularity")]
        public float? Popularity { get; init; }

        [JsonPropertyName("eta")]
        public long? EstimatedTimeOfArrival { get; init; }

        [JsonPropertyName("state")]
        public string? State { get; init; }

        [JsonPropertyName("seq_dl")]
        public bool? SequentialDownload { get; init; }

        [JsonPropertyName("f_l_piece_prio")]
        public bool? FirstLastPiecePriority { get; init; }

        [JsonPropertyName("category")]
        public string? Category { get; init; }

        [JsonPropertyName("tags")]
        [JsonConverter(typeof(CommaSeparatedJsonConverter))]
        public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

        [JsonPropertyName("super_seeding")]
        public bool? SuperSeeding { get; init; }

        [JsonPropertyName("force_start")]
        public bool? ForceStart { get; init; }

        [JsonPropertyName("save_path")]
        public string? SavePath { get; init; }

        [JsonPropertyName("download_path")]
        public string? DownloadPath { get; init; }

        [JsonPropertyName("content_path")]
        public string? ContentPath { get; init; }

        [JsonPropertyName("root_path")]
        public string? RootPath { get; init; }

        [JsonPropertyName("added_on")]
        public long? AddedOn { get; init; }

        [JsonPropertyName("completion_on")]
        public long? CompletionOn { get; init; }

        [JsonPropertyName("tracker")]
        public string? Tracker { get; init; }

        [JsonPropertyName("trackers_count")]
        public int? TrackersCount { get; init; }

        [JsonPropertyName("dl_limit")]
        public long? DownloadLimit { get; init; }

        [JsonPropertyName("up_limit")]
        public long? UploadLimit { get; init; }

        [JsonPropertyName("downloaded")]
        public long? Downloaded { get; init; }

        [JsonPropertyName("uploaded")]
        public long? Uploaded { get; init; }

        [JsonPropertyName("downloaded_session")]
        public long? DownloadedSession { get; init; }

        [JsonPropertyName("uploaded_session")]
        public long? UploadedSession { get; init; }

        [JsonPropertyName("amount_left")]
        public long? AmountLeft { get; init; }

        [JsonPropertyName("completed")]
        public long? Completed { get; init; }

        [JsonPropertyName("connections_count")]
        public int? ConnectionsCount { get; init; }

        [JsonPropertyName("connections_limit")]
        public int? ConnectionsLimit { get; init; }

        [JsonPropertyName("max_ratio")]
        public float? MaxRatio { get; init; }

        [JsonPropertyName("max_seeding_time")]
        public int? MaxSeedingTime { get; init; }

        [JsonPropertyName("max_inactive_seeding_time")]
        public float? MaxInactiveSeedingTime { get; init; }

        [JsonPropertyName("ratio_limit")]
        public float? RatioLimit { get; init; }

        [JsonPropertyName("seeding_time_limit")]
        public int? SeedingTimeLimit { get; init; }

        [JsonPropertyName("inactive_seeding_time_limit")]
        public float? InactiveSeedingTimeLimit { get; init; }

        [JsonPropertyName("share_limit_action")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ShareLimitAction? ShareLimitAction { get; init; }

        [JsonPropertyName("seen_complete")]
        public long? SeenComplete { get; init; }

        [JsonPropertyName("last_activity")]
        public long? LastActivity { get; init; }

        [JsonPropertyName("total_size")]
        public long? TotalSize { get; init; }

        [JsonPropertyName("auto_tmm")]
        public bool? AutomaticTorrentManagement { get; init; }

        [JsonPropertyName("time_active")]
        public int? TimeActive { get; init; }

        [JsonPropertyName("seeding_time")]
        public long? SeedingTime { get; init; }

        [JsonPropertyName("availability")]
        public float? Availability { get; init; }

        [JsonPropertyName("reannounce")]
        public long? Reannounce { get; init; }

        [JsonPropertyName("comment")]
        public string? Comment { get; init; }

        [JsonPropertyName("has_metadata")]
        public bool? HasMetadata { get; init; }

        [JsonPropertyName("created_by")]
        public string? CreatedBy { get; init; }

        [JsonPropertyName("creation_date")]
        public long? CreationDate { get; init; }

        [JsonPropertyName("private")]
        public bool? IsPrivate { get; init; }

        [JsonPropertyName("total_wasted")]
        public long? TotalWasted { get; init; }

        [JsonPropertyName("pieces_num")]
        public int? PiecesCount { get; init; }

        [JsonPropertyName("piece_size")]
        public long? PieceSize { get; init; }

        [JsonPropertyName("pieces_have")]
        public int? PiecesHave { get; init; }

        [JsonPropertyName("has_tracker_warning")]
        public bool? HasTrackerWarning { get; init; }

        [JsonPropertyName("has_tracker_error")]
        public bool? HasTrackerError { get; init; }

        [JsonPropertyName("has_other_announce_error")]
        public bool? HasOtherAnnounceError { get; init; }
    }
}
