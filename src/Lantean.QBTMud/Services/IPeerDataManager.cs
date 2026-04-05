using Lantean.QBTMud.Models;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Services
{
    public interface IPeerDataManager
    {
        PeerList CreatePeerList(TorrentPeers torrentPeers);

        void MergeTorrentPeers(TorrentPeers torrentPeers, PeerList peerList);
    }
}