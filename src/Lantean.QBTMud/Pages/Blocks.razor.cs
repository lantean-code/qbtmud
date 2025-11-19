using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using System;
using System.Net;

namespace Lantean.QBTMud.Pages
{
    public partial class Blocks : IAsyncDisposable
    {
        private readonly bool _refreshEnabled = true;
        private const string _selectedTypesStorageKey = "Blocks.SelectedTypes";

        private readonly CancellationTokenSource _timerCancellationToken = new();
        private bool _disposedValue;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected IClipboardService ClipboardService { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected LogForm Model { get; set; } = new LogForm();

        protected List<PeerLog>? Results { get; private set; }

        protected MudSelect<string>? CategoryMudSelect { get; set; }

        protected DynamicTable<PeerLog>? Table { get; set; }

        protected PeerLog? ContextMenuItem { get; set; }

        protected MudMenu? ContextMenu { get; set; }

        protected bool HasResults => Results is not null && Results.Count > 0;

        protected override async Task OnInitializedAsync()
        {
            var selectedTypes = await LocalStorage.GetItemAsync<IEnumerable<string>>(_selectedTypesStorageKey);
            if (selectedTypes is not null)
            {
                Model.SelectedTypes = selectedTypes;
            }
            else
            {
                Model.SelectedTypes = ["Normal"];
            }

            await DoSearch();
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task SelectedValuesChanged(IEnumerable<string> values)
        {
            Model.SelectedTypes = values;

            await LocalStorage.SetItemAsync(_selectedTypesStorageKey, Model.SelectedTypes);
        }

        protected static string GenerateSelectedText(List<string> values)
        {
            if (values.Count == 4)
            {
                return "All";
            }

            return $"{values.Count} selected";
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

            if (ContextMenu is null)
            {
                return;
            }

            var normalizedEventArgs = eventArgs.NormalizeForContextMenu();

            await ContextMenu.OpenMenuAsync(normalizedEventArgs);
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
                        await DoSearch();
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
