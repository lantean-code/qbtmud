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
            string? torrentURL)
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
        }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("comments")]
        public string? Comments { get; set; }

        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("link")]
        public string? Link { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("torrentURL")]
        public string? TorrentURL { get; set; }
    }
}
