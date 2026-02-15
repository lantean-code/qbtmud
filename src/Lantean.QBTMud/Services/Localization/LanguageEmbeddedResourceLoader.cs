using System.Text.Json;

namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Loads WebUI localization dictionaries from embedded resources in the current assembly.
    /// </summary>
    public sealed class LanguageEmbeddedResourceLoader : ILanguageEmbeddedResourceLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly IAssemblyResourceAccessor _resourceAccessor;
        private readonly ILogger<LanguageEmbeddedResourceLoader> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageEmbeddedResourceLoader"/> class.
        /// </summary>
        /// <param name="resourceAccessor">The accessor used to enumerate and open embedded resources.</param>
        /// <param name="logger">The logger instance.</param>
        public LanguageEmbeddedResourceLoader(
            IAssemblyResourceAccessor resourceAccessor,
            ILogger<LanguageEmbeddedResourceLoader> logger)
        {
            _resourceAccessor = resourceAccessor;
            _logger = logger;
        }

        /// <inheritdoc />
        public ValueTask<Dictionary<string, string>?> LoadDictionaryAsync(string fileName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var suffix = string.Concat("wwwroot.i18n.", fileName);
            var resourceName = _resourceAccessor
                .GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return ValueTask.FromResult<Dictionary<string, string>?>(null);
            }

            using var stream = _resourceAccessor.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                return ValueTask.FromResult<Dictionary<string, string>?>(null);
            }

            try
            {
                return ValueTask.FromResult(JsonSerializer.Deserialize<Dictionary<string, string>>(stream, JsonOptions));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse embedded translation resource {ResourceName}.", resourceName);
                return ValueTask.FromResult<Dictionary<string, string>?>(null);
            }
        }
    }
}
