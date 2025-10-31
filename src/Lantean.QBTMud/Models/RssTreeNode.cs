using System.Collections.ObjectModel;

namespace Lantean.QBTMud.Models
{
    public sealed class RssTreeNode
    {
        public RssTreeNode(string name, string path, bool isFolder, bool isUnread)
        {
            Name = name;
            Path = path;
            IsFolder = isFolder;
            IsUnread = isUnread;
            Children = new Collection<RssTreeNode>();
        }

        public string Name { get; }

        public string Path { get; }

        public bool IsFolder { get; }

        public bool IsUnread { get; }

        public RssFeed? Feed { get; set; }

        public Collection<RssTreeNode> Children { get; }

        public int ArticleCount { get; set; }

        public int UnreadCount { get; set; }
    }
}
