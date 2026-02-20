namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents GitHub release metadata for qbtmud.
    /// </summary>
    public sealed record AppReleaseInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppReleaseInfo"/> class.
        /// </summary>
        /// <param name="tagName">The Git tag name of the release.</param>
        /// <param name="name">The display name of the release.</param>
        /// <param name="htmlUrl">The release page URL.</param>
        /// <param name="publishedAtUtc">The published timestamp in UTC, when available.</param>
        public AppReleaseInfo(string tagName, string name, string htmlUrl, DateTime? publishedAtUtc)
        {
            TagName = tagName;
            Name = name;
            HtmlUrl = htmlUrl;
            PublishedAtUtc = publishedAtUtc;
        }

        /// <summary>
        /// Gets the Git tag name of the release.
        /// </summary>
        public string TagName { get; init; }

        /// <summary>
        /// Gets the display name of the release.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Gets the release page URL.
        /// </summary>
        public string HtmlUrl { get; init; }

        /// <summary>
        /// Gets the published timestamp in UTC, when available.
        /// </summary>
        public DateTime? PublishedAtUtc { get; init; }
    }
}
