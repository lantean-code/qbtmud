namespace QbtMudTranslations
{
    internal sealed class TranslationRetryOptions
    {
        private const int DefaultMaxValidationRetries = 1;
        private const int MaximumAllowedValidationRetries = 3;

        public required int MaxValidationRetries { get; init; }

        public static TranslationRetryOptions FromEnvironment()
        {
            var maxValidationRetriesValue = Environment.GetEnvironmentVariable("QBTMUD_TRANSLATIONS_MAX_VALIDATION_RETRIES");
            if (string.IsNullOrWhiteSpace(maxValidationRetriesValue))
            {
                return new TranslationRetryOptions
                {
                    MaxValidationRetries = DefaultMaxValidationRetries
                };
            }

            if (!int.TryParse(maxValidationRetriesValue, out var maxValidationRetries)
                || maxValidationRetries < 0
                || maxValidationRetries > MaximumAllowedValidationRetries)
            {
                throw new InvalidOperationException(
                    "QBTMUD_TRANSLATIONS_MAX_VALIDATION_RETRIES must be an integer between 0 and 3.");
            }

            return new TranslationRetryOptions
            {
                MaxValidationRetries = maxValidationRetries
            };
        }
    }
}
