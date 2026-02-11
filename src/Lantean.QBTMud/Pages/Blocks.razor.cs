using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System.Net;

namespace Lantean.QBTMud.Pages
{
    public partial class Blocks : IAsyncDisposable
    {
        private const int MaxResults = 500;
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private IManagedTimer? _refreshTimer;
        private bool _disposedValue;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IClipboardService ClipboardService { get; set; } = default!;

        [Inject]
        protected IManagedTimerFactory ManagedTimerFactory { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected LogForm Model { get; set; } = new LogForm();

        protected List<PeerLog>? Results { get; private set; }

        protected DynamicTable<PeerLog>? Table { get; set; }

        protected PeerLog? ContextMenuItem { get; set; }

        protected MudMenu? ContextMenu { get; set; }

        protected bool HasResults => Results is not null && Results.Count > 0;

        protected override async Task OnInitializedAsync()
        {
            await DoSearch();
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected Task Submit(EditContext editContext)
        {
            return DoSearch();
        }

        private async Task DoSearch()
        {
            var results = await ApiClient.GetPeerLog(Model.LastKnownId);
            if (results.Count > 0)
            {
                Results ??= [];
                Results.AddRange(results);
                Model.LastKnownId = results[^1].Id;
                TrimResults();
            }
        }

        protected static string RowClass(PeerLog log, int index)
        {
            return $"log-{(log.Blocked ? "critical" : "normal")}";
        }

        protected Task TableDataContextMenu(TableDataContextMenuEventArgs<PeerLog> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.MouseEventArgs);
        }

        protected Task TableDataLongPress(TableDataLongPressEventArgs<PeerLog> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.LongPressEventArgs);
        }

        private async Task ShowContextMenu(PeerLog? item, EventArgs eventArgs)
        {
            ContextMenuItem = item;

            var normalizedEventArgs = eventArgs.NormalizeForContextMenu();

            await ContextMenu!.OpenMenuAsync(normalizedEventArgs);
        }

        protected async Task CopyContextMenuItem()
        {
            var address = ContextMenuItem?.IPAddress;
            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            await ClipboardService.WriteToClipboard(address);
            Snackbar?.Add("Address copied to clipboard.", Severity.Info);
        }

        protected async Task ClearResults()
        {
            if (!HasResults)
            {
                return;
            }

            Results!.Clear();
            ContextMenuItem = null;
            Snackbar?.Add("Blocked IP list cleared.", Severity.Info);
            await InvokeAsync(StateHasChanged);
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
            if (!firstRender)
            {
                return;
            }

            _refreshTimer ??= ManagedTimerFactory.Create("BlocksRefresh", TimeSpan.FromMilliseconds(1500));
            await _refreshTimer.StartAsync(RefreshTickAsync, _timerCancellationToken.Token);
        }

        private async Task<ManagedTimerTickResult> RefreshTickAsync(CancellationToken cancellationToken)
        {
            try
            {
                await DoSearch();
            }
            catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.Forbidden || exception.StatusCode == HttpStatusCode.NotFound)
            {
                _timerCancellationToken.CancelIfNotDisposed();
                return ManagedTimerTickResult.Stop;
            }

            await InvokeAsync(StateHasChanged);
            return ManagedTimerTickResult.Continue;
        }

        protected IEnumerable<ColumnDefinition<PeerLog>> Columns => BuildColumns();

        private List<ColumnDefinition<PeerLog>> BuildColumns()
        {
            return
            [
                new ColumnDefinition<PeerLog>(WebUiLocalizer.Translate("ExecutionLogWidget", "ID"), l => l.Id, id: "id"),
                new ColumnDefinition<PeerLog>(WebUiLocalizer.Translate("ExecutionLogWidget", "IP"), l => l.IPAddress, id: "message"),
                new ColumnDefinition<PeerLog>(WebUiLocalizer.Translate("ExecutionLogWidget", "Timestamp"), l => l.Timestamp, l => @DisplayHelpers.DateTime(l.Timestamp), id: "timestamp"),
                new ColumnDefinition<PeerLog>(
                    WebUiLocalizer.Translate("ExecutionLogWidget", "Blocked"),
                    l => l.Blocked
                        ? WebUiLocalizer.Translate("ExecutionLogWidget", "Blocked")
                        : WebUiLocalizer.Translate("ExecutionLogWidget", "Banned"),
                    id: "blocked"),
                new ColumnDefinition<PeerLog>(WebUiLocalizer.Translate("ExecutionLogWidget", "Reason"), l => l.Reason, id: "reason"),
            ];
        }

        private void TrimResults()
        {
            if (Results is null || Results.Count <= MaxResults)
            {
                return;
            }

            var removeCount = Results.Count - MaxResults;
            Results.RemoveRange(0, removeCount);
        }
    }
}
