using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using System.Net;

namespace Lantean.QBTMud.Layout
{
    public partial class LoggedInLayout : IAsyncDisposable
    {
        private const string PendingDownloadStorageKey = "LoggedInLayout.PendingDownload";
        private const string LastProcessedDownloadStorageKey = "LoggedInLayout.LastProcessedDownload";
        private const int MaxDownloadLength = 8 * 1024;
        private const int DefaultRefreshInterval = 1500;

        private readonly bool _refreshEnabled = true;
        private int _requestId = 0;
        private bool _disposedValue;
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private IManagedTimer? _refreshTimer;
        private bool _toggleAltSpeedLimitsInProgress;
        private Task? _refreshLoopTask;
        private bool _authConfirmed;
        private bool _startupUpdateCheckRequested;
        private bool _updateSnackbarShown;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ITorrentDataManager DataManager { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected ISessionStorageService SessionStorage { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Inject]
        protected ISpeedHistoryService SpeedHistoryService { get; set; } = default!;

        [Inject]
        protected IManagedTimerFactory ManagedTimerFactory { get; set; } = default!;

        [Inject]
        protected IAppSettingsService AppSettingsService { get; set; } = default!;

        [Inject]
        protected IAppUpdateService AppUpdateService { get; set; } = default!;

        [Inject]
        protected IWelcomeWizardPlanBuilder WelcomeWizardPlanBuilder { get; set; } = default!;

        [Inject]
        protected IWelcomeWizardStateService WelcomeWizardStateService { get; set; } = default!;

        [Inject]
        protected ITorrentCompletionNotificationService TorrentCompletionNotificationService { get; set; } = default!;

        [CascadingParameter]
        public Breakpoint CurrentBreakpoint { get; set; }

        [CascadingParameter]
        public Orientation CurrentOrientation { get; set; }

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter(Name = "TimerDrawerOpen")]
        public bool TimerDrawerOpen { get; set; }

        [CascadingParameter(Name = "TimerDrawerOpenChanged")]
        public EventCallback<bool> TimerDrawerOpenChanged { get; set; }

        [CascadingParameter]
        public Menu? Menu { get; set; }

        [CascadingParameter(Name = "IsDarkMode")]
        public bool IsDarkMode { get; set; }

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
        private Task? _locationChangeTask;
        private bool _navigationHandlerAttached;
        private bool _welcomeWizardLaunched;
        private bool _lostConnectionDialogShown;
        private bool _localeMismatchWarningShown;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            _refreshTimer ??= ManagedTimerFactory.Create("MainDataRefresh", TimeSpan.FromMilliseconds(DefaultRefreshInterval));

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
            await RestoreProcessedDownloadAsync();
            await RestorePendingDownloadAsync();

            if (!await ApiClient.CheckAuthState())
            {
                await ClearPendingDownloadAsync();
                NavigationManager.NavigateTo("login");
                return;
            }

            _authConfirmed = true;
            CaptureDownloadFromUri(NavigationManager.Uri);
            await PersistPendingDownloadAsync();

            await InvokeAsync(StateHasChanged);

            Preferences = await ApiClient.GetApplicationPreferences();
            await SynchronizeLocalePreferenceAsync();
            Version = await ApiClient.GetApplicationVersion();
            var data = await ApiClient.GetMainData(_requestId);
            MainData = DataManager.CreateMainData(data);
            MarkTorrentsDirty();

            _requestId = data.ResponseId;
            await UpdateRefreshIntervalAsync(MainData.ServerState.RefreshInterval, _timerCancellationToken.Token);
            await SpeedHistoryService.InitializeAsync();
            await RecordSpeedSampleAsync(MainData.ServerState, _timerCancellationToken.Token);

            IsAuthenticated = true;

            Menu?.ShowMenu(Preferences);

            await TryProcessPendingDownloadAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_refreshEnabled)
            {
                return;
            }

            if (firstRender && _refreshLoopTask is null)
            {
                StartRefreshLoop();
            }

            if (_locationChangeTask is not null && _locationChangeTask.IsCompleted)
            {
                _locationChangeTask = null;
            }

            if (!_welcomeWizardLaunched && IsAuthenticated && Preferences is not null)
            {
                _welcomeWizardLaunched = true;
                await ShowWelcomeWizardIfNeededAsync();
            }

            if (!_startupUpdateCheckRequested && _authConfirmed)
            {
                _startupUpdateCheckRequested = true;
                await TryRunStartupUpdateCheckAsync();
            }

