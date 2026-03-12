using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Theming;
using System.Text.Json;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Manages the theme catalog and the applied theme selection.
    /// </summary>
    public sealed class ThemeManagerService : IThemeManagerService
    {
        private const string AppContext = "App";
        private const string LocalThemesStorageKey = "ThemeManager.LocalThemes";
        private const string SelectedThemeStorageKey = "ThemeManager.SelectedThemeId";
        private const string BundledThemeIndexPath = "themes/index.json";

        private readonly SemaphoreSlim _initializationSemaphore = new SemaphoreSlim(1, 1);
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISettingsStorageService _settingsStorage;
        private readonly IThemeFontCatalog _themeFontCatalog;
        private readonly ILanguageLocalizer _languageLocalizer;
        private readonly IAppSettingsService _appSettingsService;
        private readonly List<ThemeDefinition> _localThemes = [];
        private readonly List<ThemeCatalogItem> _repositoryThemes = [];
        private readonly List<ThemeCatalogItem> _bundledThemes = [];
        private readonly List<ThemeCatalogItem> _themes = [];
        private IReadOnlyList<ThemeCatalogItem> _themesView = [];
        private bool _initialized;
        private bool _lastReloadHadRepositoryIssues;
        private ThemeCatalogItem? _currentTheme;
        private string? _currentThemeId;
        private string _currentFontFamily = "Nunito Sans";

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeManagerService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory for loading theme assets.</param>
        /// <param name="settingsStorage">The local storage service.</param>
        /// <param name="themeFontCatalog">The theme font catalog.</param>
        /// <param name="languageLocalizer">The language localizer.</param>
        /// <param name="appSettingsService">The app settings service.</param>
        public ThemeManagerService(
            IHttpClientFactory httpClientFactory,
            ISettingsStorageService settingsStorage,
            IThemeFontCatalog themeFontCatalog,
            ILanguageLocalizer languageLocalizer,
            IAppSettingsService appSettingsService)
        {
            _httpClientFactory = httpClientFactory;
            _settingsStorage = settingsStorage;
            _themeFontCatalog = themeFontCatalog;
            _languageLocalizer = languageLocalizer;
            _appSettingsService = appSettingsService;
        }

        /// <summary>
        /// Occurs when the active theme changes.
        /// </summary>
        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        /// <summary>
        /// Gets the available themes.
        /// </summary>
        public IReadOnlyList<ThemeCatalogItem> Themes
        {
            get { return _themesView; }
        }

        /// <summary>
        /// Gets the currently applied theme.
        /// </summary>
        public ThemeCatalogItem? CurrentTheme
        {
            get { return _currentTheme; }
        }

        /// <summary>
        /// Gets the currently applied theme identifier.
        /// </summary>
        public string? CurrentThemeId
        {
            get { return _currentThemeId; }
        }

        /// <summary>
        /// Gets the currently applied font family.
        /// </summary>
        public string CurrentFontFamily
        {
            get { return _currentFontFamily; }
        }

        /// <summary>
        /// Gets a value indicating whether the most recent reload had repository loading issues.
        /// </summary>
        public bool LastReloadHadRepositoryIssues
        {
            get { return _lastReloadHadRepositoryIssues; }
        }

        /// <summary>
        /// Ensures the theme catalog has been loaded.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            await _initializationSemaphore.WaitAsync();
            try
            {
                if (_initialized)
                {
                    return;
                }

                await _themeFontCatalog.EnsureInitialized();

                await LoadLocalThemes();
                await LoadBundledThemes();
                await LoadRepositoryThemes(captureIssues: false);
                RebuildCatalog();
                await ApplyInitialTheme();

                _lastReloadHadRepositoryIssues = false;
                _initialized = true;
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        /// <summary>
        /// Reloads theme sources and rebuilds the catalog.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ReloadServerThemes()
        {
            if (!_initialized)
            {
                await EnsureInitialized();
                return;
            }

            await LoadBundledThemes();
            _lastReloadHadRepositoryIssues = await LoadRepositoryThemes(captureIssues: true);
            RebuildCatalog();

            if (_currentThemeId is not null && !_themes.Any(theme => theme.Id == _currentThemeId))
            {
                _currentThemeId = null;
            }

            if (_currentThemeId is not null)
            {
                await ApplyTheme(_currentThemeId);
            }
        }

        /// <summary>
        /// Applies the specified theme.
        /// </summary>
        /// <param name="themeId">The theme identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ApplyTheme(string themeId)
        {
            if (!_initialized)
            {
                await EnsureInitialized();
            }

            var theme = _themes.FirstOrDefault(entry => entry.Id == themeId);
            if (theme is null)
            {
                return;
            }

            ApplyThemeInternal(theme);
            await _settingsStorage.SetItemAsync(SelectedThemeStorageKey, theme.Id);
        }

        /// <summary>
        /// Saves a local theme definition.
        /// </summary>
        /// <param name="definition">The theme definition to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SaveLocalTheme(ThemeDefinition definition)
        {
            if (!_initialized)
            {
                await EnsureInitialized();
            }

            definition = NormalizeDefinition(definition);

            var existingIndex = _localThemes.FindIndex(theme => string.Equals(theme.Id, definition.Id, StringComparison.Ordinal));
            if (existingIndex >= 0)
            {
                _localThemes[existingIndex] = definition;
            }
            else
            {
                _localThemes.Add(definition);
            }

            await PersistLocalThemes();
            RebuildCatalog();

            if (_currentThemeId == definition.Id)
            {
                var current = _themes.FirstOrDefault(theme => theme.Id == definition.Id);
                if (current is not null)
                {
                    ApplyThemeInternal(current);
                }
            }
        }

        /// <summary>
        /// Deletes a local theme definition.
        /// </summary>
        /// <param name="themeId">The theme identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteLocalTheme(string themeId)
        {
            if (!_initialized)
            {
                await EnsureInitialized();
            }

            _localThemes.RemoveAll(theme => string.Equals(theme.Id, themeId, StringComparison.Ordinal));
            await PersistLocalThemes();
            RebuildCatalog();

            if (_currentThemeId == themeId)
            {
                _currentThemeId = null;
                await ApplyInitialTheme();
            }
        }

        private async Task ApplyInitialTheme()
        {
            var storedThemeId = await _settingsStorage.GetItemAsync<string?>(SelectedThemeStorageKey);
            if (!string.IsNullOrWhiteSpace(storedThemeId))
            {
                var storedTheme = _themes.FirstOrDefault(theme => theme.Id == storedThemeId);
                if (storedTheme is not null)
                {
                    ApplyThemeInternal(storedTheme);
                    return;
                }
            }

            if (_themes.Count > 0)
            {
                ApplyThemeInternal(_themes[0]);
                return;
            }

            var fallbackTheme = new ThemeCatalogItem(
                "default",
                TranslateApp("Default"),
                new ThemeDefinition(),
                ThemeSource.Server,
                null);
            ApplyThemeInternal(fallbackTheme);
        }

        private void ApplyThemeInternal(ThemeCatalogItem theme)
        {
            var definition = theme.Theme;
            ThemeFontHelper.ApplyFont(definition, definition.FontFamily);

            _currentTheme = theme;
            _currentThemeId = theme.Id;
            _currentFontFamily = definition.FontFamily;

            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(definition.Theme, _currentFontFamily, _currentThemeId));
        }

        private async Task LoadLocalThemes()
        {
            _localThemes.Clear();

            var themes = await _settingsStorage.GetItemAsync<List<ThemeDefinition>?>(LocalThemesStorageKey);
            if (themes is null)
            {
                return;
            }

            foreach (var theme in themes)
            {
                _localThemes.Add(NormalizeDefinition(theme));
            }
        }

        private async Task PersistLocalThemes()
        {
            await _settingsStorage.SetItemAsync(LocalThemesStorageKey, _localThemes);
        }

        private async Task LoadBundledThemes()
        {
            _bundledThemes.Clear();

            HttpClient client;
            try
            {
                client = _httpClientFactory.CreateClient("Assets");
            }
            catch (InvalidOperationException)
            {
                return;
            }

            List<string>? index;
            try
            {
                var indexResponse = await client.GetAsync(BundledThemeIndexPath);
                if (!indexResponse.IsSuccessStatusCode)
                {
                    return;
                }

                var indexJson = await indexResponse.Content.ReadAsStringAsync();
                index = JsonSerializer.Deserialize<List<string>>(indexJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            catch (HttpRequestException)
            {
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (JsonException)
            {
                return;
            }

            if (index is null || index.Count == 0)
            {
                return;
            }

            foreach (var path in index)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                var item = await TryLoadThemeCatalogItem(client, path.Trim(), path.Trim(), ThemeSource.Server);
                if (item is not null)
                {
                    _bundledThemes.Add(item);
                }
            }
        }

        private async Task<bool> LoadRepositoryThemes(bool captureIssues)
        {
            _repositoryThemes.Clear();

            var settings = await _appSettingsService.GetSettingsAsync();
            if (string.IsNullOrWhiteSpace(settings.ThemeRepositoryIndexUrl))
            {
                return false;
            }

            if (!TryCreateHttpsAbsoluteUri(settings.ThemeRepositoryIndexUrl, out var indexUri))
            {
                return captureIssues;
            }

            HttpClient client;
            try
            {
                client = _httpClientFactory.CreateClient("Assets");
            }
            catch (InvalidOperationException)
            {
                return captureIssues;
            }

            List<string>? index;
            try
            {
                var indexResponse = await client.GetAsync(indexUri);
                if (!indexResponse.IsSuccessStatusCode)
                {
                    return captureIssues;
                }

                var indexJson = await indexResponse.Content.ReadAsStringAsync();
                index = JsonSerializer.Deserialize<List<string>>(indexJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            catch (HttpRequestException)
            {
                return captureIssues;
            }
            catch (OperationCanceledException)
            {
                return captureIssues;
            }
            catch (JsonException)
            {
                return captureIssues;
            }

            if (index is null)
            {
                return captureIssues;
            }

            var hadIssues = false;
            foreach (var path in index)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (!TryResolveRepositoryThemeUri(indexUri, path.Trim(), out var themeUri))
                {
                    hadIssues = true;
                    continue;
                }

                var item = await TryLoadThemeCatalogItem(client, themeUri.AbsoluteUri, themeUri.AbsoluteUri, ThemeSource.Repository);
                if (item is null)
                {
                    hadIssues = true;
                    continue;
                }

                _repositoryThemes.Add(item);
            }

            return captureIssues && hadIssues;
        }

        private async Task<ThemeCatalogItem?> TryLoadThemeCatalogItem(HttpClient client, string requestPath, string sourcePath, ThemeSource source)
        {
            ThemeDefinition? definition;
            try
            {
                var response = await client.GetAsync(requestPath);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                definition = ThemeSerialization.DeserializeDefinition(json);
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (JsonException)
            {
                return null;
            }

            if (definition is null)
            {
                return null;
            }

            definition = NormalizeDefinition(definition);
            return new ThemeCatalogItem(definition.Id, definition.Name, definition, source, sourcePath);
        }

        private void RebuildCatalog()
        {
            _themes.Clear();

            foreach (var theme in _localThemes)
            {
                _themes.Add(new ThemeCatalogItem(theme.Id, theme.Name, theme, ThemeSource.Local, null));
            }

            var existingIds = _themes.Select(theme => theme.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var theme in _repositoryThemes)
            {
                if (!existingIds.Add(theme.Id))
                {
                    continue;
                }

                _themes.Add(theme);
            }

            foreach (var theme in _bundledThemes)
            {
                if (!existingIds.Add(theme.Id))
                {
                    continue;
                }

                _themes.Add(theme);
            }

            _themesView = _themes.ToList();
        }

        private ThemeDefinition NormalizeDefinition(ThemeDefinition definition)
        {
            var name = string.IsNullOrWhiteSpace(definition.Name) ? TranslateApp("Untitled Theme") : definition.Name.Trim();
            var id = string.IsNullOrWhiteSpace(definition.Id) ? Guid.NewGuid().ToString("N") : definition.Id.Trim();
            var description = string.IsNullOrWhiteSpace(definition.Description) ? string.Empty : definition.Description.Trim();
            var theme = definition.Theme ?? new MudBlazor.MudTheme();

            var fontFamily = string.IsNullOrWhiteSpace(definition.FontFamily) ? "Nunito Sans" : definition.FontFamily;
            if (!_themeFontCatalog.TryGetFontUrl(fontFamily, out _))
            {
                fontFamily = "Nunito Sans";
            }

            definition.Theme = theme;
            definition.FontFamily = fontFamily;
            definition.Description = description;
            ThemeFontHelper.ApplyFont(definition, fontFamily);

            return new ThemeDefinition
            {
                Id = id,
                Name = name,
                Description = description,
                Theme = theme,
                RTL = definition.RTL,
                FontFamily = definition.FontFamily,
                DefaultBorderRadius = definition.DefaultBorderRadius,
                DefaultElevation = definition.DefaultElevation,
                AppBarElevation = definition.AppBarElevation,
                DrawerElevation = definition.DrawerElevation,
                DrawerClipMode = definition.DrawerClipMode
            };
        }

        private static bool TryCreateHttpsAbsoluteUri(string value, out Uri uri)
        {
            uri = null!;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var parsed))
            {
                return false;
            }

            if (!string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            uri = parsed;
            return true;
        }

        private static bool TryResolveRepositoryThemeUri(Uri indexUri, string path, out Uri uri)
        {
            uri = null!;

            if (Uri.TryCreate(path, UriKind.Absolute, out var absolute))
            {
                if (!string.Equals(absolute.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                uri = absolute;
                return true;
            }

            if (!Uri.TryCreate(indexUri, path, out var resolved))
            {
                return false;
            }

            if (!string.Equals(resolved.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            uri = resolved;
            return true;
        }

        private string TranslateApp(string source, params object[] arguments)
        {
            return _languageLocalizer.Translate(AppContext, source, arguments);
        }
    }
}
