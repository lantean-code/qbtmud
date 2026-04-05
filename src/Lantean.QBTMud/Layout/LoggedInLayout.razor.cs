using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;
using ClientMainData = QBittorrent.ApiClient.Models.MainData;
using MudMainData = Lantean.QBTMud.Models.MainData;
using MudServerState = Lantean.QBTMud.Models.ServerState;
using MudTorrent = Lantean.QBTMud.Models.Torrent;

namespace Lantean.QBTMud.Layout
{
    public partial class LoggedInLayout : IAsyncDisposable
    {
        private const string _pendingDownloadStorageKey = "LoggedInLayout.PendingDownload";
        private const string _lastProcessedDownloadStorageKey = "LoggedInLayout.LastProcessedDownload";
        private const int _defaultRefreshInterval = 1500;
        private const string _startupApiErrorSnackbarKey = "logged-in-layout-startup-api-error";
        private const string _refreshApiErrorSnackbarKey = "logged-in-layout-refresh-api-error";
        private static readonly TimeSpan _pwaInstallPromptDisplayDelay = TimeSpan.FromSeconds(2);

        private readonly bool _refreshEnabled = true;
        private int _requestId = 0;
        private bool _disposedValue;
        private readonly CancellationTokenSource _timerCancellationToken = new();
        private IManagedTimer? _refreshTimer;
        private bool _toggleAltSpeedLimitsInProgress;
        private Task? _refreshLoopTask;
        private bool _authConfirmed;
        private bool _startupRecoveryPending;
        private bool _startupUpdateCheckRequested;
        private bool _updateSnackbarShown;
        private Task? _connectivityChangeTask;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IConnectivityStateService ConnectivityStateService { get; set; } = default!;

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
        protected ISettingsStorageService SettingsStorage { get; set; } = default!;

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
        protected IPreferencesUpdateService PreferencesUpdateService { get; set; } = default!;

        [Inject]
        protected IMagnetLinkService MagnetLinkService { get; set; } = default!;

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

        protected MudMainData? MainData { get; set; }

        protected string Category { get; set; } = FilterHelper.CATEGORY_ALL;

        protected string Tag { get; set; } = FilterHelper.TAG_ALL;

        protected string Tracker { get; set; } = FilterHelper.TRACKER_ALL;

        protected Status Status { get; set; } = Status.All;

        protected Preferences? Preferences { get; set; }

        protected AppSettings? AppSettingsState { get; set; }

        protected string? SortColumn { get; set; }

        protected SortDirection SortDirection { get; set; }

        protected string Version { get; set; } = "";

        protected string? SearchText { get; set; }

        protected TorrentFilterField SearchField { get; set; } = TorrentFilterField.Name;

        protected bool UseRegexSearch { get; set; }

        protected bool IsRegexValid { get; set; } = true;

        protected IReadOnlyList<MudTorrent> Torrents => GetTorrents();

        protected bool IsAuthenticated { get; set; }

        private IReadOnlyList<MudTorrent> _visibleTorrents = Array.Empty<MudTorrent>();

        private bool _torrentsDirty = true;
        private int _torrentsVersion;
        private string? _lastProcessedDownloadToken;
        private string? _pendingDownloadLink;
        private Task? _locationChangeTask;
        private bool _navigationHandlerAttached;
        private bool _welcomeWizardLaunched;
        private bool _lostConnectionDialogShown;
        private bool _localeMismatchWarningShown;
        private bool _showPwaInstallPrompt;
        private Task? _pwaInstallPromptDelayTask;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            _refreshTimer ??= ManagedTimerFactory.Create("MainDataRefresh", TimeSpan.FromMilliseconds(_defaultRefreshInterval), retryCount: 3);
            AppSettingsService.SettingsChanged += OnAppSettingsChanged;
            ConnectivityStateService.ConnectivityChanged += OnConnectivityChanged;

            if (!_navigationHandlerAttached)
            {
                NavigationManager.LocationChanged += NavigationManagerOnLocationChanged;
                _navigationHandlerAttached = true;
            }

            PreferencesUpdateService.PreferencesUpdated += OnPreferencesUpdated;
        }

