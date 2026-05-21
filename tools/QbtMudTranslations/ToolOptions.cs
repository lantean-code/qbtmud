namespace QbtMudTranslations
{
    internal sealed class ToolOptions
    {
        public required string EnglishFilePath { get; init; }

        public required string OutputDirectoryPath { get; init; }

        public required string UpstreamTranslationsPath { get; init; }

        public TranslationToolMode Mode { get; init; }
    }
}
