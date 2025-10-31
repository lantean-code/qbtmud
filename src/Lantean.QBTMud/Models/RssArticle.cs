namespace Lantean.QBTMud.Models
{
    public class RssArticle
    {
        public RssArticle(
            string feed,
            string? category,
            string? comments,
            string date,
            string? description,
            string id,
            string? link,
            string? thumbnail,
            string title,
            string torrentURL,
            bool isRead)
        {
            Feed = feed;
            Category = category;
            Comments = comments;
            Date = date;
            Description = description;
            Id = id;
            Link = link;
            Thumbnail = thumbnail;
            Title = title;
            TorrentURL = torrentURL;
            IsRead = isRead;
        }

        public string Feed { get; }

        public string? Category { get; }

        public string? Comments { get; }

        public string Date { get; }

        public string? Description { get; }

        public string Id { get; }

        public string? Link { get; }

        public string? Thumbnail { get; }

        public string Title { get; }

        public string TorrentURL { get; }

        public bool IsRead { get; set; }
    }
}