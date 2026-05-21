namespace QbtMudTranslations
{
    internal sealed class TranslationTool
    {
        private readonly ToolOptionsParser _parser;
        private readonly TranslationSyncService _syncService;

        public TranslationTool(
            ToolOptionsParser parser,
            TranslationSyncService syncService)
        {
            _parser = parser;
            _syncService = syncService;
        }

        public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
        {
            var options = _parser.Parse(args);
            if (options is null)
            {
                Console.WriteLine(_parser.GetUsageText());
                return 2;
            }

            try
            {
                return await _syncService.RunAsync(options, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }
    }
}