        private IReadOnlyList<MudTorrent> GetTorrents()
        {
            if (!_torrentsDirty)
            {
                return _visibleTorrents;
            }

            if (MainData is null)
            {
                _visibleTorrents = Array.Empty<MudTorrent>();
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

            var authState = await ApiClient.CheckAuthStateAsync();
            if (!authState.TryGetValue(out var isAuthenticated))
            {
                await TryHandleStartupFailureAsync(authState.Failure!, _timerCancellationToken.Token);
                return;
            }

            if (ConnectivityStateService.IsLostConnection)
            {
                return;
            }

            if (!isAuthenticated)
            {
                await HandleAuthenticationFailureAsync(_timerCancellationToken.Token);
                return;
            }

            _authConfirmed = true;
            CaptureDownloadFromUri(NavigationManager.Uri);
            await PersistPendingDownloadAsync(_timerCancellationToken.Token);

            await InvokeAsync(StateHasChanged);

            var appSettingsTask = AppSettingsService.RefreshSettingsAsync(_timerCancellationToken.Token);
            var preferencesTask = ApiClient.GetApplicationPreferencesAsync();
            var versionTask = ApiClient.GetApplicationVersionAsync();
            var mainDataTask = ApiClient.GetMainDataAsync(_requestId);

            try
            {
                await Task.WhenAll(appSettingsTask, preferencesTask, versionTask, mainDataTask);
            }
            catch (Exception exception)
            {
                if (await TryHandleStartupExceptionAsync(exception, _timerCancellationToken.Token))
                {
                    return;
                }

                throw;
            }

            AppSettingsState = await appSettingsTask;
            Preferences? preferences = null;
            string? version = null;
            ClientMainData? data = null;
            if (!preferencesTask.Result.TryGetValue(out preferences)
                || !versionTask.Result.TryGetValue(out version)
                || !mainDataTask.Result.TryGetValue(out data))
            {
                var failure = preferencesTask.Result.Failure ?? versionTask.Result.Failure ?? mainDataTask.Result.Failure;
                if (failure is not null)
                {
                    await TryHandleStartupFailureAsync(failure, _timerCancellationToken.Token);
                    return;
                }
            }

            Preferences = preferences;
            await SynchronizeLocalePreferenceAsync();
            Version = version ?? string.Empty;
            MainData = DataManager.CreateMainData(data!);
            _startupRecoveryPending = false;
            ConnectivityStateService.MarkConnected();
            MarkTorrentsDirty();

            _requestId = data!.ResponseId;
            await UpdateRefreshIntervalAsync(MainData.ServerState.RefreshInterval, _timerCancellationToken.Token);
            await SpeedHistoryService.InitializeAsync();
            await RecordSpeedSampleAsync(MainData.ServerState, _timerCancellationToken.Token);

            IsAuthenticated = true;

            Menu?.ShowMenu(Preferences);

            await TryProcessPendingDownloadAsync(_timerCancellationToken.Token);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_refreshEnabled)
            {
                return;
            }

            if (_refreshLoopTask is null && (IsAuthenticated || _startupRecoveryPending))
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
                await LaunchWelcomeWizardFlowAsync();
            }

            if (!_startupUpdateCheckRequested && IsAuthenticated)
            {
                _startupUpdateCheckRequested = true;
                await TryRunStartupUpdateCheckAsync();
            }

