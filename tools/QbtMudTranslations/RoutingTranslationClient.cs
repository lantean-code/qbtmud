namespace QbtMudTranslations
{
    internal sealed class RoutingTranslationClient : ITranslationClient
    {
        private readonly ITranslationClient? _azureTranslationClient;
        private readonly ITranslationClient? _googleTranslationClient;

        public RoutingTranslationClient(
            ITranslationClient? azureTranslationClient,
            ITranslationClient? googleTranslationClient)
        {
            _azureTranslationClient = azureTranslationClient;
            _googleTranslationClient = googleTranslationClient;
        }

        public async Task<IReadOnlyList<string>> TranslateAsync(string locale, IReadOnlyList<string> sourceTexts, CancellationToken cancellationToken)
        {
            if (sourceTexts.Count == 0)
            {
                return [];
            }

            if (CloudLocaleMapper.IsEnglishLocale(locale))
            {
                return sourceTexts.ToArray();
            }

            if (CloudLocaleMapper.TryResolveAzureLocale(locale, out var azureLocale) && !string.IsNullOrWhiteSpace(azureLocale))
            {
                if (_azureTranslationClient is null)
                {
                    throw new InvalidOperationException(
                        $"Locale '{locale}' requires Azure Translator. Set QBTMUD_TRANSLATIONS_AZURE_API_KEY and QBTMUD_TRANSLATIONS_AZURE_REGION.");
                }

                try
                {
                    return await _azureTranslationClient.TranslateAsync(azureLocale, sourceTexts, cancellationToken);
                }
                catch (TranslationBackendRejectedException)
                {
                    return await TranslateWithGoogleAsync(locale, sourceTexts, cancellationToken);
                }
            }

            return await TranslateWithGoogleAsync(locale, sourceTexts, cancellationToken);
        }

        private async Task<IReadOnlyList<string>> TranslateWithGoogleAsync(
            string locale,
            IReadOnlyList<string> sourceTexts,
            CancellationToken cancellationToken)
        {
            if (!CloudLocaleMapper.TryResolveGoogleLocale(locale, out var googleLocale) || string.IsNullOrWhiteSpace(googleLocale))
            {
                throw new InvalidOperationException($"Locale '{locale}' has no configured cloud translation route.");
            }

            if (_googleTranslationClient is null)
            {
                throw new InvalidOperationException(
                    $"Locale '{locale}' requires Google Cloud Translation. Set QBTMUD_TRANSLATIONS_GOOGLE_API_KEY.");
            }

            return await _googleTranslationClient.TranslateAsync(googleLocale, sourceTexts, cancellationToken);
        }
    }
}
