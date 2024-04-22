namespace Lantean.QBTMudBlade.Models
{
    public class Peer
    {
        public Peer(
            string ip,
            string client,
            string clientId,
            string connection,
            string country,
            string countryCode,
            long downloaded,
            long downloadSpeed,
            string files,
            string flags,
            string flagsDescription,
            string iPAddress,
            int port,
            float progress,
            float relevance,
            long uploaded,
            long uploadSpeed)
        {
            IP = ip;
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

        public string IP { get; }
        public string Client { get; set; }
        public string ClientId { get; set; }
        public string Connection { get; set; }
        public string Country { get; set; }
        public string CountryCode { get; set; }
        public long Downloaded { get; set; }
        public long DownloadSpeed { get; set; }
        public string Files { get; set; }
        public string Flags { get; set; }
        public string FlagsDescription { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public float Progress { get; set; }
        public float Relevance { get; set; }
        public long Uploaded { get; set; }
        public long UploadSpeed { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            return ((Peer)obj).IP == IP;
        }

        public override int GetHashCode()
        {
            return IP.GetHashCode();
        }
    }
}