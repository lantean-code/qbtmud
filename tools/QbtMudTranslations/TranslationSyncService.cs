namespace QbtMudTranslations
{
    internal sealed class TranslationSyncService
    {
        private readonly LocaleDiscoveryService _localeDiscoveryService;
        private readonly int _maxValidationRetries;
        private readonly ITranslationClient _translationClient;
        private readonly TranslationProviderAvailabilityValidator _translationProviderAvailabilityValidator;
        private readonly TranslationValidator _translationValidator;

        public TranslationSyncService(
            LocaleDiscoveryService localeDiscoveryService,
            ITranslationClient translationClient,
            TranslationValidator translationValidator,
            TranslationProviderAvailabilityValidator translationProviderAvailabilityValidator,
            int maxValidationRetries)
        {
            _localeDiscoveryService = localeDiscoveryService;
            _maxValidationRetries = maxValidationRetries;
            _translationClient = translationClient;
            _translationValidator = translationValidator;
            _translationProviderAvailabilityValidator = translationProviderAvailabilityValidator;
        }

        public async Task<int> RunAsync(ToolOptions options, CancellationToken cancellationToken)
        {
            var englishEntries = JsonTranslationDocument.LoadOrdered(options.EnglishFilePath);
            var englishTranslations = englishEntries.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            var locales = _localeDiscoveryService.GetSupportedLocales(options.UpstreamTranslationsPath);

            if (options.Mode == TranslationToolMode.Sync)
            {
                var providerAvailabilityErrors = _translationProviderAvailabilityValidator.ValidateSyncConfiguration(locales);
                if (providerAvailabilityErrors.Count > 0)
                {
                    WriteErrors(providerAvailabilityErrors);
                    return 1;
                }

                var failedLocales = locales.ToList();
                var localeErrors = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

                for (var attempt = 0; attempt <= _maxValidationRetries && failedLocales.Count > 0; attempt++)
                {
                    var localesToProcess = failedLocales.ToList();
                    failedLocales.Clear();

                    foreach (var locale in localesToProcess)
                    {
                        var localeErrorsForAttempt = await TryWriteLocaleFileAsync(
                            options.OutputDirectoryPath,
                            locale,
                            englishEntries,
                            englishTranslations,
                            cancellationToken);
                        if (localeErrorsForAttempt.Count == 0)
                        {
                            localeErrors.Remove(locale);
                            continue;
                        }

                        failedLocales.Add(locale);
                        localeErrors[locale] = localeErrorsForAttempt;
                    }
                }

                if (failedLocales.Count > 0)
                {
                    WriteErrors(failedLocales.SelectMany(locale => localeErrors[locale]));
                    return 1;
                }
            }

            var postValidationErrors = ValidateExistingLocales(locales, englishTranslations, options.OutputDirectoryPath);
            if (postValidationErrors.Count > 0)
            {
                WriteErrors(postValidationErrors);
                return 1;
            }

            return 0;
        }

        private async Task<Dictionary<string, string>> BuildLocaleTranslationsAsync(
            string locale,
            IReadOnlyList<KeyValuePair<string, string>> englishEntries,
            IReadOnlyDictionary<string, string> existingLocaleTranslations,
            CancellationToken cancellationToken)
        {
            if (EnglishVariantTranslationBuilder.IsDerivedLocale(locale))
            {
                return EnglishVariantTranslationBuilder.BuildLocaleTranslations(locale, englishEntries);
            }

            if (string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase))
            {
                return englishEntries.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            }

            var localeTranslations = englishEntries
                .Where(entry => existingLocaleTranslations.TryGetValue(entry.Key, out var translation) && !string.IsNullOrWhiteSpace(translation))
                .ToDictionary(entry => entry.Key, entry => existingLocaleTranslations[entry.Key], StringComparer.Ordinal);

            var entriesToTranslate = englishEntries
                .Where(entry => !localeTranslations.ContainsKey(entry.Key))
                .ToList();
            if (entriesToTranslate.Count == 0)
            {
                return localeTranslations;
            }

            var sourceTexts = entriesToTranslate.Select(entry => entry.Value).ToArray();
            var translatedTexts = await _translationClient.TranslateAsync(locale, sourceTexts, cancellationToken);
            if (translatedTexts.Count != sourceTexts.Length)
            {
                throw new InvalidOperationException($"Translation count mismatch for locale '{locale}'.");
            }

            for (var i = 0; i < entriesToTranslate.Count; i++)
            {
                localeTranslations[entriesToTranslate[i].Key] = translatedTexts[i];
            }

            return localeTranslations;
        }

        private async Task<(Dictionary<string, string> LocaleTranslations, IReadOnlyList<string> ValidationErrors)> BuildValidatedLocaleTranslationsAsync(
            string locale,
            IReadOnlyList<KeyValuePair<string, string>> englishEntries,
            IReadOnlyDictionary<string, string> englishTranslations,
            IReadOnlyDictionary<string, string> existingLocaleTranslations,
            CancellationToken cancellationToken)
        {
            Dictionary<string, string>? lastLocaleTranslations = null;
            IReadOnlyList<string> lastValidationErrors = [];
            IReadOnlyDictionary<string, string> seedLocaleTranslations = existingLocaleTranslations;

            for (var attempt = 0; attempt <= _maxValidationRetries; attempt++)
            {
                var localeTranslations = await BuildLocaleTranslationsAsync(
                    locale,
                    englishEntries,
                    seedLocaleTranslations,
                    cancellationToken);
                var validationErrors = _translationValidator.ValidateLocale(locale, englishTranslations, localeTranslations);
                if (validationErrors.Count == 0)
                {
                    return (localeTranslations, validationErrors);
                }

                lastLocaleTranslations = localeTranslations;
                lastValidationErrors = validationErrors;

                var invalidKeys = _translationValidator.GetInvalidKeys(locale, englishTranslations, localeTranslations);
                if (invalidKeys.Count == 0)
                {
                    break;
                }

                seedLocaleTranslations = localeTranslations
                    .Where(entry => !invalidKeys.Contains(entry.Key))
                    .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            }

            return (lastLocaleTranslations ?? new Dictionary<string, string>(StringComparer.Ordinal), lastValidationErrors);
        }

        private async Task<IReadOnlyList<string>> TryWriteLocaleFileAsync(
            string outputDirectoryPath,
            string locale,
            IReadOnlyList<KeyValuePair<string, string>> englishEntries,
            IReadOnlyDictionary<string, string> englishTranslations,
            CancellationToken cancellationToken)
        {
            try
            {
                var filePath = BuildOutputPath(outputDirectoryPath, locale);
                var existingLocaleTranslations = File.Exists(filePath)
                    ? JsonTranslationDocument.LoadDictionary(filePath)
                    : new Dictionary<string, string>(StringComparer.Ordinal);
                var (localeTranslations, validationErrors) = await BuildValidatedLocaleTranslationsAsync(
                    locale,
                    englishEntries,
                    englishTranslations,
                    existingLocaleTranslations,
                    cancellationToken);
                if (validationErrors.Count > 0)
                {
                    return validationErrors;
                }

                JsonTranslationDocument.WriteOrdered(filePath, englishEntries, localeTranslations);
                Console.WriteLine($"Wrote {filePath}");
                return [];
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return [$"{locale}: {ex.Message}"];
            }
        }

        private IReadOnlyList<string> ValidateExistingLocales(
            IReadOnlyList<string> locales,
            IReadOnlyDictionary<string, string> englishTranslations,
            string outputDirectoryPath)
        {
            var errors = new List<string>();
            foreach (var locale in locales)
            {
                var filePath = BuildOutputPath(outputDirectoryPath, locale);
                if (!File.Exists(filePath))
                {
                    errors.Add($"{locale}: missing file '{filePath}'.");
                    continue;
                }

                var localeTranslations = JsonTranslationDocument.LoadDictionary(filePath);
                errors.AddRange(_translationValidator.ValidateLocale(locale, englishTranslations, localeTranslations));
            }

            return errors;
        }

        private static string BuildOutputPath(string outputDirectoryPath, string locale)
        {
            return Path.Combine(outputDirectoryPath, $"qbtmud_{locale}.json");
        }

        private static void WriteErrors(IEnumerable<string> errors)
        {
            foreach (var error in errors)
            {
                Console.Error.WriteLine(error);
            }
        }
    }
}
