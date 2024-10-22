namespace Lantean.QBTMud.Models
{
    public record ServerState : GlobalTransferInfo
    {
        public ServerState(
            long allTimeDownloaded,
            long allTimeUploaded,
            int averageTimeQueue,
            string connectionStatus,
            int dHTNodes,
            long downloadInfoData,
            long downloadInfoSpeed,
            long downloadRateLimit,
            long freeSpaceOnDisk,
            float globalRatio,
            int queuedIOJobs,
            bool queuing,
            float readCacheHits,
            float readCacheOverload,
            int refreshInterval,
            int totalBuffersSize,
            int totalPeerConnections,
            int totalQueuedSize,
            long totalWastedSession,
            long uploadInfoData,
            long uploadInfoSpeed,
            long uploadRateLimit,
            bool useAltSpeedLimits,
            bool useSubcategories,
            float writeCacheOverload) : base(connectionStatus, dHTNodes, downloadInfoData, downloadInfoSpeed, downloadRateLimit, uploadInfoData, uploadInfoSpeed, uploadRateLimit)
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
        }

        public ServerState()
        {
        }

        public long AllTimeDownloaded { get; set; }

        public long AllTimeUploaded { get; set; }

        public int AverageTimeQueue { get; set; }

        public long FreeSpaceOnDisk { get; set; }

        public float GlobalRatio { get; set; }

        public int QueuedIOJobs { get; set; }

        public bool Queuing { get; set; }

        public float ReadCacheHits { get; set; }

        public float ReadCacheOverload { get; set; }

        public int RefreshInterval { get; set; }

        public int TotalBuffersSize { get; set; }

        public int TotalPeerConnections { get; set; }

        public int TotalQueuedSize { get; set; }

        public long TotalWastedSession { get; set; }

        public bool UseAltSpeedLimits { get; set; }

        public bool UseSubcategories { get; set; }

        public float WriteCacheOverload { get; set; }
    }
}