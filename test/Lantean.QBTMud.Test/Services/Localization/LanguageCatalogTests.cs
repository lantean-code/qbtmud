using AwesomeAssertions;
using Lantean.QBTMud.Services.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Globalization;
using System.Net;
using System.Text;

namespace Lantean.QBTMud.Test.Services.Localization
{
    public sealed class LanguageCatalogTests : IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LanguageCatalog> _logger;
        private readonly CatalogHttpMessageHandler _handler;
        private readonly HttpClient _httpClient;
        private readonly LanguageCatalog _target;

        public LanguageCatalogTests()
        {
            _httpClientFactory = Mock.Of<IHttpClientFactory>();
            _logger = Mock.Of<ILogger<LanguageCatalog>>();
            _handler = new CatalogHttpMessageHandler();
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
                LanguagesFileName = "webui_languages.json"
            });

            _target = new LanguageCatalog(_httpClientFactory, _logger, options);
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        [Fact]
        public void GIVEN_CatalogNotInitialized_WHEN_LanguagesRead_THEN_ShouldReturnEmptyList()
        {
            _target.Languages.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ValidLocalesIncludingEdgeCases_WHEN_EnsureInitialized_THEN_ShouldBuildCatalog()
        {
            _handler.Responses["i18n/webui_languages.json"] = JsonResponse("[\"en_US\", \"EN\", \" \", \"@\", \"zz\", \"zz@cyrillic\", \"zz@latin\", \"en@\", \"en@x\", \"zz@abcd\", \"en\"]");

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _handler.RequestedPaths.Should().ContainSingle().Which.Should().Be("i18n/webui_languages.json");

            _target.Languages.Should().HaveCount(9);
            _target.Languages.Should().ContainSingle(item => item.Code == "en_US").Which.DisplayName.Should().Be(CultureInfo.GetCultureInfo("en-US").NativeName);
            _target.Languages.Should().ContainSingle(item => item.Code == "EN").Which.DisplayName.Should().Be(CultureInfo.GetCultureInfo("en").NativeName);
            _target.Languages.Should().ContainSingle(item => item.Code == "@").Which.DisplayName.Should().Be("@");
            _target.Languages.Should().ContainSingle(item => item.Code == "zz").Which.DisplayName.Should().Be("zz");
            _target.Languages.Should().ContainSingle(item => item.Code == "zz@cyrillic").Which.DisplayName.Should().Be(GetNativeNameOrFallback("zz-Cyrl", "zz@cyrillic"));
            _target.Languages.Should().ContainSingle(item => item.Code == "zz@latin").Which.DisplayName.Should().Be(GetNativeNameOrFallback("zz-Latn", "zz@latin"));
            _target.Languages.Should().ContainSingle(item => item.Code == "en@").Which.DisplayName.Should().Be(CultureInfo.GetCultureInfo("en").NativeName);
            _target.Languages.Should().ContainSingle(item => item.Code == "en@x").Which.DisplayName.Should().Be(CultureInfo.GetCultureInfo("en").NativeName);
            _target.Languages.Should().ContainSingle(item => item.Code == "zz@abcd").Which.DisplayName.Should().Be(GetNativeNameOrFallback("zz-Abcd", "zz@abcd"));
        }

        [Fact]
        public async Task GIVEN_AlreadyInitialized_WHEN_EnsureInitializedCalledAgain_THEN_ShouldNotRequestCatalogTwice()
        {
            _handler.Responses["i18n/webui_languages.json"] = JsonResponse("[\"de\"]");

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);
            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _handler.RequestedPaths.Should().ContainSingle().Which.Should().Be("i18n/webui_languages.json");
        }

        [Fact]
        public async Task GIVEN_InvalidCultureName_WHEN_EnsureInitialized_THEN_ShouldFallbackDisplayNameToLocaleCode()
        {
            _handler.Responses["i18n/webui_languages.json"] = JsonResponse("[\"invalid!locale\"]");

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _target.Languages.Should().ContainSingle(item => item.Code == "invalid!locale").Which.DisplayName.Should().Be("invalid!locale");
        }

        [Fact]
        public async Task GIVEN_CatalogFileNotFound_WHEN_EnsureInitialized_THEN_ShouldFallbackToEnglishOnly()
        {
            _handler.Responses["i18n/webui_languages.json"] = new HttpResponseMessage(HttpStatusCode.NotFound);

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _target.Languages.Should().ContainSingle(item => item.Code == "en").Which.DisplayName.Should().Be(CultureInfo.GetCultureInfo("en").NativeName);
        }

        [Fact]
        public async Task GIVEN_CatalogRequestReturnsNonSuccess_WHEN_EnsureInitialized_THEN_ShouldFallbackToEnglishOnly()
        {
            _handler.Responses["i18n/webui_languages.json"] = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _target.Languages.Should().ContainSingle(item => item.Code == "en").Which.DisplayName.Should().Be(CultureInfo.GetCultureInfo("en").NativeName);
        }

        [Fact]
        public async Task GIVEN_CatalogRequestThrowsHttpRequestException_WHEN_EnsureInitialized_THEN_ShouldFallbackToEnglishOnly()
        {
            _handler.ThrowHttpRequest = true;

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _target.Languages.Should().ContainSingle(item => item.Code == "en").Which.DisplayName.Should().Be(CultureInfo.GetCultureInfo("en").NativeName);
        }

        [Fact]
        public async Task GIVEN_CatalogResponseContainsJsonNull_WHEN_EnsureInitialized_THEN_ShouldFallbackToEnglishOnly()
        {
            _handler.Responses["i18n/webui_languages.json"] = JsonResponse("null");

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _target.Languages.Should().ContainSingle(item => item.Code == "en").Which.DisplayName.Should().Be(CultureInfo.GetCultureInfo("en").NativeName);
        }

        [Fact]
        public async Task GIVEN_CatalogResponseContainsInvalidJson_WHEN_EnsureInitialized_THEN_ShouldFallbackToEnglishOnly()
        {
            _handler.Responses["i18n/webui_languages.json"] = JsonResponse("{");

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _target.Languages.Should().ContainSingle(item => item.Code == "en").Which.DisplayName.Should().Be(CultureInfo.GetCultureInfo("en").NativeName);
        }

        [Fact]
        public async Task GIVEN_OperationCanceledDuringRequest_WHEN_EnsureInitialized_THEN_ShouldPropagateCancellationAndAllowRetry()
        {
            _handler.ThrowOperationCanceled = true;

            Func<Task> action = async () =>
            {
                await _target.EnsureInitialized(TestContext.Current.CancellationToken);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
            _target.Languages.Should().BeEmpty();

            _handler.ThrowOperationCanceled = false;
            _handler.Responses["i18n/webui_languages.json"] = JsonResponse("[]");

            await _target.EnsureInitialized(TestContext.Current.CancellationToken);

            _target.Languages.Should().ContainSingle(item => item.Code == "en");
            _handler.RequestedPaths.Should().ContainSingle().Which.Should().Be("i18n/webui_languages.json");
        }

        private static HttpResponseMessage JsonResponse(string body)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }

        private static string GetNativeNameOrFallback(string cultureName, string fallback)
        {
            try
            {
                return CultureInfo.GetCultureInfo(cultureName).NativeName;
            }
            catch (CultureNotFoundException)
            {
                return fallback;
            }
        }

        private sealed class CatalogHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _notFoundResponse = new HttpResponseMessage(HttpStatusCode.NotFound);
            private bool _disposed;

            public Dictionary<string, HttpResponseMessage> Responses { get; }

            public List<string> RequestedPaths { get; }

            public bool ThrowOperationCanceled { get; set; }

            public bool ThrowHttpRequest { get; set; }

            public CatalogHttpMessageHandler()
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
