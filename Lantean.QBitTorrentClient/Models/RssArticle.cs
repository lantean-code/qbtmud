using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public class RssArticle
    {
        [JsonConstructor]
        public RssArticle(
            string? category,
            string? comments,
            string? date,
            string? description,
            string? id,
            string? link,
            string? thumbnail,
            string? title,
            string? torrentURL,
            bool isRead)
        {
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

        [JsonPropertyName("category")]
        public string? Category { get; }

        [JsonPropertyName("comments")]
        public string? Comments { get; }

        [JsonPropertyName("date")]
        public string? Date { get; }

        [JsonPropertyName("description")]
        public string? Description { get; }

        [JsonPropertyName("id")]
        public string? Id { get; }

        [JsonPropertyName("link")]
        public string? Link { get; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; }

        [JsonPropertyName("title")]
        public string? Title { get; }

        [JsonPropertyName("torrentURL")]
        public string? TorrentURL { get; }

        [JsonPropertyName("isRead")]
        public bool IsRead { get; }
    }
}