using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Services;

namespace Lantean.QBTMudBlade.Layout
{
    public partial class MainLayout : IBrowserViewportObserver, IAsyncDisposable
    {
        private bool _disposedValue;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private IBrowserViewportService BrowserViewportService { get; set; } = default!;

        [Inject]
        private IApiClient ApiClient { get; set; } = default!;

        protected bool DrawerOpen { get; set; } = true;

        protected bool ErrorDrawerOpen { get; set; } = false;

        protected bool ShowMenu { get; set; } = false;

        public Guid Id => Guid.NewGuid();

        protected EnhancedErrorBoundary? ErrorBoundary { get; set; }

        ResizeOptions IBrowserViewportObserver.ResizeOptions { get; } = new()
        {
            ReportRate = 50,
            NotifyOnBreakpointOnly = true
        };

        protected void ToggleDrawer()
        {
            DrawerOpen = !DrawerOpen;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (!ShowMenu)
            {
                ShowMenu = await ApiClient.CheckAuthState();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await BrowserViewportService.SubscribeAsync(this, fireImmediately: true);
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        public async Task NotifyBrowserViewportChangeAsync(BrowserViewportEventArgs browserViewportEventArgs)
        {
            if (browserViewportEventArgs.Breakpoint == Breakpoint.Sm && DrawerOpen)
            {
                DrawerOpen = false;
            }
            else if (browserViewportEventArgs.Breakpoint > Breakpoint.Sm && !DrawerOpen)
            {
                DrawerOpen = true;
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

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
