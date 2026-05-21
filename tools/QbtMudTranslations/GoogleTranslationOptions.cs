namespace QbtMudTranslations
{
    internal sealed class GoogleTranslationOptions
    {
        private const string DefaultBaseUrl = "https://translation.googleapis.com/language/translate/v2/";

        public required string ApiKey { get; init; }

        public required string BaseUrl { get; init; }

        public static GoogleTranslationOptions? FromEnvironment()
        {
            var apiKey = Environment.GetEnvironmentVariable("QBTMUD_TRANSLATIONS_GOOGLE_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return null;
            }

            return new GoogleTranslationOptions
            {
                ApiKey = apiKey,
                BaseUrl = NormalizeBaseUrl(Environment.GetEnvironmentVariable("QBTMUD_TRANSLATIONS_GOOGLE_BASE_URL"))
            };
        }

        private static string NormalizeBaseUrl(string? baseUrl)
        {
            var value = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim();
            return value.EndsWith("/", StringComparison.Ordinal) ? value : $"{value}/";
        }
    }
}
