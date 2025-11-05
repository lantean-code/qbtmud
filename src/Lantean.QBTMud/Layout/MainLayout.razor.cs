using Blazored.LocalStorage;
using Lantean.QBTMud.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Layout
{
    public partial class MainLayout
    {
        private const string _isDarkModeStorageKey = "MainLayout.IsDarkMode";
        private const string _drawerOpenStorageKey = "MainLayout.DrawerOpen";

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [CascadingParameter]
        public Breakpoint CurrentBreakpoint { get; set; }

        protected bool DrawerOpen { get; set; } = true;

        protected bool ErrorDrawerOpen { get; set; } = false;

        protected EnhancedErrorBoundary? ErrorBoundary { get; set; }

        protected bool IsDarkMode { get; set; }

        protected MudThemeProvider MudThemeProvider { get; set; } = default!;

        private int _lastErrorCount;

        private Menu Menu { get; set; } = default!;

        protected MudTheme Theme { get; set; }

        public MainLayout()
        {
            Theme = new MudTheme();
            Theme.Typography.Default.FontFamily = ["Nunito Sans"];
        }

        protected EventCallback<bool> DrawerOpenChangedCallback => EventCallback.Factory.Create<bool>(this, SetDrawerOpenAsync);

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

                var isDarkMode = await LocalStorage.GetItemAsync<bool?>(_isDarkModeStorageKey);
                IsDarkMode = isDarkMode ?? true;

                await MudThemeProvider.WatchSystemDarkModeAsync(OnSystemDarkModeChanged);
                StateHasChanged();
            }
        }

        protected Task OnSystemDarkModeChanged(bool value)
        {
            IsDarkMode = value;
            return Task.CompletedTask;
        }

        protected void ToggleErrorDrawer()
        {
            ErrorDrawerOpen = !ErrorDrawerOpen;
        }

        protected void Cleared()
        {
            ErrorDrawerOpen = false;
            _lastErrorCount = 0;
        }

        protected async Task DarkModeChanged(bool value)
        {
            IsDarkMode = value;
            await LocalStorage.SetItemAsync(_isDarkModeStorageKey, value);
        }

        private void BreakpointChanged(Breakpoint value)
        {
            if (value <= Breakpoint.Sm && DrawerOpen)
            {
                _ = SetDrawerOpenAsync(false);
            }

            if (ErrorDrawerOpen && (ErrorBoundary?.Errors.Count ?? 0) == 0)
            {
                ErrorDrawerOpen = false;
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
    }
}
