using System.Text.RegularExpressions;

namespace Lantean.QBTMud.Theming
{
    /// <summary>
    /// Provides Google font catalog helpers for theme management.
    /// </summary>
    public static class ThemeFontCatalog
    {
        private static readonly Regex _fontNamePattern = new("^[a-zA-Z0-9][a-zA-Z0-9\\s\\-]*$", RegexOptions.Compiled);

        private static readonly IReadOnlyDictionary<string, string> _fontUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Nunito Sans", BuildFontUrl("Nunito Sans") },
            { "Roboto", BuildFontUrl("Roboto") },
            { "Montserrat", BuildFontUrl("Montserrat") },
            { "Ubuntu", BuildFontUrl("Ubuntu") },
            { "Source Sans 3", BuildFontUrl("Source Sans 3") },
            { "Poppins", BuildFontUrl("Poppins") }
        };

        /// <summary>
        /// Gets the list of suggested Google font families.
        /// </summary>
        public static IReadOnlyList<string> SuggestedFonts { get; } = _fontUrls.Keys.OrderBy(font => font).ToList();

        /// <summary>
        /// Attempts to resolve a Google Fonts stylesheet URL for a font family.
        /// </summary>
        /// <param name="fontFamily">The font family name.</param>
        /// <param name="url">The resolved stylesheet URL.</param>
        /// <returns>True if a valid URL was resolved; otherwise false.</returns>
        public static bool TryGetFontUrl(string fontFamily, out string url)
        {
            if (string.IsNullOrWhiteSpace(fontFamily))
            {
                url = string.Empty;
                return false;
            }

            var trimmed = fontFamily.Trim();
            if (_fontUrls.TryGetValue(trimmed, out var resolvedUrl))
            {
                url = resolvedUrl;
                return true;
            }

            if (!IsValidFontFamily(trimmed))
            {
                url = string.Empty;
                return false;
            }

            url = BuildFontUrl(trimmed);
            return true;
        }

        /// <summary>
        /// Builds a stable DOM id for a font family link element.
        /// </summary>
        /// <param name="fontFamily">The font family name.</param>
        /// <returns>The generated DOM id.</returns>
        public static string BuildFontId(string fontFamily)
        {
            if (string.IsNullOrWhiteSpace(fontFamily))
            {
                return "qbt-font-default";
            }

            var normalized = new string(fontFamily
                .Trim()
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                .ToArray());

            return $"qbt-font-{normalized}";
        }

        private static bool IsValidFontFamily(string fontFamily)
        {
            return _fontNamePattern.IsMatch(fontFamily);
        }

        private static string BuildFontUrl(string fontFamily)
        {
            var encoded = Uri.EscapeDataString(fontFamily).Replace("%20", "+");
            return $"https://fonts.googleapis.com/css2?family={encoded}&display=swap";
        }
    }
}
