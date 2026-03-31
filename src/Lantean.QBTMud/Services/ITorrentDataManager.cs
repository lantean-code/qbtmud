using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    public interface ITorrentDataManager
    {
        MainData CreateMainData(QBittorrent.ApiClient.Models.MainData mainData);

        Torrent CreateTorrent(string hash, QBittorrent.ApiClient.Models.Torrent torrent);

        bool MergeMainData(QBittorrent.ApiClient.Models.MainData mainData, MainData torrentList, out bool filterChanged);

        bool MergeMainData(
            QBittorrent.ApiClient.Models.MainData mainData,
            MainData torrentList,
            out bool filterChanged,
            out IReadOnlyList<TorrentTransition> torrentTransitions);

        Dictionary<string, ContentItem> CreateContentsList(IReadOnlyList<QBittorrent.ApiClient.Models.FileData> files);

        bool MergeContentsList(IReadOnlyList<QBittorrent.ApiClient.Models.FileData> files, Dictionary<string, ContentItem> contents);
    }
}
