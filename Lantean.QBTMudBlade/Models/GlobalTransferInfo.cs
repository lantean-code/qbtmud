namespace Lantean.QBTMudBlade.Models
{
    public record GlobalTransferInfo
    {
        public GlobalTransferInfo(
            string connectionStatus,
            int dHTNodes,
            long downloadInfoData,
            long downloadInfoSpeed,
            long downloadRateLimit,
            long uploadInfoData,
            long uploadInfoSpeed,
            long uploadRateLimit)
        {
            ConnectionStatus = connectionStatus;
            DHTNodes = dHTNodes;
            DownloadInfoData = downloadInfoData;
            DownloadInfoSpeed = downloadInfoSpeed;
            DownloadRateLimit = downloadRateLimit;
            UploadInfoData = uploadInfoData;
            UploadInfoSpeed = uploadInfoSpeed;
            UploadRateLimit = uploadRateLimit;
        }

        public GlobalTransferInfo()
        {
            ConnectionStatus = "Unknown";
        }

        public string ConnectionStatus { get; set; }

        public int DHTNodes { get; set; }

        public long DownloadInfoData { get; set; }

        public long DownloadInfoSpeed { get; set; }

        public long DownloadRateLimit { get; set; }

        public long UploadInfoData { get; set; }

        public long UploadInfoSpeed { get; set; }

        public long UploadRateLimit { get; set; }
    }
}