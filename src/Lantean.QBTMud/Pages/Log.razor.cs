using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using QBittorrent.ApiClient;
using ClientLog = QBittorrent.ApiClient.Models.Log;

namespace Lantean.QBTMud.Pages
{
    public partial class Log : IAsyncDisposable
    {
        private const string _selectedTypesStorageKey = "Log.SelectedTypes";
        private const int _maxResults = 500;
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
        protected ISettingsStorageService SettingsStorage { get; set; } = default!;

        [Inject]
        protected IClipboardService ClipboardService { get; set; } = default!;

        [Inject]
        protected IManagedTimerFactory ManagedTimerFactory { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Inject]
        protected IApiFeedbackWorkflow ApiFeedbackWorkflow { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected LogForm Model { get; set; } = new LogForm();

        protected List<ClientLog>? Results { get; private set; }

        protected MudSelect<string>? CategoryMudSelect { get; set; }

        protected DynamicTable<ClientLog>? Table { get; set; }

        protected ClientLog? ContextMenuItem { get; set; }

        protected MudMenu? ContextMenu { get; set; }

        protected bool HasResults => Results is not null && Results.Count > 0;

        protected override async Task OnInitializedAsync()
        {
            var selectedTypes = await SettingsStorage.GetItemAsync<IReadOnlyCollection<string>>(_selectedTypesStorageKey);
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

        protected async Task SelectedValuesChanged(IReadOnlyCollection<string> values)
        {
            Model.SelectedTypes = values;

            await SettingsStorage.SetItemAsync(_selectedTypesStorageKey, Model.SelectedTypes);
        }

        protected string GenerateSelectedText(IReadOnlyList<string> values)
        {
            if (values.Count == 4)
            {
                return LanguageLocalizer.Translate("ExecutionLogWidget", "All");
            }

            if (values.Count == 1)
            {
                return GetLogLevelLabel(values[0]);
            }

            return $"{values.Count} {LanguageLocalizer.Translate("ExecutionLogWidget", "items")}";
        }

        protected Task Submit(EditContext editContext)
        {
            return DoSearch();
        }

        private async Task DoSearch()
        {
            var results = await ApiClient.GetLogAsync(Model.Normal, Model.Info, Model.Warning, Model.Critical, Model.LastKnownId);
            if (results.IsFailure)
            {
                await ApiFeedbackWorkflow.HandleFailureAsync(results);
                return;
            }

            var logEntries = results.Value;
            if (logEntries.Count > 0)
            {
                Results ??= [];
                Results.AddRange(logEntries);
                Model.LastKnownId = logEntries[^1].Id;
                TrimResults();
            }
        }

        protected static string RowClass(ClientLog log, int index)
        {
            return $"log-{log.Type.ToString().ToLower()}";
        }

        protected Task TableDataContextMenu(TableDataContextMenuEventArgs<ClientLog> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.MouseEventArgs);
        }

        protected Task TableDataLongPress(TableDataLongPressEventArgs<ClientLog> eventArgs)
        {
            return ShowContextMenu(eventArgs.Item, eventArgs.LongPressEventArgs);
        }

        private async Task ShowContextMenu(ClientLog? item, EventArgs eventArgs)
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
            SnackbarWorkflow.ShowTransientMessage(TranslateLog("Log entry copied to clipboard."), Severity.Info);
        }

        protected async Task ClearResults()
        {
            if (!HasResults)
            {
                return;
            }

            Results!.Clear();
            ContextMenuItem = null;
            SnackbarWorkflow.ShowTransientMessage(TranslateLog("Log view cleared."), Severity.Info);
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
            var results = await ApiClient.GetLogAsync(Model.Normal, Model.Info, Model.Warning, Model.Critical, Model.LastKnownId);
            if (results.IsFailure)
            {
                await ApiFeedbackWorkflow.HandleFailureAsync(
                    results,
                    failure =>
                    {
                        if (failure.Kind == ApiFailureKind.AuthenticationRequired)
                        {
                            _timerCancellationToken.CancelIfNotDisposed();
                        }

                        return ApiFeedbackCustomFailureResult.ContinueWithWorkflow;
                    },
                    cancellationToken: cancellationToken);

                return results.Failure?.Kind == ApiFailureKind.AuthenticationRequired
                    ? ManagedTimerTickResult.Stop
                    : ManagedTimerTickResult.Continue;
            }

            var logEntries = results.Value;
            if (logEntries.Count > 0)
            {
                Results ??= [];
                Results.AddRange(logEntries);
                Model.LastKnownId = logEntries[^1].Id;
                TrimResults();
            }

            await InvokeAsync(StateHasChanged);
            return ManagedTimerTickResult.Continue;
        }

        protected IEnumerable<ColumnDefinition<ClientLog>> Columns => BuildColumns();

        private List<ColumnDefinition<ClientLog>> BuildColumns()
        {
            return
            [
                new ColumnDefinition<ClientLog>(LanguageLocalizer.Translate("ExecutionLogWidget", "ID"), l => l.Id, id: "id"),
                new ColumnDefinition<ClientLog>(LanguageLocalizer.Translate("ExecutionLogWidget", "Message"), l => l.Message, id: "message"),
                new ColumnDefinition<ClientLog>(LanguageLocalizer.Translate("ExecutionLogWidget", "Timestamp"), l => l.Timestamp, l => @DisplayHelpers.DateTime(l.Timestamp), id: "timestamp"),
                new ColumnDefinition<ClientLog>(LanguageLocalizer.Translate("ExecutionLogWidget", "Log Type"), l => l.Type, id: "log_type"),
            ];
        }

        private string GetLogLevelLabel(string value)
        {
            return value switch
            {
                "Normal" => LanguageLocalizer.Translate("ExecutionLogWidget", "Normal Messages"),
                "Info" => LanguageLocalizer.Translate("ExecutionLogWidget", "Information Messages"),
                "Warning" => LanguageLocalizer.Translate("ExecutionLogWidget", "Warning Messages"),
                "Critical" => LanguageLocalizer.Translate("ExecutionLogWidget", "Critical Messages"),
                _ => value
            };
        }

        private void TrimResults()
        {
            if (Results is null || Results.Count <= _maxResults)
            {
                return;
            }

            var removeCount = Results.Count - _maxResults;
            Results.RemoveRange(0, removeCount);
        }

        private string TranslateLog(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppLog", source, arguments);
        }
    }
}
