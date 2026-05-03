using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Core.Theming;
using Microsoft.Extensions.Logging;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Infrastructure.Test.Services
{
    public sealed class ThemeManagerServiceTests
    {
        private const string _localThemesStorageKey = "ThemeManager.LocalThemes";
        private const string _selectedThemeStorageKey = "ThemeManager.SelectedThemeId";
        private const string _selectedThemeDefinitionStorageKey = "ThemeManager.SelectedThemeDefinition";
        private const string _themeIndexPath = "/themes/index.json";
        private const string _repositoryIndexPath = "/qbtmud-themes/index.json";
        private const string _repositoryIndexUrl = "https://lantean-code.github.io/qbtmud-themes/index.json";

        private static readonly HttpResponseMessage _notFoundResponseMessage = new HttpResponseMessage(HttpStatusCode.NotFound);

        private readonly TestLocalStorageService _localStorage;
        private readonly IThemeFontCatalog _fontCatalog;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILanguageLocalizer _languageLocalizer;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ILogger<ThemeManagerService> _logger;
        private readonly ThemeManagerService _target;

        public ThemeManagerServiceTests()
        {
            _localStorage = new TestLocalStorageService();
            _fontCatalog = Mock.Of<IThemeFontCatalog>();
            _httpClientFactory = Mock.Of<IHttpClientFactory>();
            _languageLocalizer = Mock.Of<ILanguageLocalizer>();
            _appSettingsService = Mock.Of<IAppSettingsService>();
            _logger = Mock.Of<ILogger<ThemeManagerService>>();
            Mock.Get(_languageLocalizer)
                .Setup(localizer => localizer.Translate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string _, string source, object[] _) => source);
            SetupThemeRepositoryIndexUrl(string.Empty);
            _target = new ThemeManagerService(_httpClientFactory, _localStorage, _fontCatalog, _languageLocalizer, _appSettingsService, _logger);
        }

        [Fact]
        public async Task GIVEN_ThemeModePreferenceConfiguredInAppSettings_WHEN_Initialized_THEN_CurrentThemeModePreferenceMatchesSettings()
        {
            var settings = AppSettings.Default.Clone();
            settings.ThemeModePreference = ThemeModePreference.Dark;
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);
            SetupHttpClient(CreateIndexResponse());
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.CurrentThemeModePreference.Should().Be(ThemeModePreference.Dark);
        }

        [Fact]
        public async Task GIVEN_InvalidThemeModePreferenceConfiguredInAppSettings_WHEN_Initialized_THEN_CurrentThemeModePreferenceFallsBackToSystem()
        {
            var settings = AppSettings.Default.Clone();
            settings.ThemeModePreference = (ThemeModePreference)999;
            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);
            SetupHttpClient(CreateIndexResponse());
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.CurrentThemeModePreference.Should().Be(ThemeModePreference.System);
        }

        [Fact]
        public void GIVEN_NewThemeModePreference_WHEN_ApplyPersistedThemeModePreferenceInvoked_THEN_RaisesThemeModePreferenceChangedEvent()
        {
            ThemeModePreferenceChangedEventArgs? captured = null;
            _target.ThemeModePreferenceChanged += (_, args) => captured = args;

            _target.ApplyPersistedThemeModePreference(ThemeModePreference.Dark);

            _target.CurrentThemeModePreference.Should().Be(ThemeModePreference.Dark);
            captured.Should().NotBeNull();
            captured!.ThemeModePreference.Should().Be(ThemeModePreference.Dark);
            Mock.Get(_appSettingsService)
                .Verify(service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void GIVEN_UnchangedThemeModePreference_WHEN_ApplyPersistedThemeModePreferenceInvoked_THEN_DoesNotRaiseThemeModePreferenceChangedEvent()
        {
            ThemeModePreferenceChangedEventArgs? captured = null;
            _target.ThemeModePreferenceChanged += (_, args) => captured = args;

            _target.ApplyPersistedThemeModePreference(ThemeModePreference.System);

            captured.Should().BeNull();
            Mock.Get(_appSettingsService)
                .Verify(service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void GIVEN_InvalidThemeModePreference_WHEN_ApplyPersistedThemeModePreferenceInvoked_THEN_NormalizesToSystem()
        {
            ThemeModePreferenceChangedEventArgs? captured = null;
            _target.ThemeModePreferenceChanged += (_, args) => captured = args;

            _target.ApplyPersistedThemeModePreference((ThemeModePreference)999);

            _target.CurrentThemeModePreference.Should().Be(ThemeModePreference.System);
            captured.Should().BeNull();
            Mock.Get(_appSettingsService)
                .Verify(service => service.SaveSettingsAsync(It.IsAny<AppSettings>(), It.IsAny<CancellationToken>()), Times.Never);
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
            await _localStorage.SetItemAsync(_selectedThemeStorageKey, "ThemeId", TestContext.Current.CancellationToken);
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
            var storedThemeId = await _localStorage.GetItemAsync<string?>(_selectedThemeStorageKey, TestContext.Current.CancellationToken);
            storedThemeId.Should().Be("ThemeId");
            var storedThemeDefinition = await _localStorage.GetItemAsync<ThemeDefinition?>(_selectedThemeDefinitionStorageKey, TestContext.Current.CancellationToken);
            storedThemeDefinition.Should().NotBeNull();
            storedThemeDefinition!.Id.Should().Be("ThemeId");
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

            var stored = await _localStorage.GetItemAsync<List<ThemeDefinition>>(_localThemesStorageKey, TestContext.Current.CancellationToken);
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

            await _localStorage.SetItemAsync(_localThemesStorageKey, new List<ThemeDefinition> { localTheme }, TestContext.Current.CancellationToken);
            await _localStorage.SetItemAsync(_selectedThemeStorageKey, "LocalId", TestContext.Current.CancellationToken);

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
                { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/themes/theme-one.json", CreateThemeHttpResponse(themeJson) }
            });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            handler.SetResponses(new Dictionary<string, HttpResponseMessage>
            {
                { _themeIndexPath, CreateIndexResponse() }
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
                { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/themes/theme-one.json", CreateThemeHttpResponse(themeJson) }
            });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            var storedThemeIdBeforeReload = await _localStorage.GetItemAsync<string?>(_selectedThemeStorageKey, TestContext.Current.CancellationToken);
            storedThemeIdBeforeReload.Should().BeNull();

            handler.SetResponses(new Dictionary<string, HttpResponseMessage>
            {
                { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/themes/theme-one.json", CreateThemeHttpResponse(themeJson) }
            });

            await _target.ReloadServerThemes();

            var storedThemeIdAfterReload = await _localStorage.GetItemAsync<string?>(_selectedThemeStorageKey, TestContext.Current.CancellationToken);
            storedThemeIdAfterReload.Should().Be("ThemeId");
        }

        [Fact]
        public async Task GIVEN_InitializedService_WHEN_ReloadServerThemesIndexRequestThrows_THEN_DoesNotThrow()
        {
            var themeJson = CreateThemeJson("ThemeId", "Name", "Nunito Sans");
            var handler = new ThemeMessageHandler(new Dictionary<string, HttpResponseMessage>
            {
                { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/themes/theme-one.json", CreateThemeHttpResponse(themeJson) }
            });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            handler.SetResponses(
                new Dictionary<string, HttpResponseMessage>(),
                new Dictionary<string, Exception>
                {
                    { _themeIndexPath, new HttpRequestException("Error") }
                });

            var action = async () => await _target.ReloadServerThemes();

            await action.Should().NotThrowAsync();
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
            await _localStorage.SetItemAsync(_localThemesStorageKey, new List<ThemeDefinition> { localTheme }, TestContext.Current.CancellationToken);

            var serverThemeJson = CreateThemeJson("SharedId", "ServerName", "Nunito Sans");
            SetupHttpClient(
                CreateIndexResponse("themes/theme-one.json"),
                CreateThemeResponse("themes/theme-one.json", serverThemeJson));
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.Themes.Should().ContainSingle(theme => theme.Id == "SharedId" && theme.Name == "LocalName" && theme.Source == ThemeSource.Local);
        }

        [Fact]
        public async Task GIVEN_RepositoryConfigured_WHEN_Initialized_THEN_DoesNotLoadRepositoryThemes()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var bundledThemeJson = CreateThemeJson("BundledId", "BundledName", "Nunito Sans");
            var handler = new ThemeMessageHandler(
                new Dictionary<string, HttpResponseMessage>
                {
                    { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                    { "/themes/theme-one.json", CreateThemeHttpResponse(bundledThemeJson) }
                },
                new Dictionary<string, Exception>
                {
                    { _repositoryIndexPath, new HttpRequestException("Repository should not be loaded during initialization.") }
                });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.Themes.Should().ContainSingle(theme => theme.Id == "BundledId" && theme.Source == ThemeSource.Server);
        }

        [Fact]
        public async Task GIVEN_RepositoryConfigured_WHEN_PreloadedAndEnsured_THEN_LoadsRepositoryThemesWithoutIssueFlag()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var bundledThemeJson = CreateThemeJson("BundledId", "BundledName", "Nunito Sans");
            var repositoryThemeJson = CreateThemeJson("RepositoryId", "RepositoryName", "Nunito Sans");

            var handler = new ThemeMessageHandler(new Dictionary<string, HttpResponseMessage>
            {
                { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/themes/theme-one.json", CreateThemeHttpResponse(bundledThemeJson) },
                { _repositoryIndexPath, CreateIndexResponse("themes/repository-theme.json") },
                { "/qbtmud-themes/themes/repository-theme.json", CreateThemeHttpResponse(repositoryThemeJson) }
            });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.PreloadRepositoryThemes();
            await _target.EnsureRepositoryThemesLoaded();

            _target.LastReloadHadRepositoryIssues.Should().BeFalse();
            _target.Themes.Should().Contain(theme => theme.Id == "RepositoryId" && theme.Source == ThemeSource.Repository);
        }

        [Fact]
        public async Task GIVEN_RepositoryConfigured_WHEN_PreloadedAndEnsured_THEN_DoesNotReloadBundledThemes()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var bundledIndexRequests = 0;
            var bundledThemeRequests = 0;
            var bundledThemeJson = CreateThemeJson("BundledId", "BundledName", "Nunito Sans");
            var repositoryThemeJson = CreateThemeJson("RepositoryId", "RepositoryName", "Nunito Sans");

            var handler = new ThemeMessageHandler(
                new Dictionary<string, HttpResponseMessage>
                {
                    { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                    { "/themes/theme-one.json", CreateThemeHttpResponse(bundledThemeJson) },
                    { _repositoryIndexPath, CreateIndexResponse("themes/repository-theme.json") },
                    { "/qbtmud-themes/themes/repository-theme.json", CreateThemeHttpResponse(repositoryThemeJson) }
                },
                customResponses: request =>
                {
                    var path = request.RequestUri?.AbsolutePath;
                    if (string.Equals(path, _themeIndexPath, StringComparison.Ordinal))
                    {
                        bundledIndexRequests++;
                    }

                    if (string.Equals(path, "/themes/theme-one.json", StringComparison.Ordinal))
                    {
                        bundledThemeRequests++;
                    }

                    return null;
                });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.PreloadRepositoryThemes();
            await _target.EnsureRepositoryThemesLoaded();

            bundledIndexRequests.Should().Be(1);
            bundledThemeRequests.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_StoredRepositoryThemeId_WHEN_Initialized_THEN_PreservesThemeIdUntilRepositoryReload()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);
            await _localStorage.SetItemAsync(_selectedThemeStorageKey, "RepositoryId", TestContext.Current.CancellationToken);
            await _localStorage.SetItemAsync(
                _selectedThemeDefinitionStorageKey,
                new ThemeDefinition
                {
                    Id = "RepositoryId",
                    Name = "RepositoryName",
                    FontFamily = "Nunito Sans",
                    Theme = new MudTheme()
                },
                TestContext.Current.CancellationToken);

            var bundledThemeJson = CreateThemeJson("BundledId", "BundledName", "Nunito Sans");
            var handler = new ThemeMessageHandler(
                new Dictionary<string, HttpResponseMessage>
                {
                    { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                    { "/themes/theme-one.json", CreateThemeHttpResponse(bundledThemeJson) },
                    { _repositoryIndexPath, CreateIndexResponse("themes/repository-theme.json") },
                    { "/qbtmud-themes/themes/repository-theme.json", CreateThemeHttpResponse(CreateThemeJson("RepositoryId", "RepositoryName", "Nunito Sans")) }
                });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.CurrentTheme.Should().NotBeNull();
            _target.CurrentTheme!.Id.Should().Be("RepositoryId");
            _target.CurrentTheme.Source.Should().Be(ThemeSource.Repository);
            _target.CurrentThemeId.Should().Be("RepositoryId");
            _target.Themes.Should().ContainSingle(theme => theme.Id == "BundledId" && theme.Source == ThemeSource.Server);
        }

        [Fact]
        public async Task GIVEN_StoredRepositoryThemeIdWithoutSnapshot_WHEN_Initialized_THEN_PreservesIdWithoutCurrentTheme()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);
            await _localStorage.SetItemAsync(_selectedThemeStorageKey, "RepositoryId", TestContext.Current.CancellationToken);
            SetupHttpClient(CreateIndexResponse());
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.CurrentThemeId.Should().Be("RepositoryId");
            _target.CurrentTheme.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_StoredRepositorySnapshotIdMismatch_WHEN_Initialized_THEN_PreservesIdWithoutCurrentTheme()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);
            await _localStorage.SetItemAsync(_selectedThemeStorageKey, "RepositoryId", TestContext.Current.CancellationToken);
            await _localStorage.SetItemAsync(
                _selectedThemeDefinitionStorageKey,
                new ThemeDefinition
                {
                    Id = "OtherId",
                    Name = "RepositoryName",
                    FontFamily = "Nunito Sans",
                    Theme = new MudTheme()
                },
                TestContext.Current.CancellationToken);
            SetupHttpClient(CreateIndexResponse());
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.CurrentThemeId.Should().Be("RepositoryId");
            _target.CurrentTheme.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_RepositoryThemeSharesBundledId_WHEN_ReloadedAfterInitialization_THEN_RepositoryThemeTakesPrecedence()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var bundledThemeJson = CreateThemeJson("SharedId", "BundledName", "Nunito Sans");
            var repositoryThemeJson = CreateThemeJson("SharedId", "RepositoryName", "Nunito Sans");

            var handler = new ThemeMessageHandler(new Dictionary<string, HttpResponseMessage>
            {
                { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/themes/theme-one.json", CreateThemeHttpResponse(bundledThemeJson) },
                { _repositoryIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/qbtmud-themes/themes/theme-one.json", CreateThemeHttpResponse(repositoryThemeJson) }
            });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.ReloadServerThemes();

            _target.Themes.Should().ContainSingle(theme => theme.Id == "SharedId" && theme.Name == "RepositoryName" && theme.Source == ThemeSource.Repository);
        }

        [Fact]
        public async Task GIVEN_LocalThemeSharesRepositoryId_WHEN_ReloadedAfterInitialization_THEN_LocalThemeTakesPrecedence()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var localTheme = new ThemeDefinition
            {
                Id = "SharedId",
                Name = "LocalName",
                FontFamily = "Nunito Sans",
                Theme = new MudTheme()
            };
            ThemeFontHelper.ApplyFont(localTheme, localTheme.FontFamily);
            await _localStorage.SetItemAsync(_localThemesStorageKey, new List<ThemeDefinition> { localTheme }, TestContext.Current.CancellationToken);

            var repositoryThemeJson = CreateThemeJson("SharedId", "RepositoryName", "Nunito Sans");
            var handler = new ThemeMessageHandler(new Dictionary<string, HttpResponseMessage>
            {
                { _themeIndexPath, CreateIndexResponse() },
                { _repositoryIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/qbtmud-themes/themes/theme-one.json", CreateThemeHttpResponse(repositoryThemeJson) }
            });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.ReloadServerThemes();

            _target.Themes.Should().ContainSingle(theme => theme.Id == "SharedId" && theme.Name == "LocalName" && theme.Source == ThemeSource.Local);
        }

        [Fact]
        public async Task GIVEN_BundledClientFactoryThrows_WHEN_Initialized_THEN_UsesFallbackTheme()
        {
            Mock.Get(_httpClientFactory)
                .Setup(factory => factory.CreateClient("Assets"))
                .Throws(new InvalidOperationException("Failure"));
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.CurrentThemeId.Should().Be("default");
            _target.Themes.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_BundledIndexRequestCanceled_WHEN_Initialized_THEN_UsesFallbackTheme()
        {
            var handler = new ThemeMessageHandler(
                new Dictionary<string, HttpResponseMessage>(),
                new Dictionary<string, Exception>
                {
                    { _themeIndexPath, new TaskCanceledException("Canceled") }
                });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.CurrentThemeId.Should().Be("default");
            _target.Themes.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_RepositoryIndexUnavailable_WHEN_InitializedAndReloaded_THEN_ReloadSetsRepositoryIssueFlag()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var bundledThemeJson = CreateThemeJson("ThemeId", "BundledName", "Nunito Sans");
            var handler = new ThemeMessageHandler(new Dictionary<string, HttpResponseMessage>
            {
                { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/themes/theme-one.json", CreateThemeHttpResponse(bundledThemeJson) },
                { _repositoryIndexPath, _notFoundResponseMessage }
            });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            _target.LastReloadHadRepositoryIssues.Should().BeFalse();

            await _target.ReloadServerThemes();

            _target.LastReloadHadRepositoryIssues.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_PreloadFails_WHEN_Preloaded_THEN_LogsWarning()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);
            Mock.Get(_logger)
                .Setup(logger => logger.IsEnabled(LogLevel.Warning))
                .Returns(true);

            var handler = new ThemeMessageHandler(
                new Dictionary<string, HttpResponseMessage>
                {
                    { _themeIndexPath, CreateIndexResponse() }
                },
                new Dictionary<string, Exception>
                {
                    { _repositoryIndexPath, new HttpRequestException("Failure") }
                });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.PreloadRepositoryThemes();
            await _target.EnsureRepositoryThemesLoaded();

            Mock.Get(_logger).Verify(
                logger => logger.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Background repository theme preload", StringComparison.Ordinal)),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_PreloadFails_WHEN_ThemesEnsured_THEN_RetriesAndSetsIssueFlagFromRetry()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var bundledThemeJson = CreateThemeJson("BundledId", "BundledName", "Nunito Sans");
            var repositoryThemeJson = CreateThemeJson("RepositoryId", "RepositoryName", "Nunito Sans");
            var repositoryIndexRequests = 0;

            var handler = new ThemeMessageHandler(
                new Dictionary<string, HttpResponseMessage>
                {
                    { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                    { "/themes/theme-one.json", CreateThemeHttpResponse(bundledThemeJson) },
                    { "/qbtmud-themes/themes/repository-theme.json", CreateThemeHttpResponse(repositoryThemeJson) }
                },
                customResponses: request =>
                {
                    if (string.Equals(request.RequestUri?.AbsolutePath, _repositoryIndexPath, StringComparison.Ordinal))
                    {
                        repositoryIndexRequests++;
                        if (repositoryIndexRequests == 1)
                        {
                            throw new HttpRequestException("Failure");
                        }

                        return CreateIndexResponse("themes/repository-theme.json");
                    }

                    return null;
                });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.PreloadRepositoryThemes();
            await _target.EnsureRepositoryThemesLoaded();

            repositoryIndexRequests.Should().Be(2);
            _target.LastReloadHadRepositoryIssues.Should().BeFalse();
            _target.Themes.Should().Contain(theme => theme.Id == "RepositoryId" && theme.Source == ThemeSource.Repository);
        }

        [Fact]
        public async Task GIVEN_RepositoryIndexUrlUsesHttp_WHEN_Reloaded_THEN_SetsRepositoryIssueFlag()
        {
            SetupThemeRepositoryIndexUrl("http://example.com/index.json");
            SetupHttpClient(CreateIndexResponse());
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.ReloadServerThemes();

            _target.LastReloadHadRepositoryIssues.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_RepositoryIndexUrlIsInvalid_WHEN_Reloaded_THEN_SetsRepositoryIssueFlag()
        {
            SetupThemeRepositoryIndexUrl("not a uri");
            SetupHttpClient(CreateIndexResponse());
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.ReloadServerThemes();

            _target.LastReloadHadRepositoryIssues.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_RepositoryClientFactoryThrows_WHEN_Reloaded_THEN_SetsRepositoryIssueFlag()
        {
            var handler = new ThemeMessageHandler(new Dictionary<string, HttpResponseMessage>
            {
                { _themeIndexPath, CreateIndexResponse() }
            });
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);
            Mock.Get(_httpClientFactory)
                .SetupSequence(factory => factory.CreateClient("Assets"))
                .Returns(TestHttpClientFactory.CreateClient(handler))
                .Returns(TestHttpClientFactory.CreateClient(handler))
                .Throws(new InvalidOperationException("Failure"));
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.ReloadServerThemes();

            _target.LastReloadHadRepositoryIssues.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_RepositoryIndexRequestThrows_WHEN_Reloaded_THEN_SetsRepositoryIssueFlag()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var handler = new ThemeMessageHandler(
                new Dictionary<string, HttpResponseMessage>
                {
                    { _themeIndexPath, CreateIndexResponse() }
                },
                new Dictionary<string, Exception>
                {
                    { _repositoryIndexPath, new HttpRequestException("Failure") }
                });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.ReloadServerThemes();

            _target.LastReloadHadRepositoryIssues.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_RepositoryIndexJsonInvalid_WHEN_Reloaded_THEN_SetsRepositoryIssueFlag()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var handler = new ThemeMessageHandler(
                new Dictionary<string, HttpResponseMessage>
                {
                    { _themeIndexPath, CreateIndexResponse() },
                    { _repositoryIndexPath, CreateThemeHttpResponse("{invalid json}") }
                });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.ReloadServerThemes();

            _target.LastReloadHadRepositoryIssues.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_RepositoryIndexPayloadNull_WHEN_Reloaded_THEN_SetsRepositoryIssueFlag()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var handler = new ThemeMessageHandler(
                new Dictionary<string, HttpResponseMessage>
                {
                    { _themeIndexPath, CreateIndexResponse() },
                    { _repositoryIndexPath, CreateThemeHttpResponse("null") }
                });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.ReloadServerThemes();

            _target.LastReloadHadRepositoryIssues.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_MissingSelectedThemeWithoutRepository_WHEN_Initialized_THEN_ClearsStoredSelection()
        {
            await _localStorage.SetItemAsync(_selectedThemeStorageKey, "MissingId", TestContext.Current.CancellationToken);
            await _localStorage.SetItemAsync(
                _selectedThemeDefinitionStorageKey,
                new ThemeDefinition
                {
                    Id = "MissingId",
                    Name = "MissingName",
                    FontFamily = "Nunito Sans",
                    Theme = new MudTheme()
                },
                TestContext.Current.CancellationToken);
            SetupHttpClient(CreateIndexResponse());
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();

            var storedThemeId = await _localStorage.GetItemAsync<string?>(_selectedThemeStorageKey, TestContext.Current.CancellationToken);
            var storedThemeDefinition = await _localStorage.GetItemAsync<ThemeDefinition?>(_selectedThemeDefinitionStorageKey, TestContext.Current.CancellationToken);

            storedThemeId.Should().BeNull();
            storedThemeDefinition.Should().BeNull();
            _target.CurrentThemeId.Should().Be("default");
        }

        [Fact]
        public async Task GIVEN_CurrentThemeRemovedOnReload_WHEN_Reloaded_THEN_ClearsStoredSelection()
        {
            var themeJson = CreateThemeJson("ThemeId", "Name", "Nunito Sans");
            var handler = new ThemeMessageHandler(new Dictionary<string, HttpResponseMessage>
            {
                { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                { "/themes/theme-one.json", CreateThemeHttpResponse(themeJson) }
            });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.ApplyTheme("ThemeId");

            handler.SetResponses(new Dictionary<string, HttpResponseMessage>
            {
                { _themeIndexPath, CreateIndexResponse() }
            });

            await _target.ReloadServerThemes();

            var storedThemeId = await _localStorage.GetItemAsync<string?>(_selectedThemeStorageKey, TestContext.Current.CancellationToken);
            var storedThemeDefinition = await _localStorage.GetItemAsync<ThemeDefinition?>(_selectedThemeDefinitionStorageKey, TestContext.Current.CancellationToken);

            storedThemeId.Should().BeNull();
            storedThemeDefinition.Should().BeNull();
            _target.CurrentThemeId.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_RepositoryIndexRequestCanceled_WHEN_InitializedAndReloaded_THEN_DoesNotThrowAndSetsRepositoryIssueFlag()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var bundledThemeJson = CreateThemeJson("ThemeId", "BundledName", "Nunito Sans");
            var handler = new ThemeMessageHandler(
                new Dictionary<string, HttpResponseMessage>
                {
                    { _themeIndexPath, CreateIndexResponse("themes/theme-one.json") },
                    { "/themes/theme-one.json", CreateThemeHttpResponse(bundledThemeJson) }
                },
                new Dictionary<string, Exception>
                {
                    { _repositoryIndexPath, new TaskCanceledException("Timeout") }
                });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            var initializeAction = async () => await _target.EnsureInitialized();
            await initializeAction.Should().NotThrowAsync();

            _target.Themes.Should().ContainSingle(theme => theme.Id == "ThemeId" && theme.Source == ThemeSource.Server);
            _target.LastReloadHadRepositoryIssues.Should().BeFalse();

            var reloadAction = async () => await _target.ReloadServerThemes();
            await reloadAction.Should().NotThrowAsync();

            _target.LastReloadHadRepositoryIssues.Should().BeTrue();
            _target.Themes.Should().ContainSingle(theme => theme.Id == "ThemeId" && theme.Source == ThemeSource.Server);
        }

        [Fact]
        public async Task GIVEN_RepositoryIndexContainsMixedEntries_WHEN_Reloaded_THEN_LoadsValidThemesAndCapturesIssues()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var absoluteThemeJson = CreateThemeJson("AbsoluteId", "AbsoluteName", "Nunito Sans");
            var relativeThemeJson = CreateThemeJson("RelativeId", "RelativeName", "Nunito Sans");

            var handler = new ThemeMessageHandler(
                new Dictionary<string, HttpResponseMessage>
                {
                    { _themeIndexPath, CreateIndexResponse() },
                    { _repositoryIndexPath, CreateIndexResponse(" ", "http://example.com/insecure.json", "http://[", "https://cdn.example.com/absolute.json", "themes/canceled.json", "themes/relative.json") },
                    { "/absolute.json", CreateThemeHttpResponse(absoluteThemeJson) },
                    { "/qbtmud-themes/themes/relative.json", CreateThemeHttpResponse(relativeThemeJson) }
                },
                new Dictionary<string, Exception>
                {
                    { "/qbtmud-themes/themes/canceled.json", new TaskCanceledException("Canceled") }
                });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.ReloadServerThemes();

            _target.LastReloadHadRepositoryIssues.Should().BeTrue();
            _target.Themes.Should().Contain(theme => theme.Id == "AbsoluteId" && theme.Source == ThemeSource.Repository);
            _target.Themes.Should().Contain(theme => theme.Id == "RelativeId" && theme.Source == ThemeSource.Repository);
        }

        [Fact]
        public async Task GIVEN_RepositoryContainsDuplicateThemeIds_WHEN_ReloadedAfterInitialization_THEN_OnlyFirstRepositoryThemeIsKept()
        {
            SetupThemeRepositoryIndexUrl(_repositoryIndexUrl);

            var firstThemeJson = CreateThemeJson("SharedId", "RepositoryFirst", "Nunito Sans");
            var secondThemeJson = CreateThemeJson("SharedId", "RepositorySecond", "Nunito Sans");

            var handler = new ThemeMessageHandler(new Dictionary<string, HttpResponseMessage>
            {
                { _themeIndexPath, CreateIndexResponse() },
                { _repositoryIndexPath, CreateIndexResponse("themes/theme-one.json", "themes/theme-two.json") },
                { "/qbtmud-themes/themes/theme-one.json", CreateThemeHttpResponse(firstThemeJson) },
                { "/qbtmud-themes/themes/theme-two.json", CreateThemeHttpResponse(secondThemeJson) }
            });
            SetupHttpClient(handler);
            SetupFontCatalogValid("Nunito Sans");

            await _target.EnsureInitialized();
            await _target.ReloadServerThemes();

            _target.Themes.Should().ContainSingle(theme =>
                theme.Id == "SharedId"
                && theme.Name == "RepositoryFirst"
                && theme.Source == ThemeSource.Repository);
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
                    { _themeIndexPath, CreateIndexResponse(" ", "themes/not-found.json", "themes/http-exception.json", "themes/json-exception.json", "themes/null-definition.json", "themes/valid.json") },
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

        private void SetupThemeRepositoryIndexUrl(string value)
        {
            var settings = AppSettings.Default.Clone();
            settings.ThemeRepositoryIndexUrl = value;

            Mock.Get(_appSettingsService)
                .Setup(service => service.GetSettingsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);
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
                { _themeIndexPath, indexResponse }
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
            private readonly Func<HttpRequestMessage, HttpResponseMessage?>? _customResponses;

            public ThemeMessageHandler(IDictionary<string, HttpResponseMessage> responses)
                : this(responses, new Dictionary<string, Exception>())
            {
            }

            public ThemeMessageHandler(IDictionary<string, HttpResponseMessage> responses, IDictionary<string, Exception> exceptions)
                : this(responses, exceptions, null)
            {
            }

            public ThemeMessageHandler(IDictionary<string, HttpResponseMessage> responses, Func<HttpRequestMessage, HttpResponseMessage?> customResponses)
                : this(responses, new Dictionary<string, Exception>(), customResponses)
            {
            }

            public ThemeMessageHandler(IDictionary<string, HttpResponseMessage> responses, IDictionary<string, Exception> exceptions, Func<HttpRequestMessage, HttpResponseMessage?>? customResponses)
            {
                _responses = responses;
                _exceptions = exceptions;
                _customResponses = customResponses;
            }

            public void SetResponses(IDictionary<string, HttpResponseMessage> responses, IDictionary<string, Exception>? exceptions = null)
            {
                _responses = responses;
                _exceptions = exceptions ?? new Dictionary<string, Exception>();
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                if (_customResponses is not null)
                {
                    try
                    {
                        var customResponse = _customResponses(request);
                        if (customResponse is not null)
                        {
                            return Task.FromResult(customResponse);
                        }
                    }
                    catch (Exception ex)
                    {
                        return Task.FromException<HttpResponseMessage>(ex);
                    }
                }

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
