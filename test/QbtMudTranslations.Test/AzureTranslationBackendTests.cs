using System.Net;
using System.Text;
using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class AzureTranslationBackendTests
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
[
  {
    "translations": [
      {
        "text": "Uno",
        "to": "es"
      }
    ]
  },
  {
    "translations": [
      {
        "text": "Dos",
        "to": "es"
      }
    ]
  }
]
""", Encoding.UTF8, "application/json")
                });
            using var httpClient = new HttpClient(messageHandler.Object);
            var target = new AzureTranslationBackend(
                httpClient,
                new AzureTranslationOptions
                {
                    ApiKey = "ApiKey",
                    BaseUrl = "https://api.cognitive.microsofttranslator.com/",
                    Region = "westeurope"
                });

            var result = await target.TranslateAsync(
                "es",
                ["<div>One</div>", "<div>Two</div>"],
                TestContext.Current.CancellationToken);

            result.Should().Equal("Uno", "Dos");
            capturedRequest.Should().NotBeNull();
            capturedRequest!.Method.Should().Be(HttpMethod.Post);
            capturedRequest.RequestUri.Should().Be(new Uri("https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from=en&to=es&textType=html"));
            capturedRequest.Headers.Contains("Ocp-Apim-Subscription-Key").Should().BeTrue();
            capturedRequest.Headers.Contains("Ocp-Apim-Subscription-Region").Should().BeTrue();
            capturedRequestBody.Should().Contain("\\u003Cdiv\\u003EOne\\u003C/div\\u003E");
            capturedRequestBody.Should().Contain("\\u003Cdiv\\u003ETwo\\u003C/div\\u003E");
        }

        [Fact]
        public async Task GIVEN_ClientErrorResponse_WHEN_TranslateAsync_THEN_ShouldThrowRejectedException()
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
                    Content = new StringContent("Bad request", Encoding.UTF8, "application/json")
                });
            using var httpClient = new HttpClient(messageHandler.Object);
            var target = new AzureTranslationBackend(
                httpClient,
                new AzureTranslationOptions
                {
                    ApiKey = "ApiKey",
                    BaseUrl = "https://api.cognitive.microsofttranslator.com/",
                    Region = "westeurope"
                });

            var action = async () => await target.TranslateAsync("es", ["One"], TestContext.Current.CancellationToken);

            var exception = await action.Should().ThrowAsync<TranslationBackendRejectedException>();
            exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GIVEN_ServerErrorResponse_WHEN_TranslateAsync_THEN_ShouldThrowInvalidOperationException()
        {
            var messageHandler = new Mock<HttpMessageHandler>();
            messageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("Server failed", Encoding.UTF8, "application/json")
                });
            using var httpClient = new HttpClient(messageHandler.Object);
            var target = new AzureTranslationBackend(
                httpClient,
                new AzureTranslationOptions
                {
                    ApiKey = "ApiKey",
                    BaseUrl = "https://api.cognitive.microsofttranslator.com/",
                    Region = "westeurope"
                });

            var action = async () => await target.TranslateAsync("es", ["One"], TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Server failed*");
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
[
  {
    "translations": [
      {
        "text": "Uno",
        "to": "es"
      }
    ]
  }
]
""", Encoding.UTF8, "application/json")
                });
            using var httpClient = new HttpClient(messageHandler.Object);
            var target = new AzureTranslationBackend(
                httpClient,
                new AzureTranslationOptions
                {
                    ApiKey = "ApiKey",
                    BaseUrl = "https://api.cognitive.microsofttranslator.com/",
                    Region = "westeurope"
                });

            var action = async () => await target.TranslateAsync("es", ["One", "Two"], TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*count mismatch*");
        }

        [Fact]
        public async Task GIVEN_MoreThan50SourceTexts_WHEN_TranslateAsync_THEN_ShouldBatchRequestsAndPreserveOrder()
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
                    Content = new StringContent(BuildResponseBody(0, 50), Encoding.UTF8, "application/json")
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildResponseBody(50, 1), Encoding.UTF8, "application/json")
                });
            using var httpClient = new HttpClient(messageHandler.Object);
            var target = new AzureTranslationBackend(
                httpClient,
                new AzureTranslationOptions
                {
                    ApiKey = "ApiKey",
                    BaseUrl = "https://api.cognitive.microsofttranslator.com/",
                    Region = "westeurope"
                });
            var sourceTexts = Enumerable.Range(0, 51)
                .Select(index => $"Source-{index}")
                .ToArray();

            var result = await target.TranslateAsync("es", sourceTexts, TestContext.Current.CancellationToken);

            result.Should().HaveCount(51);
            result[0].Should().Be("Translated-0");
            result[49].Should().Be("Translated-49");
            result[50].Should().Be("Translated-50");
            messageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Exactly(2),
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GIVEN_ProtectedSourceTexts_WHEN_TranslateAsync_THEN_ShouldSendEachProtectedTextInItsOwnRequest()
        {
            var requestBodies = new List<string>();
            var messageHandler = new Mock<HttpMessageHandler>();
            messageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    requestBodies.Add(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
                })
                .ReturnsAsync(() => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
[
  {
    "translations": [
      {
        "text": "Translated",
        "to": "es"
      }
    ]
  }
]
""", Encoding.UTF8, "application/json")
                });
            using var httpClient = new HttpClient(messageHandler.Object);
            var target = new AzureTranslationBackend(
                httpClient,
                new AzureTranslationOptions
                {
                    ApiKey = "ApiKey",
                    BaseUrl = "https://api.cognitive.microsofttranslator.com/",
                    Region = "westeurope"
                });

            var result = await target.TranslateAsync(
                "es",
                [
                    "<div><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN0</span></div>",
                    "<div><mstrans:dictionary translation=\"qBittorrent\">qBittorrent</mstrans:dictionary></div>"
                ],
                TestContext.Current.CancellationToken);

            result.Should().Equal("Translated", "Translated");
            requestBodies.Should().HaveCount(2);
            requestBodies[0].Should().Contain("\\u003Cspan class=\\u0022notranslate\\u0022 translate=\\u0022no\\u0022\\u003EQBTMUDTOKEN0\\u003C/span\\u003E");
            requestBodies[0].Should().NotContain("mstrans:dictionary");
            requestBodies[1].Should().Contain("\\u003Cmstrans:dictionary translation=\\u0022qBittorrent\\u0022\\u003EqBittorrent\\u003C/mstrans:dictionary\\u003E");
            requestBodies[1].Should().NotContain("QBTMUDTOKEN0");
            messageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Exactly(2),
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>());
        }

        private static string BuildResponseBody(int startIndex, int count)
        {
            var items = Enumerable.Range(startIndex, count)
                .Select(index => $$"""
  {
    "translations": [
      {
        "text": "Translated-{{index}}",
        "to": "es"
      }
    ]
  }
""");

            return $$"""
[
{{string.Join(",\n", items)}}
]
""";
        }
    }
}
