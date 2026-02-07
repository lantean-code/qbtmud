using Lantean.QBTMud.Components;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMud.Layout
{
    public partial class MainLayout : IDisposable
    {
        private const string _isDarkModeStorageKey = "MainLayout.IsDarkMode";
        private const string _drawerOpenStorageKey = "MainLayout.DrawerOpen";
        private const string _bootstrapThemeCssLightStorageKey = "ThemeManager.BootstrapCss.Light";
        private const string _bootstrapThemeCssDarkStorageKey = "ThemeManager.BootstrapCss.Dark";
        private const string _bootstrapThemeIsDarkStorageKey = "ThemeManager.BootstrapIsDark";

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected IThemeManagerService ThemeManagerService { get; set; } = default!;

        [Inject]
        protected IThemeFontCatalog ThemeFontCatalog { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        [CascadingParameter]
        public Breakpoint CurrentBreakpoint { get; set; }

        protected bool DrawerOpen { get; set; } = true;

        protected bool ErrorDrawerOpen { get; set; } = false;

        protected bool TimerDrawerOpen { get; set; }

        protected EnhancedErrorBoundary? ErrorBoundary { get; set; }

        protected string AppBarTitle => UseShortTitle ? "qBittorrent" : BuildFullTitle();

        protected bool IsDarkMode { get; set; }

        protected MudThemeProvider MudThemeProvider { get; set; } = default!;

        private int _lastErrorCount;
        private bool _pendingThemeUpdate;
        private bool _pendingBootstrapThemeUpdate;
        private bool _pendingBootstrapThemeRemoval;
        private bool _bootstrapThemeRemoved;
        private string? _pendingFontFamily;
        private bool _themeInitialized;

        private Menu Menu { get; set; } = default!;

        protected MudTheme Theme { get; set; }

        private bool UseShortTitle => CurrentBreakpoint <= Breakpoint.Sm;

        public MainLayout()
        {
            Theme = QbtMudThemeFactory.CreateDefaultTheme();
        }

        protected string BuildPageTitle()
        {
            return BuildFullTitle();
        }

        private string BuildFullTitle()
        {
            return WebUiLocalizer.Translate("Login", "qBittorrent WebUI");
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            ThemeManagerService.ThemeChanged += OnThemeChanged;
        }

        protected override async Task OnInitializedAsync()
        {
            var isDarkMode = await LocalStorage.GetItemAsync<bool?>(_isDarkModeStorageKey);
            IsDarkMode = isDarkMode ?? true;

            await ThemeManagerService.EnsureInitialized();
            _themeInitialized = true;
        }

        protected EventCallback<bool> DrawerOpenChangedCallback => EventCallback.Factory.Create<bool>(this, SetDrawerOpenAsync);

        protected EventCallback<bool> TimerDrawerOpenChangedCallback => EventCallback.Factory.Create<bool>(this, SetTimerDrawerOpenAsync);

        protected async Task ToggleDrawer()
        {
            await SetDrawerOpenAsync(!DrawerOpen);
            await LocalStorage.SetItemAsync(_drawerOpenStorageKey, DrawerOpen);
        }

        protected override void OnParametersSet()
        {
            var currentErrorCount = ErrorBoundary?.Errors.Count ?? 0;

            if (currentErrorCount != _lastErrorCount)
            {
                if (currentErrorCount > _lastErrorCount && currentErrorCount > 0)
                {
                    ErrorDrawerOpen = true;
                    TimerDrawerOpen = false;
                }
                else if (currentErrorCount == 0)
                {
                    ErrorDrawerOpen = false;
                }

                _lastErrorCount = currentErrorCount;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var storedDrawerOpen = await LocalStorage.GetItemAsync<bool?>(_drawerOpenStorageKey);

                if (storedDrawerOpen.GetValueOrDefault())
                {
                    DrawerOpen = true;
                }
                else
                {
                    DrawerOpen = CurrentBreakpoint > Breakpoint.Sm;
                }

                await MudThemeProvider.WatchSystemDarkModeAsync(OnSystemDarkModeChanged);
                StateHasChanged();
            }

            if (_pendingThemeUpdate && _themeInitialized)
            {
                _pendingThemeUpdate = false;
                await LoadPendingFontAsync();
            }

            if (_pendingBootstrapThemeUpdate && _themeInitialized)
            {
                _pendingBootstrapThemeUpdate = false;
                await PersistBootstrapThemeAsync();
            }

            if (_pendingBootstrapThemeRemoval && _themeInitialized && !_bootstrapThemeRemoved)
            {
                _pendingBootstrapThemeRemoval = false;
                _bootstrapThemeRemoved = true;
                await JSRuntime.RemoveBootstrapTheme();
            }
        }

        protected Task OnSystemDarkModeChanged(bool value)
        {
            IsDarkMode = value;
            _pendingBootstrapThemeUpdate = true;
            StateHasChanged();
            return Task.CompletedTask;
        }

        protected void ToggleErrorDrawer()
        {
            ErrorDrawerOpen = !ErrorDrawerOpen;
            if (ErrorDrawerOpen)
            {
                TimerDrawerOpen = false;
            }
        }

        protected void Cleared()
        {
            ErrorDrawerOpen = false;
            _lastErrorCount = 0;
        }

        protected async Task DarkModeChanged(bool value)
        {
            IsDarkMode = value;
            _pendingBootstrapThemeUpdate = true;
            await LocalStorage.SetItemAsync(_isDarkModeStorageKey, value);
        }

        public void Dispose()
        {
            ThemeManagerService.ThemeChanged -= OnThemeChanged;
        }

        private void BreakpointChanged(Breakpoint value)
        {
            CurrentBreakpoint = value;

            if (value <= Breakpoint.Sm && DrawerOpen)
            {
                DrawerOpen = false;
            }

            if (ErrorDrawerOpen && (ErrorBoundary?.Errors.Count ?? 0) == 0)
            {
                ErrorDrawerOpen = false;
                StateHasChanged();
            }
            else
            {
                StateHasChanged();
            }
        }

        private Task SetDrawerOpenAsync(bool value)
        {
            if (DrawerOpen == value)
            {
                return Task.CompletedTask;
            }

            DrawerOpen = value;
            return InvokeAsync(StateHasChanged);
        }

        private Task SetTimerDrawerOpenAsync(bool value)
        {
            if (TimerDrawerOpen == value)
            {
                return Task.CompletedTask;
            }

            TimerDrawerOpen = value;
            if (TimerDrawerOpen)
            {
                ErrorDrawerOpen = false;
            }

            return InvokeAsync(StateHasChanged);
        }

        private void OnThemeChanged(object? sender, Models.ThemeChangedEventArgs args)
        {
            Theme = args.Theme;
            _pendingFontFamily = args.FontFamily;
            _pendingThemeUpdate = true;
            _pendingBootstrapThemeUpdate = true;
            _pendingBootstrapThemeRemoval = true;
            StateHasChanged();
        }

        private async Task LoadPendingFontAsync()
        {
            if (string.IsNullOrWhiteSpace(_pendingFontFamily))
            {
                return;
            }

            if (!ThemeFontCatalog.TryGetFontUrl(_pendingFontFamily, out var url))
            {
                return;
            }

            var fontId = ThemeFontCatalog.BuildFontId(_pendingFontFamily);
            await JSRuntime.LoadGoogleFont(url, fontId);
        }

        private async Task PersistBootstrapThemeAsync()
        {
            var lightCss = ThemeCssBuilder.BuildCssVariables(Theme, false);
            var darkCss = ThemeCssBuilder.BuildCssVariables(Theme, true);

            await LocalStorage.SetItemAsStringAsync(_bootstrapThemeCssLightStorageKey, lightCss);
            await LocalStorage.SetItemAsStringAsync(_bootstrapThemeCssDarkStorageKey, darkCss);
            await LocalStorage.SetItemAsync(_bootstrapThemeIsDarkStorageKey, IsDarkMode);
        }
    }
}
