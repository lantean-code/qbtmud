using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Loads WebUI localization dictionaries from HTTP-accessible files.
    /// </summary>
    public sealed class LanguageFileLoader : ILanguageFileLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<LanguageFileLoader> _logger;
        private readonly WebUiLocalizationOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageFileLoader"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="options">The localization options.</param>
        public LanguageFileLoader(
            IHttpClientFactory httpClientFactory,
            ILogger<LanguageFileLoader> logger,
            IOptions<WebUiLocalizationOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient("WebUiAssets");
            _logger = logger;
            _options = options.Value;
        }

        /// <inheritdoc />
        public async ValueTask<Dictionary<string, string>?> LoadDictionaryAsync(string fileName, CancellationToken cancellationToken = default)
        {
            var relativePath = BuildPath(fileName);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.GetAsync(relativePath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to request translation file {Path}.", relativePath);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to load translation file {Path}. Status: {StatusCode}", relativePath, response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            try
            {
                var data = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, JsonOptions, cancellationToken);
                return data ?? new Dictionary<string, string>(StringComparer.Ordinal);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse translation file {Path}.", relativePath);
                return null;
            }
        }

        private string BuildPath(string fileName)
        {
            var basePath = _options.BasePath.TrimEnd('/');
            return string.Concat(basePath, '/', fileName);
        }
    }
}
