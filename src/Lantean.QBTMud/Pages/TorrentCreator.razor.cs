using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMud.Pages
{
    public partial class TorrentCreator : IAsyncDisposable
    {
        private const int PollIntervalMilliseconds = 1500;

        private static readonly StringComparison StatusComparison = StringComparison.OrdinalIgnoreCase;

        private readonly Dictionary<string, RenderFragment<RowContext<TorrentCreationTaskStatus>>> _columnRenderFragments = [];
        private IManagedTimer? _pollingTimer;
        private CancellationTokenSource? _pollingCancellationToken;
        private IReadOnlyList<TorrentCreationTaskStatus> _tasks = [];
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

        protected bool IsPolling
        {
            get { return _pollingTimer?.State == ManagedTimerState.Running; }
        }

        protected DynamicTable<TorrentCreationTaskStatus>? Table { get; set; }

        protected IEnumerable<TorrentCreationTaskStatus> Tasks
        {
            get { return _tasks; }
        }

        public TorrentCreator()
        {
            _columnRenderFragments.Add("Status", StatusColumn);
            _columnRenderFragments.Add("Progress", ProgressColumn);
            _columnRenderFragments.Add("Actions", ActionsColumn);
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

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task OpenCreateDialog()
        {
            var reference = await DialogService.ShowAsync<CreateTorrentDialog>("Create Torrent", DialogWorkflow.FormDialogOptions);
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
                Snackbar.Add($"Unable to create torrent: {exception.Message}", Severity.Error);
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
            get { return GetColumnDefinitions(); }
        }

        public static List<ColumnDefinition<TorrentCreationTaskStatus>> ColumnsDefinitions { get; } =
        [
            new ColumnDefinition<TorrentCreationTaskStatus>("Status", t => t.Status ?? string.Empty),
            new ColumnDefinition<TorrentCreationTaskStatus>("Progress", t => t.Progress ?? 0.0, tdClass: "table-progress"),
            new ColumnDefinition<TorrentCreationTaskStatus>("Name", t => ResolveFileName(t)),
            new ColumnDefinition<TorrentCreationTaskStatus>("Source Path", t => t.SourcePath),
            new ColumnDefinition<TorrentCreationTaskStatus>("Torrent File", t => t.TorrentFilePath ?? string.Empty) { Enabled = false },
            new ColumnDefinition<TorrentCreationTaskStatus>("Added", t => t.TimeAdded ?? string.Empty) { Enabled = false },
            new ColumnDefinition<TorrentCreationTaskStatus>("Started", t => t.TimeStarted ?? string.Empty) { Enabled = false },
            new ColumnDefinition<TorrentCreationTaskStatus>("Finished", t => t.TimeFinished ?? string.Empty) { Enabled = false },
            new ColumnDefinition<TorrentCreationTaskStatus>("Error", t => t.ErrorMessage ?? string.Empty) { Enabled = false },
            new ColumnDefinition<TorrentCreationTaskStatus>("Actions", t => t.TaskId) { Width = 140 }
        ];

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    StopPolling();
                }

                _disposedValue = true;
            }
        }

        private IEnumerable<ColumnDefinition<TorrentCreationTaskStatus>> GetColumnDefinitions()
        {
            foreach (var columnDefinition in ColumnsDefinitions)
            {
                if (_columnRenderFragments.TryGetValue(columnDefinition.Header, out var fragment))
                {
                    columnDefinition.RowTemplate = fragment;
                }

                yield return columnDefinition;
            }
        }

        private async Task RefreshTasksAsync()
        {
            if (MainData?.LostConnection == true)
            {
                Snackbar.Add("qBittorrent client is not reachable.", Severity.Warning);
                StopPolling();
                return;
            }

            _isLoading = true;
            try
            {
                _tasks = await ApiClient.GetTorrentCreationTasks();
            }
            catch (HttpRequestException exception)
            {
                Snackbar.Add($"Unable to load torrent creation tasks: {exception.Message}", Severity.Error);
            }
            finally
            {
                _isLoading = false;
            }

            EnsurePollingStarted();
            await InvokeAsync(StateHasChanged);
        }

        private void EnsurePollingStarted()
        {
            if (_tasks.Count == 0 || !_tasks.Any(task => !IsTerminalStatus(task.Status)))
            {
                StopPolling();
                return;
            }

            if (_pollingTimer is not null &&
                _pollingCancellationToken is not null &&
                !_pollingCancellationToken.IsCancellationRequested &&
                _pollingTimer.State == ManagedTimerState.Running)
            {
                return;
            }

            StopPolling();
            _pollingCancellationToken = new CancellationTokenSource();
            _pollingTimer = ManagedTimerFactory.Create("TorrentCreatorPolling", TimeSpan.FromMilliseconds(PollIntervalMilliseconds));
            _ = _pollingTimer.StartAsync(PollTasksAsync, _pollingCancellationToken.Token);
            _ = PollTasksAsync(_pollingCancellationToken.Token);
        }

        private void StopPolling()
        {
            if (_pollingCancellationToken is not null)
            {
                _pollingCancellationToken.Cancel();
                _pollingCancellationToken.Dispose();
                _pollingCancellationToken = null;
            }

            if (_pollingTimer is not null)
            {
                _ = _pollingTimer.DisposeAsync();
                _pollingTimer = null;
            }
        }

        private async Task<ManagedTimerTickResult> PollTasksAsync(CancellationToken cancellationToken)
        {
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
    }
}
