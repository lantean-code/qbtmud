namespace QbtMudTranslations
{
    internal sealed class ProtectedTranslationClient : ITranslationClient
    {
        private readonly ITranslationBackend _translationBackend;
        private readonly ITranslationTextProtector _translationTextProtector;

        public ProtectedTranslationClient(
            ITranslationBackend translationBackend,
            ITranslationTextProtector translationTextProtector)
        {
            _translationBackend = translationBackend;
            _translationTextProtector = translationTextProtector;
        }

        public async Task<IReadOnlyList<string>> TranslateAsync(string locale, IReadOnlyList<string> sourceTexts, CancellationToken cancellationToken)
        {
            if (sourceTexts.Count == 0)
            {
                return [];
            }

            var protectedTexts = _translationTextProtector.Protect(sourceTexts);
            var translatedTexts = await _translationBackend.TranslateAsync(
                locale,
                protectedTexts.Select(item => item.ProtectedText).ToArray(),
                cancellationToken);

            if (translatedTexts.Count != sourceTexts.Count)
            {
                throw new InvalidOperationException($"Translation count mismatch for locale '{locale}'.");
            }

            return translatedTexts
                .Select((text, index) => _translationTextProtector.Restore(text, protectedTexts[index].TokenMap))
                .ToArray();
        }
    }
}
