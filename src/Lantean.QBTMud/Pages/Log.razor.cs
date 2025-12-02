using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
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
    public partial class Log : IAsyncDisposable
    {
        private const string _selectedTypesStorageKey = "Log.SelectedTypes";
        private const int MaxResults = 500;
        private readonly bool _refreshEnabled = true;

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

        protected List<QBitTorrentClient.Models.Log>? Results { get; private set; }

        protected MudSelect<string>? CategoryMudSelect { get; set; }

        protected DynamicTable<QBitTorrentClient.Models.Log>? Table { get; set; }

        protected QBitTorrentClient.Models.Log? ContextMenuItem { get; set; }

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

            if (values.Count == 1)
            {
                return values[0];
            }

            return $"{values.Count} selected";
        }

        protected Task Submit(EditContext editContext)
        {
            return DoSearch();
        }

        private async Task DoSearch()
        {
            var results = await ApiClient.GetLog(Model.Normal, Model.Info, Model.Warning, Model.Critical, Model.LastKnownId);
            if (results.Count > 0)
            {
                Results ??= [];
                Results.AddRange(results);
                Model.LastKnownId = results[^1].Id;
                TrimResults();
            }
        }

        protected static string RowClass(QBitTorrentClient.Models.Log log, int index)
        {
            return $"log-{log.Type.ToString().ToLower()}";
        }

        protected Task TableDataContextMenu(TableDataContextMenuEventArgs<QBitTorrentClient.Models.Log> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.MouseEventArgs);
        }

        protected Task TableDataLongPress(TableDataLongPressEventArgs<QBitTorrentClient.Models.Log> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.LongPressEventArgs);
        }

        private async Task ShowContextMenu(QBitTorrentClient.Models.Log? item, EventArgs eventArgs)
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
            var message = ContextMenuItem?.Message;
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            await ClipboardService.WriteToClipboard(message);
            Snackbar?.Add("Log entry copied to clipboard.", Severity.Info);
        }

        protected async Task ClearResults()
        {
            if (!HasResults)
            {
                return;
            }

            Results!.Clear();
            ContextMenuItem = null;
            Snackbar?.Add("Log view cleared.", Severity.Info);
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

        protected IEnumerable<ColumnDefinition<QBitTorrentClient.Models.Log>> Columns => ColumnsDefinitions;

        public static List<ColumnDefinition<QBitTorrentClient.Models.Log>> ColumnsDefinitions { get; } =
        [
            new ColumnDefinition<QBitTorrentClient.Models.Log>("Id", l => l.Id),
            new ColumnDefinition<QBitTorrentClient.Models.Log>("Message", l => l.Message),
            new ColumnDefinition<QBitTorrentClient.Models.Log>("Timestamp", l => l.Timestamp, l => @DisplayHelpers.DateTime(l.Timestamp)),
            new ColumnDefinition<QBitTorrentClient.Models.Log>("Log type", l => l.Type),
        ];

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