            if (ConnectivityStateService.IsLostConnection)
            {
                await ShowLostConnectionDialogAsync();
            }
        }

        private async Task LaunchWelcomeWizardFlowAsync()
        {
            var canShowPwaInstallPrompt = await ShowWelcomeWizardIfNeededAsync();
            if (!canShowPwaInstallPrompt)
            {
                return;
            }

            SchedulePwaInstallPromptDisplay();
        }

        private void SchedulePwaInstallPromptDisplay()
        {
            if (_showPwaInstallPrompt || _pwaInstallPromptDelayTask is not null)
            {
                return;
            }

            _pwaInstallPromptDelayTask = ShowPwaInstallPromptAfterDelayAsync(_timerCancellationToken.Token);
        }

        private async Task ShowPwaInstallPromptAfterDelayAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(_pwaInstallPromptDisplayDelay, cancellationToken);
                _showPwaInstallPrompt = true;
                await InvokeAsync(StateHasChanged);
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                _pwaInstallPromptDelayTask = null;
            }
        }

        private async Task<bool> ShowWelcomeWizardIfNeededAsync()
        {
            var plan = await WelcomeWizardPlanBuilder.BuildPlanAsync();
            if (!plan.ShouldShowWizard)
            {
                return true;
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
            var dialogReference = await DialogService.ShowAsync<WelcomeWizardDialog>(title, parameters, options);
            var dialogResult = await dialogReference.Result;

            return dialogResult is { Canceled: false };
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
                var storedLocaleWhenApiMissing = await SettingsStorage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale);
                if (!string.IsNullOrWhiteSpace(storedLocaleWhenApiMissing))
                {
                    await SettingsStorage.RemoveItemAsync(LanguageStorageKeys.PreferredLocale);
                }

                return;
            }

            var apiLocale = WebUiLocaleNormalizer.Normalize(Preferences.Locale);
            var storedLocale = await SettingsStorage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale);

            if (string.IsNullOrWhiteSpace(storedLocale))
            {
                await SettingsStorage.SetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, apiLocale);
                return;
            }

            var normalizedStoredLocale = WebUiLocaleNormalizer.Normalize(storedLocale);
            if (!string.Equals(storedLocale.Trim(), normalizedStoredLocale, StringComparison.Ordinal))
            {
                await SettingsStorage.SetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, normalizedStoredLocale);
            }

            if (string.Equals(normalizedStoredLocale, apiLocale, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await SettingsStorage.SetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, apiLocale);

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
                var settings = AppSettingsState ?? await AppSettingsService.GetSettingsAsync(_timerCancellationToken.Token);
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
            catch (Exception)
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
            catch (Exception)
            {
            }
        }

        private async Task<ManagedTimerTickResult> RefreshTickAsync(CancellationToken cancellationToken)
        {
            if (!IsAuthenticated)
            {
                if (_startupRecoveryPending)
                {
                    return await RetryStartupTickAsync(cancellationToken);
                }

                return ManagedTimerTickResult.Stop;
            }

            var dataResult = await ApiClient.GetMainDataAsync(_requestId, cancellationToken);
            if (!dataResult.TryGetValue(out var data))
            {
                if (dataResult.Failure.IsAuthenticationFailure())
                {
                    await HandleAuthenticationFailureAsync(cancellationToken);
                    return ManagedTimerTickResult.Stop;
                }

                if (dataResult.Failure.IsConnectivityFailure())
                {
                    ConnectivityStateService.MarkLostConnection();
                    _timerCancellationToken.CancelIfNotDisposed();
                    await InvokeAsync(StateHasChanged);
                    return ManagedTimerTickResult.Stop;
                }

                SnackbarWorkflow.ShowLocalizedMessage(
                    "AppConnectivity",
                    "qBittorrent returned an error. Please try again.",
                    Severity.Error,
                    configure: null,
                    key: _refreshApiErrorSnackbarKey);
                return ManagedTimerTickResult.Continue;
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

            ConnectivityStateService.MarkConnected();
            _requestId = data.ResponseId;

            if (shouldRender)
            {
                await InvokeAsync(StateHasChanged);
            }

            return ManagedTimerTickResult.Continue;
        }

        private async Task HandleAuthenticationFailureAsync(CancellationToken cancellationToken)
        {
            IsAuthenticated = false;
            _authConfirmed = false;
            _startupRecoveryPending = false;
            ConnectivityStateService.MarkConnected();

            await ClearPendingDownloadAsync(cancellationToken);
            _timerCancellationToken.CancelIfNotDisposed();
            await InvokeAsync(() => NavigationManager.NavigateTo("login"));
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
            _refreshTimer ??= ManagedTimerFactory.Create("MainDataRefresh", TimeSpan.FromMilliseconds(_defaultRefreshInterval), retryCount: 3);
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

            var downloadValue = MagnetLinkService.ExtractDownloadLink(uri);
            if (string.IsNullOrWhiteSpace(downloadValue))
            {
                return;
            }

            if (HasAlreadyProcessed(downloadValue))
            {
                return;
            }

            _pendingDownloadLink = downloadValue;
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

            var stored = await SessionStorage.GetItemAsync<string>(_pendingDownloadStorageKey);
            if (!MagnetLinkService.IsSupportedDownloadLink(stored))
            {
                await SessionStorage.RemoveItemAsync(_pendingDownloadStorageKey);
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

            var stored = await SessionStorage.GetItemAsync<string>(_lastProcessedDownloadStorageKey);
            if (string.IsNullOrWhiteSpace(stored))
            {
                return;
            }

            _lastProcessedDownloadToken = stored;
        }

        private async Task PersistPendingDownloadAsync(CancellationToken cancellationToken)
        {
            if (SessionStorage is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_pendingDownloadLink))
            {
                await SessionStorage.RemoveItemAsync(_pendingDownloadStorageKey, cancellationToken);
                return;
            }

            await SessionStorage.SetItemAsync(_pendingDownloadStorageKey, _pendingDownloadLink, cancellationToken);
        }

        private async Task TryProcessPendingDownloadAsync(CancellationToken cancellationToken)
        {
            if (!IsAuthenticated || string.IsNullOrWhiteSpace(_pendingDownloadLink))
            {
                return;
            }

            var magnet = _pendingDownloadLink;

            if (string.Equals(_lastProcessedDownloadToken, magnet, StringComparison.Ordinal))
            {
                await ClearPendingDownloadAsync(cancellationToken);
                NavigationManager.NavigateToHome(forceLoad: true);
                return;
            }

            try
            {
                await InvokeAsync(() => DialogWorkflow.InvokeAddTorrentLinkDialog(magnet));
                await SaveLastProcessedDownloadAsync(magnet, cancellationToken);
                await ClearPendingDownloadAsync(cancellationToken);
                NavigationManager.NavigateToHome(forceLoad: true);
            }
            catch (Exception)
            {
                _pendingDownloadLink = magnet;
                await PersistPendingDownloadAsync(cancellationToken);
                throw;
            }
        }

        private async Task SaveLastProcessedDownloadAsync(string download, CancellationToken cancellationToken)
        {
            _lastProcessedDownloadToken = download;

            if (SessionStorage is null)
            {
                return;
            }

            await SessionStorage.SetItemAsync(_lastProcessedDownloadStorageKey, download, cancellationToken);
        }

        private async Task ClearPendingDownloadAsync(CancellationToken cancellationToken)
        {
            _pendingDownloadLink = null;
            if (SessionStorage is not null)
            {
                await SessionStorage.RemoveItemAsync(_pendingDownloadStorageKey, cancellationToken);
            }
        }

        private async Task HandleLocationChangedAsync(string? location, CancellationToken cancellationToken = default)
        {
            if (!_authConfirmed)
            {
                return;
            }

            CaptureDownloadFromUri(location);
            await PersistPendingDownloadAsync(cancellationToken);

            if (IsAuthenticated)
            {
                await TryProcessPendingDownloadAsync(cancellationToken);
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

        private Task RecordSpeedSampleAsync(MudServerState? serverState, CancellationToken cancellationToken)
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
            var toggleResult = await ApiClient.ToggleAlternativeSpeedLimitsAsync();
            if (!toggleResult.IsSuccess)
            {
                SnackbarWorkflow.ShowTransientMessage(
                    LanguageLocalizer.Translate(
                        "AppLoggedInLayout",
                        "Unable to toggle alternative speed limits: %1",
                        toggleResult.Failure?.UserMessage ?? string.Empty),
                    Severity.Error);
            }
            else
            {
                var isEnabledResult = await ApiClient.GetAlternativeSpeedLimitsStateAsync();
                if (isEnabledResult.TryGetValue(out var isEnabled))
                {
                    if (MainData is not null)
                    {
                        MainData.ServerState.UseAltSpeedLimits = isEnabled;
                    }

                    SnackbarWorkflow.ShowTransientMessage(BuildAlternativeSpeedLimitsStatusMessage(isEnabled), Severity.Info);
                }
                else
                {
                    SnackbarWorkflow.ShowTransientMessage(
                        LanguageLocalizer.Translate(
                            "AppLoggedInLayout",
                            "Unable to toggle alternative speed limits: %1",
                            isEnabledResult.Failure?.UserMessage ?? string.Empty),
                        Severity.Error);
                }
            }

            _toggleAltSpeedLimitsInProgress = false;

            await InvokeAsync(StateHasChanged);
        }

        protected async Task ShowGlobalDownloadRateLimit()
        {
            try
            {
                var appliedRate = await DialogWorkflow.InvokeGlobalDownloadRateDialog(MainData?.ServerState.DownloadRateLimit ?? 0);
                if (!appliedRate.HasValue || MainData is null)
                {
                    return;
                }

                MainData.ServerState.DownloadRateLimit = checked((int)appliedRate.Value);
            }
            catch (HttpRequestException exception)
            {
                SnackbarWorkflow.ShowTransientMessage(
                    LanguageLocalizer.Translate("AppLoggedInLayout", "Unable to set global download rate limit: %1", exception.Message),
                    Severity.Error);
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task ShowGlobalUploadRateLimit()
        {
            try
            {
                var appliedRate = await DialogWorkflow.InvokeGlobalUploadRateDialog(MainData?.ServerState.UploadRateLimit ?? 0);
                if (!appliedRate.HasValue || MainData is null)
                {
                    return;
                }

                MainData.ServerState.UploadRateLimit = checked((int)appliedRate.Value);
            }
            catch (HttpRequestException exception)
            {
                SnackbarWorkflow.ShowTransientMessage(
                    LanguageLocalizer.Translate("AppLoggedInLayout", "Unable to set global upload rate limit: %1", exception.Message),
                    Severity.Error);
            }

            await InvokeAsync(StateHasChanged);
        }

        private void MarkTorrentsDirty()
        {
            _torrentsDirty = true;
            IncrementTorrentsVersion();
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

        private void OnAppSettingsChanged(object? sender, AppSettingsChangedEventArgs args)
        {
            AppSettingsState = args.Settings.Clone();
            StateHasChanged();
        }

        private void OnConnectivityChanged(bool isLostConnection)
        {
            _connectivityChangeTask = HandleConnectivityChangedAsync(isLostConnection);
        }

        private async ValueTask OnPreferencesUpdated(Preferences preferences)
        {
            ArgumentNullException.ThrowIfNull(preferences);

            Preferences = preferences;
            Menu?.ShowMenu(preferences);
            await InvokeAsync(StateHasChanged);
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

                if (_pwaInstallPromptDelayTask is not null)
                {
                    try
                    {
                        await _pwaInstallPromptDelayTask;
                    }
                    catch (OperationCanceledException exception) when (exception.CancellationToken == _timerCancellationToken.Token)
                    {
                    }
                }

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

                AppSettingsService.SettingsChanged -= OnAppSettingsChanged;
                ConnectivityStateService.ConnectivityChanged -= OnConnectivityChanged;
                PreferencesUpdateService.PreferencesUpdated -= OnPreferencesUpdated;

                if (_connectivityChangeTask is not null)
                {
                    try
                    {
                        await _connectivityChangeTask;
                    }
                    catch (InvalidOperationException) when (_disposedValue)
                    {
                    }
                    catch (ObjectDisposedException) when (_disposedValue)
                    {
                    }
                }
            }

            _disposedValue = true;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }

        private Task<bool> TryHandleStartupExceptionAsync(Exception exception, CancellationToken cancellationToken)
        {
            var failure = new ApiFailure()
            {
                Kind = ApiFailureKind.UnexpectedResponse,
                Operation = "startup",
                UserMessage = exception.Message,
                Detail = exception.Message,
            };
            return TryHandleStartupFailureAsync(failure, cancellationToken);
        }

        private async Task<bool> TryHandleStartupFailureAsync(ApiFailure failure, CancellationToken cancellationToken)
        {
            if (failure.IsAuthenticationFailure())
            {
                await HandleAuthenticationFailureAsync(cancellationToken);
                return true;
            }

            if (failure.IsConnectivityFailure())
            {
                _startupRecoveryPending = false;
                ConnectivityStateService.MarkLostConnection();
                return true;
            }

            _startupRecoveryPending = true;
            ConnectivityStateService.MarkConnected();
            SnackbarWorkflow.ShowLocalizedMessage(
                "AppConnectivity",
                "qBittorrent returned an error. Please try again.",
                Severity.Error,
                configure: null,
                key: _startupApiErrorSnackbarKey);
            return true;
        }

        private async Task HandleConnectivityChangedAsync(bool isLostConnection)
        {
            try
            {
                if (!isLostConnection)
                {
                    _lostConnectionDialogShown = false;
                }

                await InvokeAsync(StateHasChanged);
            }
            catch (InvalidOperationException) when (_disposedValue)
            {
            }
            catch (ObjectDisposedException) when (_disposedValue)
            {
            }
        }

        private async Task<ManagedTimerTickResult> RetryStartupTickAsync(CancellationToken cancellationToken)
        {
            var authState = await ApiClient.CheckAuthStateAsync(cancellationToken);
            if (!authState.TryGetValue(out var isAuthenticated))
            {
                return await HandleStartupRecoveryFailureAsync(authState.Failure!, cancellationToken);
            }

            if (!isAuthenticated)
            {
                await HandleAuthenticationFailureAsync(cancellationToken);
                return ManagedTimerTickResult.Stop;
            }

            _authConfirmed = true;
            CaptureDownloadFromUri(NavigationManager.Uri);
            await PersistPendingDownloadAsync(cancellationToken);

            var appSettingsTask = AppSettingsService.RefreshSettingsAsync(cancellationToken);
            var preferencesTask = ApiClient.GetApplicationPreferencesAsync(cancellationToken);
            var versionTask = ApiClient.GetApplicationVersionAsync(cancellationToken);
            var mainDataTask = ApiClient.GetMainDataAsync(_requestId, cancellationToken);

            try
            {
                await Task.WhenAll(appSettingsTask, preferencesTask, versionTask, mainDataTask);
            }
            catch (Exception exception)
            {
                return await HandleStartupRecoveryExceptionAsync(exception, cancellationToken);
            }

            AppSettingsState = appSettingsTask.Result;
            string? version = null;
            ClientMainData? data = null;
            if (!preferencesTask.Result.TryGetValue(out var preferences)
                || !versionTask.Result.TryGetValue(out version)
                || !mainDataTask.Result.TryGetValue(out data))
            {
                var failure = preferencesTask.Result.Failure ?? versionTask.Result.Failure ?? mainDataTask.Result.Failure;
                if (failure is not null)
                {
                    return await HandleStartupRecoveryFailureAsync(failure, cancellationToken);
                }
            }

            Preferences = preferences;
            await SynchronizeLocalePreferenceAsync();
            Version = version ?? string.Empty;
            MainData = DataManager.CreateMainData(data!);
            _startupRecoveryPending = false;
            ConnectivityStateService.MarkConnected();
            MarkTorrentsDirty();

            _requestId = data!.ResponseId;
            await UpdateRefreshIntervalAsync(MainData.ServerState.RefreshInterval, cancellationToken);
            await SpeedHistoryService.InitializeAsync(cancellationToken);
            await RecordSpeedSampleAsync(MainData.ServerState, cancellationToken);

            IsAuthenticated = true;

            Menu?.ShowMenu(Preferences);

            await TryProcessPendingDownloadAsync(cancellationToken);
            await InvokeAsync(StateHasChanged);

            return ManagedTimerTickResult.Continue;
        }

        private Task<ManagedTimerTickResult> HandleStartupRecoveryExceptionAsync(Exception exception, CancellationToken cancellationToken)
        {
            return HandleStartupRecoveryFailureAsync(
                new ApiFailure
                {
                    Kind = ApiFailureKind.UnexpectedResponse,
                    Operation = "startup-recovery",
                    UserMessage = exception.Message,
                    Detail = exception.Message,
                },
                cancellationToken);
        }

        private async Task<ManagedTimerTickResult> HandleStartupRecoveryFailureAsync(ApiFailure failure, CancellationToken cancellationToken)
        {
            if (failure.IsAuthenticationFailure())
            {
                await HandleAuthenticationFailureAsync(cancellationToken);
                return ManagedTimerTickResult.Stop;
            }

            if (failure.IsConnectivityFailure())
            {
                _startupRecoveryPending = false;
                ConnectivityStateService.MarkLostConnection();
                _timerCancellationToken.CancelIfNotDisposed();
                await InvokeAsync(StateHasChanged);
                return ManagedTimerTickResult.Stop;
            }

            ConnectivityStateService.MarkConnected();
            SnackbarWorkflow.ShowLocalizedMessage(
                "AppConnectivity",
                "qBittorrent returned an error. Please try again.",
                Severity.Error,
                configure: null,
                key: _startupApiErrorSnackbarKey);

            return ManagedTimerTickResult.Continue;
        }
    }
}
