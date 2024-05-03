using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMudBlade.Interop;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Services;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components
{
    public partial class PieceProgress : IBrowserViewportObserver, IAsyncDisposable
    {
        private bool _disposedValue;

        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        private IBrowserViewportService BrowserViewportService { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public string Hash { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public IReadOnlyList<PieceState> Pieces { get; set; } = [];

        public Guid Id => Guid.NewGuid();

        protected override async Task OnParametersSetAsync()
        {
            await JSRuntime.RenderPiecesBar("progress", Hash, Pieces.Select(s => (int)s).ToArray());
        }

        ResizeOptions IBrowserViewportObserver.ResizeOptions { get; } = new()
        {
            ReportRate = 50,
            NotifyOnBreakpointOnly = false
        };

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
            await JSRuntime.RenderPiecesBar("progress", Hash, Pieces.Select(s => (int)s).ToArray());
            await InvokeAsync(StateHasChanged);
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
