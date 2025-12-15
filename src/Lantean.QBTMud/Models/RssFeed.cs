namespace Lantean.QBTMud.Models
{
    public class RssFeed
    {
        public RssFeed(
            bool hasError,
            bool isLoading,
            string? lastBuildDate,
            string? title,
            string uid,
            string url,
            string path)
        {
            HasError = hasError;
            IsLoading = isLoading;
            LastBuildDate = lastBuildDate;
            Title = title;
            Uid = uid;
            Url = url;
            Path = path;
        }

        public bool HasError { get; }

        public bool IsLoading { get; internal set; }

        public string? LastBuildDate { get; }

        public string? Title { get; }

        public string Uid { get; }

        public string Url { get; }

        public string Path { get; }

        public int ArticleCount { get; internal set; }

        public int UnreadCount { get; internal set; }
    }
}
