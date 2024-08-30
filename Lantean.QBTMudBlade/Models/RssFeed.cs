using System.Text.Json.Serialization;

namespace Lantean.QBTMudBlade.Models
{
    public class RssFeed
    {
        public RssFeed(
            bool hasError,
            bool isLoading,
            string? lastBuildDate,
            string? title,
            string uid,
            string url)
        {
            HasError = hasError;
            IsLoading = isLoading;
            LastBuildDate = lastBuildDate;
            Title = title;
            Uid = uid;
            Url = url;
        }

        public bool HasError { get; }

        public bool IsLoading { get; internal set; }

        public string? LastBuildDate { get; }

        public string? Title { get; }

        public string Uid { get; }

        public string Url { get; }

        public int ArticleCount { get; internal set; }

        public int UnreadCount { get; internal set; }
    }
}