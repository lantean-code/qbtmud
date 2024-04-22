using Lantean.QBitTorrentClient.Converters;
using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record Torrent
    {
        [JsonConstructor]
        public Torrent(
            long? addedOn,
            long? amountLeft,
            bool? automaticTorrentManagement,
            float? availability,
            string? category,
            long? completed,
            long? completionOn,
            string? contentPath,
            long? downloadLimit,
            long? downloadSpeed,
            long? downloaded,
            long? downloadedSession,
            long? estimatedTimeOfArrival,
            bool? firstLastPiecePriority,
            bool? forceStart,
            string? infoHashV1,
            string? infoHashV2,
            long? lastActivity,
            string? magnetUri,
            float? maxRatio,
            int? maxSeedingTime,
            string? name,
            int? numberComplete,
            int? numberIncomplete,
            int? numberLeeches,
            int? numberSeeds,
            int? priority,
            float? progress,
            float? ratio,
            float? ratioLimit,
            string? savePath,
            long? seedingTime,
            int? seedingTimeLimit,
            long? seenComplete,
            bool? sequentialDownload,
            long? size,
            string? state,
            bool? superSeeding,
            IReadOnlyList<string>? tags,
            int? timeActive,
            long? totalSize,
            string? tracker,
            long? uploadLimit,
            long? uploaded,
            long? uploadedSession,
            long? uploadSpeed,
            long? reannounce)
        {
            AddedOn = addedOn;
            AmountLeft = amountLeft;
            AutomaticTorrentManagement = automaticTorrentManagement;
            Availability = availability;
            Category = category;
            Completed = completed;
            CompletionOn = completionOn;
            ContentPath = contentPath;
            DownloadLimit = downloadLimit;
            DownloadSpeed = downloadSpeed;
            Downloaded = downloaded;
            DownloadedSession = downloadedSession;
            EstimatedTimeOfArrival = estimatedTimeOfArrival;
            FirstLastPiecePriority = firstLastPiecePriority;
            ForceStart = forceStart;
            InfoHashV1 = infoHashV1;
            InfoHashV2 = infoHashV2;
            LastActivity = lastActivity;
            MagnetUri = magnetUri;
            MaxRatio = maxRatio;
            MaxSeedingTime = maxSeedingTime;
            Name = name;
            NumberComplete = numberComplete;
            NumberIncomplete = numberIncomplete;
            NumberLeeches = numberLeeches;
            NumberSeeds = numberSeeds;
            Priority = priority;
            Progress = progress;
            Ratio = ratio;
            RatioLimit = ratioLimit;
            SavePath = savePath;
            SeedingTime = seedingTime;
            SeedingTimeLimit = seedingTimeLimit;
            SeenComplete = seenComplete;
            SequentialDownload = sequentialDownload;
            Size = size;
            State = state;
            SuperSeeding = superSeeding;
            Tags = tags ?? [];
            TimeActive = timeActive;
            TotalSize = totalSize;
            Tracker = tracker;
            UploadLimit = uploadLimit;
            Uploaded = uploaded;
            UploadedSession = uploadedSession;
            UploadSpeed = uploadSpeed;
            Reannounce = reannounce;
        }

        [JsonPropertyName("added_on")]
        public long? AddedOn { get; }

        [JsonPropertyName("amount_left")]
        public long? AmountLeft { get; }

        [JsonPropertyName("auto_tmm")]
        public bool? AutomaticTorrentManagement { get; }

        [JsonPropertyName("availability")]
        public float? Availability { get; }

        [JsonPropertyName("category")]
        public string? Category { get; }

        [JsonPropertyName("completed")]
        public long? Completed { get; }

        [JsonPropertyName("completion_on")]
        public long? CompletionOn { get; }

        [JsonPropertyName("content_path")]
        public string? ContentPath { get; }

        [JsonPropertyName("dl_limit")]
        public long? DownloadLimit { get; }

        [JsonPropertyName("dlspeed")]
        public long? DownloadSpeed { get; }

        [JsonPropertyName("downloaded")]
        public long? Downloaded { get; }

        [JsonPropertyName("downloaded_session")]
        public long? DownloadedSession { get; }

        [JsonPropertyName("eta")]
        public long? EstimatedTimeOfArrival { get; }

        [JsonPropertyName("f_l_piece_prio")]
        public bool? FirstLastPiecePriority { get; }

        [JsonPropertyName("force_start")]
        public bool? ForceStart { get; }

        [JsonPropertyName("infohash_v1")]
        public string? InfoHashV1 { get; }

        [JsonPropertyName("infohash_v2")]
        public string? InfoHashV2 { get; }

        [JsonPropertyName("last_activity")]
        public long? LastActivity { get; }

        [JsonPropertyName("magnet_uri")]
        public string? MagnetUri { get; }

        [JsonPropertyName("max_ratio")]
        public float? MaxRatio { get; }

        [JsonPropertyName("max_seeding_time")]
        public int? MaxSeedingTime { get; }

        [JsonPropertyName("name")]
        public string? Name { get; }

        [JsonPropertyName("num_complete")]
        public int? NumberComplete { get; }

        [JsonPropertyName("num_incomplete")]
        public int? NumberIncomplete { get; }

        [JsonPropertyName("num_leechs")]
        public int? NumberLeeches { get; }

        [JsonPropertyName("num_seeds")]
        public int? NumberSeeds { get; }

        [JsonPropertyName("priority")]
        public int? Priority { get; }

        [JsonPropertyName("progress")]
        public float? Progress { get; }

        [JsonPropertyName("ratio")]
        public float? Ratio { get; }

        [JsonPropertyName("ratio_limit")]
        public float? RatioLimit { get; }

        [JsonPropertyName("save_path")]
        public string? SavePath { get; }

        [JsonPropertyName("seeding_time")]
        public long? SeedingTime { get; }

        [JsonPropertyName("seeding_time_limit")]
        public int? SeedingTimeLimit { get; }

        [JsonPropertyName("seen_complete")]
        public long? SeenComplete { get; }

        [JsonPropertyName("seq_dl")]
        public bool? SequentialDownload { get; }

        [JsonPropertyName("size")]
        public long? Size { get; }

        [JsonPropertyName("state")]
        public string? State { get; }

        [JsonPropertyName("super_seeding")]
        public bool? SuperSeeding { get; }

        [JsonPropertyName("tags")]
        [JsonConverter(typeof(CommaSeparatedJsonConverter))]
        public IReadOnlyList<string>? Tags { get; }

        [JsonPropertyName("time_active")]
        public int? TimeActive { get; }

        [JsonPropertyName("total_size")]
        public long? TotalSize { get; }

        [JsonPropertyName("tracker")]
        public string? Tracker { get; }

        [JsonPropertyName("up_limit")]
        public long? UploadLimit { get; }

        [JsonPropertyName("uploaded")]
        public long? Uploaded { get; }

        [JsonPropertyName("uploaded_session")]
        public long? UploadedSession { get; }

        [JsonPropertyName("upspeed")]
        public long? UploadSpeed { get; }

        [JsonPropertyName("reannounce")]
        public long? Reannounce { get; }
    }
}