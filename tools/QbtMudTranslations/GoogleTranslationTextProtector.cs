namespace QbtMudTranslations
{
    internal sealed class GoogleTranslationTextProtector : ITranslationTextProtector
    {
        public IReadOnlyList<ProtectedTranslationText> Protect(IReadOnlyList<string> sourceTexts)
        {
            return TranslationTextProtector.ProtectForGoogle(sourceTexts);
        }

        public string Restore(string translatedText, IReadOnlyDictionary<string, string> tokenMap)
        {
            return TranslationTextProtector.RestoreFromHtml(translatedText, tokenMap);
        }
    }
}
