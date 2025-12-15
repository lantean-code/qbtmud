using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record SslParameters
    {
        [JsonConstructor]
        public SslParameters(string? certificate, string? privateKey, string? dhParams)
        {
            Certificate = certificate;
            PrivateKey = privateKey;
            DhParams = dhParams;
        }

        [JsonPropertyName("ssl_certificate")]
        public string? Certificate { get; }

        [JsonPropertyName("ssl_private_key")]
        public string? PrivateKey { get; }

        [JsonPropertyName("ssl_dh_params")]
        public string? DhParams { get; }
    }
}
