namespace QbtMudTranslations
{
    internal interface ITranslationClient
    {
        Task<IReadOnlyList<string>> TranslateAsync(string locale, IReadOnlyList<string> sourceTexts, CancellationToken cancellationToken);
    }
}
