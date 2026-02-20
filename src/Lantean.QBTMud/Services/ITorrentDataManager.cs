using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    public interface ITorrentDataManager
    {
        MainData CreateMainData(QBitTorrentClient.Models.MainData mainData);

        Torrent CreateTorrent(string hash, QBitTorrentClient.Models.Torrent torrent);

        bool MergeMainData(QBitTorrentClient.Models.MainData mainData, MainData torrentList, out bool filterChanged);

        bool MergeMainData(
            QBitTorrentClient.Models.MainData mainData,
            MainData torrentList,
            out bool filterChanged,
            out IReadOnlyList<TorrentTransition> torrentTransitions);

        Dictionary<string, ContentItem> CreateContentsList(IReadOnlyList<QBitTorrentClient.Models.FileData> files);

        bool MergeContentsList(IReadOnlyList<QBitTorrentClient.Models.FileData> files, Dictionary<string, ContentItem> contents);
    }
}
