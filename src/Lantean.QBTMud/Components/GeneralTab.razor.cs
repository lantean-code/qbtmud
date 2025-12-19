using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using System.Net;

namespace Lantean.QBTMud.Components
{
    public partial class GeneralTab : IAsyncDisposable
    {
        private readonly bool _refreshEnabled = true;

        private readonly CancellationTokenSource _timerCancellationToken = new();
        private bool _disposedValue;
        private bool _piecesLoaded;
        private bool _piecesLoading = true;
        private bool _piecesFailed;

        [Parameter, EditorRequired]
        public string? Hash { get; set; }

        [Parameter]
        public bool Active { get; set; }

        [CascadingParameter(Name = "RefreshInterval")]
        public int RefreshInterval { get; set; }

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ITorrentDataManager DataManager { get; set; } = default!;

        protected IReadOnlyList<PieceState> Pieces { get; set; } = [];

        protected TorrentProperties Properties { get; set; } = default!;

        protected bool PiecesLoading => _piecesLoading;

        protected bool PiecesFailed => _piecesFailed;

        protected override async Task OnParametersSetAsync()
        {
            if (Hash is null)
            {
                return;
            }

            if (!Active)
            {
                return;
            }

            if (!_piecesLoaded)
            {
                _piecesLoading = true;
                _piecesFailed = false;
            }

            try
            {
                Properties = await ApiClient.GetTorrentProperties(Hash);
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                MarkPiecesFailed();
                _timerCancellationToken.CancelIfNotDisposed();
                await InvokeAsync(StateHasChanged);
                return;
            }
            catch (HttpRequestException)
            {
                MarkPiecesFailed();
                await InvokeAsync(StateHasChanged);
                return;
            }

            try
            {
                Pieces = await ApiClient.GetTorrentPieceStates(Hash);
                MarkPiecesLoaded();
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
            {
                MarkPiecesFailed();
            }

            await InvokeAsync(StateHasChanged);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_refreshEnabled)
            {
                return;
            }

            if (firstRender)
            {
                using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(RefreshInterval)))
                {
                    while (!_timerCancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
                    {
                        if (Active && Hash is not null)
                        {
                            try
                            {
                                Properties = await ApiClient.GetTorrentProperties(Hash);
                            }
                            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                            {
                                MarkPiecesFailed();
                                _timerCancellationToken.CancelIfNotDisposed();
                                await InvokeAsync(StateHasChanged);
                                return;
                            }
                            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden)
                            {
                                MarkPiecesFailed();
                                _timerCancellationToken.CancelIfNotDisposed();
                                await InvokeAsync(StateHasChanged);
                                return;
                            }

                            try
                            {
                                Pieces = await ApiClient.GetTorrentPieceStates(Hash);
                                MarkPiecesLoaded();
                            }
                            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                            {
                                MarkPiecesFailed();
                                await InvokeAsync(StateHasChanged);
                                return;
                            }
                        }

                        await InvokeAsync(StateHasChanged);
                    }
                }
            }
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timerCancellationToken.Cancel();
                    _timerCancellationToken.Dispose();

                    await Task.CompletedTask;
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

        private void MarkPiecesLoaded()
        {
            _piecesLoaded = true;
            _piecesLoading = false;
            _piecesFailed = false;
        }

        private void MarkPiecesFailed()
        {
            _piecesLoaded = true;
            _piecesLoading = false;
            _piecesFailed = true;
            Pieces = [];
        }
    }
}
