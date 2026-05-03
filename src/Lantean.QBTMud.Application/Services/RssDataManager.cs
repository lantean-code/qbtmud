using Lantean.QBTMud.Core.Helpers;
using Lantean.QBTMud.Core.Models;
using ClientRssArticle = QBittorrent.ApiClient.Models.RssArticle;
using ClientRssItem = QBittorrent.ApiClient.Models.RssItem;
using MudRssArticle = Lantean.QBTMud.Core.Models.RssArticle;

namespace Lantean.QBTMud.Application.Services
{
    public class RssDataManager : IRssDataManager
    {
        public RssList CreateRssList(IReadOnlyDictionary<string, ClientRssItem> rssItems)
        {
            var articles = new List<MudRssArticle>();
            var feeds = new Dictionary<string, RssFeed>(StringComparer.Ordinal);
            foreach (var (key, rssItem) in RssItemTreeHelper.EnumerateFeeds(rssItems))
            {
                feeds.Add(
                    key,
                    new RssFeed(
                        rssItem.HasError ?? false,
                        rssItem.IsLoading ?? false,
                        rssItem.LastBuildDate,
                        rssItem.Title,
                        rssItem.Uid,
                        rssItem.Url,
                        key));
                if (rssItem.Articles is null)
                {
                    continue;
                }
                foreach (ClientRssArticle rssArticle in rssItem.Articles)
                {
                    var article = new MudRssArticle(
                        key,
                        rssArticle.Category,
                        rssArticle.Comments,
                        rssArticle.Date ?? string.Empty,
                        rssArticle.Description,
                        rssArticle.Id ?? string.Empty,
                        rssArticle.Link,
                        rssArticle.Thumbnail,
                        rssArticle.Title ?? string.Empty,
                        rssArticle.TorrentURL ?? string.Empty,
                        rssArticle.IsRead);

                    articles.Add(article);
                }
            }

            return new RssList(feeds, articles);
        }
    }
}
