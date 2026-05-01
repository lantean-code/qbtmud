using Lantean.QBTMud.Core.Models;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Application.Services
{
    public interface IRssDataManager
    {
        RssList CreateRssList(IReadOnlyDictionary<string, RssItem> rssItems);
    }
}
