using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    public class RssDataManager : IRssDataManager
    {
        public RssList CreateRssList(IReadOnlyDictionary<string, QBitTorrentClient.Models.RssItem> rssItems)
        {
            var articles = new List<RssArticle>();
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
                foreach (var rssArticle in rssItem.Articles)
                {
                    var article = new RssArticle(
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
