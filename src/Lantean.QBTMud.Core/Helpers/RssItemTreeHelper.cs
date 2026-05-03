using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Core.Helpers
{
    /// <summary>
    /// Provides helpers for traversing qBittorrent RSS item trees.
    /// </summary>
    public static class RssItemTreeHelper
    {
        private const char _pathSeparator = '\\';

        /// <summary>
        /// Enumerates RSS feed items by their full tree path.
        /// </summary>
        /// <param name="rssItems">The RSS item tree to enumerate.</param>
        /// <returns>The feed items keyed by full tree path.</returns>
        public static IEnumerable<KeyValuePair<string, RssFeedItem>> EnumerateFeeds(IReadOnlyDictionary<string, RssItem> rssItems)
        {
            foreach (var entry in rssItems)
            {
                foreach (var feedEntry in EnumerateFeeds(entry.Key, entry.Value))
                {
                    yield return feedEntry;
                }
            }
        }

        private static IEnumerable<KeyValuePair<string, RssFeedItem>> EnumerateFeeds(string currentPath, RssItem rssItem)
        {
            if (rssItem is RssFeedItem feedItem)
            {
                yield return new KeyValuePair<string, RssFeedItem>(currentPath, feedItem);
                yield break;
            }

            if (rssItem is not RssFolderItem folderItem)
            {
                yield break;
            }

            foreach (var child in folderItem.Children)
            {
                var childPath = string.Concat(currentPath, _pathSeparator, child.Key);
                foreach (var feedEntry in EnumerateFeeds(childPath, child.Value))
                {
                    yield return feedEntry;
                }
            }
        }
    }
}
