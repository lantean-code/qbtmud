using Blazored.LocalStorage;
using Lantean.QBTMud.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;

namespace Lantean.QBTMud.Layout
{
    public partial class MainLayout : IBrowserViewportObserver, IAsyncDisposable
    {
        private const string _isDarkModeStorageKey = "MainLayout.IsDarkMode";
        private const string _drawerOpenStorageKey = "MainLayout.DrawerOpen";

        private bool _disposedValue;

        [Inject]
        private IBrowserViewportService BrowserViewportService { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        protected bool DrawerOpen { get; set; } = true;

        protected bool ErrorDrawerOpen { get; set; } = false;

        public Guid Id => Guid.NewGuid();

        protected EnhancedErrorBoundary? ErrorBoundary { get; set; }

        protected bool IsDarkMode { get; set; }

        protected MudThemeProvider MudThemeProvider { get; set; } = default!;

        private Menu Menu { get; set; } = default!;

        ResizeOptions IBrowserViewportObserver.ResizeOptions { get; } = new()
        {
            ReportRate = 50,
            NotifyOnBreakpointOnly = true
        };

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

        protected override async Task OnParametersSetAsync()
        {
            var drawerOpen = await LocalStorage.GetItemAsync<bool?>(_drawerOpenStorageKey);
            if (drawerOpen is not null)
            {
                DrawerOpen = drawerOpen.Value;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var isDarkMode = await LocalStorage.GetItemAsync<bool?>(_isDarkModeStorageKey);
                if (isDarkMode is null)
                {
                    IsDarkMode = true;
                }
                else
                {
                    IsDarkMode = isDarkMode.Value;
                }
                await MudThemeProvider.WatchSystemDarkModeAsync(OnSystemDarkModeChanged);
                await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);
                await InvokeAsync(StateHasChanged);
            }
        }

        protected Task OnSystemDarkModeChanged(bool value)
        {
            IsDarkMode = value;
            return Task.CompletedTask;
        }

        public async Task NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs browserViewportEventArgs)
        {
            if (browserViewportEventArgs.Breakpoint <= Breakpoint.Sm)
            {
                DrawerOpen = false;
            }
            else if (browserViewportEventArgs.Breakpoint > Breakpoint.Sm && !DrawerOpen)
            {
                DrawerOpen = true;
            }

            if (ErrorBoundary?.Errors.Count > 0)
            {
                ErrorDrawerOpen = true;
            }
            else
            {
                await Task.Delay(250);
                ErrorDrawerOpen = false;
            }

            await InvokeAsync(StateHasChanged);
        }

        protected void ToggleErrorDrawer()
        {
            ErrorDrawerOpen = !ErrorDrawerOpen;
        }

        protected void Cleared()
        {
            ErrorDrawerOpen = false;
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    await BrowserViewportService.UnsubscribeAsync(this);
                }

                _disposedValue = true;
            }
        }

        protected async Task DarkModeChanged(bool value)
        {
            IsDarkMode = value;
            await LocalStorage.SetItemAsync(_isDarkModeStorageKey, value);
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}