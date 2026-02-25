using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Test.Infrastructure;
using Lantean.QBTMud.Theming;
using Moq;
using MudBlazor;
using System.Net;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class ThemeManagerServiceTests
    {
        private const string LocalThemesStorageKey = "ThemeManager.LocalThemes";
        private const string SelectedThemeStorageKey = "ThemeManager.SelectedThemeId";
        private const string ThemeIndexPath = "/themes/index.json";

        private static readonly HttpResponseMessage _notFoundResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);

        private readonly TestLocalStorageService _localStorage;
        private readonly IThemeFontCatalog _fontCatalog;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILanguageLocalizer _languageLocalizer;
        private readonly ThemeManagerService _target;

        public ThemeManagerServiceTests()
        {
            _localStorage = new TestLocalStorageService();
            _fontCatalog = Mock.Of<IThemeFontCatalog>();
            _httpClientFactory = Mock.Of<IHttpClientFactory>();
            _languageLocalizer = Mock.Of<ILanguageLocalizer>();
            Mock.Get(_languageLocalizer)
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] _) => source);
            _target = new ThemeManagerService(_httpClientFactory, _localStorage, _fontCatalog, _languageLocalizer);
        }

        [Fact]
        public async Task GIVEN_ServerThemes_WHEN_Initialized_THEN_AppliesFirstThemeAndRaisesEvent()
        {
            var themeJson = CreateThemeJson("ThemeId", "Name", "Nunito Sans");
            SetupHttpClient(CreateIndexResponse("themes/theme-one.json"),
                CreateThemeResponse("themes/theme-one.json", themeJson));
            SetupFontCatalogValid("Nunito Sans");

            ThemeChangedEventArgs? captured = null;
            _target.ThemeChanged += (_, args) => captured = args;

            await _target.EnsureInitialized();

            _target.Themes.Should().ContainSingle(theme => theme.Id == "ThemeId");
            _target.CurrentThemeId.Should().Be("ThemeId");
            _target.CurrentFontFamily.Should().Be("Nunito Sans");
            captured.Should().NotBeNull();
            captured!.ThemeId.Should().Be("ThemeId");
            captured.FontFamily.Should().Be("Nunito Sans");
        }

        [Fact]
        public async Task GIVEN_StoredThemeId_WHEN_Initialized_THEN_AppliesStoredTheme()
        {
            await _localStorage.SetItemAsync(SelectedThemeStorageKey, "ThemeId", TestContext.Current.CancellationToken);
            var themeJson = CreateThemeJson("ThemeId", "Name", "Nunito Sans");
            SetupHttpClient(CreateIndexResponse("themes/theme-one.json"),
                CreateThemeResponse("themes/theme-one.json", themeJson));
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.CurrentThemeId.Should().Be("ThemeId");
        }

        [Fact]
        public async Task GIVEN_InitializedService_WHEN_EnsureInitializedCalledAgain_THEN_DoesNotReinitialize()
        {
            var themeJson = CreateThemeJson("ThemeId", "Name", "Nunito Sans");
            SetupHttpClient(CreateIndexResponse("themes/theme-one.json"),
                CreateThemeResponse("themes/theme-one.json", themeJson));
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.EnsureInitialized();

            Mock.Get(_fontCatalog)
                .Verify(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_NotInitialized_WHEN_ApplyTheme_THEN_InitializesAndPersistsSelection()
        {
            var themeJson = CreateThemeJson("ThemeId", "Name", "Nunito Sans");
            SetupHttpClient(CreateIndexResponse("themes/theme-one.json"),
                CreateThemeResponse("themes/theme-one.json", themeJson));
            SetupFontCatalogValid("Nunito Sans");

            await _target.ApplyTheme("ThemeId");

            _target.CurrentThemeId.Should().Be("ThemeId");
            var storedThemeId = await _localStorage.GetItemAsync<string?>(SelectedThemeStorageKey, TestContext.Current.CancellationToken);
            storedThemeId.Should().Be("ThemeId");
        }

        [Fact]
        public async Task GIVEN_NoThemes_WHEN_Initialized_THEN_UsesFallbackTheme()
        {
            SetupHttpClient(_notFoundResponseMessage);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.Themes.Should().BeEmpty();
            _target.CurrentThemeId.Should().Be("default");
        }

        [Fact]
        public async Task GIVEN_ServerTheme_WHEN_Initialized_THEN_CurrentThemePropertyReturnsAppliedTheme()
        {
            var themeJson = CreateThemeJson("ThemeId", "Name", "Nunito Sans");
            SetupHttpClient(CreateIndexResponse("themes/theme-one.json"),
                CreateThemeResponse("themes/theme-one.json", themeJson));
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.CurrentTheme.Should().NotBeNull();
            _target.CurrentTheme!.Id.Should().Be("ThemeId");
        }

        [Fact]
        public async Task GIVEN_InvalidThemeId_WHEN_Applied_THEN_DoesNotChangeCurrentTheme()
        {
            var themeJson = CreateThemeJson("ThemeId", "Name", "Nunito Sans");
            SetupHttpClient(CreateIndexResponse("themes/theme-one.json"),
                CreateThemeResponse("themes/theme-one.json", themeJson));
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            await _target.ApplyTheme("Missing");

            _target.CurrentThemeId.Should().Be("ThemeId");
        }

        [Fact]
        public async Task GIVEN_InvalidDefinition_WHEN_Saved_THEN_NormalizesNameAndFont()
        {
            SetupHttpClient(CreateIndexResponse());
            SetupFontCatalogInvalid();

            var definition = new ThemeDefinition
            {
                Id = " ",
                Name = " ",
                Description = " Description ",
                FontFamily = "Invalid!",
                Theme = new MudTheme()
            };

            await _target.SaveLocalTheme(definition);

            _target.Themes.Should().ContainSingle(themeItem =>
                themeItem.Name == "Untitled Theme"
                && themeItem.Theme.FontFamily == "Nunito Sans"
                && themeItem.Theme.Description == "Description");

            var stored = await _localStorage.GetItemAsync<List<ThemeDefinition>>(LocalThemesStorageKey, TestContext.Current.CancellationToken);
            stored.Should().NotBeNull();
            stored!.Should().ContainSingle(item => item.Name == "Untitled Theme" && item.FontFamily == "Nunito Sans" && item.Description == "Description");
        }

        [Fact]
        public async Task GIVEN_DefinitionWithNullThemeAndBlankFont_WHEN_Saved_THEN_DefaultThemeAndFontAreApplied()
        {
            SetupHttpClient(CreateIndexResponse());
            SetupFontCatalogValid("Nunito Sans");

            var definition = new ThemeDefinition
            {
                Id = "ThemeId",
                Name = "ThemeName",
                FontFamily = " ",
                Theme = null!
            };

            await _target.SaveLocalTheme(definition);

            _target.Themes.Should().ContainSingle(item =>
                item.Id == "ThemeId"
                && item.Theme.Theme != null
                && item.Theme.FontFamily == "Nunito Sans");
        }

        [Fact]
        public async Task GIVEN_CurrentThemeDeleted_WHEN_Removed_THEN_AppliesNextTheme()
        {
            var themeJson = CreateThemeJson("ServerId", "Name", "Nunito Sans");
            SetupHttpClient(CreateIndexResponse("themes/theme-one.json"),
                CreateThemeResponse("themes/theme-one.json", themeJson));
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            var localDefinition = new ThemeDefinition
            {
                Id = "LocalId",
                Name = "Local",
                FontFamily = "Nunito Sans",
                Theme = new MudTheme()
            };

            await _target.SaveLocalTheme(localDefinition);
            await _target.ApplyTheme("LocalId");

            await _target.DeleteLocalTheme("LocalId");

            _target.CurrentThemeId.Should().Be("ServerId");
        }

        [Fact]
        public async Task GIVEN_NotInitializedAndCurrentLocalTheme_WHEN_Deleted_THEN_InitializesAndFallsBackToDefaultTheme()
        {
            SetupHttpClient(CreateIndexResponse());
            SetupFontCatalogValid("Nunito Sans");

            var localTheme = new ThemeDefinition
            {
                Id = "LocalId",
                Name = "LocalName",
                FontFamily = "Nunito Sans",
                Theme = new MudTheme()
            };
            ThemeFontHelper.ApplyFont(localTheme, localTheme.FontFamily);

            await _localStorage.SetItemAsync(LocalThemesStorageKey, new List<ThemeDefinition> { localTheme }, TestContext.Current.CancellationToken);
            await _localStorage.SetItemAsync(SelectedThemeStorageKey, "LocalId", TestContext.Current.CancellationToken);

            await _target.DeleteLocalTheme("LocalId");

            _target.CurrentThemeId.Should().Be("default");
            _target.Themes.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_NotInitialized_WHEN_ReloadServerThemes_THEN_InitializesCatalog()
        {
            var themeJson = CreateThemeJson("ThemeId", "Name", "Nunito Sans");
            SetupHttpClient(CreateIndexResponse("themes/theme-one.json"),
                CreateThemeResponse("themes/theme-one.json", themeJson));
            SetupFontCatalogValid("Nunito Sans");

            await _target.ReloadServerThemes();

            _target.Themes.Should().ContainSingle(theme => theme.Id == "ThemeId");
        }

        [Fact]
        public async Task GIVEN_NotInitializedWithPendingInitialization_WHEN_ReloadServerThemes_THEN_AwaitsInitialization()
        {
            SetupHttpClient(CreateIndexResponse());

            var gate = new TaskCompletionSource<bool>();
            Mock.Get(_fontCatalog)
                .Setup(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()))
                .Returns(gate.Task);

            var url = "Url";
            Mock.Get(_fontCatalog)
                .Setup(catalog => catalog.TryGetFontUrl(It.IsAny<string>(), out url))
                .Returns(true);

            var reloadTask = _target.ReloadServerThemes();
            reloadTask.IsCompleted.Should().BeFalse();

            gate.SetResult(true);
            await reloadTask;

            _target.CurrentThemeId.Should().Be("default");
        }

        [Fact]
        public async Task GIVEN_RemovedServerTheme_WHEN_Reloaded_THEN_ClearsCurrentThemeId()
        {
            var themeJson = CreateThemeJson("ThemeId", "Name", "Nunito Sans");
            var handler = new ThemeMessageHandler(new Dictionary<string, HttpResponseMessage>
            {
                { ThemeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/themes/theme-one.json", CreateThemeHttpResponse(themeJson) }
            });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            handler.SetResponses(new Dictionary<string, HttpResponseMessage>
            {
                { ThemeIndexPath, CreateIndexResponse() }
            });

            await _target.ReloadServerThemes();

            _target.CurrentThemeId.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_CurrentThemeStillAvailable_WHEN_ReloadingServerThemes_THEN_ReappliesAndPersistsTheme()
        {
            var themeJson = CreateThemeJson("ThemeId", "Name", "Nunito Sans");
            var handler = new ThemeMessageHandler(new Dictionary<string, HttpResponseMessage>
            {
                { ThemeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/themes/theme-one.json", CreateThemeHttpResponse(themeJson) }
            });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            var storedThemeIdBeforeReload = await _localStorage.GetItemAsync<string?>(SelectedThemeStorageKey, TestContext.Current.CancellationToken);
            storedThemeIdBeforeReload.Should().BeNull();

            handler.SetResponses(new Dictionary<string, HttpResponseMessage>
            {
                { ThemeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/themes/theme-one.json", CreateThemeHttpResponse(themeJson) }
            });

            await _target.ReloadServerThemes();

            var storedThemeIdAfterReload = await _localStorage.GetItemAsync<string?>(SelectedThemeStorageKey, TestContext.Current.CancellationToken);
            storedThemeIdAfterReload.Should().Be("ThemeId");
        }

        [Fact]
        public async Task GIVEN_InitializedService_WHEN_ReloadServerThemesIndexRequestThrows_THEN_ThrowsHttpRequestException()
        {
            var themeJson = CreateThemeJson("ThemeId", "Name", "Nunito Sans");
            var handler = new ThemeMessageHandler(new Dictionary<string, HttpResponseMessage>
            {
                { ThemeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/themes/theme-one.json", CreateThemeHttpResponse(themeJson) }
            });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            handler.SetResponses(
                new Dictionary<string, HttpResponseMessage>(),
                new Dictionary<string, Exception>
                {
                    { ThemeIndexPath, new HttpRequestException("Error") }
                });

            var action = () => _target.ReloadServerThemes();

            await action.Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GIVEN_LocalThemeSharesServerId_WHEN_Initialized_THEN_LocalThemeTakesPrecedence()
        {
            var localTheme = new ThemeDefinition
            {
                Id = "SharedId",
                Name = "LocalName",
                FontFamily = "Nunito Sans",
                Theme = new MudTheme()
            };
            ThemeFontHelper.ApplyFont(localTheme, localTheme.FontFamily);
            await _localStorage.SetItemAsync(LocalThemesStorageKey, new List<ThemeDefinition> { localTheme }, TestContext.Current.CancellationToken);

            var serverThemeJson = CreateThemeJson("SharedId", "ServerName", "Nunito Sans");
            SetupHttpClient(
                CreateIndexResponse("themes/theme-one.json"),
                CreateThemeResponse("themes/theme-one.json", serverThemeJson));
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.Themes.Should().ContainSingle(theme => theme.Id == "SharedId" && theme.Name == "LocalName" && theme.Source == ThemeSource.Local);
        }

        [Fact]
        public async Task GIVEN_ExistingCurrentLocalTheme_WHEN_SavedWithSameId_THEN_ReplacesAndReappliesTheme()
        {
            SetupHttpClient(CreateIndexResponse());
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            await _target.SaveLocalTheme(new ThemeDefinition
            {
                Id = "LocalId",
                Name = "InitialName",
                FontFamily = "Nunito Sans",
                Theme = new MudTheme()
            });

            await _target.ApplyTheme("LocalId");

            await _target.SaveLocalTheme(new ThemeDefinition
            {
                Id = "LocalId",
                Name = "UpdatedName",
                FontFamily = "Fira Sans",
                Theme = new MudTheme()
            });

            _target.Themes.Should().ContainSingle(theme => theme.Id == "LocalId" && theme.Name == "UpdatedName");
            _target.CurrentTheme.Should().NotBeNull();
            _target.CurrentTheme!.Name.Should().Be("UpdatedName");
            _target.CurrentFontFamily.Should().Be("Fira Sans");
        }

        [Fact]
        public async Task GIVEN_InvalidThemeIndexJson_WHEN_Initialized_THEN_UsesFallbackTheme()
        {
            var invalidIndexResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{invalid json}")
            };

            SetupHttpClient(invalidIndexResponse);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.Themes.Should().BeEmpty();
            _target.CurrentThemeId.Should().Be("default");
        }

        [Fact]
        public async Task GIVEN_NullThemeIndexPayload_WHEN_Initialized_THEN_UsesFallbackTheme()
        {
            var nullIndexResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null")
            };

            SetupHttpClient(nullIndexResponse);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.Themes.Should().BeEmpty();
            _target.CurrentThemeId.Should().Be("default");
        }

        [Fact]
        public async Task GIVEN_ServerThemeEntriesWithFailures_WHEN_Initialized_THEN_LoadsOnlyValidThemes()
        {
            var validThemeJson = CreateThemeJson("ValidId", "ValidName", "Nunito Sans");

            var handler = new ThemeMessageHandler(
                new Dictionary<string, HttpResponseMessage>
                {
                    { ThemeIndexPath, CreateIndexResponse(" ", "themes/not-found.json", "themes/http-exception.json", "themes/json-exception.json", "themes/null-definition.json", "themes/valid.json") },
                    { "/themes/not-found.json", _notFoundResponseMessage },
                    { "/themes/json-exception.json", CreateThemeHttpResponse("{invalid json}") },
                    { "/themes/null-definition.json", CreateThemeHttpResponse("null") },
                    { "/themes/valid.json", CreateThemeHttpResponse(validThemeJson) }
                },
                new Dictionary<string, Exception>
                {
                    { "/themes/http-exception.json", new HttpRequestException("Error") }
                });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.Themes.Should().ContainSingle(theme => theme.Id == "ValidId");
            _target.CurrentThemeId.Should().Be("ValidId");
        }

        [Fact]
        public async Task GIVEN_ConcurrentEnsureInitializedCalls_WHEN_BlockedOnFontCatalog_THEN_InvokesFontCatalogOnce()
        {
            SetupHttpClient(CreateIndexResponse());

            var gate = new TaskCompletionSource<bool>();
            var callCount = 0;
            Mock.Get(_fontCatalog)
                .Setup(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    Interlocked.Increment(ref callCount);
                    return gate.Task;
                });

            var url = "Url";
            Mock.Get(_fontCatalog)
                .Setup(catalog => catalog.TryGetFontUrl(It.IsAny<string>(), out url))
                .Returns(true);

            var first = _target.EnsureInitialized();
            var second = _target.EnsureInitialized();

            callCount.Should().Be(1);

            gate.SetResult(true);
            await Task.WhenAll(first, second);
        }

        private static string CreateThemeJson(string id, string name, string fontFamily)
        {
            var definition = new ThemeDefinition
            {
                Id = id,
                Name = name,
                FontFamily = fontFamily,
                Theme = new MudTheme()
            };
            ThemeFontHelper.ApplyFont(definition, fontFamily);

            return ThemeSerialization.SerializeDefinition(definition, false);
        }

        private static (string Path, HttpResponseMessage Response) CreateThemeResponse(string path, string themeJson)
        {
            return (path, CreateThemeHttpResponse(themeJson));
        }

        private static HttpResponseMessage CreateThemeHttpResponse(string themeJson)
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(themeJson)
            };
        }

        private static HttpResponseMessage CreateIndexResponse(params string[] entries)
        {
            var json = JsonSerializer.Serialize(entries.ToList(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
        }

        private void SetupFontCatalogValid(string fontFamily)
        {
            Mock.Get(_fontCatalog)
                .Setup(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var url = "Url";
            Mock.Get(_fontCatalog)
                .Setup(catalog => catalog.TryGetFontUrl(It.IsAny<string>(), out url))
                .Returns(true);
        }

        private void SetupFontCatalogInvalid()
        {
            Mock.Get(_fontCatalog)
                .Setup(catalog => catalog.EnsureInitialized(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var url = string.Empty;
            Mock.Get(_fontCatalog)
                .Setup(catalog => catalog.TryGetFontUrl(It.IsAny<string>(), out url))
                .Returns(false);
        }

        private void SetupHttpClient(HttpResponseMessage indexResponse, params (string Path, HttpResponseMessage Response)[] responses)
        {
            var responseMap = new Dictionary<string, HttpResponseMessage>
            {
                { ThemeIndexPath, indexResponse }
            };

            foreach (var response in responses)
            {
                responseMap["/" + response.Path.TrimStart('/')] = response.Response;
            }

            var handler = new ThemeMessageHandler(responseMap);
            SetupHttpClient(handler);
        }

        private void SetupHttpClient(ThemeMessageHandler handler)
        {
            Mock.Get(_httpClientFactory)
                .Setup(factory => factory.CreateClient("Assets"))
                .Returns(TestHttpClientFactory.CreateClient(handler));
        }

        private sealed class ThemeMessageHandler : HttpMessageHandler
        {
            private IDictionary<string, HttpResponseMessage> _responses;
            private IDictionary<string, Exception> _exceptions;

            public ThemeMessageHandler(IDictionary<string, HttpResponseMessage> responses)
                : this(responses, new Dictionary<string, Exception>())
            {
            }

            public ThemeMessageHandler(IDictionary<string, HttpResponseMessage> responses, IDictionary<string, Exception> exceptions)
            {
                _responses = responses;
                _exceptions = exceptions;
            }

            public void SetResponses(IDictionary<string, HttpResponseMessage> responses, IDictionary<string, Exception>? exceptions = null)
            {
                _responses = responses;
                _exceptions = exceptions ?? new Dictionary<string, Exception>();
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                if (_exceptions.TryGetValue(path, out var exception))
                {
                    return Task.FromException<HttpResponseMessage>(exception);
                }

                var response = _responses.TryGetValue(path, out var value)
                    ? value
                    : _notFoundResponseMessage;
                return Task.FromResult(response);
            }
        }
    }
}
