using System.Net;
using System.Text;
using System.Text.Json;

namespace QbtMudTranslations
{
    internal sealed class AzureTranslationBackend : ITranslationBackend
    {
        private const int MaxSegmentsPerRequest = 50;

        private readonly HttpClient _httpClient;
        private readonly AzureTranslationOptions _options;

        public AzureTranslationBackend(HttpClient httpClient, AzureTranslationOptions options)
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
                    if ((int)response.StatusCode is >= 400 and < 500)
                    {
                        throw new TranslationBackendRejectedException(
                            $"Azure translation request failed ({(int)response.StatusCode}) for locale '{locale}': {responseBody}",
                            response.StatusCode);
                    }

                    throw new InvalidOperationException($"Azure translation request failed ({(int)response.StatusCode}): {responseBody}");
                }

                using var document = JsonDocument.Parse(responseBody);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    throw new InvalidOperationException("Azure translation response did not contain a result array.");
                }

                var batchTranslations = document.RootElement
                    .EnumerateArray()
                    .Select(GetTranslatedText)
                    .ToArray();

                if (batchTranslations.Length != batch.Count)
                {
                    throw new InvalidOperationException($"Azure translation response count mismatch for locale '{locale}'.");
                }

                translations.AddRange(batchTranslations);
            }

            return translations;
        }

        private HttpRequestMessage BuildRequest(string locale, IReadOnlyList<string> sourceTexts)
        {
            var requestUri = new Uri(
                new Uri(_options.BaseUrl, UriKind.Absolute),
                $"translate?api-version=3.0&from=en&to={Uri.EscapeDataString(locale)}&textType=html");
            var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _options.ApiKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", _options.Region);

            var requestBody = sourceTexts
                .Select(text => new AzureTranslationRequestItem
                {
                    Text = text
                })
                .ToArray();
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            return request;
        }

        private static string GetTranslatedText(JsonElement item)
        {
            if (!item.TryGetProperty("translations", out var translationsElement)
                || translationsElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Azure translation response did not contain translations.");
            }

            var translationElement = translationsElement.EnumerateArray().FirstOrDefault();
            if (translationElement.ValueKind != JsonValueKind.Object
                || !translationElement.TryGetProperty("text", out var textElement)
                || textElement.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException("Azure translation response did not contain translation text.");
            }

            return textElement.GetString() ?? string.Empty;
        }

        private sealed class AzureTranslationRequestItem
        {
            public required string Text { get; init; }
        }

        private static IEnumerable<IReadOnlyList<string>> Batch(IReadOnlyList<string> sourceTexts)
        {
            var batch = new List<string>(MaxSegmentsPerRequest);

            foreach (var sourceText in sourceTexts)
            {
                if (RequiresIsolatedRequest(sourceText))
                {
                    if (batch.Count > 0)
                    {
                        yield return batch.ToArray();
                        batch.Clear();
                    }

                    yield return [sourceText];
                    continue;
                }

                batch.Add(sourceText);
                if (batch.Count == MaxSegmentsPerRequest)
                {
                    yield return batch.ToArray();
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                yield return batch.ToArray();
            }
        }

        private static bool RequiresIsolatedRequest(string sourceText)
        {
            return sourceText.Contains("QBTMUDTOKEN", StringComparison.Ordinal)
                || sourceText.Contains("<mstrans:dictionary", StringComparison.Ordinal)
                || sourceText.Contains("class=\"notranslate\"", StringComparison.Ordinal)
                || sourceText.Contains("translate=\"no\"", StringComparison.Ordinal);
        }
    }
}
