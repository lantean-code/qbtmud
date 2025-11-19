namespace Lantean.QBTMud.Models
{
    public sealed class RssTreeItem
    {
        public RssTreeItem(RssTreeNode node, int depth)
        {
            Node = node;
            Depth = depth;
        }

        public RssTreeNode Node { get; }

        public int Depth { get; }
    }
}
