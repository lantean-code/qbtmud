using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    public interface IPeerDataManager
    {
        PeerList CreatePeerList(QBitTorrentClient.Models.TorrentPeers torrentPeers);

        void MergeTorrentPeers(QBitTorrentClient.Models.TorrentPeers torrentPeers, PeerList peerList);
    }
}
