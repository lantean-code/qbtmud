using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMud.Pages
{
    public partial class TorrentCreator : IAsyncDisposable
    {
        private const int PollIntervalMilliseconds = 1500;

        private static readonly StringComparison StatusComparison = StringComparison.OrdinalIgnoreCase;

        private readonly CancellationTokenSource _timerCancellationToken = new();
        private IManagedTimer? _refreshTimer;
        private IReadOnlyList<TorrentCreationTaskStatus> _tasks = [];
        private IReadOnlyList<ColumnDefinition<TorrentCreationTaskStatus>>? _columnsDefinitions;
        private bool _disposedValue;
        private bool _isLoading;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected IManagedTimerFactory ManagedTimerFactory { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter]
        public Lantean.QBTMud.Models.MainData? MainData { get; set; }

        protected bool HasTasks
        {
            get { return _tasks.Count > 0; }
        }

        protected bool IsLoading
        {
            get { return _isLoading; }
        }

        protected DynamicTable<TorrentCreationTaskStatus>? Table { get; set; }

        protected IEnumerable<TorrentCreationTaskStatus> Tasks
        {
            get { return _tasks; }
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected override async Task OnInitializedAsync()
        {
            await RefreshTasksAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            _refreshTimer ??= ManagedTimerFactory.Create("TorrentCreatorPolling", TimeSpan.FromMilliseconds(PollIntervalMilliseconds));
            await _refreshTimer.StartAsync(PollTasksAsync, _timerCancellationToken.Token);
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task OpenCreateDialog()
        {
            var reference = await DialogService.ShowAsync<CreateTorrentDialog>(
                WebUiLocalizer.Translate("TorrentCreator", "Create New Torrent"),
                DialogWorkflow.FormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var request = (TorrentCreationTaskRequest)dialogResult.Data;
            try
            {
                await ApiClient.AddTorrentCreationTask(request);
            }
            catch (HttpRequestException exception)
            {
                Snackbar.Add($"{WebUiLocalizer.Translate("TorrentCreator", "Unable to create torrent.")} {exception.Message}", Severity.Error);
                return;
            }

            await RefreshTasksAsync();
        }

        protected async Task RefreshTasks()
        {
            await RefreshTasksAsync();
        }

        protected async Task DownloadTask(TorrentCreationTaskStatus task)
        {
            if (!CanDownload(task))
            {
                return;
            }

            var fileName = ResolveFileName(task);
            var url = BuildDownloadUrl(task.TaskId);
            await JSRuntime.FileDownload(url, fileName);
        }

        protected async Task DeleteTask(TorrentCreationTaskStatus task)
        {
            try
            {
                await ApiClient.DeleteTorrentCreationTask(task.TaskId);
            }
            catch (HttpRequestException exception)
            {
                Snackbar.Add($"Unable to delete task: {exception.Message}", Severity.Error);
                return;
            }

            await RefreshTasksAsync();
        }

        protected IEnumerable<ColumnDefinition<TorrentCreationTaskStatus>> Columns
        {
            get { return _columnsDefinitions ??= BuildColumnsDefinitions(); }
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
                        _refreshTimer = null;
                    }
                }

                _disposedValue = true;
            }
        }

        private async Task RefreshTasksAsync()
        {
            if (MainData?.LostConnection == true)
            {
                Snackbar.Add("qBittorrent client is not reachable.", Severity.Warning);
                _timerCancellationToken.CancelIfNotDisposed();
                return;
            }

            _isLoading = true;
            try
            {
                _tasks = await ApiClient.GetTorrentCreationTasks() ?? [];
            }
            catch (HttpRequestException exception)
            {
                Snackbar.Add($"{WebUiLocalizer.Translate("TorrentCreator", "Unable to load torrent creation tasks")}: {exception.Message}", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }

            await InvokeAsync(StateHasChanged);
        }

        private bool ShouldPollTasks()
        {
            return _tasks.Count > 0 && _tasks.Any(task => !IsTerminalStatus(task.Status));
        }

        private async Task<ManagedTimerTickResult> PollTasksAsync(CancellationToken cancellationToken)
        {
            if (!ShouldPollTasks())
            {
                return ManagedTimerTickResult.Continue;
            }

            try
            {
                await InvokeAsync(RefreshTasksAsync);
            }
            catch (OperationCanceledException)
            {
                return ManagedTimerTickResult.Stop;
            }
            catch (Exception exception)
            {
                Snackbar.Add($"Unable to refresh torrent creation tasks: {exception.Message}", Severity.Error);
                return ManagedTimerTickResult.Stop;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return ManagedTimerTickResult.Stop;
            }

            return ManagedTimerTickResult.Continue;
        }

        private static bool IsTerminalStatus(string? status)
        {
            return string.Equals(status, "Finished", StatusComparison) ||
                string.Equals(status, "Failed", StatusComparison);
        }

        private string GetStatusDisplayText(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return string.Empty;
            }

            return status switch
            {
                "Running" => WebUiLocalizer.Translate("TorrentCreator", "Running"),
                "Finished" => WebUiLocalizer.Translate("TorrentCreator", "Finished"),
                "Failed" => WebUiLocalizer.Translate("TorrentCreator", "Failed"),
                "Queued" => WebUiLocalizer.Translate("TorrentCreator", "Queued"),
                _ => status
            };
        }

        private static bool CanDownload(TorrentCreationTaskStatus task)
        {
            if (!string.Equals(task.Status, "Finished", StatusComparison))
            {
                return false;
            }

            return true;
        }

        private static double GetProgress(TorrentCreationTaskStatus task)
        {
            if (task.Progress.HasValue)
            {
                return Math.Clamp(task.Progress.Value, 0, 100);
            }

            if (string.Equals(task.Status, "Finished", StatusComparison))
            {
                return 100;
            }

            return 0;
        }

        private static Color GetStatusColor(string? status)
        {
            if (string.Equals(status, "Running", StatusComparison))
            {
                return Color.Info;
            }

            if (string.Equals(status, "Finished", StatusComparison))
            {
                return Color.Success;
            }

            if (string.Equals(status, "Failed", StatusComparison))
            {
                return Color.Error;
            }

            return Color.Default;
        }

        private static string BuildDownloadUrl(string taskId)
        {
            var escaped = Uri.EscapeDataString(taskId);
            return $"api/v2/torrentcreator/torrentFile?taskID={escaped}";
        }

        private static string ResolveFileName(TorrentCreationTaskStatus task)
        {
            var fileName = GetFileNameFromPath(task.SourcePath) ?? GetFileNameFromPath(task.TorrentFilePath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return $"{task.TaskId}.torrent";
            }

            return fileName.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase)
                ? fileName
                : $"{fileName}.torrent";
        }

        private static string? GetFileNameFromPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var trimmed = path.TrimEnd('/', '\\');
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return null;
            }

            var separatorIndex = trimmed.LastIndexOfAny(['/', '\\']);
            var fileName = separatorIndex < 0
                ? trimmed
                : trimmed[(separatorIndex + 1)..];

            if (fileName.EndsWith(":", StringComparison.Ordinal))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(fileName) ? null : fileName;
        }

        private IReadOnlyList<ColumnDefinition<TorrentCreationTaskStatus>> BuildColumnsDefinitions()
        {
            var statusLabel = WebUiLocalizer.Translate("TorrentCreator", "Status");
            var progressLabel = WebUiLocalizer.Translate("TorrentCreator", "Progress");
            var nameLabel = WebUiLocalizer.Translate("TransferListModel", "Name");
            var sourcePathLabel = WebUiLocalizer.Translate("TorrentCreator", "Source Path");
            var addedOnLabel = WebUiLocalizer.Translate("TorrentCreator", "Added On");
            var startedOnLabel = WebUiLocalizer.Translate("TorrentCreator", "Started On");
            var completedOnLabel = WebUiLocalizer.Translate("TorrentCreator", "Completed On");
            var errorLabel = WebUiLocalizer.Translate("TorrentCreator", "Error Message");

            return
            [
                new ColumnDefinition<TorrentCreationTaskStatus>(statusLabel, t => t.Status ?? string.Empty, StatusColumn),
                new ColumnDefinition<TorrentCreationTaskStatus>(progressLabel, t => t.Progress ?? 0.0, ProgressColumn, tdClass: "table-progress"),
                new ColumnDefinition<TorrentCreationTaskStatus>(nameLabel, t => ResolveFileName(t)),
                new ColumnDefinition<TorrentCreationTaskStatus>(sourcePathLabel, t => t.SourcePath),
                new ColumnDefinition<TorrentCreationTaskStatus>("Torrent File", t => t.TorrentFilePath ?? string.Empty) { Enabled = false },
                new ColumnDefinition<TorrentCreationTaskStatus>(addedOnLabel, t => t.TimeAdded ?? string.Empty) { Enabled = false },
                new ColumnDefinition<TorrentCreationTaskStatus>(startedOnLabel, t => t.TimeStarted ?? string.Empty) { Enabled = false },
                new ColumnDefinition<TorrentCreationTaskStatus>(completedOnLabel, t => t.TimeFinished ?? string.Empty) { Enabled = false },
                new ColumnDefinition<TorrentCreationTaskStatus>(errorLabel, t => t.ErrorMessage ?? string.Empty) { Enabled = false },
                new ColumnDefinition<TorrentCreationTaskStatus>("Actions", t => t.TaskId, ActionsColumn) { Width = 140 }
            ];
        }
    }
}
