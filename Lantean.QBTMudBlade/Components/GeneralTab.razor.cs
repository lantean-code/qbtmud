﻿using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components;
using System.Net;

namespace Lantean.QBTMudBlade.Components
{
    public partial class GeneralTab : IAsyncDisposable
    {
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private bool _disposedValue;

        [Parameter, EditorRequired]
        public string? Hash { get; set; }

        [Parameter]
        public bool Active { get; set; }

        [CascadingParameter]
        public int RefreshInterval { get; set; }

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDataManager DataManager { get; set; } = default!;

        protected IReadOnlyList<PieceState> Pieces { get; set; } = [];

        protected TorrentProperties Properties { get; set; } = default!;

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

            Pieces = await ApiClient.GetTorrentPieceStates(Hash);
            Properties = await ApiClient.GetTorrentProperties(Hash);

            await InvokeAsync(StateHasChanged);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
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
                                Pieces = await ApiClient.GetTorrentPieceStates(Hash);
                                Properties = await ApiClient.GetTorrentProperties(Hash);
                            }
                            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden)
                            {
                                _timerCancellationToken.CancelIfNotDisposed();
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
                    await Task.Delay(0);
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