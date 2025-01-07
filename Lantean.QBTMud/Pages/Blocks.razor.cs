using Lantean.QBitTorrentClient.Models;
using Lantean.QBT.ViewModels;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Net;

namespace Lantean.QBTMud.Pages
{
    public partial class Blocks : IAsyncDisposable
    {
        private readonly bool _refreshEnabled = true;

        private readonly CancellationTokenSource _timerCancellationToken = new();
        private bool _disposedValue;

        [Inject]
        protected BlocksViewModel ViewModel { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected override void OnInitialized()
        {
            ViewModel.PropertyChanged += (_, _) => StateHasChanged();
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }

        protected static string GenerateSelectedText(List<string> values)
        {
            if (values.Count == 4)
            {
                return "All";
            }

            return $"{values.Count} selected";
        }

        protected void Submit(EditContext editContext)
        {
            ViewModel.SearchCommand.Execute(editContext);
        }

        protected static string RowClass(PeerLog log, int index)
        {
            return $"log-{(log.Blocked ? "critical" : "normal")}";
        }

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
                    await _timerCancellationToken.CancelAsync();
                    _timerCancellationToken.Dispose();

                    await Task.CompletedTask;
                }

                _disposedValue = true;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_refreshEnabled)
            {
                return;
            }

            if (!firstRender)
            {
                return;
            }

            using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1500)))
            {
                while (!_timerCancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
                {
                    try
                    {
                        await ViewModel.SearchCommand.ExecuteAsync(null);
                    }
                    catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden || exception.StatusCode == HttpStatusCode.NotFound)
                    {
                        _timerCancellationToken.CancelIfNotDisposed();
                        return;
                    }

                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        protected IEnumerable<ColumnDefinition<PeerLog>> Columns => ColumnsDefinitions;

        public static List<ColumnDefinition<PeerLog>> ColumnsDefinitions { get; } =
        [
            new ColumnDefinition<PeerLog>("Id", l => l.Id),
            new ColumnDefinition<PeerLog>("Message", l => l.IPAddress),
            new ColumnDefinition<PeerLog>("Timestamp", l => l.Timestamp, l => @DisplayHelpers.DateTime(l.Timestamp)),
            new ColumnDefinition<PeerLog>("Blocked", l => l.Blocked ? "Blocked" : "Banned"),
            new ColumnDefinition<PeerLog>("Reason", l => l.Reason),
        ];
    }
}