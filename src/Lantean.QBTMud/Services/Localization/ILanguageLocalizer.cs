namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Resolves WebUI translations using qBittorrent-style context and source keys.
    /// </summary>
    public interface ILanguageLocalizer
    {
        /// <summary>
        /// Translates the provided source text within the given context.
        /// </summary>
        /// <param name="context">The translation context name.</param>
        /// <param name="source">The source text to translate.</param>
        /// <param name="arguments">Optional format arguments.</param>
        /// <returns>The translated string or a fallback when none is available.</returns>
        string Translate(string context, string source, params object[] arguments);
    }
}
