namespace Lantean.QBTMud.Models
{
    public class RssList
    {
        public RssList(Dictionary<string, RssFeed> feeds, List<RssArticle> articles)
        {
            Feeds = feeds;

            foreach (var article in articles)
            {
                var feed = Feeds[article.Feed];
                feed.ArticleCount++;
                if (!article.IsRead)
                {
                    feed.UnreadCount++;
                }

                Articles.Add(article);
            }
        }

        public Dictionary<string, RssFeed> Feeds { get; }

        public List<RssArticle> Articles { get; } = [];

        public int UnreadCount => Feeds.Values.Sum(f => f.UnreadCount);

        internal void MarkAllUnreadAsRead()
        {
            foreach (var feed in Feeds)
            {
                feed.Value.UnreadCount = 0;
            }
        }

        internal void MarkAsUnread(string selectedFeed)
        {
            Feeds[selectedFeed].UnreadCount = 0;
        }
    }
}