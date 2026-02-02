using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lantean.QBTMud.Theming
{
    /// <summary>
    /// Provides Google font catalog helpers for theme management.
    /// </summary>
    public sealed class ThemeFontCatalog : IThemeFontCatalog
    {
        private const string FontCatalogPath = "fonts/theme-fonts.json";

        private static readonly Regex _fontNamePattern = new("^[a-zA-Z0-9][a-zA-Z0-9\\s\\-]*$", RegexOptions.Compiled);
        private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private IReadOnlyDictionary<string, string> _fontUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private IReadOnlyList<string> _suggestedFonts = Array.Empty<string>();
        private bool _initialized;

        /// <summary>
        /// Gets the list of suggested Google font families.
        /// </summary>
        public IReadOnlyList<string> SuggestedFonts
        {
            get { return _suggestedFonts; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeFontCatalog"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory used to load the catalog.</param>
        public ThemeFontCatalog(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Ensures the font catalog has been loaded from the configured JSON file.
        /// </summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnsureInitialized(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            await _loadLock.WaitAsync(cancellationToken);
            try
            {
                if (_initialized)
                {
                    return;
                }

                var fonts = await LoadFonts(cancellationToken);
                _suggestedFonts = fonts;
                _fontUrls = fonts.ToDictionary(font => font, BuildFontUrl, StringComparer.OrdinalIgnoreCase);
                _initialized = true;
            }
            finally
            {
                _loadLock.Release();
            }
        }

        /// <summary>
        /// Attempts to resolve a Google Fonts stylesheet URL for a font family.
        /// </summary>
        /// <param name="fontFamily">The font family name.</param>
        /// <param name="url">The resolved stylesheet URL.</param>
        /// <returns>True if a valid URL was resolved; otherwise false.</returns>
        public bool TryGetFontUrl(string fontFamily, out string url)
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
        public string BuildFontId(string fontFamily)
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

        private async Task<IReadOnlyList<string>> LoadFonts(CancellationToken cancellationToken)
        {
            HttpClient client;
            try
            {
                client = _httpClientFactory.CreateClient("Assets");
            }
            catch (InvalidOperationException)
            {
                return Array.Empty<string>();
            }

            try
            {
                var response = await client.GetAsync(FontCatalogPath, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return Array.Empty<string>();
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var fonts = JsonSerializer.Deserialize<List<string>>(json, _serializerOptions);
                return NormalizeFonts(fonts);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Array.Empty<string>();
            }
            catch (HttpRequestException)
            {
                return Array.Empty<string>();
            }
            catch (JsonException)
            {
                return Array.Empty<string>();
            }
        }

        private static IReadOnlyList<string> NormalizeFonts(IEnumerable<string>? fonts)
        {
            if (fonts is null)
            {
                return Array.Empty<string>();
            }

            var uniqueFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var font in fonts)
            {
                if (string.IsNullOrWhiteSpace(font))
                {
                    continue;
                }

                var trimmed = font.Trim();
                if (!IsValidFontFamily(trimmed))
                {
                    continue;
                }

                uniqueFonts.Add(trimmed);
            }

            return uniqueFonts
                .OrderBy(font => font, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildFontUrl(string fontFamily)
        {
            var encoded = Uri.EscapeDataString(fontFamily).Replace("%20", "+");
            return $"https://fonts.googleapis.com/css2?family={encoded}&display=swap";
        }
    }
}
