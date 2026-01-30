using Lantean.QBTMud.Resources;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Provides qBittorrent-style translation lookup using context and source keys.
    /// </summary>
    public sealed class WebUiLocalizer : IWebUiLocalizer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private static readonly Regex PlaceholderRegex = new("%([1-9][0-9]?)", RegexOptions.Compiled);

        private readonly HttpClient _httpClient;
        private readonly ILogger<WebUiLocalizer> _logger;
        private readonly IStringLocalizer<AppStrings> _fallbackLocalizer;
        private readonly WebUiLocalizationOptions _options;
        private readonly SemaphoreSlim _initLock;
        private Dictionary<string, string> _aliases;
        private Dictionary<string, string> _overrides;
        private Dictionary<string, string> _translations;
        private string? _loadedLocale;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebUiLocalizer"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to load translation assets.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="fallbackLocalizer">The fallback localizer for app-specific strings.</param>
        /// <param name="options">The localization options.</param>
        public WebUiLocalizer(
            IHttpClientFactory httpClientFactory,
            ILogger<WebUiLocalizer> logger,
            IStringLocalizer<AppStrings> fallbackLocalizer,
            IOptions<WebUiLocalizationOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient("WebUiAssets");
            _logger = logger;
            _fallbackLocalizer = fallbackLocalizer;
            _options = options.Value;
            _initLock = new SemaphoreSlim(1, 1);
            _aliases = new Dictionary<string, string>(StringComparer.Ordinal);
            _overrides = new Dictionary<string, string>(StringComparer.Ordinal);
            _translations = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        /// <inheritdoc />
        public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            await _initLock.WaitAsync(cancellationToken);
            try
            {
                var cultureName = CultureInfo.CurrentUICulture.Name;
                if (string.Equals(_loadedLocale, cultureName, StringComparison.Ordinal))
                {
                    return;
                }

                await LoadAliasesAsync(cancellationToken);
                await LoadTranslationsAsync(cancellationToken);
            }
            finally
            {
                _initLock.Release();
            }
        }

        /// <inheritdoc />
        public string Translate(string context, string source, params object[] arguments)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            var formatArguments = arguments ?? Array.Empty<object>();
            var key = CreateKey(context, source);

            if (_aliases.TryGetValue(key, out var aliasKey) && !string.IsNullOrWhiteSpace(aliasKey))
            {
                key = aliasKey;
            }

            if (_overrides.TryGetValue(key, out var overrideTranslation))
            {
                return FormatTranslation(overrideTranslation, formatArguments, source);
            }

            if (_translations.TryGetValue(key, out var translation))
            {
                return FormatTranslation(translation, formatArguments, source);
            }

            return FormatTranslation(ResolveFallbackTranslation(key, source), formatArguments, source);
        }

        private string ResolveFallbackTranslation(string key, string source)
        {
            var localized = _fallbackLocalizer[key];
            if (!localized.ResourceNotFound)
            {
                return localized.Value;
            }

            localized = _fallbackLocalizer[source];
            if (!localized.ResourceNotFound)
            {
                return localized.Value;
            }

            return source;
        }

        private async Task LoadAliasesAsync(CancellationToken cancellationToken)
        {
            var aliasPath = BuildPath(_options.AliasFileName);
            var aliases = await TryLoadDictionaryAsync(aliasPath, cancellationToken);
            if (aliases is null)
            {
                _aliases = new Dictionary<string, string>(StringComparer.Ordinal);
                return;
            }

            _aliases = new Dictionary<string, string>(aliases, StringComparer.Ordinal);
        }

        private async Task LoadTranslationsAsync(CancellationToken cancellationToken)
        {
            var candidates = GetCandidateLocales();
            Dictionary<string, string>? loaded = null;
            string? loadedLocale = null;

            if (string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "en", StringComparison.OrdinalIgnoreCase))
            {
                loaded = TryLoadEmbeddedEnglish();
                if (loaded is not null)
                {
                    loadedLocale = "en";
                }
            }

            if (loaded is not null)
            {
                await LoadOverridesAsync(loaded, loadedLocale, cancellationToken);
                return;
            }

            foreach (var locale in candidates)
            {
                Dictionary<string, string>? baseTranslations = null;
                var basePath = BuildPath(string.Format(CultureInfo.InvariantCulture, _options.BaseFileNameFormat, locale));
                baseTranslations = await TryLoadDictionaryAsync(basePath, cancellationToken);

                if (baseTranslations is not null)
                {
                    loaded = baseTranslations;
                    loadedLocale = locale;
                    break;
                }
            }

            if (loaded is null || loadedLocale is null)
            {
                _logger.LogWarning("WebUI translations not found for culture {Culture}.", CultureInfo.CurrentUICulture.Name);
                _translations = new Dictionary<string, string>(StringComparer.Ordinal);
                _overrides = new Dictionary<string, string>(StringComparer.Ordinal);
                _loadedLocale = CultureInfo.CurrentUICulture.Name;
                return;
            }

            await LoadOverridesAsync(loaded, loadedLocale, cancellationToken);
        }

        private async Task LoadOverridesAsync(Dictionary<string, string> loaded, string? loadedLocale, CancellationToken cancellationToken)
        {
            if (loadedLocale is null)
            {
                _translations = new Dictionary<string, string>(StringComparer.Ordinal);
                _overrides = new Dictionary<string, string>(StringComparer.Ordinal);
                _loadedLocale = CultureInfo.CurrentUICulture.Name;
                return;
            }

            var overridePath = BuildPath(string.Format(CultureInfo.InvariantCulture, _options.OverrideFileNameFormat, loadedLocale));
            var overrides = await TryLoadDictionaryAsync(overridePath, cancellationToken) ?? new Dictionary<string, string>(StringComparer.Ordinal);

            _translations = new Dictionary<string, string>(loaded, StringComparer.Ordinal);
            _overrides = new Dictionary<string, string>(overrides, StringComparer.Ordinal);
            _loadedLocale = loadedLocale;
        }

        private static Dictionary<string, string>? TryLoadEmbeddedEnglish()
        {
            var assembly = typeof(WebUiLocalizer).Assembly;
            var resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith("wwwroot.i18n.webui_en.json", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return null;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                return null;
            }

            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(stream, JsonOptions);
                return data ?? new Dictionary<string, string>(StringComparer.Ordinal);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task<Dictionary<string, string>?> TryLoadDictionaryAsync(string relativePath, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(relativePath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to request translation file {Path}.", relativePath);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to load translation file {Path}. Status: {StatusCode}", relativePath, response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            try
            {
                var data = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, JsonOptions, cancellationToken);
                return data ?? new Dictionary<string, string>(StringComparer.Ordinal);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse translation file {Path}.", relativePath);
                return null;
            }
        }

        private string BuildPath(string fileName)
        {
            var basePath = _options.BasePath.TrimEnd('/');
            return string.Concat(basePath, '/', fileName);
        }

        private static string CreateKey(string context, string source)
        {
            if (string.IsNullOrWhiteSpace(context))
            {
                return source;
            }

            return string.Concat(context, '|', source);
        }

        private static string FormatTranslation(string translation, object[] arguments, string source)
        {
            var value = string.IsNullOrWhiteSpace(translation) ? source : translation;
            if (arguments.Length == 0)
            {
                return value;
            }

            var normalized = NormalizePlaceholders(value);
            try
            {
                return string.Format(CultureInfo.CurrentCulture, normalized, arguments);
            }
            catch (FormatException)
            {
                return value;
            }
        }

        private static string NormalizePlaceholders(string value)
        {
            return PlaceholderRegex.Replace(value, match =>
            {
                if (!int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
                {
                    return match.Value;
                }

                return string.Concat("{", index - 1, "}");
            });
        }

        private static List<string> GetCandidateLocales()
        {
            var current = CultureInfo.CurrentUICulture;
            var candidates = new List<string>();

            if (!string.IsNullOrWhiteSpace(current.Name))
            {
                candidates.Add(current.Name);
            }

            if (!string.IsNullOrWhiteSpace(current.TwoLetterISOLanguageName))
            {
                candidates.Add(current.TwoLetterISOLanguageName);
            }

            candidates.Add("en");

            return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
