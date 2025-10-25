using System.Text.Json.Serialization;

namespace Lantean.QBitTorrentClient.Models
{
    public record SearchResult
    {
        [JsonConstructor]
        public SearchResult(
            string descriptionLink,
            string fileName,
            long fileSize,
            string fileUrl,
            int leechers,
            int seeders,
            string siteUrl)
        {
            DescriptionLink = descriptionLink;
            FileName = fileName;
            FileSize = fileSize;
            FileUrl = fileUrl;
            Leechers = leechers;
            Seeders = seeders;
            SiteUrl = siteUrl;
        }

        [JsonPropertyName("descrLink")]
        public string DescriptionLink { get; set; }

        [JsonPropertyName("fileName")]
        public string FileName { get; set; }

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("fileUrl")]
        public string FileUrl { get; set; }

        [JsonPropertyName("nbLeechers")]
        public int Leechers { get; set; }

        [JsonPropertyName("nbSeeders")]
        public int Seeders { get; set; }

        [JsonPropertyName("siteUrl")]
        public string SiteUrl { get; set; }
    }
}