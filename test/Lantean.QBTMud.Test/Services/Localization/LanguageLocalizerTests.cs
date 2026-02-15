using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Globalization;
using System.Net;
using System.Text;

namespace Lantean.QBTMud.Test.Services.Localization
{
    public sealed class LanguageLocalizerTests
    {
        [Fact]
        public async Task GIVEN_AliasOverrideAndLocaleFallback_WHEN_Translate_THEN_ShouldUseOverrideWithFormattedArguments()
        {
            var culture = new CultureInfo("fr-CA");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = JsonResponse("{\"Ctx|Source\":\"Ctx|Alias\"}"),
                ["i18n/webui_fr-CA.json"] = new HttpResponseMessage(HttpStatusCode.NotFound),
                ["i18n/webui_fr_CA.json"] = JsonResponse("{\"Ctx|Alias\":\"Translated %1\"}"),
                ["i18n/webui_overrides_fr_CA.json"] = JsonResponse("{\"Ctx|Alias\":\"Override %1\"}")
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                target.Localizer.Translate("Ctx", "Source", "Value").Should().Be("Override Value");
            });
        }

        [Fact]
        public async Task GIVEN_OnlyBaseTranslation_WHEN_Translate_THEN_ShouldUseBaseValue()
        {
            var culture = new CultureInfo("de-DE");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = JsonResponse("{}"),
                ["i18n/webui_de-DE.json"] = JsonResponse("{\"Ctx|Source\":\"Hallo %1\"}"),
                ["i18n/webui_overrides_de-DE.json"] = JsonResponse("{}")
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                target.Localizer.Translate("Ctx", "Source", "World").Should().Be("Hallo World");
            });
        }

        [Fact]
        public async Task GIVEN_MissingTranslation_WHEN_Translate_THEN_ShouldFallbackToSource()
        {
            var culture = new CultureInfo("it-IT");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = JsonResponse("{}"),
                ["i18n/webui_it-IT.json"] = JsonResponse("{}"),
                ["i18n/webui_overrides_it-IT.json"] = JsonResponse("{}")
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                target.Localizer.Translate("Ctx", "Source %1", "Value").Should().Be("Source Value");
            });
        }

        [Fact]
        public async Task GIVEN_InvalidFormatString_WHEN_Translate_THEN_ShouldReturnRawTranslation()
        {
            var culture = new CultureInfo("nl-NL");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = JsonResponse("{}"),
                ["i18n/webui_nl-NL.json"] = JsonResponse("{\"Ctx|Source\":\"{broken\"}"),
                ["i18n/webui_overrides_nl-NL.json"] = JsonResponse("{}")
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                target.Localizer.Translate("Ctx", "Source", "Value").Should().Be("{broken");
            });
        }

        [Fact]
        public async Task GIVEN_NumberedPlaceholders_WHEN_Translate_THEN_ShouldNormalizeToStringFormat()
        {
            var culture = new CultureInfo("es-ES");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = JsonResponse("{}"),
                ["i18n/webui_es-ES.json"] = JsonResponse("{\"Ctx|Source\":\"%2 %1\"}"),
                ["i18n/webui_overrides_es-ES.json"] = JsonResponse("{}")
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                target.Localizer.Translate("Ctx", "Source", "one", "two").Should().Be("two one");
            });
        }

        [Fact]
        public async Task GIVEN_LiteralPercentSigns_WHEN_Translate_THEN_ShouldPreserveLiteralPercentOutput()
        {
            var culture = new CultureInfo("de-DE");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = JsonResponse("{}"),
                ["i18n/webui_de-DE.json"] = JsonResponse("{\"Ctx|Source\":\"CPU 100%\"}"),
                ["i18n/webui_overrides_de-DE.json"] = JsonResponse("{}")
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                target.Localizer.Translate("Ctx", "Source", "ignored").Should().Be("CPU 100%");
                target.Localizer.Translate("Ctx", "Source", "ignored").Should().Be("CPU 100%");
            });
        }

        [Fact]
        public async Task GIVEN_SameCultureInitializedTwice_WHEN_InitializeAsync_THEN_ShouldNotReload()
        {
            var culture = new CultureInfo("pt-BR");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = JsonResponse("{}"),
                ["i18n/webui_pt-BR.json"] = JsonResponse("{}"),
                ["i18n/webui_overrides_pt-BR.json"] = JsonResponse("{}")
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                await target.ResourceLoader.EnsureInitialized();
            });

            handler.RequestCount.Should().Be(3);
        }

        [Fact]
        public async Task GIVEN_NonSuccessAndMissingFiles_WHEN_InitializeAsync_THEN_ShouldFallbackToSourceWithoutThrowing()
        {
            var culture = new CultureInfo("sv-SE");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = new HttpResponseMessage(HttpStatusCode.InternalServerError),
                ["i18n/webui_sv-SE.json"] = new HttpResponseMessage(HttpStatusCode.NotFound),
                ["i18n/webui_sv_SE.json"] = new HttpResponseMessage(HttpStatusCode.NotFound),
                ["i18n/webui_sv.json"] = new HttpResponseMessage(HttpStatusCode.NotFound),
                ["i18n/webui_en.json"] = new HttpResponseMessage(HttpStatusCode.NotFound)
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                target.Localizer.Translate("Ctx", "Missing Source").Should().Be("Missing Source");
                target.Localizer.Translate(string.Empty, "NoContext").Should().Be("NoContext");
            });
        }

        [Fact]
        public async Task GIVEN_CancellationToken_WHEN_RequestCanceled_THEN_ShouldPropagateOperationCanceledException()
        {
            var culture = new CultureInfo("fi-FI");
            var handler = new DictionaryHttpMessageHandler(new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal));
            handler.ThrowOperationCanceled = true;
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);
            using var source = new CancellationTokenSource();
            source.Cancel();

            Func<Task> action = async () =>
            {
                await WithCultureAsync(culture, async () =>
                {
                    await target.ResourceLoader.EnsureInitialized(source.Token);
                });
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public void GIVEN_WhitespaceSource_WHEN_Translate_THEN_ShouldReturnEmptyString()
        {
            using var httpClient = new HttpClient(new DictionaryHttpMessageHandler(new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)))
            {
                BaseAddress = new Uri("http://localhost/")
            };
            var target = CreateTarget(httpClient);

            target.Localizer.Translate("Ctx", " ").Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_NullArguments_WHEN_Translate_THEN_ShouldUseEmptyArguments()
        {
            var culture = new CultureInfo("de-DE");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = JsonResponse("{}"),
                ["i18n/webui_de-DE.json"] = JsonResponse("{\"Ctx|Source\":\"Hallo\"}"),
                ["i18n/webui_overrides_de-DE.json"] = JsonResponse("{}")
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                object[]? arguments = null;
                target.Localizer.Translate("Ctx", "Source", arguments!).Should().Be("Hallo");
            });
        }

        [Fact]
        public async Task GIVEN_EnglishCulture_WHEN_InitializeAsync_THEN_ShouldLoadEmbeddedEnglishWithoutBaseFileRequest()
        {
            var culture = new CultureInfo("en-US");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = JsonResponse("{}"),
                ["i18n/webui_overrides_en.json"] = new HttpResponseMessage(HttpStatusCode.NotFound)
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
            });

            handler.RequestedPaths.Should().Contain("i18n/webui_aliases.json");
            handler.RequestedPaths.Should().Contain("i18n/webui_overrides_en.json");
            handler.RequestedPaths.Should().NotContain("i18n/webui_en.json");
        }

        [Fact]
        public async Task GIVEN_HttpRequestException_WHEN_InitializeAsync_THEN_ShouldFallbackToSource()
        {
            var culture = new CultureInfo("tr-TR");
            var handler = new DictionaryHttpMessageHandler(new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal))
            {
                ThrowHttpRequest = true
            };
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                target.Localizer.Translate("Ctx", "Source").Should().Be("Source");
            });
        }

        [Fact]
        public async Task GIVEN_InvalidAliasJson_WHEN_InitializeAsync_THEN_ShouldIgnoreAliasFile()
        {
            var culture = new CultureInfo("da-DK");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = JsonResponse("{"),
                ["i18n/webui_da-DK.json"] = JsonResponse("{\"Ctx|Source\":\"Hej\"}"),
                ["i18n/webui_overrides_da-DK.json"] = JsonResponse("{}")
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                target.Localizer.Translate("Ctx", "Source").Should().Be("Hej");
            });
        }

        [Fact]
        public async Task GIVEN_NullAliasJson_WHEN_InitializeAsync_THEN_ShouldTreatAliasesAsEmpty()
        {
            var culture = new CultureInfo("nb-NO");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = JsonResponse("null"),
                ["i18n/webui_nb-NO.json"] = JsonResponse("{\"Ctx|Source\":\"Hei\"}"),
                ["i18n/webui_overrides_nb-NO.json"] = JsonResponse("{}")
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                target.Localizer.Translate("Ctx", "Source").Should().Be("Hei");
            });
        }

        [Fact]
        public async Task GIVEN_OperationCanceledDuringHttpCall_WHEN_InitializeAsync_THEN_ShouldPropagateOperationCanceledException()
        {
            var culture = new CultureInfo("fi-FI");
            var handler = new DictionaryHttpMessageHandler(new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal))
            {
                ThrowOperationCanceled = true
            };
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            Func<Task> action = async () =>
            {
                await WithCultureAsync(culture, async () =>
                {
                    await target.ResourceLoader.EnsureInitialized();
                });
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GIVEN_WhitespaceTranslationValue_WHEN_Translate_THEN_ShouldFallbackToSourceFormat()
        {
            var culture = new CultureInfo("pl-PL");
            var responses = new Dictionary<string, HttpResponseMessage>(StringComparer.Ordinal)
            {
                ["i18n/webui_aliases.json"] = JsonResponse("{}"),
                ["i18n/webui_pl-PL.json"] = JsonResponse("{\"Ctx|Source %1\":\"   \"}"),
                ["i18n/webui_overrides_pl-PL.json"] = JsonResponse("{}")
            };
            var handler = new DictionaryHttpMessageHandler(responses);
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var target = CreateTarget(httpClient);

            await WithCultureAsync(culture, async () =>
            {
                await target.ResourceLoader.EnsureInitialized();
                target.Localizer.Translate("Ctx", "Source %1", "Value").Should().Be("Source Value");
            });
        }

        private static async Task WithCultureAsync(CultureInfo culture, Func<Task> action)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;

            try
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                await action();
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
            }
        }

        private static HttpResponseMessage JsonResponse(string content)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
        }

        private static LanguageLocalizerTestTarget CreateTarget(HttpClient httpClient)
        {
            var factory = Mock.Of<IHttpClientFactory>();
            Mock.Get(factory)
                .Setup(clientFactory => clientFactory.CreateClient("WebUiAssets"))
                .Returns(httpClient);

            var options = Options.Create(new WebUiLocalizationOptions
            {
                BasePath = "i18n",
                AliasFileName = "webui_aliases.json",
                BaseFileNameFormat = "webui_{0}.json",
                OverrideFileNameFormat = "webui_overrides_{0}.json"
            });

            var fileProviderLogger = Mock.Of<ILogger<LanguageFileLoader>>();
            var assemblyProviderLogger = Mock.Of<ILogger<LanguageEmbeddedResourceLoader>>();
            var resourceLoaderLogger = Mock.Of<ILogger<LanguageResourceLoader>>();

            var fileProvider = new LanguageFileLoader(factory, fileProviderLogger, options);
            var assemblyResourceAccessor = new AssemblyResourceAccessor();
            var assemblyProvider = new LanguageEmbeddedResourceLoader(assemblyResourceAccessor, assemblyProviderLogger);
            var resourceProvider = new LanguageResourceProvider();
            var resourceLoader = new LanguageResourceLoader(fileProvider, assemblyProvider, resourceProvider, resourceLoaderLogger, options);
            var localizer = new LanguageLocalizer(resourceProvider);

            return new LanguageLocalizerTestTarget(localizer, resourceLoader);
        }

        private sealed class LanguageLocalizerTestTarget
        {
            public LanguageLocalizerTestTarget(LanguageLocalizer localizer, ILanguageResourceLoader resourceLoader)
            {
                Localizer = localizer;
                ResourceLoader = resourceLoader;
            }

            public LanguageLocalizer Localizer { get; }

            public ILanguageResourceLoader ResourceLoader { get; }
        }

        private sealed class DictionaryHttpMessageHandler : HttpMessageHandler
        {
            private readonly IDictionary<string, HttpResponseMessage> _responses;

            public DictionaryHttpMessageHandler(IDictionary<string, HttpResponseMessage> responses)
            {
                _responses = responses;
            }

            public int RequestCount { get; private set; }

            public bool ThrowOperationCanceled { get; set; }

            public bool ThrowHttpRequest { get; set; }

            public List<string> RequestedPaths { get; } = [];

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (ThrowOperationCanceled || cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                RequestCount++;
                var path = request.RequestUri!.AbsolutePath.TrimStart('/');
                RequestedPaths.Add(path);

                if (ThrowHttpRequest)
                {
                    throw new HttpRequestException("request failure");
                }

                if (_responses.TryGetValue(path, out var response))
                {
                    return Task.FromResult(CloneResponse(response));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
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
        }
    }
}
