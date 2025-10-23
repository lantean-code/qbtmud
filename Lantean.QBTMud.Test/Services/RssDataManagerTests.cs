using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Services;

namespace Lantean.QBTMud.Test.Services
{
    public class RssDataManagerTests
    {
        private readonly RssDataManager _target = new RssDataManager();

        [Fact]
        public void GIVEN_MultipleFeedsWithAndWithoutArticles_WHEN_CreateRssList_THEN_MetaCountsAndFlatteningCorrect()
        {
            // arrange
            var items = new Dictionary<string, RssItem>
            {
                // key != uid on purpose in another test; here keep them equal
                ["feed-a"] = new RssItem(
                    articles: new[]
                    {
                        new Lantean.QBitTorrentClient.Models.RssArticle(
                            category: "cat-1", comments: "c1", date: "2025-01-01",
                            description: "d1", id: "a1", link: "http://a/1",
                            thumbnail: "http://a/t1", title: "t1",
                            torrentURL: "http://a.torrent/1", isRead: false),
                        new Lantean.QBitTorrentClient.Models.RssArticle(
                            category: "cat-2", comments: "c2", date: "2025-01-02",
                            description: "d2", id: "a2", link: "http://a/2",
                            thumbnail: "http://a/t2", title: "t2",
                            torrentURL: "http://a.torrent/2", isRead: true),
                        new Lantean.QBitTorrentClient.Models.RssArticle(
                            category: "cat-3", comments: "c3", date: "2025-01-03",
                            description: "d3", id: "a3", link: "http://a/3",
                            thumbnail: "http://a/t3", title: "t3",
                            torrentURL: "http://a.torrent/3", isRead: false),
                    },
                    hasError: false,
                    isLoading: false,
                    lastBuildDate: "2025-01-05",
                    title: "Feed A",
                    uid: "feed-a",
                    url: "http://feed/a"),

                // feed with one read article
                ["feed-b"] = new RssItem(
                    articles: new[]
                    {
                        new Lantean.QBitTorrentClient.Models.RssArticle(
                            category: "movies", comments: null, date: "2025-02-01",
                            description: null, id: "b1", link: "http://b/1",
                            thumbnail: null, title: "m1",
                            torrentURL: "http://b.torrent/1", isRead: true),
                    },
                    hasError: true,
                    isLoading: true,
                    lastBuildDate: "2025-02-02",
                    title: "Feed B",
                    uid: "feed-b",
                    url: "http://feed/b"),

                // feed with null article list -> should create feed only
                ["feed-c"] = new RssItem(
                    articles: null,
                    hasError: false,
                    isLoading: false,
                    lastBuildDate: "2025-03-03",
                    title: "Feed C",
                    uid: "feed-c",
                    url: "http://feed/c"),
            };

            // act
            var result = _target.CreateRssList(items);

            // assert: feeds exist
            result.Feeds.Keys.Should().BeEquivalentTo(new[] { "feed-a", "feed-b", "feed-c" });

            // feed-a meta and counts
            var fa = result.Feeds["feed-a"];
            fa.Uid.Should().Be("feed-a");
            fa.Url.Should().Be("http://feed/a");
            fa.Title.Should().Be("Feed A");
            fa.LastBuildDate.Should().Be("2025-01-05");
            fa.HasError.Should().BeFalse();
            fa.IsLoading.Should().BeFalse();
            fa.ArticleCount.Should().Be(3);
            fa.UnreadCount.Should().Be(2); // two IsRead=false in feed-a

            // feed-b meta and counts
            var fb = result.Feeds["feed-b"];
            fb.Uid.Should().Be("feed-b");
            fb.Url.Should().Be("http://feed/b");
            fb.Title.Should().Be("Feed B");
            fb.LastBuildDate.Should().Be("2025-02-02");
            fb.HasError.Should().BeTrue();
            fb.IsLoading.Should().BeTrue();
            fb.ArticleCount.Should().Be(1);
            fb.UnreadCount.Should().Be(0);

            // feed-c meta and counts
            var fc = result.Feeds["feed-c"];
            fc.Uid.Should().Be("feed-c");
            fc.Url.Should().Be("http://feed/c");
            fc.Title.Should().Be("Feed C");
            fc.LastBuildDate.Should().Be("2025-03-03");
            fc.HasError.Should().BeFalse();
            fc.IsLoading.Should().BeFalse();
            fc.ArticleCount.Should().Be(0);
            fc.UnreadCount.Should().Be(0);

            // articles flattened correctly (3 + 1)
            result.Articles.Count.Should().Be(4);
            result.Articles.Any(a => a.Feed == "feed-c").Should().BeFalse(); // none from 'c'

            // total unread
            result.UnreadCount.Should().Be(2);
        }

