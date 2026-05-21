using System.Net;
using System.Text;
using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class GoogleTranslationBackendTests
    {
        [Fact]
        public async Task GIVEN_ResponsePayload_WHEN_TranslateAsync_THEN_ShouldParseTranslationsArray()
        {
            var messageHandler = new Mock<HttpMessageHandler>();
            HttpRequestMessage? capturedRequest = null;
            string? capturedRequestBody = null;
            messageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    capturedRequest = request;
                    capturedRequestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
{
  "data": {
    "translations": [
      {
        "translatedText": "Uno"
      },
      {
        "translatedText": "Dos"
      }
    ]
  }
}
""", Encoding.UTF8, "application/json")
                });
            using var httpClient = new HttpClient(messageHandler.Object);
            var target = new GoogleTranslationBackend(
                httpClient,
                new GoogleTranslationOptions
                {
                    ApiKey = "ApiKey",
                    BaseUrl = "https://translation.googleapis.com/language/translate/v2/"
                });

            var result = await target.TranslateAsync(
                "es",
                ["<div><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN0</span></div>", "<div><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN1</span></div>"],
                TestContext.Current.CancellationToken);

            result.Should().Equal("Uno", "Dos");
            capturedRequest.Should().NotBeNull();
            capturedRequest!.Method.Should().Be(HttpMethod.Post);
            capturedRequest.RequestUri.Should().Be(new Uri("https://translation.googleapis.com/language/translate/v2/?key=ApiKey"));
            capturedRequestBody.Should().Contain("\"format\":\"html\"");
            capturedRequestBody.Should().Contain("\\u003Cspan class=\\u0022notranslate\\u0022 translate=\\u0022no\\u0022\\u003EQBTMUDTOKEN0\\u003C/span\\u003E");
        }

        [Fact]
        public async Task GIVEN_NonSuccessResponse_WHEN_TranslateAsync_THEN_ShouldThrowInvalidOperationException()
        {
            var messageHandler = new Mock<HttpMessageHandler>();
            messageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Request failed", Encoding.UTF8, "application/json")
                });
            using var httpClient = new HttpClient(messageHandler.Object);
            var target = new GoogleTranslationBackend(
                httpClient,
                new GoogleTranslationOptions
                {
                    ApiKey = "ApiKey",
                    BaseUrl = "https://translation.googleapis.com/language/translate/v2/"
                });

            var action = async () => await target.TranslateAsync("es", ["One"], TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Request failed*");
        }

        [Fact]
        public async Task GIVEN_MoreThan128SourceTexts_WHEN_TranslateAsync_THEN_ShouldBatchRequestsAndPreserveOrder()
        {
            var messageHandler = new Mock<HttpMessageHandler>();
            messageHandler
                .Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildResponseBody(0, 128), Encoding.UTF8, "application/json")
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildResponseBody(128, 1), Encoding.UTF8, "application/json")
                });
            using var httpClient = new HttpClient(messageHandler.Object);
            var target = new GoogleTranslationBackend(
                httpClient,
                new GoogleTranslationOptions
                {
                    ApiKey = "ApiKey",
                    BaseUrl = "https://translation.googleapis.com/language/translate/v2/"
                });
            var sourceTexts = Enumerable.Range(0, 129)
                .Select(index => $"Source-{index}")
                .ToArray();

            var result = await target.TranslateAsync("es", sourceTexts, TestContext.Current.CancellationToken);

            result.Should().HaveCount(129);
            result[0].Should().Be("Translated-0");
            result[127].Should().Be("Translated-127");
            result[128].Should().Be("Translated-128");
            messageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Exactly(2),
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GIVEN_ResponseCountMismatch_WHEN_TranslateAsync_THEN_ShouldThrowInvalidOperationException()
        {
            var messageHandler = new Mock<HttpMessageHandler>();
            messageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
{
  "data": {
    "translations": [
      {
        "translatedText": "Uno"
      }
    ]
  }
}
""", Encoding.UTF8, "application/json")
                });
            using var httpClient = new HttpClient(messageHandler.Object);
            var target = new GoogleTranslationBackend(
                httpClient,
                new GoogleTranslationOptions
                {
                    ApiKey = "ApiKey",
                    BaseUrl = "https://translation.googleapis.com/language/translate/v2/"
                });

            var action = async () => await target.TranslateAsync("es", ["One", "Two"], TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*count mismatch*");
        }

        private static string BuildResponseBody(int startIndex, int count)
        {
            var translations = Enumerable.Range(startIndex, count)
                .Select(index => $$"""
      {
        "translatedText": "Translated-{{index}}"
      }
""");

            return $$"""
{
  "data": {
    "translations": [
{{string.Join(",\n", translations)}}
    ]
  }
}
""";
        }
    }
}
