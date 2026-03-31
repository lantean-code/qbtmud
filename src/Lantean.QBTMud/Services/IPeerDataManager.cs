using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    public interface IPeerDataManager
    {
        PeerList CreatePeerList(QBittorrent.ApiClient.Models.TorrentPeers torrentPeers);

        void MergeTorrentPeers(QBittorrent.ApiClient.Models.TorrentPeers torrentPeers, PeerList peerList);
    }
}