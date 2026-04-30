using Lantean.QBTMud.Models;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Services
{
    public interface IRssDataManager
    {
        RssList CreateRssList(IReadOnlyDictionary<string, RssItem> rssItems);
    }
}
