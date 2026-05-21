namespace QbtMudTranslations
{
    internal sealed class TranslationProviderAvailabilityValidator
    {
        private readonly bool _hasAzureProvider;
        private readonly bool _hasGoogleProvider;

        public TranslationProviderAvailabilityValidator(bool hasAzureProvider, bool hasGoogleProvider)
        {
            _hasAzureProvider = hasAzureProvider;
            _hasGoogleProvider = hasGoogleProvider;
        }

        public IReadOnlyList<string> ValidateSyncConfiguration(IReadOnlyList<string> locales)
        {
            var errors = new List<string>();
            var nonEnglishLocales = locales.Where(locale => !CloudLocaleMapper.IsEnglishLocale(locale)).ToList();
            if (nonEnglishLocales.Count == 0)
            {
                return errors;
            }

            var requiresAzureProvider = nonEnglishLocales.Any(locale => CloudLocaleMapper.TryResolveAzureLocale(locale, out _));
            var requiresGoogleProvider = nonEnglishLocales.Any(locale => !CloudLocaleMapper.TryResolveAzureLocale(locale, out _));

            if (requiresAzureProvider && !_hasAzureProvider)
            {
                errors.Add(
                    "Sync mode requires Azure Translator configuration. Set QBTMUD_TRANSLATIONS_AZURE_API_KEY and QBTMUD_TRANSLATIONS_AZURE_REGION.");
            }

            if (requiresGoogleProvider && !_hasGoogleProvider)
            {
                errors.Add(
                    "Sync mode requires Google Cloud Translation configuration. Set QBTMUD_TRANSLATIONS_GOOGLE_API_KEY.");
            }

            foreach (var locale in nonEnglishLocales)
            {
                if (!CloudLocaleMapper.TryResolveAzureLocale(locale, out _) && !CloudLocaleMapper.TryResolveGoogleLocale(locale, out _))
                {
                    errors.Add($"Locale '{locale}' has no configured cloud translation route.");
                }
            }

            return errors;
        }
    }
}
