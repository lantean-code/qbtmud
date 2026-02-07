namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Represents an available WebUI language option.
    /// </summary>
    /// <param name="Code">The WebUI locale code.</param>
    /// <param name="DisplayName">The display name for the locale.</param>
    public sealed record WebUiLanguageCatalogItem
    {
        public WebUiLanguageCatalogItem(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }

        /// <summary>
        /// Gets the WebUI locale code.
        /// </summary>
        public string Code { get; init; }

        /// <summary>
        /// Gets the display name for the locale.
        /// </summary>
        public string DisplayName { get; init; }
    }
}
