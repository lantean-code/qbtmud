using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    public interface IRssDataManager
    {
        RssList CreateRssList(IReadOnlyDictionary<string, QBitTorrentClient.Models.RssItem> rssItems);
    }
}