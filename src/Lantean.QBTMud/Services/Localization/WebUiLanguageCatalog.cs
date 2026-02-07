using Lantean.QBTMud.Models;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Provides access to the WebUI language catalog.
    /// </summary>
    public sealed class WebUiLanguageCatalog : IWebUiLanguageCatalog
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<WebUiLanguageCatalog> _logger;
        private readonly WebUiLocalizationOptions _options;
        private readonly SemaphoreSlim _initLock;
        private IReadOnlyList<WebUiLanguageCatalogItem> _languages;
        private bool _initialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebUiLanguageCatalog"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory for loading assets.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The localization options.</param>
        public WebUiLanguageCatalog(
            IHttpClientFactory httpClientFactory,
            ILogger<WebUiLanguageCatalog> logger,
            IOptions<WebUiLocalizationOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient("WebUiAssets");
            _logger = logger;
            _options = options.Value;
            _initLock = new SemaphoreSlim(1, 1);
            _languages = Array.Empty<WebUiLanguageCatalogItem>();
        }

        /// <summary>
        /// Gets the available WebUI languages.
        /// </summary>
        public IReadOnlyList<WebUiLanguageCatalogItem> Languages
        {
            get { return _languages; }
        }

        /// <inheritdoc />
        public async Task EnsureInitialized(CancellationToken cancellationToken = default)
        {
            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (_initialized)
                {
                    return;
                }

                var locales = await TryLoadLocalesAsync(cancellationToken);
                _languages = BuildCatalog(locales);
                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async Task<List<string>> TryLoadLocalesAsync(CancellationToken cancellationToken)
        {
            var path = BuildPath(_options.LanguagesFileName);
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync(path, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to request language catalog {Path}.", path);
                return [];
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return [];
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to load language catalog {Path}. Status: {StatusCode}", path, response.StatusCode);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            try
            {
                var locales = await JsonSerializer.DeserializeAsync<List<string>>(stream, JsonOptions, cancellationToken);
                return locales ?? [];
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse language catalog {Path}.", path);
                return [];
            }
        }

        private static IReadOnlyList<WebUiLanguageCatalogItem> BuildCatalog(IEnumerable<string> locales)
        {
            var items = new List<WebUiLanguageCatalogItem>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var locale in locales)
            {
                if (string.IsNullOrWhiteSpace(locale))
                {
                    continue;
                }

                if (!seen.Add(locale))
                {
                    continue;
                }

                items.Add(new WebUiLanguageCatalogItem(locale, ResolveDisplayName(locale)));
            }

            if (seen.Add("en"))
            {
                items.Add(new WebUiLanguageCatalogItem("en", ResolveDisplayName("en")));
            }

            return items;
        }

        private string BuildPath(string fileName)
        {
            var basePath = _options.BasePath.TrimEnd('/');
            return string.Concat(basePath, '/', fileName);
        }

        private static string ResolveDisplayName(string locale)
        {
            var normalized = NormalizeLocaleForCulture(locale);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return locale;
            }

            try
            {
                var culture = CultureInfo.GetCultureInfo(normalized);
                return culture.NativeName;
            }
            catch (CultureNotFoundException)
            {
                return locale;
            }
        }

        private static string NormalizeLocaleForCulture(string locale)
        {
            var normalized = locale.Replace('_', '-');
            var atIndex = normalized.IndexOf('@', StringComparison.Ordinal);
            if (atIndex < 0)
            {
                return normalized;
            }

            var basePart = normalized[..atIndex];
            var scriptPart = normalized[(atIndex + 1)..];
            if (string.IsNullOrWhiteSpace(scriptPart))
            {
                return basePart;
            }

            var script = NormalizeScriptTag(scriptPart);
            return string.Concat(basePart, "-", script);
        }

        private static string NormalizeScriptTag(string script)
        {
            if (string.Equals(script, "latin", StringComparison.OrdinalIgnoreCase))
            {
                return "Latn";
            }

            if (string.Equals(script, "cyrillic", StringComparison.OrdinalIgnoreCase))
            {
                return "Cyrl";
            }

            if (script.Length == 4)
            {
                return string.Concat(char.ToUpperInvariant(script[0]), script.Substring(1).ToLowerInvariant());
            }

            return script;
        }
    }
}
