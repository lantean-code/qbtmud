using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    public interface IDataManager
    {
        MainData CreateMainData(QBitTorrentClient.Models.MainData mainData);

        Torrent CreateTorrent(string hash, QBitTorrentClient.Models.Torrent torrent);

        bool MergeMainData(QBitTorrentClient.Models.MainData mainData, MainData torrentList, out bool filterChanged);

        PeerList CreatePeerList(QBitTorrentClient.Models.TorrentPeers torrentPeers);

        void MergeTorrentPeers(QBitTorrentClient.Models.TorrentPeers torrentPeers, PeerList peerList);

        Dictionary<string, ContentItem> CreateContentsList(IReadOnlyList<QBitTorrentClient.Models.FileData> files);

        bool MergeContentsList(IReadOnlyList<QBitTorrentClient.Models.FileData> files, Dictionary<string, ContentItem> contents);

        QBitTorrentClient.Models.UpdatePreferences MergePreferences(QBitTorrentClient.Models.UpdatePreferences? original, QBitTorrentClient.Models.UpdatePreferences changed);

        RssList CreateRssList(IReadOnlyDictionary<string, QBitTorrentClient.Models.RssItem> rssItems);
    }
}