            if (MainData?.LostConnection == true)
            {
                await ShowLostConnectionDialogAsync();
            }
        }

        private async Task ShowWelcomeWizardIfNeededAsync()
        {
            var plan = await WelcomeWizardPlanBuilder.BuildPlanAsync();
            if (!plan.ShouldShowWizard)
            {
                return;
            }

            await WelcomeWizardStateService.MarkShownAsync();
            var useFullScreenWizardDialog = CurrentBreakpoint == Breakpoint.None || CurrentBreakpoint <= Breakpoint.Sm;

            var parameters = new DialogParameters
            {
                { nameof(WelcomeWizardDialog.InitialLocale), Preferences?.Locale },
                { nameof(WelcomeWizardDialog.PendingStepIds), plan.PendingSteps.Select(step => step.Id).ToArray() },
                { nameof(WelcomeWizardDialog.ShowWelcomeBackIntro), plan.IsReturningUser }
            };

            var options = new DialogOptions
            {
                CloseOnEscapeKey = false,
                BackdropClick = false,
                NoHeader = false,
                FullWidth = true,
                FullScreen = useFullScreenWizardDialog,
                MaxWidth = MaxWidth.Medium,
                BackgroundClass = "background-blur background-blur-strong"
            };

            var title = LanguageLocalizer.Translate("AppWelcomeWizard", plan.IsReturningUser ? "Welcome back" : "Welcome");
            await DialogService.ShowAsync<WelcomeWizardDialog>(title, parameters, options);
        }

        private async Task ShowLostConnectionDialogAsync()
        {
            if (_lostConnectionDialogShown)
            {
                return;
            }

            _lostConnectionDialogShown = true;

            var options = new DialogOptions
            {
                CloseOnEscapeKey = false,
                BackdropClick = false,
                NoHeader = true,
                FullWidth = true,
                MaxWidth = MaxWidth.ExtraSmall,
                BackgroundClass = "background-blur background-blur-strong"
            };

            await DialogService.ShowAsync<LostConnectionDialog>(title: null, options);
        }

        private async Task SynchronizeLocalePreferenceAsync()
        {
            if (Preferences is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Preferences.Locale))
            {
                var storedLocaleWhenApiMissing = await LocalStorage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale);
                if (!string.IsNullOrWhiteSpace(storedLocaleWhenApiMissing))
                {
                    await LocalStorage.RemoveItemAsync(LanguageStorageKeys.PreferredLocale);
                }

                return;
            }

            var apiLocale = Preferences.Locale.Trim();
            var storedLocale = await LocalStorage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale);

            if (string.IsNullOrWhiteSpace(storedLocale))
            {
                await LocalStorage.SetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, apiLocale);
                return;
            }

            if (string.Equals(storedLocale.Trim(), apiLocale, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await LocalStorage.SetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, apiLocale);

            if (_localeMismatchWarningShown)
            {
                return;
            }

            _localeMismatchWarningShown = true;
            SnackbarWorkflow.ShowActionMessage(
                LanguageLocalizer.Translate("AppLocalization", "Language preference changed on server. Click Reload to apply it."),
                Severity.Warning,
                LanguageLocalizer.Translate("AppLocalization", "Reload"),
                _ =>
                {
                    NavigationManager.NavigateToHome(forceLoad: true);
                    return Task.CompletedTask;
                },
                configure: options =>
                {
                    options.CloseAfterNavigation = true;
                });
        }

        private async Task TryRunStartupUpdateCheckAsync()
        {
            try
            {
                var settings = await AppSettingsService.GetSettingsAsync(_timerCancellationToken.Token);
                if (!settings.UpdateChecksEnabled)
                {
                    return;
                }

                var status = await AppUpdateService.GetUpdateStatusAsync(cancellationToken: _timerCancellationToken.Token);
                var latestTag = status.LatestRelease?.TagName;
                if (!status.IsUpdateAvailable || string.IsNullOrWhiteSpace(latestTag))
                {
                    return;
                }

                if (string.Equals(settings.DismissedReleaseTag, latestTag, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (_updateSnackbarShown)
                {
                    return;
                }

                _updateSnackbarShown = true;

                SnackbarWorkflow.ShowActionMessage(
                    LanguageLocalizer.Translate("AppUpdates", "A new qbtmud build (%1) is available.", latestTag),
                    Severity.Info,
                    LanguageLocalizer.Translate("AppUpdates", "Dismiss"),
                    async _ =>
                    {
                        await AppSettingsService.SaveDismissedReleaseTagAsync(latestTag);
                    },
                    key: $"qbtmud-update-{latestTag}");
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == _timerCancellationToken.Token)
            {
            }
            catch
            {
            }
        }

        private async Task TryProcessTorrentNotificationsAsync(IReadOnlyList<TorrentTransition> transitions, CancellationToken cancellationToken)
        {
            if (transitions.Count == 0)
            {
                return;
            }

            try
            {
                await TorrentCompletionNotificationService.ProcessTransitionsAsync(transitions, cancellationToken);
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
            {
                throw;
            }
            catch
            {
            }
        }

        private async Task<ManagedTimerTickResult> RefreshTickAsync(CancellationToken cancellationToken)
        {
            if (!IsAuthenticated)
            {
                return ManagedTimerTickResult.Stop;
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

                await InvokeAsync(ShowLostConnectionDialogAsync);
                _timerCancellationToken.CancelIfNotDisposed();
                await InvokeAsync(StateHasChanged);
                return ManagedTimerTickResult.Stop;
            }

            var shouldRender = false;
            IReadOnlyList<TorrentTransition> transitionBatch = Array.Empty<TorrentTransition>();

            if (MainData is null || data.FullUpdate)
            {
                MainData = DataManager.CreateMainData(data);
                MarkTorrentsDirty();
                shouldRender = true;
            }
            else
            {
                var dataChanged = DataManager.MergeMainData(data, MainData, out var filterChanged, out transitionBatch);
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
                await UpdateRefreshIntervalAsync(MainData.ServerState.RefreshInterval, cancellationToken);
                await RecordSpeedSampleAsync(MainData.ServerState, cancellationToken);
                await TryProcessTorrentNotificationsAsync(transitionBatch, cancellationToken);
            }

            _requestId = data.ResponseId;

            if (shouldRender)
            {
                await InvokeAsync(StateHasChanged);
            }

            return ManagedTimerTickResult.Continue;
        }

        private Task UpdateRefreshIntervalAsync(int newInterval, CancellationToken cancellationToken)
        {
            if (newInterval <= 0 || _refreshTimer is null)
            {
                return Task.CompletedTask;
            }

            return _refreshTimer.UpdateIntervalAsync(TimeSpan.FromMilliseconds(newInterval), cancellationToken);
        }

        private void StartRefreshLoop()
        {
            _refreshTimer ??= ManagedTimerFactory.Create("MainDataRefresh", TimeSpan.FromMilliseconds(DefaultRefreshInterval));
            _refreshLoopTask = _refreshTimer.StartAsync(RefreshTickAsync, _timerCancellationToken.Token);
        }

        private void NavigationManagerOnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            if (!_authConfirmed)
            {
                return;
            }

            _locationChangeTask = InvokeAsync(() => HandleLocationChangedAsync(e.Location));
        }

        private void CaptureDownloadFromUri(string? uri)
        {
            if (!_authConfirmed)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(uri))
            {
                return;
            }

            var downloadValue = ExtractDownloadParameter(uri);
            if (string.IsNullOrWhiteSpace(downloadValue))
            {
                return;
            }

            var decoded = WebUtility.UrlDecode(downloadValue);
            if (string.IsNullOrWhiteSpace(decoded))
            {
                return;
            }

            if (!IsValidDownloadValue(decoded))
            {
                return;
            }

            if (HasAlreadyProcessed(decoded))
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

            if (SessionStorage is null)
            {
                return;
            }

            var stored = await SessionStorage.GetItemAsync<string>(PendingDownloadStorageKey);
            if (!IsValidDownloadValue(stored))
            {
                await SessionStorage.RemoveItemAsync(PendingDownloadStorageKey);
                return;
            }

            _pendingDownloadLink = stored;
        }

        private async Task RestoreProcessedDownloadAsync()
        {
            if (SessionStorage is null)
            {
                return;
            }

            var stored = await SessionStorage.GetItemAsync<string>(LastProcessedDownloadStorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return;
            }

            _lastProcessedDownloadToken = stored;
        }

        private async Task PersistPendingDownloadAsync()
        {
            if (SessionStorage is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_pendingDownloadLink))
            {
                await SessionStorage.RemoveItemAsync(PendingDownloadStorageKey);
                return;
            }

            await SessionStorage.SetItemAsync(PendingDownloadStorageKey, _pendingDownloadLink);
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
                NavigationManager.NavigateToHome(forceLoad: true);
                return;
            }

            try
            {
                await InvokeAsync(() => DialogWorkflow.InvokeAddTorrentLinkDialog(magnet));
                await SaveLastProcessedDownloadAsync(magnet);
                await ClearPendingDownloadAsync();
                NavigationManager.NavigateToHome(forceLoad: true);
            }
            catch
            {
                _pendingDownloadLink = magnet;
                await PersistPendingDownloadAsync();
                throw;
            }
        }

        private async Task SaveLastProcessedDownloadAsync(string download)
        {
            _lastProcessedDownloadToken = download;

            if (SessionStorage is null)
            {
                return;
            }

            await SessionStorage.SetItemAsync(LastProcessedDownloadStorageKey, download);
        }

        private async Task ClearPendingDownloadAsync()
        {
            _pendingDownloadLink = null;
            if (SessionStorage is not null)
            {
                await SessionStorage.RemoveItemAsync(PendingDownloadStorageKey);
            }
        }

        private async Task HandleLocationChangedAsync(string? location)
        {
            if (!_authConfirmed)
            {
                return;
            }

            CaptureDownloadFromUri(location);
            await PersistPendingDownloadAsync();

            if (IsAuthenticated)
            {
                await TryProcessPendingDownloadAsync();
            }
        }

        protected EventCallback<string> CategoryChanged => EventCallback.Factory.Create<string>(this, OnCategoryChanged);

        protected EventCallback<Status> StatusChanged => EventCallback.Factory.Create<Status>(this, OnStatusChanged);

        protected EventCallback<string> TagChanged => EventCallback.Factory.Create<string>(this, OnTagChanged);

        protected EventCallback<string> TrackerChanged => EventCallback.Factory.Create<string>(this, OnTrackerChanged);

        protected EventCallback<FilterSearchState> SearchTermChanged => EventCallback.Factory.Create<FilterSearchState>(this, OnSearchTermChanged);

        protected EventCallback<string> SortColumnChanged => EventCallback.Factory.Create<string>(this, columnId => SortColumn = columnId);

        protected EventCallback<SortDirection> SortDirectionChanged => EventCallback.Factory.Create<SortDirection>(this, sortDirection => SortDirection = sortDirection);

        protected async Task ToggleTimerDrawerAsync()
        {
            var nextValue = !TimerDrawerOpen;
            TimerDrawerOpen = nextValue;
            if (TimerDrawerOpenChanged.HasDelegate)
            {
                await TimerDrawerOpenChanged.InvokeAsync(nextValue);
            }
        }

        private string BuildPageTitle()
        {
            var webUiLabel = LanguageLocalizer.Translate("OptionsDialog", "WebUI");
            var versionPart = string.IsNullOrWhiteSpace(Version) ? string.Empty : $" {Version}";
            return $"qBittorrent{versionPart} {webUiLabel}";
        }

        private string BuildAlternativeSpeedLimitsStatusMessage(bool isEnabled)
        {
            return LanguageLocalizer.Translate(
                "MainWindow",
                isEnabled ? "Alternative speed limits: On" : "Alternative speed limits: Off");
        }

        private Task RecordSpeedSampleAsync(ServerState? serverState, CancellationToken cancellationToken)
        {
            if (serverState is null)
            {
                return Task.CompletedTask;
            }

            return SpeedHistoryService.PushSampleAsync(DateTime.UtcNow, serverState.DownloadInfoSpeed, serverState.UploadInfoSpeed, cancellationToken);
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

                SnackbarWorkflow.ShowTransientMessage(BuildAlternativeSpeedLimitsStatusMessage(isEnabled), Severity.Info);
            }
            catch (HttpRequestException exception)
            {
                SnackbarWorkflow.ShowTransientMessage(
                    LanguageLocalizer.Translate("AppLoggedInLayout", "Unable to toggle alternative speed limits: %1", exception.Message),
                    Severity.Error);
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

        private static bool IsValidDownloadValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (value.Length > MaxDownloadLength)
            {
                return false;
            }

            if (value.IndexOfAny(new[] { '\r', '\n' }) >= 0)
            {
                return false;
            }

            if (value.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            {
                // Require a magnet URN for basic validation.
                return value.Contains("xt=urn:btih", StringComparison.OrdinalIgnoreCase);
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return uri.AbsolutePath.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasAlreadyProcessed(string download)
        {
            if (string.IsNullOrWhiteSpace(download))
            {
                return true;
            }

            return string.Equals(_lastProcessedDownloadToken, download, StringComparison.Ordinal);
        }

        private void IncrementTorrentsVersion()
        {
            unchecked
            {
                _torrentsVersion++;
            }
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (_disposedValue)
            {
                return;
            }

            if (disposing)
            {
                _timerCancellationToken.Cancel();
                _timerCancellationToken.Dispose();

                if (_refreshTimer is not null)
                {
                    await _refreshTimer.DisposeAsync();
                    _refreshTimer = null;
                }

                if (_navigationHandlerAttached)
                {
                    NavigationManager.LocationChanged -= NavigationManagerOnLocationChanged;
                    _navigationHandlerAttached = false;
                }
            }

            _disposedValue = true;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
