using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Lantean.QBTMud.Layout
{
    public partial class LoggedInLayout : IDisposable
    {
        private readonly bool _refreshEnabled = true;
        private const string PendingDownloadStorageKey = "LoggedInLayout.PendingDownload";

        private int _requestId = 0;
        private bool _disposedValue;
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private CancellationTokenSource? _refreshDelayCancellation;
        private int _refreshInterval = 1500;
        private bool _toggleAltSpeedLimitsInProgress;
        private Task? _refreshLoopTask;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ITorrentDataManager DataManager { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter]
        public Menu? Menu { get; set; }

        protected MainData? MainData { get; set; }

        protected string Category { get; set; } = FilterHelper.CATEGORY_ALL;

        protected string Tag { get; set; } = FilterHelper.TAG_ALL;

        protected string Tracker { get; set; } = FilterHelper.TRACKER_ALL;

        protected Status Status { get; set; } = Status.All;

        protected QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        protected string? SortColumn { get; set; }

        protected SortDirection SortDirection { get; set; }

        protected string Version { get; set; } = "";

        protected string? SearchText { get; set; }

        protected TorrentFilterField SearchField { get; set; } = TorrentFilterField.Name;

        protected bool UseRegexSearch { get; set; }

        protected bool IsRegexValid { get; set; } = true;

        protected IReadOnlyList<Torrent> Torrents => GetTorrents();

        protected bool IsAuthenticated { get; set; }

        protected bool LostConnection { get; set; }

        private IReadOnlyList<Torrent> _visibleTorrents = Array.Empty<Torrent>();

        private bool _torrentsDirty = true;
        private int _torrentsVersion;
        private string? _lastProcessedDownloadToken;
        private string? _pendingDownloadLink;
        private bool _navigationHandlerAttached;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (!_navigationHandlerAttached)
            {
                NavigationManager.LocationChanged += NavigationManagerOnLocationChanged;
                _navigationHandlerAttached = true;
            }
        }

        private IReadOnlyList<Torrent> GetTorrents()
        {
            if (!_torrentsDirty)
            {
                return _visibleTorrents;
            }

            if (MainData is null)
            {
                _visibleTorrents = Array.Empty<Torrent>();
                _torrentsDirty = false;
                return _visibleTorrents;
            }

            var filterState = new FilterState(
                Category,
                Status,
                Tag,
                Tracker,
                MainData.ServerState.UseSubcategories,
                SearchText,
                SearchField,
                UseRegexSearch,
                IsRegexValid);
            _visibleTorrents = MainData.Torrents.Values.Filter(filterState).ToList();
            _torrentsDirty = false;

            return _visibleTorrents;
        }

        protected override async Task OnInitializedAsync()
        {
            await RestorePendingDownloadAsync();
            CaptureDownloadFromUri(NavigationManager.Uri);
            await PersistPendingDownloadAsync();

            if (!await ApiClient.CheckAuthState())
            {
                await PersistPendingDownloadAsync();
                NavigationManager.NavigateTo("/login");
                return;
            }

            await InvokeAsync(StateHasChanged);

            Preferences = await ApiClient.GetApplicationPreferences();
            Version = await ApiClient.GetApplicationVersion();
            var data = await ApiClient.GetMainData(_requestId);
            MainData = DataManager.CreateMainData(data);
            MarkTorrentsDirty();

            _requestId = data.ResponseId;
            _refreshInterval = MainData.ServerState.RefreshInterval;

            IsAuthenticated = true;

            Menu?.ShowMenu(Preferences);

            await TryProcessPendingDownloadAsync();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (!_refreshEnabled)
            {
                return;
            }

            if (firstRender && _refreshLoopTask is null)
            {
                StartRefreshLoop();
            }
        }

        private async Task RefreshLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!await WaitForNextIntervalAsync(cancellationToken))
                {
                    return;
                }

                if (!IsAuthenticated)
                {
                    return;
                }

                QBitTorrentClient.Models.MainData data;
                try
                {
                    data = await ApiClient.GetMainData(_requestId);
                }
                catch (HttpRequestException)
                {
                    if (MainData is not null)
                    {
                        MainData.LostConnection = true;
                    }
                    _timerCancellationToken.CancelIfNotDisposed();
                    await InvokeAsync(StateHasChanged);
                    return;
                }

                var shouldRender = false;

                if (MainData is null || data.FullUpdate)
                {
                    MainData = DataManager.CreateMainData(data);
                    MarkTorrentsDirty();
                    shouldRender = true;
                }
                else
                {
                    var dataChanged = DataManager.MergeMainData(data, MainData, out var filterChanged);
                    if (filterChanged)
                    {
                        MarkTorrentsDirty();
                    }
                    else if (dataChanged)
                    {
                        IncrementTorrentsVersion();
                    }
                    shouldRender = dataChanged;
                }

                if (MainData is not null)
                {
                    var newInterval = MainData.ServerState.RefreshInterval;
                    UpdateRefreshInterval(newInterval);
                }

                _requestId = data.ResponseId;

                if (shouldRender)
                {
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private async Task<bool> WaitForNextIntervalAsync(CancellationToken cancellationToken)
        {
            var delayCancellation = new CancellationTokenSource();
            _refreshDelayCancellation = delayCancellation;

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, delayCancellation.Token);
            try
            {
                await Task.Delay(_refreshInterval, linkedCancellation.Token);
                return !cancellationToken.IsCancellationRequested;
            }
            catch (TaskCanceledException)
            {
                return !cancellationToken.IsCancellationRequested;
            }
            finally
            {
                if (ReferenceEquals(_refreshDelayCancellation, delayCancellation))
                {
                    _refreshDelayCancellation = null;
                }

                delayCancellation.Dispose();
            }
        }

        private void UpdateRefreshInterval(int newInterval)
        {
            if (newInterval <= 0 || newInterval == _refreshInterval)
            {
                return;
            }

            _refreshInterval = newInterval;
            CancelRefreshDelay();
        }

        private void CancelRefreshDelay()
        {
            var delayCancellation = _refreshDelayCancellation;
            if (delayCancellation is null)
            {
                return;
            }

            try
            {
                delayCancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed.
            }
        }

        private void StartRefreshLoop()
        {
            var refreshLoop = RefreshLoopAsync(_timerCancellationToken.Token);
            _refreshLoopTask = refreshLoop;
            _ = refreshLoop.ContinueWith(
                t =>
                {
                    // Observe exceptions to prevent UnobservedTaskException.
                    _ = t.Exception;
                },
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        private void NavigationManagerOnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            CaptureDownloadFromUri(e.Location);
            _ = PersistPendingDownloadAsync();

            if (IsAuthenticated)
            {
                _ = TryProcessPendingDownloadAsync();
            }
        }

        private void CaptureDownloadFromUri(string? uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return;
            }

            var downloadValue = ExtractDownloadParameter(uri);
            if (string.IsNullOrWhiteSpace(downloadValue))
            {
                return;
            }

            var decoded = Uri.UnescapeDataString(downloadValue);

            if (string.Equals(_lastProcessedDownloadToken, decoded, StringComparison.Ordinal))
            {
                return;
            }

            _pendingDownloadLink = decoded;
        }

        private static string? ExtractDownloadParameter(string uri)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var absoluteUri))
            {
                return null;
            }

            var fragmentValue = ExtractDownloadParameterFromComponent(absoluteUri.Fragment);
            if (!string.IsNullOrWhiteSpace(fragmentValue))
            {
                return fragmentValue;
            }

            var queryValue = ExtractDownloadParameterFromComponent(absoluteUri.Query);
            if (!string.IsNullOrWhiteSpace(queryValue))
            {
                return queryValue;
            }

            return null;
        }

        private static string? ExtractDownloadParameterFromComponent(string component)
        {
            if (string.IsNullOrEmpty(component))
            {
                return null;
            }

            var trimmed = component.StartsWith("#", StringComparison.Ordinal) || component.StartsWith("?", StringComparison.Ordinal)
                ? component[1..]
                : component;

            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            var segments = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                var separatorIndex = segment.IndexOf('=');
                string key;
                string value;
                if (separatorIndex >= 0)
                {
                    key = segment[..separatorIndex];
                    value = separatorIndex < segment.Length - 1 ? segment[(separatorIndex + 1)..] : string.Empty;
                }
                else
                {
                    key = segment;
                    value = string.Empty;
                }

                if (string.Equals(key, "download", StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
            }

            return null;
        }

        private async Task RestorePendingDownloadAsync()
        {
            if (_pendingDownloadLink is not null)
            {
                return;
            }

            if (LocalStorage is null)
            {
                return;
            }

            var stored = await LocalStorage.GetItemAsync<string>(PendingDownloadStorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return;
            }

            _pendingDownloadLink = stored;
        }

        private async Task PersistPendingDownloadAsync()
        {
            if (LocalStorage is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_pendingDownloadLink))
            {
                await LocalStorage.RemoveItemAsync(PendingDownloadStorageKey);
                return;
            }

            await LocalStorage.SetItemAsync(PendingDownloadStorageKey, _pendingDownloadLink);
        }

        private async Task TryProcessPendingDownloadAsync()
        {
            if (!IsAuthenticated || string.IsNullOrWhiteSpace(_pendingDownloadLink))
            {
                return;
            }

            var magnet = _pendingDownloadLink;

            if (string.Equals(_lastProcessedDownloadToken, magnet, StringComparison.Ordinal))
            {
                await ClearPendingDownloadAsync();
                NavigationManager.NavigateTo("/", true);
                return;
            }

            try
            {
                await InvokeAsync(() => DialogWorkflow.InvokeAddTorrentLinkDialog(magnet));
                _lastProcessedDownloadToken = magnet;
                await ClearPendingDownloadAsync();
                NavigationManager.NavigateTo("/", true);
            }
            catch
            {
                _pendingDownloadLink = magnet;
                await PersistPendingDownloadAsync();
                throw;
            }
        }

        private async Task ClearPendingDownloadAsync()
        {
            _pendingDownloadLink = null;
            if (LocalStorage is not null)
            {
                await LocalStorage.RemoveItemAsync(PendingDownloadStorageKey);
            }
        }

        protected EventCallback<string> CategoryChanged => EventCallback.Factory.Create<string>(this, OnCategoryChanged);

        protected EventCallback<Status> StatusChanged => EventCallback.Factory.Create<Status>(this, OnStatusChanged);

        protected EventCallback<string> TagChanged => EventCallback.Factory.Create<string>(this, OnTagChanged);

        protected EventCallback<string> TrackerChanged => EventCallback.Factory.Create<string>(this, OnTrackerChanged);

        protected EventCallback<FilterSearchState> SearchTermChanged => EventCallback.Factory.Create<FilterSearchState>(this, OnSearchTermChanged);

        protected EventCallback<string> SortColumnChanged => EventCallback.Factory.Create<string>(this, columnId => SortColumn = columnId);

        protected EventCallback<SortDirection> SortDirectionChanged => EventCallback.Factory.Create<SortDirection>(this, sortDirection => SortDirection = sortDirection);

        protected static (string, Color) GetConnectionIcon(string? status)
        {
            return status switch
            {
                "firewalled" => (Icons.Material.Outlined.SignalWifiStatusbarConnectedNoInternet4, Color.Warning),
                "connected" => (Icons.Material.Outlined.SignalWifi4Bar, Color.Success),
                _ => (Icons.Material.Outlined.SignalWifiOff, Color.Error),
            };
        }

        private static string? BuildExternalIpLabel(ServerState? serverState)
        {
            if (serverState is null)
            {
                return null;
            }

            var v4 = serverState.LastExternalAddressV4;
            var v6 = serverState.LastExternalAddressV6;
            var hasV4 = !string.IsNullOrWhiteSpace(v4);
            var hasV6 = !string.IsNullOrWhiteSpace(v6);

            if (!hasV4 && !hasV6)
            {
                return "External IP: N/A";
            }

            if (hasV4 && hasV6)
            {
                return $"External IPs: {v4}, {v6}";
            }

            var address = hasV4 ? v4 : v6;
            return $"External IP: {address}";
        }

        private void OnCategoryChanged(string category)
        {
            if (Category == category)
            {
                return;
            }

            Category = category;
            MarkTorrentsDirty();
        }

        private void OnStatusChanged(Status status)
        {
            if (Status == status)
            {
                return;
            }

            Status = status;
            MarkTorrentsDirty();
        }

        private void OnTagChanged(string tag)
        {
            if (Tag == tag)
            {
                return;
            }

            Tag = tag;
            MarkTorrentsDirty();
        }

        private void OnTrackerChanged(string tracker)
        {
            if (Tracker == tracker)
            {
                return;
            }

            Tracker = tracker;
            MarkTorrentsDirty();
        }

        private void OnSearchTermChanged(FilterSearchState state)
        {
            var hasChanges =
                SearchText != state.Text ||
                SearchField != state.Field ||
                UseRegexSearch != state.UseRegex ||
                IsRegexValid != state.IsRegexValid;

            if (!hasChanges)
            {
                return;
            }

            SearchText = state.Text;
            SearchField = state.Field;
            UseRegexSearch = state.UseRegex;
            IsRegexValid = state.IsRegexValid;
            MarkTorrentsDirty();
        }

        protected async Task ToggleAlternativeSpeedLimits()
        {
            if (_toggleAltSpeedLimitsInProgress)
            {
                return;
            }

            _toggleAltSpeedLimitsInProgress = true;
            try
            {
                await ApiClient.ToggleAlternativeSpeedLimits();
                var isEnabled = await ApiClient.GetAlternativeSpeedLimitsState();

                if (MainData is not null)
                {
                    MainData.ServerState.UseAltSpeedLimits = isEnabled;
                }

                Snackbar?.Add(isEnabled ? "Alternative speed limits enabled." : "Alternative speed limits disabled.", Severity.Info);
            }
            catch (HttpRequestException exception)
            {
                Snackbar?.Add($"Unable to toggle alternative speed limits: {exception.Message}", Severity.Error);
            }
            finally
            {
                _toggleAltSpeedLimitsInProgress = false;
            }

            await InvokeAsync(StateHasChanged);
        }

        private void MarkTorrentsDirty()
        {
            _torrentsDirty = true;
            IncrementTorrentsVersion();
        }

        private void IncrementTorrentsVersion()
        {
            unchecked
            {
                _torrentsVersion++;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    CancelRefreshDelay();
                    _timerCancellationToken.Cancel();
                    _timerCancellationToken.Dispose();

                    if (_navigationHandlerAttached)
                    {
                        NavigationManager.LocationChanged -= NavigationManagerOnLocationChanged;
                        _navigationHandlerAttached = false;
                    }
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
