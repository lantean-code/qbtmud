using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Models
{
    public class Peer
    {
        public Peer(
            string key,
            string client,
            string clientId,
            PeerConnectionType? connection,
            string? country,
            string? countryCode,
            long downloaded,
            int downloadSpeed,
            string files,
            string flags,
            string flagsDescription,
            string iPAddress,
            int port,
            double progress,
            double relevance,
            long uploaded,
            int uploadSpeed)
        {
            Key = key;
            Client = client;
            ClientId = clientId;
            Connection = connection;
            Country = country;
            CountryCode = countryCode;
            Downloaded = downloaded;
            DownloadSpeed = downloadSpeed;
            Files = files;
            Flags = flags;
            FlagsDescription = flagsDescription;
            IPAddress = iPAddress;
            Port = port;
            Progress = progress;
            Relevance = relevance;
            Uploaded = uploaded;
            UploadSpeed = uploadSpeed;
        }

        public string Key { get; }
        public string Client { get; set; }
        public string ClientId { get; set; }
        public PeerConnectionType? Connection { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public long Downloaded { get; set; }
        public int DownloadSpeed { get; set; }
        public string Files { get; set; }
        public string Flags { get; set; }
        public string FlagsDescription { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public double Progress { get; set; }
        public double Relevance { get; set; }
        public long Uploaded { get; set; }
        public int UploadSpeed { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            return ((Peer)obj).Key == Key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }
    }
}