        [Fact]
        public void GIVEN_ClientArticleFieldsNulls_WHEN_CreateRssList_THEN_NullsPropagateWithoutNormalization()
        {
            // arrange: provide nulls in fields (the implementation uses '!' but does not normalize)
            var items = new Dictionary<string, RssItem>
            {
                ["feed-null"] = new RssItem(
                    articles: new[]
                    {
                        new Lantean.QBitTorrentClient.Models.RssArticle(
                            category: null, comments: null, date: null,
                            description: null, id: null, link: null,
                            thumbnail: null, title: null,
                            torrentURL: null, isRead: false),
                    },
                    hasError: false,
                    isLoading: false,
                    lastBuildDate: null,
                    title: null,
                    uid: "uid-null",
                    url: "http://feed/null"),
            };

            // act
            var result = _target.CreateRssList(items);

            // assert feed meta
            result.Feeds.Count.Should().Be(1);
            var f = result.Feeds["feed-null"];
            f.Uid.Should().Be("uid-null");
            f.Url.Should().Be("http://feed/null");
            f.Title.Should().BeNull();
            f.LastBuildDate.Should().BeNull();
            f.ArticleCount.Should().Be(1);
            f.UnreadCount.Should().Be(1);

            // assert article null propagation (no normalization to empty string)
            var art = result.Articles.Single();
            art.Feed.Should().Be("feed-null"); // dictionary key is used as article.Feed
            art.Category.Should().BeNull();
            art.Comments.Should().BeNull();
            art.Date.Should().BeNull();
            art.Description.Should().BeNull();
            art.Id.Should().BeNull();
            art.Link.Should().BeNull();
            art.Thumbnail.Should().BeNull();
            art.Title.Should().BeNull();
            art.TorrentURL.Should().BeNull();
            art.IsRead.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_UidDiffersFromDictionaryKey_WHEN_CreateRssList_THEN_ArticleFeedUsesKey_And_FeedUidUsesItemUid()
        {
            // arrange: key != uid
            var items = new Dictionary<string, RssItem>
            {
                ["dict-key"] = new RssItem(
                    articles: new[]
                    {
                        new Lantean.QBitTorrentClient.Models.RssArticle(
                            category: "x", comments: "c", date: "d",
                            description: "desc", id: "id1", link: "l",
                            thumbnail: "t", title: "title",
                            torrentURL: "u", isRead: true),
                    },
                    hasError: false,
                    isLoading: false,
                    lastBuildDate: "lb",
                    title: "T",
                    uid: "uid-different",
                    url: "http://u"),
            };

            // act
            var result = _target.CreateRssList(items);

            // assert: feed uid == item.Uid; article.Feed == dictionary key
            var feed = result.Feeds["dict-key"];
            feed.Uid.Should().Be("uid-different");
            var art = result.Articles.Single();
            art.Feed.Should().Be("dict-key");
        }

        [Fact]
        public void GIVEN_RssListWithUnread_WHEN_MarkAllUnreadAsRead_THEN_AllFeedUnreadZero_And_TotalZero()
        {
            // arrange
            var items = new Dictionary<string, RssItem>
            {
                ["fa"] = new RssItem(
                    articles: new[]
                    {
                        new Lantean.QBitTorrentClient.Models.RssArticle(
                            category: "c", comments: "c", date: "d1",
                            description: "d", id: "a1", link: "l",
                            thumbnail: "t", title: "t",
                            torrentURL: "u", isRead: false),
                        new Lantean.QBitTorrentClient.Models.RssArticle(
                            category: "c", comments: "c", date: "d2",
                            description: "d", id: "a2", link: "l",
                            thumbnail: "t", title: "t",
                            torrentURL: "u", isRead: false),
                    },
                    hasError: false, isLoading: false,
                    lastBuildDate: "lb", title: "TA", uid: "fa", url: "http://fa"),

                ["fb"] = new RssItem(
                    articles: new[]
                    {
                        new Lantean.QBitTorrentClient.Models.RssArticle(
                            category: "c", comments: "c", date: "d3",
                            description: "d", id: "b1", link: "l",
                            thumbnail: "t", title: "t",
                            torrentURL: "u", isRead: true),
                    },
                    hasError: false, isLoading: false,
                    lastBuildDate: "lb", title: "TB", uid: "fb", url: "http://fb"),
            };
            var list = _target.CreateRssList(items);
            list.UnreadCount.Should().Be(2);

            // act
            list.MarkAllUnreadAsRead();

            // assert
            list.UnreadCount.Should().Be(0);
            foreach (var f in list.Feeds.Values)
                f.UnreadCount.Should().Be(0);
        }

        [Fact]
        public void GIVEN_RssListWithMultipleFeeds_WHEN_MarkAsUnread_ForSpecificFeed_THEN_OnlyThatFeedZeroed()
        {
            // arrange
            var items = new Dictionary<string, RssItem>
            {
                ["fa"] = new RssItem(
                    articles: new[]
                    {
                        new Lantean.QBitTorrentClient.Models.RssArticle(
                            category: "c", comments: "c", date: "d1",
                            description: "d", id: "a1", link: "l",
                            thumbnail: "t", title: "t",
                            torrentURL: "u", isRead: false),
                        new Lantean.QBitTorrentClient.Models.RssArticle(
                            category: "c", comments: "c", date: "d2",
                            description: "d", id: "a2", link: "l",
                            thumbnail: "t", title: "t",
                            torrentURL: "u", isRead: false),
                    },
                    hasError: false, isLoading: false,
                    lastBuildDate: "lb", title: "TA", uid: "fa", url: "http://fa"),

                ["fb"] = new RssItem(
                    articles: new[]
                    {
                        new Lantean.QBitTorrentClient.Models.RssArticle(
                            category: "c", comments: "c", date: "d3",
                            description: "d", id: "b1", link: "l",
                            thumbnail: "t", title: "t",
                            torrentURL: "u", isRead: false),
                    },
                    hasError: false, isLoading: false,
                    lastBuildDate: "lb", title: "TB", uid: "fb", url: "http://fb"),
            };
            var list = _target.CreateRssList(items);
            list.UnreadCount.Should().Be(3);
            list.Feeds["fa"].UnreadCount.Should().Be(2);
            list.Feeds["fb"].UnreadCount.Should().Be(1);

            // act
            list.MarkAsUnread("fa"); // per implementation: zeroes the feed's unread count

            // assert
            list.Feeds["fa"].UnreadCount.Should().Be(0);
            list.Feeds["fb"].UnreadCount.Should().Be(1);
            list.UnreadCount.Should().Be(1);
        }
    }
}