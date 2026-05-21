namespace QbtMudTranslations
{
    internal sealed class ToolOptionsParser
    {
        public ToolOptions? Parse(string[] args)
        {
            string? englishFilePath = null;
            string? outputDirectoryPath = null;
            string? upstreamTranslationsPath = null;
            var mode = TranslationToolMode.Sync;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--english":
                        if (!TryReadValue(args, ref i, out englishFilePath))
                        {
                            return null;
                        }

                        break;

                    case "--output":
                        if (!TryReadValue(args, ref i, out outputDirectoryPath))
                        {
                            return null;
                        }

                        break;

                    case "--upstream":
                        if (!TryReadValue(args, ref i, out upstreamTranslationsPath))
                        {
                            return null;
                        }

                        break;

                    case "--mode":
                        if (!TryReadValue(args, ref i, out var modeValue))
                        {
                            return null;
                        }

                        if (!Enum.TryParse<TranslationToolMode>(modeValue, ignoreCase: true, out mode))
                        {
                            return null;
                        }

                        break;

                    case "--help":
                    case "-h":
                        return null;

                    default:
                        return null;
                }
            }

            if (string.IsNullOrWhiteSpace(englishFilePath)
                || string.IsNullOrWhiteSpace(outputDirectoryPath)
                || string.IsNullOrWhiteSpace(upstreamTranslationsPath))
            {
                return null;
            }

            return new ToolOptions
            {
                EnglishFilePath = englishFilePath,
                OutputDirectoryPath = outputDirectoryPath,
                UpstreamTranslationsPath = upstreamTranslationsPath,
                Mode = mode
            };
        }

        public string GetUsageText()
        {
            return "Usage:\r\n  dotnet run --project tools/QbtMudTranslations -- --english <path> --output <path> --upstream <path> [--mode Sync|Validate]";
        }

        private static bool TryReadValue(string[] args, ref int index, out string? value)
        {
            if (index + 1 >= args.Length || string.IsNullOrWhiteSpace(args[index + 1]))
            {
                value = null;
                return false;
            }

            value = args[++index];
            return true;
        }
    }
}
