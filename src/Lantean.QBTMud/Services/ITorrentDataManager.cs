using Lantean.QBTMud.Models;
using QBittorrent.ApiClient.Models;
using ClientMainData = QBittorrent.ApiClient.Models.MainData;
using ClientTorrent = QBittorrent.ApiClient.Models.Torrent;
using MudMainData = Lantean.QBTMud.Models.MainData;
using MudTorrent = Lantean.QBTMud.Models.Torrent;

namespace Lantean.QBTMud.Services
{
    public interface ITorrentDataManager
    {
        MudMainData CreateMainData(ClientMainData mainData);

        MudTorrent CreateTorrent(string hash, ClientTorrent torrent);

        bool MergeMainData(ClientMainData mainData, MudMainData torrentList, out bool filterChanged);

        bool MergeMainData(
            ClientMainData mainData,
            MudMainData torrentList,
            out bool filterChanged,
            out IReadOnlyList<TorrentTransition> torrentTransitions);

        Dictionary<string, ContentItem> CreateContentsList(IReadOnlyList<FileData> files);

        bool MergeContentsList(IReadOnlyList<FileData> files, Dictionary<string, ContentItem> contents);
    }
}
