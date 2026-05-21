namespace QbtMudTranslations
{
    internal interface ITranslationTextProtector
    {
        IReadOnlyList<ProtectedTranslationText> Protect(IReadOnlyList<string> sourceTexts);

        string Restore(string translatedText, IReadOnlyDictionary<string, string> tokenMap);
    }
}
