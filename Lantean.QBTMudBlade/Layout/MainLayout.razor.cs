using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;

namespace Lantean.QBTMudBlade.Layout
{
    public partial class MainLayout : IBrowserViewportObserver, IAsyncDisposable
    {
        private const string _isDarkModeStorageKey = "MainLayout.IsDarkMode";
        private const string _drawerOpenStorageKey = "MainLayout.DrawerOpen";

        private bool _disposedValue;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private IBrowserViewportService BrowserViewportService { get; set; } = default!;

        [Inject]
        private IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        protected bool DrawerOpen { get; set; } = true;

        protected bool ErrorDrawerOpen { get; set; } = false;

        protected bool ShowMenu { get; set; } = false;

        public Guid Id => Guid.NewGuid();

        protected EnhancedErrorBoundary? ErrorBoundary { get; set; }

        protected bool IsDarkMode { get; set; }

        protected MudThemeProvider MudThemeProvider { get; set; } = default!;

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
            if (!ShowMenu)
            {
                ShowMenu = await ApiClient.CheckAuthState();
            }

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
                    IsDarkMode = await MudThemeProvider.GetSystemPreference();
                }
                else
                {
                    IsDarkMode = isDarkMode.Value;
                }
                await MudThemeProvider.WatchSystemPreference(OnSystemPreferenceChanged);
                await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);
                await InvokeAsync(StateHasChanged);
            }
        }

        protected Task OnSystemPreferenceChanged(bool value)
        {
            IsDarkMode = value;
            return Task.CompletedTask;
        }

        public Task NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs browserViewportEventArgs)
        {
            if (browserViewportEventArgs.Breakpoint == Breakpoint.Sm && DrawerOpen)
            {
                DrawerOpen = false;
            }
            else if (browserViewportEventArgs.Breakpoint > Breakpoint.Sm && !DrawerOpen)
            {
                DrawerOpen = true;
            }

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