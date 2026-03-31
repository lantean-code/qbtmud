using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    public interface IRssDataManager
    {
        RssList CreateRssList(IReadOnlyDictionary<string, QBittorrent.ApiClient.Models.RssItem> rssItems);
    }
}