using Lantean.QBTMud.Services.Localization;
using System.Globalization;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Resolves the preferred locale, applies the corresponding culture, and loads language resources.
    /// </summary>
    public sealed class LanguageInitializationService : ILanguageInitializationService
    {
        private readonly ILocalStorageService _localStorage;
        private readonly ILanguageResourceLoader _languageResourceLoader;
        private readonly ILogger<LanguageInitializationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageInitializationService"/> class.
        /// </summary>
        /// <param name="localStorage">The local storage service.</param>
        /// <param name="languageResourceLoader">The language resource loader.</param>
        /// <param name="logger">The logger instance.</param>
        public LanguageInitializationService(
            ILocalStorageService localStorage,
            ILanguageResourceLoader languageResourceLoader,
            ILogger<LanguageInitializationService> logger)
        {
            _localStorage = localStorage;
            _languageResourceLoader = languageResourceLoader;
            _logger = logger;
        }

        /// <inheritdoc />
        public async ValueTask EnsureLanguageResourcesInitialized(CancellationToken cancellationToken = default)
        {
            var locale = await ResolvePreferredLocaleAsync(cancellationToken);
            ApplyCulture(locale);
            await _languageResourceLoader.LoadLocaleAsync(locale, cancellationToken);
        }

        private async ValueTask<string> ResolvePreferredLocaleAsync(CancellationToken cancellationToken)
        {
            var locale = await _localStorage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, cancellationToken);
            if (!string.IsNullOrWhiteSpace(locale))
            {
                return locale.Trim();
            }

            if (!string.IsNullOrWhiteSpace(CultureInfo.CurrentUICulture.Name))
            {
                return CultureInfo.CurrentUICulture.Name;
            }

            if (!string.IsNullOrWhiteSpace(CultureInfo.CurrentCulture.Name))
            {
                return CultureInfo.CurrentCulture.Name;
            }

            return "en";
        }

        private void ApplyCulture(string locale)
        {
            var normalized = NormalizeLocaleForCulture(locale);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            try
            {
                var culture = CultureInfo.GetCultureInfo(normalized);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            catch (CultureNotFoundException ex)
            {
                _logger.LogWarning(ex, "Unable to apply culture {Locale}.", normalized);
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
            if (string.IsNullOrWhiteSpace(script))
            {
                return basePart;
            }

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

            return string.Empty;
        }
    }
}
