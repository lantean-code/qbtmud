using Lantean.QBTMud.Models;
using Lantean.QBTMud.Theming;
using System.Text.Json;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Manages the theme catalog and the applied theme selection.
    /// </summary>
    public sealed class ThemeManagerService : IThemeManagerService
    {
        private const string LocalThemesStorageKey = "ThemeManager.LocalThemes";
        private const string SelectedThemeStorageKey = "ThemeManager.SelectedThemeId";
        private const string ThemeIndexPath = "themes/index.json";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILocalStorageService _localStorage;
        private readonly IThemeFontCatalog _themeFontCatalog;
        private readonly List<ThemeDefinition> _localThemes = [];
        private readonly List<ThemeCatalogItem> _serverThemes = [];
        private readonly List<ThemeCatalogItem> _themes = [];
        private IReadOnlyList<ThemeCatalogItem> _themesView = [];
        private bool _initialized;
        private ThemeCatalogItem? _currentTheme;
        private string? _currentThemeId;
        private string _currentFontFamily = "Nunito Sans";

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemeManagerService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory for loading server assets.</param>
        /// <param name="localStorage">The local storage service.</param>
        public ThemeManagerService(
            IHttpClientFactory httpClientFactory,
            ILocalStorageService localStorage,
            IThemeFontCatalog themeFontCatalog)
        {
            _httpClientFactory = httpClientFactory;
            _localStorage = localStorage;
            _themeFontCatalog = themeFontCatalog;
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
        /// Ensures the theme catalog has been loaded.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            await _themeFontCatalog.EnsureInitialized();

            await LoadLocalThemes();
            await LoadServerThemes();
            RebuildCatalog();
            await ApplyInitialTheme();

            _initialized = true;
        }

        /// <summary>
        /// Reloads server-provided themes and rebuilds the catalog.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ReloadServerThemes()
        {
            if (!_initialized)
            {
                await EnsureInitialized();
                return;
            }

            await LoadServerThemes();
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
            await _localStorage.SetItemAsync(SelectedThemeStorageKey, theme.Id);
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
            var storedThemeId = await _localStorage.GetItemAsync<string?>(SelectedThemeStorageKey);
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
                "Default",
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

            var themes = await _localStorage.GetItemAsync<List<ThemeDefinition>?>(LocalThemesStorageKey);
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
            await _localStorage.SetItemAsync(LocalThemesStorageKey, _localThemes);
        }

        private async Task LoadServerThemes()
        {
            _serverThemes.Clear();

            var client = _httpClientFactory.CreateClient("Assets");
            var indexResponse = await client.GetAsync(ThemeIndexPath);

            if (!indexResponse.IsSuccessStatusCode)
            {
                return;
            }

            var indexJson = await indexResponse.Content.ReadAsStringAsync();
            List<string>? index;
            try
            {
                index = JsonSerializer.Deserialize<List<string>>(indexJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
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

                ThemeDefinition? definition;
                try
                {
                    var response = await client.GetAsync(path);
                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    definition = ThemeSerialization.DeserializeDefinition(json);
                }
                catch (HttpRequestException)
                {
                    continue;
                }
                catch (JsonException)
                {
                    continue;
                }

                if (definition is null)
                {
                    continue;
                }

                definition = NormalizeDefinition(definition);
                _serverThemes.Add(new ThemeCatalogItem(definition.Id, definition.Name, definition, ThemeSource.Server, path));
            }
        }

        private void RebuildCatalog()
        {
            _themes.Clear();

            foreach (var theme in _localThemes)
            {
                _themes.Add(new ThemeCatalogItem(theme.Id, theme.Name, theme, ThemeSource.Local, null));
            }

            var localIds = _themes.Select(theme => theme.Id).ToHashSet(StringComparer.Ordinal);
            foreach (var theme in _serverThemes)
            {
                if (localIds.Contains(theme.Id))
                {
                    continue;
                }

                _themes.Add(theme);
            }

            _themesView = _themes.ToList();
        }

        private ThemeDefinition NormalizeDefinition(ThemeDefinition definition)
        {
            var name = string.IsNullOrWhiteSpace(definition.Name) ? "Untitled Theme" : definition.Name.Trim();
            var id = string.IsNullOrWhiteSpace(definition.Id) ? Guid.NewGuid().ToString("N") : definition.Id.Trim();
            var theme = definition.Theme ?? new MudBlazor.MudTheme();

            var fontFamily = string.IsNullOrWhiteSpace(definition.FontFamily) ? "Nunito Sans" : definition.FontFamily;
            if (!_themeFontCatalog.TryGetFontUrl(fontFamily, out _))
            {
                fontFamily = "Nunito Sans";
            }

            definition.Theme = theme;
            definition.FontFamily = fontFamily;
            ThemeFontHelper.ApplyFont(definition, fontFamily);

            return new ThemeDefinition
            {
                Id = id,
                Name = name,
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
    }
}
