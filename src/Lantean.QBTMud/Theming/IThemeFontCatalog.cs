namespace Lantean.QBTMud.Theming
{
    /// <summary>
    /// Provides access to the curated Google font catalog.
    /// </summary>
    public interface IThemeFontCatalog
    {
        /// <summary>
        /// Gets the list of suggested Google font families.
        /// </summary>
        IReadOnlyList<string> SuggestedFonts { get; }

        /// <summary>
        /// Ensures the font catalog has been loaded from the configured JSON file.
        /// </summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EnsureInitialized(CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to resolve a Google Fonts stylesheet URL for a font family.
        /// </summary>
        /// <param name="fontFamily">The font family name.</param>
        /// <param name="url">The resolved stylesheet URL.</param>
        /// <returns>True if a valid URL was resolved; otherwise false.</returns>
        bool TryGetFontUrl(string fontFamily, out string url);

        /// <summary>
        /// Builds a stable DOM id for a font family link element.
        /// </summary>
        /// <param name="fontFamily">The font family name.</param>
        /// <returns>The generated DOM id.</returns>
        string BuildFontId(string fontFamily);
    }
}
