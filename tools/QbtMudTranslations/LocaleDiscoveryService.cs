namespace QbtMudTranslations
{
    internal sealed class LocaleDiscoveryService
    {
        public IReadOnlyList<string> GetSupportedLocales(string upstreamTranslationsPath)
        {
            if (!Directory.Exists(upstreamTranslationsPath))
            {
                throw new DirectoryNotFoundException($"Upstream translations directory not found: {upstreamTranslationsPath}");
            }

            return Directory.EnumerateFiles(upstreamTranslationsPath, "webui_*.ts", SearchOption.TopDirectoryOnly)
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .Where(fileName => fileName.StartsWith("webui_", StringComparison.Ordinal))
                .Select(fileName => fileName["webui_".Length..])
                .Where(locale => !string.IsNullOrWhiteSpace(locale))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(locale => locale, StringComparer.Ordinal)
                .ToList();
        }
    }
}
