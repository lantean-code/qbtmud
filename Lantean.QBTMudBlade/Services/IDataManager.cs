using Lantean.QBTMudBlade.Models;

namespace Lantean.QBTMudBlade.Services
{
    public interface IDataManager
    {
        MainData CreateMainData(QBitTorrentClient.Models.MainData mainData);

        Torrent CreateTorrent(string hash, QBitTorrentClient.Models.Torrent torrent);

        void MergeMainData(QBitTorrentClient.Models.MainData mainData, MainData torrentList);

        PeerList CreatePeerList(QBitTorrentClient.Models.TorrentPeers torrentPeers);

        void MergeTorrentPeers(QBitTorrentClient.Models.TorrentPeers torrentPeers, PeerList peerList);

        Dictionary<string, ContentItem> CreateContentsList(IReadOnlyList<QBitTorrentClient.Models.FileData> files);

        void MergeContentsList(IReadOnlyList<QBitTorrentClient.Models.FileData> files, Dictionary<string, ContentItem> contents);

        QBitTorrentClient.Models.UpdatePreferences MergePreferences(QBitTorrentClient.Models.UpdatePreferences? original, QBitTorrentClient.Models.UpdatePreferences changed);
    }
}