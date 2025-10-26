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

        private Menu Menu { get; set; } = default!;

        protected MudTheme Theme { get; set; }

        public MainLayout()
        {
            Theme = new MudTheme();
            Theme.Typography.Default.FontFamily = ["Nunito Sans"];
        }

        protected async Task ToggleDrawer()
        {
            DrawerOpen = !DrawerOpen;
            await LocalStorage.SetItemAsync(_drawerOpenStorageKey, DrawerOpen);
        }

        protected override void OnParametersSet()
        {
            if (ErrorBoundary?.Errors.Count > 0)
            {
                ErrorDrawerOpen = true;
            }
            else
            {
                ErrorDrawerOpen = false;
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
                DrawerOpen = false;
                StateHasChanged();
            }
        }
    }
}