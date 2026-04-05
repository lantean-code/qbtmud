using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Models
{
    public record GlobalTransferInfo
    {
        public GlobalTransferInfo(
            ConnectionStatus? connectionStatus,
            long dHTNodes,
            long downloadInfoData,
            long downloadInfoSpeed,
            int downloadRateLimit,
            long uploadInfoData,
            long uploadInfoSpeed,
            int uploadRateLimit)
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
        }

        public ConnectionStatus? ConnectionStatus { get; set; }

        public long DHTNodes { get; set; }

        public long DownloadInfoData { get; set; }

        public long DownloadInfoSpeed { get; set; }

        public int DownloadRateLimit { get; set; }

        public long UploadInfoData { get; set; }

        public long UploadInfoSpeed { get; set; }

        public int UploadRateLimit { get; set; }
    }
}
