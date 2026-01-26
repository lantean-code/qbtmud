using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using System.Net;

namespace Lantean.QBTMud.Components
{
    public partial class WebSeedsTab : IAsyncDisposable
    {
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private IManagedTimer? _refreshTimer;
        private bool _disposedValue;

        [Parameter]
        public bool Active { get; set; }

        [Parameter, EditorRequired]
        public string? Hash { get; set; }

        [CascadingParameter(Name = "RefreshInterval")]
        public int RefreshInterval { get; set; }

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ITorrentDataManager DataManager { get; set; } = default!;

        [Inject]
        protected IManagedTimerFactory ManagedTimerFactory { get; set; } = default!;

        protected IReadOnlyList<WebSeed>? WebSeeds { get; set; }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timerCancellationToken.Cancel();
                    _timerCancellationToken.Dispose();
                    if (_refreshTimer is not null)
                    {
                        await _refreshTimer.DisposeAsync();
                    }

                    await Task.CompletedTask;
                }

                _disposedValue = true;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _refreshTimer ??= ManagedTimerFactory.Create("WebSeedsTabRefresh", TimeSpan.FromMilliseconds(RefreshInterval));
                await _refreshTimer.StartAsync(RefreshTickAsync, _timerCancellationToken.Token);
            }
        }

        private async Task<ManagedTimerTickResult> RefreshTickAsync(CancellationToken cancellationToken)
        {
            if (Active && Hash is not null)
            {
                try
                {
                    WebSeeds = await ApiClient.GetTorrentWebSeeds(Hash);
                }
                catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden || exception.StatusCode == HttpStatusCode.NotFound)
                {
                    _timerCancellationToken.CancelIfNotDisposed();
                    return ManagedTimerTickResult.Stop;
                }

                await InvokeAsync(StateHasChanged);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ManagedTimerTickResult.Stop;
            }

            return ManagedTimerTickResult.Continue;
        }

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

            WebSeeds = await ApiClient.GetTorrentWebSeeds(Hash);

            await InvokeAsync(StateHasChanged);
        }

        protected IEnumerable<ColumnDefinition<WebSeed>> Columns => ColumnsDefinitions;

        public static List<ColumnDefinition<WebSeed>> ColumnsDefinitions { get; } =
        [
            new ColumnDefinition<WebSeed>("URL", w => w.Url, w => w.Url),
        ];
    }
}
