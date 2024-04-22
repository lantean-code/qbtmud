using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record BuildInfo
    {
        [JsonConstructor]
        public BuildInfo(
            string qTVersion,
            string libTorrentVersion,
            string boostVersion,
            string openSSLVersion,
            int bitness)
        {
            QTVersion = qTVersion;
            LibTorrentVersion = libTorrentVersion;
            BoostVersion = boostVersion;
            OpenSSLVersion = openSSLVersion;
            Bitness = bitness;
        }

        [JsonPropertyName("qt")]
        public string QTVersion { get; }

        [JsonPropertyName("libtorrent")]
        public string LibTorrentVersion { get; }

        [JsonPropertyName("boost")]
        public string BoostVersion { get; }

        [JsonPropertyName("openssl")]
        public string OpenSSLVersion { get; }

        [JsonPropertyName("bitness")]
        public int Bitness { get; }
    }
}