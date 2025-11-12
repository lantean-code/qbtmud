using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record TorrentProperties
    {
        [JsonConstructor]
        public TorrentProperties(
            long additionDate,
            string comment,
            long completionDate,
            string createdBy,
            long creationDate,
            long downloadLimit,
            long downloadSpeed,
            long downloadSpeedAverage,
            long estimatedTimeOfArrival,
            long lastSeen,
            int connections,
            int connectionsLimit,
            int peers,
            int peersTotal,
            int pieceSize,
            int piecesHave,
            int piecesNum,
            int reannounce,
            string savePath,
            int seedingTime,
            int seeds,
            int seedsTotal,
            float shareRatio,
            int timeElapsed,
            long totalDownloaded,
            long totalDownloadedSession,
            long totalSize,
            long totalUploaded,
            long totalUploadedSession,
            long totalWasted,
            long uploadLimit,
            long uploadSpeed,
            long uploadSpeedAverage,
            string infoHashV1,
            string infoHashV2,
            string? hash = null,
            string? name = null,
            string? downloadPath = null,
            float? popularity = null,
            float? progress = null,
            bool? isPrivate = null,
            bool? @private = null,
            bool? hasMetadata = null)
        {
            AdditionDate = additionDate;
            Comment = comment;
            CompletionDate = completionDate;
            CreatedBy = createdBy;
            CreationDate = creationDate;
            DownloadLimit = downloadLimit;
            DownloadSpeed = downloadSpeed;
            DownloadSpeedAverage = downloadSpeedAverage;
            EstimatedTimeOfArrival = estimatedTimeOfArrival;
            LastSeen = lastSeen;
            Connections = connections;
            ConnectionsLimit = connectionsLimit;
            Peers = peers;
            PeersTotal = peersTotal;
            PieceSize = pieceSize;
            PiecesHave = piecesHave;
            PiecesNum = piecesNum;
            Reannounce = reannounce;
            SavePath = savePath;
            SeedingTime = seedingTime;
            Seeds = seeds;
            SeedsTotal = seedsTotal;
            ShareRatio = shareRatio;
            TimeElapsed = timeElapsed;
            TotalDownloaded = totalDownloaded;
            TotalDownloadedSession = totalDownloadedSession;
            TotalSize = totalSize;
            TotalUploaded = totalUploaded;
            TotalUploadedSession = totalUploadedSession;
            TotalWasted = totalWasted;
            UploadLimit = uploadLimit;
            UploadSpeed = uploadSpeed;
            UploadSpeedAverage = uploadSpeedAverage;
            InfoHashV1 = infoHashV1;
            InfoHashV2 = infoHashV2;
            Hash = hash;
            Name = name;
            DownloadPath = downloadPath;
            Popularity = popularity;
            Progress = progress;
            IsPrivate = isPrivate;
            Private = @private;
            HasMetadata = hasMetadata;
        }

        [JsonPropertyName("addition_date")]
        public long AdditionDate { get; }

        [JsonPropertyName("comment")]
        public string Comment { get; }

        [JsonPropertyName("completion_date")]
        public long CompletionDate { get; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; }

        [JsonPropertyName("creation_date")]
        public long CreationDate { get; }

        [JsonPropertyName("dl_limit")]
        public long DownloadLimit { get; }

        [JsonPropertyName("dl_speed")]
        public long DownloadSpeed { get; }

        [JsonPropertyName("dl_speed_avg")]
        public long DownloadSpeedAverage { get; }

        [JsonPropertyName("eta")]
        public long EstimatedTimeOfArrival { get; }

        [JsonPropertyName("last_seen")]
        public long LastSeen { get; }

        [JsonPropertyName("nb_connections")]
        public int Connections { get; }

        [JsonPropertyName("nb_connections_limit")]
        public int ConnectionsLimit { get; }

        [JsonPropertyName("peers")]
        public int Peers { get; }

        [JsonPropertyName("peers_total")]
        public int PeersTotal { get; }

        [JsonPropertyName("piece_size")]
        public int PieceSize { get; }

        [JsonPropertyName("pieces_have")]
        public int PiecesHave { get; }

        [JsonPropertyName("pieces_num")]
        public int PiecesNum { get; }

        [JsonPropertyName("reannounce")]
        public int Reannounce { get; }

        [JsonPropertyName("save_path")]
        public string SavePath { get; }

        [JsonPropertyName("download_path")]
        public string? DownloadPath { get; }

        [JsonPropertyName("seeding_time")]
        public int SeedingTime { get; }

        [JsonPropertyName("seeds")]
        public int Seeds { get; }

        [JsonPropertyName("seeds_total")]
        public int SeedsTotal { get; }

        [JsonPropertyName("share_ratio")]
        public float ShareRatio { get; }

        [JsonPropertyName("popularity")]
        public float? Popularity { get; }

        [JsonPropertyName("progress")]
        public float? Progress { get; }

        [JsonPropertyName("time_elapsed")]
        public int TimeElapsed { get; }

        [JsonPropertyName("total_downloaded")]
        public long TotalDownloaded { get; }

        [JsonPropertyName("total_downloaded_session")]
        public long TotalDownloadedSession { get; }

        [JsonPropertyName("total_size")]
        public long TotalSize { get; }

        [JsonPropertyName("total_uploaded")]
        public long TotalUploaded { get; }

        [JsonPropertyName("total_uploaded_session")]
        public long TotalUploadedSession { get; }

        [JsonPropertyName("total_wasted")]
        public long TotalWasted { get; }

        [JsonPropertyName("up_limit")]
        public long UploadLimit { get; }

        [JsonPropertyName("up_speed")]
        public long UploadSpeed { get; }

        [JsonPropertyName("up_speed_avg")]
        public long UploadSpeedAverage { get; }

        [JsonPropertyName("infohash_v1")]
        public string InfoHashV1 { get; }

        [JsonPropertyName("infohash_v2")]
        public string InfoHashV2 { get; }

        [JsonPropertyName("hash")]
        public string? Hash { get; }

        [JsonPropertyName("name")]
        public string? Name { get; }

        [JsonPropertyName("is_private")]
        public bool? IsPrivate { get; }

        [JsonPropertyName("private")]
        public bool? Private { get; }

        [JsonPropertyName("has_metadata")]
        public bool? HasMetadata { get; }
    }
}
