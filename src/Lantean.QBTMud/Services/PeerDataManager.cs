using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    public class PeerDataManager : IPeerDataManager
    {
        public PeerList CreatePeerList(QBitTorrentClient.Models.TorrentPeers torrentPeers)
        {
            var peers = new Dictionary<string, Peer>();
            if (torrentPeers.Peers is not null)
            {
                foreach (var (key, peer) in torrentPeers.Peers)
                {
                    var newPeer = CreatePeer(key, peer);

                    peers[key] = newPeer;
                }
            }

            var peerList = new PeerList(peers);

            return peerList;
        }

        public void MergeTorrentPeers(QBitTorrentClient.Models.TorrentPeers torrentPeers, PeerList peerList)
        {
            if (torrentPeers.PeersRemoved is not null)
            {
                foreach (var key in torrentPeers.PeersRemoved)
                {
                    peerList.Peers.Remove(key);
                }
            }

            if (torrentPeers.Peers is not null)
            {
                foreach (var (key, peer) in torrentPeers.Peers)
                {
                    if (!peerList.Peers.TryGetValue(key, out var existingPeer))
                    {
                        var newPeer = CreatePeer(key, peer);
                        peerList.Peers.Add(key, newPeer);
                    }
                    else
                    {
                        UpdatePeer(existingPeer, peer);
                    }
                }
            }
        }

        private static Peer CreatePeer(string key, QBitTorrentClient.Models.Peer peer)
        {
            return new Peer(
                key,
                peer.Client ?? string.Empty,
                peer.ClientId ?? string.Empty,
                peer.Connection ?? string.Empty,
                peer.Country,
                peer.CountryCode,
                peer.Downloaded.GetValueOrDefault(),
                peer.DownloadSpeed.GetValueOrDefault(),
                peer.Files ?? string.Empty,
                peer.Flags ?? string.Empty,
                peer.FlagsDescription ?? string.Empty,
                peer.IPAddress ?? string.Empty,
                peer.Port.GetValueOrDefault(),
                peer.Progress.GetValueOrDefault(),
                peer.Relevance.GetValueOrDefault(),
                peer.Uploaded.GetValueOrDefault(),
                peer.UploadSpeed.GetValueOrDefault());
        }

        private static void UpdatePeer(Peer existingPeer, QBitTorrentClient.Models.Peer peer)
        {
            existingPeer.Client = peer.Client ?? existingPeer.Client;
            existingPeer.ClientId = peer.ClientId ?? existingPeer.ClientId;
            existingPeer.Connection = peer.Connection ?? existingPeer.Connection;
            existingPeer.Country = peer.Country ?? existingPeer.Country;
            existingPeer.CountryCode = peer.CountryCode ?? existingPeer.CountryCode;
            existingPeer.Downloaded = peer.Downloaded ?? existingPeer.Downloaded;
            existingPeer.DownloadSpeed = peer.DownloadSpeed ?? existingPeer.DownloadSpeed;
            existingPeer.Files = peer.Files ?? existingPeer.Files;
            existingPeer.Flags = peer.Flags ?? existingPeer.Flags;
            existingPeer.FlagsDescription = peer.FlagsDescription ?? existingPeer.FlagsDescription;
            existingPeer.IPAddress = peer.IPAddress ?? existingPeer.IPAddress;
            existingPeer.Port = peer.Port ?? existingPeer.Port;
            existingPeer.Progress = peer.Progress ?? existingPeer.Progress;
            existingPeer.Relevance = peer.Relevance ?? existingPeer.Relevance;
            existingPeer.Uploaded = peer.Uploaded ?? existingPeer.Uploaded;
            existingPeer.UploadSpeed = peer.UploadSpeed ?? existingPeer.UploadSpeed;
        }
    }
}
