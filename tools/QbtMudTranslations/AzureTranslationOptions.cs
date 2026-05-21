namespace QbtMudTranslations
{
    internal sealed class AzureTranslationOptions
    {
        private const string DefaultBaseUrl = "https://api.cognitive.microsofttranslator.com/";

        public required string ApiKey { get; init; }

        public required string BaseUrl { get; init; }

        public required string Region { get; init; }

        public static AzureTranslationOptions? FromEnvironment()
        {
            var apiKey = Environment.GetEnvironmentVariable("QBTMUD_TRANSLATIONS_AZURE_API_KEY");
            var region = Environment.GetEnvironmentVariable("QBTMUD_TRANSLATIONS_AZURE_REGION");
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(region))
            {
                return null;
            }

            return new AzureTranslationOptions
            {
                ApiKey = apiKey,
                BaseUrl = NormalizeBaseUrl(Environment.GetEnvironmentVariable("QBTMUD_TRANSLATIONS_AZURE_BASE_URL")),
                Region = region.Trim()
            };
        }

        private static string NormalizeBaseUrl(string? baseUrl)
        {
            var value = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim();
            return value.EndsWith("/", StringComparison.Ordinal) ? value : $"{value}/";
        }
    }
}
