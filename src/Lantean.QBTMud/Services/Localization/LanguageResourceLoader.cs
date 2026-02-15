using Microsoft.Extensions.Options;
using System.Globalization;

namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Coordinates WebUI localization resource loading across file and embedded-resource providers.
    /// </summary>
    public sealed class LanguageResourceLoader : ILanguageResourceLoader
    {
        private readonly ILanguageFileLoader _fileResourceProvider;
        private readonly ILanguageEmbeddedResourceLoader _assemblyResourceProvider;
        private readonly ILanguageResourceProvider _resourceProvider;
        private readonly ILogger<LanguageResourceLoader> _logger;
        private readonly WebUiLocalizationOptions _options;
        private readonly SemaphoreSlim _initLock;

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageResourceLoader"/> class.
        /// </summary>
        /// <param name="fileResourceProvider">The file-based resource provider.</param>
        /// <param name="assemblyResourceProvider">The assembly-based resource provider.</param>
        /// <param name="resourceProvider">The active resource provider.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The localization options.</param>
        public LanguageResourceLoader(
            ILanguageFileLoader fileResourceProvider,
            ILanguageEmbeddedResourceLoader assemblyResourceProvider,
            ILanguageResourceProvider resourceProvider,
            ILogger<LanguageResourceLoader> logger,
            IOptions<WebUiLocalizationOptions> options)
        {
            _fileResourceProvider = fileResourceProvider;
            _assemblyResourceProvider = assemblyResourceProvider;
            _resourceProvider = resourceProvider;
            _logger = logger;
            _options = options.Value;
            _initLock = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc />
        public async ValueTask EnsureInitialized(CancellationToken cancellationToken = default)
        {
            var cultureName = CultureInfo.CurrentUICulture.Name;
            if (string.Equals(_resourceProvider.Resources.LoadedCultureName, cultureName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await LoadLocaleAsync(cultureName, cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask LoadLocaleAsync(string locale, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(locale);

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (string.Equals(_resourceProvider.Resources.LoadedCultureName, locale, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var resources = await LoadResourcesAsync(locale, cancellationToken);
                _resourceProvider.SetResources(resources);
            }
            finally
            {
                _initLock.Release();
            }
        }

        private async ValueTask<LanguageResources> LoadResourcesAsync(string locale, CancellationToken cancellationToken = default)
        {
            var aliases = await _fileResourceProvider.LoadDictionaryAsync(_options.AliasFileName, cancellationToken)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);

            Dictionary<string, string>? translations = null;
            var loadedLocale = string.Empty;
            var baseLocale = GetBaseLocale(locale);

            if (string.Equals(baseLocale, "en", StringComparison.OrdinalIgnoreCase))
            {
                translations = await _assemblyResourceProvider.LoadDictionaryAsync("webui_en.json", cancellationToken);
                if (translations is not null)
                {
                    loadedLocale = "en";
                }
            }

            if (translations is null)
            {
                foreach (var candidateLocale in GetCandidateLocales(locale))
                {
                    var fileName = string.Format(CultureInfo.InvariantCulture, _options.BaseFileNameFormat, candidateLocale);
                    var baseTranslations = await _fileResourceProvider.LoadDictionaryAsync(fileName, cancellationToken);
                    if (baseTranslations is null)
                    {
                        continue;
                    }

                    translations = baseTranslations;
                    loadedLocale = candidateLocale;
                    break;
                }
            }

            if (translations is null)
            {
                _logger.LogWarning("WebUI translations not found for locale {Locale}.", locale);
                return new LanguageResources(
                    aliases,
                    new Dictionary<string, string>(StringComparer.Ordinal),
                    new Dictionary<string, string>(StringComparer.Ordinal),
                    locale);
            }

            var overridesFileName = string.Format(CultureInfo.InvariantCulture, _options.OverrideFileNameFormat, loadedLocale);
            var overrides = await _fileResourceProvider.LoadDictionaryAsync(overridesFileName, cancellationToken)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);

            return new LanguageResources(
                aliases,
                overrides,
                translations,
                locale);
        }

        private static List<string> GetCandidateLocales(string locale)
        {
            var candidates = new List<string>();
            var trimmedLocale = locale.Trim();

            if (!string.IsNullOrWhiteSpace(trimmedLocale))
            {
                candidates.Add(trimmedLocale);

                if (trimmedLocale.Contains('-', StringComparison.Ordinal))
                {
                    candidates.Add(trimmedLocale.Replace("-", "_", StringComparison.Ordinal));
                }
                else if (trimmedLocale.Contains('_', StringComparison.Ordinal))
                {
                    candidates.Add(trimmedLocale.Replace("_", "-", StringComparison.Ordinal));
                }

                var baseLocale = GetBaseLocale(trimmedLocale);
                if (!string.IsNullOrWhiteSpace(baseLocale))
                {
                    candidates.Add(baseLocale);
                }
            }

            candidates.Add("en");

            return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static string GetBaseLocale(string locale)
        {
            var trimmedLocale = locale.Trim();
            var atIndex = trimmedLocale.IndexOf('@', StringComparison.Ordinal);
            if (atIndex >= 0)
            {
                trimmedLocale = trimmedLocale[..atIndex];
            }

            var dashIndex = trimmedLocale.IndexOf('-', StringComparison.Ordinal);
            if (dashIndex >= 0)
            {
                return trimmedLocale[..dashIndex];
            }

            var underscoreIndex = trimmedLocale.IndexOf('_', StringComparison.Ordinal);
            if (underscoreIndex >= 0)
            {
                return trimmedLocale[..underscoreIndex];
            }

            return trimmedLocale;
        }
    }
}
