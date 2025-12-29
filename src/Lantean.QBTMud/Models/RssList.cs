namespace Lantean.QBTMud.Models
{
    public class RssList
    {
        private const char _pathSeparator = '\\';
        private const string _unreadKey = "__unread__";

        private readonly Dictionary<string, RssTreeNode> _nodesByPath;

        public RssList(Dictionary<string, RssFeed> feeds, List<RssArticle> articles)
        {
            Feeds = feeds;

            foreach (var article in articles)
            {
                if (Feeds.TryGetValue(article.Feed, out var feed))
                {
                    feed.ArticleCount++;
                    if (!article.IsRead)
                    {
                        feed.UnreadCount++;
                    }
                }

                Articles.Add(article);
            }

            Root = BuildTree(feeds);
            _nodesByPath = BuildNodeLookup(Root);
            TreeItems = BuildTreeItems(Root);
            UnreadNode = _nodesByPath[_unreadKey];
            RecalculateCounts();
        }

        public Dictionary<string, RssFeed> Feeds { get; }

        public List<RssArticle> Articles { get; } = [];

        public RssTreeNode Root { get; }

        public RssTreeNode UnreadNode { get; }

        public IReadOnlyList<RssTreeItem> TreeItems { get; }

        public int UnreadCount => Feeds.Values.Sum(f => f.UnreadCount);

        public bool TryGetNode(string path, out RssTreeNode node)
        {
            return _nodesByPath.TryGetValue(path, out node!);
        }

        internal void MarkAllUnreadAsRead()
        {
            foreach (var feed in Feeds.Values)
            {
                feed.UnreadCount = 0;
            }

            foreach (var article in Articles)
            {
                article.IsRead = true;
            }

            RecalculateCounts();
        }

        internal void MarkAsUnread(string selectedFeed)
        {
            if (Feeds.TryGetValue(selectedFeed, out var feed))
            {
                feed.UnreadCount = 0;
            }

            RecalculateCounts();
        }

        internal void RecalculateCounts()
        {
            foreach (var feed in Feeds.Values)
            {
                if (_nodesByPath.TryGetValue(feed.Path, out var node))
                {
                    node.ArticleCount = feed.ArticleCount;
                    node.UnreadCount = feed.UnreadCount;
                }
            }

            UpdateFolderCounts(Root);

            UnreadNode.UnreadCount = Feeds.Values.Sum(f => f.UnreadCount);
            UnreadNode.ArticleCount = UnreadNode.UnreadCount;
        }

        private static RssTreeNode BuildTree(Dictionary<string, RssFeed> feeds)
        {
            var root = new RssTreeNode("Feeds", string.Empty, isFolder: true, isUnread: false);
            var unreadNode = new RssTreeNode("Unread", string.Empty, isFolder: false, isUnread: true);
            root.Children.Add(unreadNode);

            var folderLookup = new Dictionary<string, RssTreeNode>(StringComparer.Ordinal)
            {
                [string.Empty] = root
            };

            var orderedFeeds = feeds.Values.OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var feed in orderedFeeds)
            {
                var path = feed.Path;
                var segments = path.Split(_pathSeparator, StringSplitOptions.RemoveEmptyEntries);

                var parent = root;
                var currentPath = string.Empty;
                for (var i = 0; i < segments.Length - 1; i++)
                {
                    currentPath = string.IsNullOrEmpty(currentPath)
                        ? segments[i]
                        : $"{currentPath}{_pathSeparator}{segments[i]}";

                    if (!folderLookup.TryGetValue(currentPath, out var folderNode))
                    {
                        folderNode = new RssTreeNode(segments[i], currentPath, isFolder: true, isUnread: false);
                        folderLookup[currentPath] = folderNode;
                        parent.Children.Add(folderNode);
                        parent = folderNode;
                    }
                    else
                    {
                        parent = folderNode;
                    }
                }

                var feedName = segments.Length > 0 ? segments[^1] : feed.Title ?? feed.Path;
                var feedNode = new RssTreeNode(feedName, path, isFolder: false, isUnread: false)
                {
                    Feed = feed
                };
                parent.Children.Add(feedNode);
            }

            SortChildren(root);

            return root;
        }

        private static Dictionary<string, RssTreeNode> BuildNodeLookup(RssTreeNode root)
        {
            var dict = new Dictionary<string, RssTreeNode>(StringComparer.Ordinal)
            {
                [_unreadKey] = root.Children.First(n => n.IsUnread)
            };

            void Traverse(RssTreeNode node)
            {
                if (!node.IsFolder && !node.IsUnread)
                {
                    dict[node.Path] = node;
                }
                else if (node.IsFolder)
                {
                    if (!string.IsNullOrEmpty(node.Path))
                    {
                        dict[node.Path] = node;
                    }

                    foreach (var child in node.Children.Where(c => !c.IsUnread))
                    {
                        Traverse(child);
                    }
                }
            }

            foreach (var child in root.Children.Where(c => !c.IsUnread))
            {
                Traverse(child);
            }

            return dict;
        }

        private static IReadOnlyList<RssTreeItem> BuildTreeItems(RssTreeNode root)
        {
            var items = new List<RssTreeItem>();

            foreach (var child in root.Children)
            {
                if (child.IsUnread)
                {
                    items.Add(new RssTreeItem(child, depth: 0));
                    continue;
                }

                AddNode(child, 0);
            }

            void AddNode(RssTreeNode node, int depth)
            {
                items.Add(new RssTreeItem(node, depth));

                foreach (var child in node.Children)
                {
                    AddNode(child, depth + 1);
                }
            }

            return items;
        }

        private static void SortChildren(RssTreeNode node)
        {
            var unreadChild = node.Children.FirstOrDefault(c => c.IsUnread);
            var orderedChildren = node.Children
                .Where(c => !c.IsUnread)
                .OrderBy(c => c.IsFolder ? 0 : 1)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            node.Children.Clear();

            if (unreadChild is not null)
            {
                node.Children.Add(unreadChild);
            }

            foreach (var child in orderedChildren)
            {
                node.Children.Add(child);
                SortChildren(child);
            }
        }

        private static void UpdateFolderCounts(RssTreeNode node)
        {
            if (!node.IsFolder)
            {
                if (node.IsUnread)
                {
                    return;
                }

                if (node.Feed is not null)
                {
                    node.ArticleCount = node.Feed.ArticleCount;
                    node.UnreadCount = node.Feed.UnreadCount;
                }

                return;
            }

            var articleCount = 0;
            var unreadCount = 0;
            foreach (var child in node.Children)
            {
                UpdateFolderCounts(child);
                articleCount += child.ArticleCount;
                unreadCount += child.UnreadCount;
            }

            node.ArticleCount = articleCount;
            node.UnreadCount = unreadCount;
        }
    }
}
