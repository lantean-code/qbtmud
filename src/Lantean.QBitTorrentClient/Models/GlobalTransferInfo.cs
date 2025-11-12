using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record GlobalTransferInfo
    {
        [JsonConstructor]
        public GlobalTransferInfo(
            string? connectionStatus,
            int? dHTNodes,
            long? downloadInfoData,
            long? downloadInfoSpeed,
            long? downloadRateLimit,
            long? uploadInfoData,
            long? uploadInfoSpeed,
            long? uploadRateLimit,
            string? lastExternalAddressV4 = null,
            string? lastExternalAddressV6 = null)
        {
            ConnectionStatus = connectionStatus;
            DHTNodes = dHTNodes;
            DownloadInfoData = downloadInfoData;
            DownloadInfoSpeed = downloadInfoSpeed;
            DownloadRateLimit = downloadRateLimit;
            UploadInfoData = uploadInfoData;
            UploadInfoSpeed = uploadInfoSpeed;
            UploadRateLimit = uploadRateLimit;
            LastExternalAddressV4 = lastExternalAddressV4;
            LastExternalAddressV6 = lastExternalAddressV6;
        }

        [JsonPropertyName("connection_status")]
        public string? ConnectionStatus { get; }

        [JsonPropertyName("dht_nodes")]
        public int? DHTNodes { get; }

        [JsonPropertyName("dl_info_data")]
        public long? DownloadInfoData { get; }

        [JsonPropertyName("dl_info_speed")]
        public long? DownloadInfoSpeed { get; }

        [JsonPropertyName("dl_rate_limit")]
        public long? DownloadRateLimit { get; }

        [JsonPropertyName("up_info_data")]
        public long? UploadInfoData { get; }

        [JsonPropertyName("up_info_speed")]
        public long? UploadInfoSpeed { get; }

        [JsonPropertyName("up_rate_limit")]
        public long? UploadRateLimit { get; }

        [JsonPropertyName("last_external_address_v4")]
        public string? LastExternalAddressV4 { get; }

        [JsonPropertyName("last_external_address_v6")]
        public string? LastExternalAddressV6 { get; }
    }
}
