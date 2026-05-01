using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Core.Models
{
    public record ServerState : GlobalTransferInfo
    {
        public ServerState(
            long allTimeDownloaded,
            long allTimeUploaded,
            long averageTimeQueue,
            ConnectionStatus? connectionStatus,
            long dHTNodes,
            long downloadInfoData,
            long downloadInfoSpeed,
            int downloadRateLimit,
            long freeSpaceOnDisk,
            double globalRatio,
            long queuedIOJobs,
            bool queuing,
            double readCacheHits,
            double readCacheOverload,
            int refreshInterval,
            long totalBuffersSize,
            long totalPeerConnections,
            long totalQueuedSize,
            long totalWastedSession,
            long uploadInfoData,
            long uploadInfoSpeed,
            int uploadRateLimit,
            bool useAltSpeedLimits,
            bool useSubcategories,
            double writeCacheOverload,
            string lastExternalAddressV4,
            string lastExternalAddressV6) : base(
                connectionStatus,
                dHTNodes,
                downloadInfoData,
                downloadInfoSpeed,
                downloadRateLimit,
                uploadInfoData,
                uploadInfoSpeed,
                uploadRateLimit)
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

        public ServerState()
        {
        }

        public long AllTimeDownloaded { get; set; }

        public long AllTimeUploaded { get; set; }

        public long AverageTimeQueue { get; set; }

        public long FreeSpaceOnDisk { get; set; }

        public double GlobalRatio { get; set; }

        public long QueuedIOJobs { get; set; }

        public bool Queuing { get; set; }

        public double ReadCacheHits { get; set; }

        public double ReadCacheOverload { get; set; }

        public int RefreshInterval { get; set; }

        public long TotalBuffersSize { get; set; }

        public long TotalPeerConnections { get; set; }

        public long TotalQueuedSize { get; set; }

        public long TotalWastedSession { get; set; }

        public bool UseAltSpeedLimits { get; set; }

        public bool UseSubcategories { get; set; }

        public double WriteCacheOverload { get; set; }

        public string LastExternalAddressV4 { get; set; } = string.Empty;

        public string LastExternalAddressV6 { get; set; } = string.Empty;
    }
}
