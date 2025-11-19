namespace Lantean.QBitTorrentClient.Models
{
    public readonly struct PeerId(string host, int port)
    {
        public string Host { get; } = host;

        public int Port { get; } = port;

        public override string ToString()
        {
            return $"{Host}:{Port}";
        }
    }
}