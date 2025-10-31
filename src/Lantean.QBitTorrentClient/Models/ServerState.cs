using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record ServerState : GlobalTransferInfo
    {
        [JsonConstructor]
        public ServerState(
            long? allTimeDownloaded,
            long? allTimeUploaded,
            int? averageTimeQueue,
            string? connectionStatus,
            int? dHTNodes,
            long? downloadInfoData,
            long? downloadInfoSpeed,
            long? downloadRateLimit,
            long? freeSpaceOnDisk,
            float? globalRatio,
            int? queuedIOJobs,
            bool? queuing,
            float? readCacheHits,
            float? readCacheOverload,
            int? refreshInterval,
            int? totalBuffersSize,
            int? totalPeerConnections,
            int? totalQueuedSize,
            long? totalWastedSession,
            long? uploadInfoData,
            long? uploadInfoSpeed,
            long? uploadRateLimit,
            bool? useAltSpeedLimits,
            bool? useSubcategories,
            float? writeCacheOverload,
            string? lastExternalAddressV4 = null,
            string? lastExternalAddressV6 = null) : base(connectionStatus, dHTNodes, downloadInfoData, downloadInfoSpeed, downloadRateLimit, uploadInfoData, uploadInfoSpeed, uploadRateLimit)
        {
            AllTimeDownloaded = allTimeDownloaded;
            AllTimeUploaded = allTimeUploaded;
            AverageTimeQueue = averageTimeQueue;
            FreeSpaceOnDisk = freeSpaceOnDisk;
            GlobalRatio = globalRatio;
            QueuedIOJobs = queuedIOJobs;
            Queuing = queuing;
            ReadCacheHits = readCacheHits;
            ReadCacheOverload = readCacheOverload;
            RefreshInterval = refreshInterval;
            TotalBuffersSize = totalBuffersSize;
            TotalPeerConnections = totalPeerConnections;
            TotalQueuedSize = totalQueuedSize;
            TotalWastedSession = totalWastedSession;
            UseAltSpeedLimits = useAltSpeedLimits;
            UseSubcategories = useSubcategories;
            WriteCacheOverload = writeCacheOverload;
            LastExternalAddressV4 = lastExternalAddressV4;
            LastExternalAddressV6 = lastExternalAddressV6;
        }

        [JsonPropertyName("alltime_dl")]
        public long? AllTimeDownloaded { get; }

        [JsonPropertyName("alltime_ul")]
        public long? AllTimeUploaded { get; }

        [JsonPropertyName("average_time_queue")]
        public int? AverageTimeQueue { get; }

        [JsonPropertyName("free_space_on_disk")]
        public long? FreeSpaceOnDisk { get; }

        [JsonPropertyName("global_ratio")]
        public float? GlobalRatio { get; }

        [JsonPropertyName("queued_io_jobs")]
        public int? QueuedIOJobs { get; }

        [JsonPropertyName("queueing")]
        public bool? Queuing { get; }

        [JsonPropertyName("read_cache_hits")]
        public float? ReadCacheHits { get; }

        [JsonPropertyName("read_cache_overload")]
        public float? ReadCacheOverload { get; }

        [JsonPropertyName("refresh_interval")]
        public int? RefreshInterval { get; }

        [JsonPropertyName("total_buffers_size")]
        public int? TotalBuffersSize { get; }

        [JsonPropertyName("total_peer_connections")]
        public int? TotalPeerConnections { get; }

        [JsonPropertyName("total_queued_size")]
        public int? TotalQueuedSize { get; }

        [JsonPropertyName("total_wasted_session")]
        public long? TotalWastedSession { get; }

        [JsonPropertyName("use_alt_speed_limits")]
        public bool? UseAltSpeedLimits { get; }

        [JsonPropertyName("use_subcategories")]
        public bool? UseSubcategories { get; }

        [JsonPropertyName("write_cache_overload")]
        public float? WriteCacheOverload { get; }

        [JsonPropertyName("last_external_address_v4")]
        public string? LastExternalAddressV4 { get; }

        [JsonPropertyName("last_external_address_v6")]
        public string? LastExternalAddressV6 { get; }
    }
}