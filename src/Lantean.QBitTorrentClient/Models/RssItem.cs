using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record RssItem
    {
        [JsonConstructor]
        public RssItem(
            IReadOnlyList<RssArticle>? articles,
            bool hasError,
            bool isLoading,
            string? lastBuildDate,
            string? title,
            string uid,
            string url)
        {
            Articles = articles;
            HasError = hasError;
            IsLoading = isLoading;
            LastBuildDate = lastBuildDate;
            Title = title;
            Uid = uid;
            Url = url;
        }

        [JsonPropertyName("articles")]
        public IReadOnlyList<RssArticle>? Articles { get; }

        [JsonPropertyName("hasError")]
        public bool HasError { get; }

        [JsonPropertyName("IsLoading")]
        public bool IsLoading { get; }

        [JsonPropertyName("lastBuildDate")]
        public string? LastBuildDate { get; }

        [JsonPropertyName("title")]
        public string? Title { get; }

        [JsonPropertyName("uid")]
        public string Uid { get; }

        [JsonPropertyName("url")]
        public string Url { get; }
    }
}