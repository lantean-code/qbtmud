namespace QbtMudTranslations
{
    internal sealed class AzureTranslationTextProtector : ITranslationTextProtector
    {
        public IReadOnlyList<ProtectedTranslationText> Protect(IReadOnlyList<string> sourceTexts)
        {
            return TranslationTextProtector.ProtectForAzure(sourceTexts);
        }

        public string Restore(string translatedText, IReadOnlyDictionary<string, string> tokenMap)
        {
            return TranslationTextProtector.RestoreFromHtml(translatedText, tokenMap);
        }
    }
}
