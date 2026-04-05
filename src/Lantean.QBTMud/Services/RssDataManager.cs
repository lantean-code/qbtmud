using Lantean.QBTMud.Models;
using ClientRssArticle = QBittorrent.ApiClient.Models.RssArticle;
using ClientRssItem = QBittorrent.ApiClient.Models.RssItem;
using MudRssArticle = Lantean.QBTMud.Models.RssArticle;

namespace Lantean.QBTMud.Services
{
    public class RssDataManager : IRssDataManager
    {
        public RssList CreateRssList(IReadOnlyDictionary<string, ClientRssItem> rssItems)
        {
            var articles = new List<MudRssArticle>();
            var feeds = new Dictionary<string, RssFeed>(StringComparer.Ordinal);
            foreach (var (key, rssItem) in rssItems)
            {
                feeds.Add(
                    key,
                    new RssFeed(
                        rssItem.HasError,
                        rssItem.IsLoading,
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
