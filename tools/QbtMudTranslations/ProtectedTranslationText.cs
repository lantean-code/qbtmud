namespace QbtMudTranslations
{
    internal sealed class ProtectedTranslationText
    {
        public required string SourceText { get; init; }

        public required string ProtectedText { get; init; }

        public required IReadOnlyDictionary<string, string> TokenMap { get; init; }
    }
}
