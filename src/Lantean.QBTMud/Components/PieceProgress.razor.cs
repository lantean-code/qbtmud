using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Interop;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;

namespace Lantean.QBTMud.Components
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

        [CascadingParameter(Name = "IsDarkMode")]
        public bool IsDarkMode { get; set; }

        [CascadingParameter]
        public MudTheme Theme { get; set; } = default!;

        public Guid Id => Guid.NewGuid();

        protected override async Task OnParametersSetAsync()
        {
            await RenderPiecesBar();
        }

        private async Task RenderPiecesBar()
        {
            string downloadingColor;
            string haveColor;
            string borderColor;
            if (IsDarkMode)
            {
                downloadingColor = Theme.PaletteDark.Success.ToString(MudBlazor.Utilities.MudColorOutputFormats.RGBA);
                haveColor = Theme.PaletteDark.Info.ToString(MudBlazor.Utilities.MudColorOutputFormats.RGBA);
                borderColor = Theme.PaletteDark.White.ToString(MudBlazor.Utilities.MudColorOutputFormats.RGBA);
            }
            else
            {
                downloadingColor = Theme.PaletteLight.Success.ToString(MudBlazor.Utilities.MudColorOutputFormats.RGBA);
                haveColor = Theme.PaletteLight.Info.ToString(MudBlazor.Utilities.MudColorOutputFormats.RGBA);
                borderColor = Theme.PaletteLight.Black.ToString(MudBlazor.Utilities.MudColorOutputFormats.RGBA);
            }
            await JSRuntime.RenderPiecesBar("progress", Hash, Pieces.Select(s => (int)s).ToArray(), downloadingColor, haveColor, borderColor);
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
            await RenderPiecesBar();
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