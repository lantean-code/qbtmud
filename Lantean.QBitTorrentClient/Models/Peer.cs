using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record Peer
    {
        [JsonConstructor]
        public Peer(
            string? client,
            string? connection,
            string? country,
            string? countryCode,
            long? downloadSpeed,
            long? downloaded,
            string? files,
            string? flags,
            string? flagsDescription,
            string? iPAddress,
            string? clientId,
            int? port,
            float? progress,
            float? relevance,
            long? uploadSpeed,
            long? uploaded)
        {
            Client = client;
            Connection = connection;
            Country = country;
            CountryCode = countryCode;
            DownloadSpeed = downloadSpeed;
            Downloaded = downloaded;
            Files = files;
            Flags = flags;
            FlagsDescription = flagsDescription;
            IPAddress = iPAddress;
            ClientId = clientId;
            Port = port;
            Progress = progress;
            Relevance = relevance;
            UploadSpeed = uploadSpeed;
            Uploaded = uploaded;
        }

        [JsonPropertyName("client")]
        public string? Client { get; }

        [JsonPropertyName("connection")]
        public string? Connection { get; }

        [JsonPropertyName("country")]
        public string? Country { get; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; }

        [JsonPropertyName("dl_speed")]
        public long? DownloadSpeed { get; }

        [JsonPropertyName("downloaded")]
        public long? Downloaded { get; }

        [JsonPropertyName("files")]
        public string? Files { get; }

        [JsonPropertyName("flags")]
        public string? Flags { get; }

        [JsonPropertyName("flags_desc")]
        public string? FlagsDescription { get; }

        [JsonPropertyName("ip")]
        public string? IPAddress { get; }

        [JsonPropertyName("peer_id_client")]
        public string? ClientId { get; }

        [JsonPropertyName("port")]
        public int? Port { get; }

        [JsonPropertyName("progress")]
        public float? Progress { get; }

        [JsonPropertyName("relevance")]
        public float? Relevance { get; }

        [JsonPropertyName("up_speed")]
        public long? UploadSpeed { get; }

        [JsonPropertyName("uploaded")]
        public long? Uploaded { get; }
    }
}