using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QbtMudTranslations
{
    internal sealed class GoogleTranslationBackend : ITranslationBackend
    {
        private const int MaxSegmentsPerRequest = 128;

        private readonly HttpClient _httpClient;
        private readonly GoogleTranslationOptions _options;

        public GoogleTranslationBackend(HttpClient httpClient, GoogleTranslationOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public async Task<IReadOnlyList<string>> TranslateAsync(string locale, IReadOnlyList<string> sourceTexts, CancellationToken cancellationToken)
        {
            var translations = new List<string>(sourceTexts.Count);
            foreach (var batch in Batch(sourceTexts))
            {
                using var request = BuildRequest(locale, batch);
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Google translation request failed ({(int)response.StatusCode}): {responseBody}");
                }

                using var document = JsonDocument.Parse(responseBody);
                if (!document.RootElement.TryGetProperty("data", out var dataElement)
                    || !dataElement.TryGetProperty("translations", out var translationsElement)
                    || translationsElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("Google translation response did not contain a translations array.");
                }

                var batchTranslations = translationsElement.EnumerateArray().Select(GetTranslatedText).ToArray();
                if (batchTranslations.Length != batch.Count)
                {
                    throw new InvalidOperationException($"Google translation response count mismatch for locale '{locale}'.");
                }

                translations.AddRange(batchTranslations);
            }

            return translations;
        }

        private HttpRequestMessage BuildRequest(string locale, IReadOnlyList<string> sourceTexts)
        {
            var uriBuilder = new UriBuilder(new Uri(_options.BaseUrl, UriKind.Absolute))
            {
                Query = $"key={Uri.EscapeDataString(_options.ApiKey)}"
            };
            var request = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri);

            var requestBody = new GoogleTranslationRequest
            {
                Queries = sourceTexts.ToArray(),
                Source = "en",
                Target = locale,
                Format = "html"
            };
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            return request;
        }

        private static string GetTranslatedText(JsonElement item)
        {
            if (!item.TryGetProperty("translatedText", out var translatedTextElement)
                || translatedTextElement.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException("Google translation response did not contain translatedText.");
            }

            return translatedTextElement.GetString() ?? string.Empty;
        }

        private static IEnumerable<IReadOnlyList<string>> Batch(IReadOnlyList<string> sourceTexts)
        {
            for (var i = 0; i < sourceTexts.Count; i += MaxSegmentsPerRequest)
            {
                var count = Math.Min(MaxSegmentsPerRequest, sourceTexts.Count - i);
                var batch = new string[count];
                for (var j = 0; j < count; j++)
                {
                    batch[j] = sourceTexts[i + j];
                }

                yield return batch;
            }
        }

        private sealed class GoogleTranslationRequest
        {
            [JsonPropertyName("format")]
            public required string Format { get; init; }

            [JsonPropertyName("q")]
            public required IReadOnlyList<string> Queries { get; init; }

            [JsonPropertyName("source")]
            public required string Source { get; init; }

            [JsonPropertyName("target")]
            public required string Target { get; init; }
        }
    }
}
