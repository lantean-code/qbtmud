using AwesomeAssertions;
using Lantean.QBTMud.Services.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Text;

namespace Lantean.QBTMud.Test.Services.Localization
{
    public sealed class LanguageFileLoaderTests : IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LanguageFileLoader> _logger;
        private readonly DictionaryHttpMessageHandler _handler;
        private readonly HttpClient _httpClient;
        private readonly LanguageFileLoader _target;

        public LanguageFileLoaderTests()
        {
            _httpClientFactory = Mock.Of<IHttpClientFactory>();
            _logger = Mock.Of<ILogger<LanguageFileLoader>>();
            _handler = new DictionaryHttpMessageHandler();

            _httpClient = new HttpClient(_handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };

            Mock.Get(_httpClientFactory)
                .Setup(factory => factory.CreateClient("WebUiAssets"))
                .Returns(_httpClient);

            var options = Options.Create(new WebUiLocalizationOptions
            {
                BasePath = "i18n/",
                AliasFileName = "webui_aliases.json",
                BaseFileNameFormat = "webui_{0}.json",
                OverrideFileNameFormat = "webui_overrides_{0}.json"
            });

            _target = new LanguageFileLoader(_httpClientFactory, _logger, options);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        [Fact]
        public async Task GIVEN_ValidJsonResponse_WHEN_LoadDictionaryAsync_THEN_ShouldReturnDictionaryAndNormalizedPath()
        {
            _handler.Responses["i18n/webui_en.json"] = JsonResponse("{\"Ctx|Source\":\"Translated\"}");

            var result = await _target.LoadDictionaryAsync("webui_en.json", TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result.Should().ContainKey("Ctx|Source").WhoseValue.Should().Be("Translated");
            _handler.RequestedPaths.Should().ContainSingle().Which.Should().Be("i18n/webui_en.json");
        }

        [Fact]
        public async Task GIVEN_JsonNullResponse_WHEN_LoadDictionaryAsync_THEN_ShouldReturnEmptyDictionary()
        {
            _handler.Responses["i18n/webui_en.json"] = JsonResponse("null");

            var result = await _target.LoadDictionaryAsync("webui_en.json", TestContext.Current.CancellationToken);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_NotFoundResponse_WHEN_LoadDictionaryAsync_THEN_ShouldReturnNull()
        {
            _handler.Responses["i18n/webui_en.json"] = new HttpResponseMessage(HttpStatusCode.NotFound);

            var result = await _target.LoadDictionaryAsync("webui_en.json", TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_NonSuccessResponse_WHEN_LoadDictionaryAsync_THEN_ShouldReturnNull()
        {
            _handler.Responses["i18n/webui_en.json"] = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            var result = await _target.LoadDictionaryAsync("webui_en.json", TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_InvalidJsonResponse_WHEN_LoadDictionaryAsync_THEN_ShouldReturnNull()
        {
            _handler.Responses["i18n/webui_en.json"] = JsonResponse("{");

            var result = await _target.LoadDictionaryAsync("webui_en.json", TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_HttpRequestFailure_WHEN_LoadDictionaryAsync_THEN_ShouldReturnNull()
        {
            _handler.ThrowHttpRequest = true;

            var result = await _target.LoadDictionaryAsync("webui_en.json", TestContext.Current.CancellationToken);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_CanceledToken_WHEN_LoadDictionaryAsync_THEN_ShouldThrowOperationCanceledException()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            Func<Task> action = async () =>
            {
                await _target.LoadDictionaryAsync("webui_en.json", cancellationTokenSource.Token);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GIVEN_OperationCanceledDuringRequest_WHEN_LoadDictionaryAsync_THEN_ShouldThrowOperationCanceledException()
        {
            _handler.ThrowOperationCanceled = true;

            Func<Task> action = async () =>
            {
                await _target.LoadDictionaryAsync("webui_en.json", TestContext.Current.CancellationToken);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        private static HttpResponseMessage JsonResponse(string body)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        private sealed class DictionaryHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _notFoundResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
            private bool _disposed;

            public Dictionary<string, HttpResponseMessage> Responses { get; }

            public bool ThrowOperationCanceled { get; set; }

            public bool ThrowHttpRequest { get; set; }

            public List<string> RequestedPaths { get; }

            public DictionaryHttpMessageHandler()
            {
                Responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal);
                RequestedPaths = new List<string>();
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (ThrowOperationCanceled || cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                if (ThrowHttpRequest)
                {
                    throw new HttpRequestException("request failure");
                }

                var path = request.RequestUri!.AbsolutePath.TrimStart('/');
                RequestedPaths.Add(path);

                if (Responses.TryGetValue(path, out var response))
                {
                    return Task.FromResult(CloneResponse(response));
                }

                return Task.FromResult(CloneResponse(_notFoundResponse));
            }

            private static HttpResponseMessage CloneResponse(HttpResponseMessage source)
            {
                var target = new HttpResponseMessage(source.StatusCode);
                if (source.Content is not null)
                {
                    var body = source.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    target.Content = new StringContent(body, Encoding.UTF8, source.Content.Headers.ContentType?.MediaType ?? "application/json");
                }

                return target;
            }

            protected override void Dispose(bool disposing)
            {
                if (_disposed)
                {
                    return;
                }

                if (disposing)
                {
                    _notFoundResponse.Dispose();
                    foreach (var response in Responses.Values)
                    {
                        response.Dispose();
                    }
                }

                _disposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
