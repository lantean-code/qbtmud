namespace QbtMudTranslations
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            using var httpClient = new HttpClient();
            var parser = new ToolOptionsParser();
            var localeDiscoveryService = new LocaleDiscoveryService();
            ITranslationClient? azureTranslationClient = null;
            var azureOptions = AzureTranslationOptions.FromEnvironment();
            if (azureOptions is not null)
            {
                azureTranslationClient = new ProtectedTranslationClient(
                    new AzureTranslationBackend(httpClient, azureOptions),
                    new AzureTranslationTextProtector());
            }

            ITranslationClient? googleTranslationClient = null;
            var googleOptions = GoogleTranslationOptions.FromEnvironment();
            if (googleOptions is not null)
            {
                googleTranslationClient = new ProtectedTranslationClient(
                    new GoogleTranslationBackend(httpClient, googleOptions),
                    new GoogleTranslationTextProtector());
            }

            var translationClient = new RoutingTranslationClient(azureTranslationClient, googleTranslationClient);
            var retryOptions = TranslationRetryOptions.FromEnvironment();
            var validator = new TranslationValidator();
            var availabilityValidator = new TranslationProviderAvailabilityValidator(
                azureTranslationClient is not null,
                googleTranslationClient is not null);
            var syncService = new TranslationSyncService(
                localeDiscoveryService,
                translationClient,
                validator,
                availabilityValidator,
                retryOptions.MaxValidationRetries);

            var tool = new TranslationTool(parser, syncService);
            return await tool.RunAsync(args, CancellationToken.None);
        }
    }
}
