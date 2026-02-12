using Lantean.QBitTorrentClient;
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
    public partial class Log : IAsyncDisposable
    {
        private const string _selectedTypesStorageKey = "Log.SelectedTypes";
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
        protected ILocalStorageService LocalStorage { get; set; } = default!;

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

        protected string GenerateSelectedText(List<string> values)
        {
            if (values.Count == 4)
            {
                return WebUiLocalizer.Translate("ExecutionLogWidget", "All");
            }

            if (values.Count == 1)
            {
                return GetLogLevelLabel(values[0]);
            }

            return $"{values.Count} {WebUiLocalizer.Translate("ExecutionLogWidget", "items")}";
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

            var normalizedEventArgs = eventArgs.NormalizeForContextMenu();

            await ContextMenu!.OpenMenuAsync(normalizedEventArgs);
        }

        protected async Task CopyContextMenuItem()
        {
            var message = ContextMenuItem?.Message;
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            await ClipboardService.WriteToClipboard(message);
            Snackbar?.Add(TranslateLog("Log entry copied to clipboard."), Severity.Info);
        }

        protected async Task ClearResults()
        {
            if (!HasResults)
            {
                return;
            }

            Results!.Clear();
            ContextMenuItem = null;
            Snackbar?.Add(TranslateLog("Log view cleared."), Severity.Info);
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

            _refreshTimer ??= ManagedTimerFactory.Create("LogRefresh", TimeSpan.FromMilliseconds(1500));
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

        protected IEnumerable<ColumnDefinition<QBitTorrentClient.Models.Log>> Columns => BuildColumns();

        private List<ColumnDefinition<QBitTorrentClient.Models.Log>> BuildColumns()
        {
            return
            [
                new ColumnDefinition<QBitTorrentClient.Models.Log>(WebUiLocalizer.Translate("ExecutionLogWidget", "ID"), l => l.Id, id: "id"),
                new ColumnDefinition<QBitTorrentClient.Models.Log>(WebUiLocalizer.Translate("ExecutionLogWidget", "Message"), l => l.Message, id: "message"),
                new ColumnDefinition<QBitTorrentClient.Models.Log>(WebUiLocalizer.Translate("ExecutionLogWidget", "Timestamp"), l => l.Timestamp, l => @DisplayHelpers.DateTime(l.Timestamp), id: "timestamp"),
                new ColumnDefinition<QBitTorrentClient.Models.Log>(WebUiLocalizer.Translate("ExecutionLogWidget", "Log Type"), l => l.Type, id: "log_type"),
            ];
        }

        private string GetLogLevelLabel(string value)
        {
            return value switch
            {
                "Normal" => WebUiLocalizer.Translate("ExecutionLogWidget", "Normal Messages"),
                "Info" => WebUiLocalizer.Translate("ExecutionLogWidget", "Information Messages"),
                "Warning" => WebUiLocalizer.Translate("ExecutionLogWidget", "Warning Messages"),
                "Critical" => WebUiLocalizer.Translate("ExecutionLogWidget", "Critical Messages"),
                _ => value
            };
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

        private string TranslateLog(string source, params object[] arguments)
        {
            return WebUiLocalizer.Translate("AppLog", source, arguments);
        }
    }
}
