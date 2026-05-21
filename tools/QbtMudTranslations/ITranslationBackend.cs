namespace QbtMudTranslations
{
    internal interface ITranslationBackend
    {
        Task<IReadOnlyList<string>> TranslateAsync(string locale, IReadOnlyList<string> sourceTexts, CancellationToken cancellationToken);
    }
}
