using Lantean.QBTMud.Core.Models;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Application.Services
{
    public interface IPeerDataManager
    {
        PeerList CreatePeerList(TorrentPeers torrentPeers);

        void MergeTorrentPeers(TorrentPeers torrentPeers, PeerList peerList);
    }
}
