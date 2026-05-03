using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Core;
using Lantean.QBTMud.Core.Helpers;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using MudMainData = Lantean.QBTMud.Core.Models.MainData;
using MudTorrent = Lantean.QBTMud.Core.Models.Torrent;

namespace Lantean.QBTMud.Layout
{
    public partial class LoggedInLayout : IAsyncDisposable
    {
        private const int _defaultRefreshInterval = 1500;
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
        private Task? _preferencesChangeTask;

        [Inject]
        protected ILostConnectionWorkflow LostConnectionWorkflow { get; set; } = default!;

        [Inject]
        protected IShellSessionWorkflow ShellSessionWorkflow { get; set; } = default!;

        [Inject]
        protected IPreferencesDataManager PreferencesDataManager { get; set; } = default!;

        [Inject]
        protected IQBittorrentPreferencesStateService QBittorrentPreferencesStateService { get; set; } = default!;

        [Inject]
        protected IPendingDownloadWorkflow PendingDownloadWorkflow { get; set; } = default!;

        [Inject]
        protected IStartupExperienceWorkflow StartupExperienceWorkflow { get; set; } = default!;

        [Inject]
        protected IStatusBarWorkflow StatusBarWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Inject]
        protected IManagedTimerFactory ManagedTimerFactory { get; set; } = default!;

        [Inject]
        protected ITorrentQueryState TorrentQueryState { get; set; } = default!;

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

        protected QBittorrentPreferences? Preferences { get; set; }

        protected string Version { get; set; } = "";

        protected IReadOnlyList<MudTorrent> Torrents => GetTorrents();

        protected bool IsAuthenticated { get; set; }

        private IReadOnlyList<MudTorrent> _visibleTorrents = Array.Empty<MudTorrent>();

        private bool _torrentsDirty = true;
        private int _torrentsVersion;
        private Task? _locationChangeTask;
        private bool _navigationHandlerAttached;
        private bool _welcomeWizardLaunched;
        private bool _showPwaInstallPrompt;
        private Task? _pwaInstallPromptDelayTask;
        private bool _startupUpdateCheckReady;
        private bool _startupUpdateChecksEnabled;
        private string? _startupDismissedReleaseTag;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            _refreshTimer ??= ManagedTimerFactory.Create("MainDataRefresh", TimeSpan.FromMilliseconds(_defaultRefreshInterval), retryCount: 3);

            if (!_navigationHandlerAttached)
            {
                NavigationManager.LocationChanged += NavigationManagerOnLocationChanged;
                _navigationHandlerAttached = true;
            }

            TorrentQueryState.Changed += OnTorrentQueryStateChanged;
            QBittorrentPreferencesStateService.Changed += OnQBittorrentPreferencesChanged;
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
                TorrentQueryState.Category,
                TorrentQueryState.Status,
                TorrentQueryState.Tag,
                TorrentQueryState.Tracker,
                Preferences?.UseSubcategories ?? MainData.ServerState.UseSubcategories,
                TorrentQueryState.SearchText,
                TorrentQueryState.SearchField,
                TorrentQueryState.UseRegexSearch,
                TorrentQueryState.IsRegexValid);
            _visibleTorrents = MainData.Torrents.Values.Filter(filterState).ToList();
            _torrentsDirty = false;

            return _visibleTorrents;
        }

        protected override async Task OnInitializedAsync()
        {
            await PendingDownloadWorkflow.RestoreAsync(_timerCancellationToken.Token);

            var loadResult = await ShellSessionWorkflow.LoadAsync(_timerCancellationToken.Token);
            if (loadResult.Outcome == ShellSessionLoadOutcome.AuthenticationRequired)
            {
                await HandleAuthenticationFailureAsync(_timerCancellationToken.Token);
                return;
            }

            if (loadResult.Outcome == ShellSessionLoadOutcome.LostConnection)
            {
                _startupRecoveryPending = false;
                await LostConnectionWorkflow.MarkLostConnectionAsync();
                return;
            }

            if (loadResult.Outcome == ShellSessionLoadOutcome.RetryableFailure)
            {
                _startupRecoveryPending = true;
                return;
            }

            _authConfirmed = true;
            await PendingDownloadWorkflow.CaptureFromUriAsync(NavigationManager.Uri, _timerCancellationToken.Token);

            await InvokeAsync(StateHasChanged);
            await ApplyLoadResultAsync(loadResult, _timerCancellationToken.Token);
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

            if (_preferencesChangeTask is not null && _preferencesChangeTask.IsCompleted)
            {
                _preferencesChangeTask = null;
            }

            if (!_welcomeWizardLaunched && IsAuthenticated && Preferences is not null)
            {
                _welcomeWizardLaunched = true;
                await LaunchWelcomeWizardFlowAsync();
            }

            if (!_startupUpdateCheckRequested && IsAuthenticated && _startupUpdateCheckReady)
            {
                _startupUpdateCheckRequested = true;
                await StartupExperienceWorkflow.RunUpdateCheckAsync(
                    _startupUpdateChecksEnabled,
                    _startupDismissedReleaseTag,
                    _timerCancellationToken.Token);
            }
        }

        private async Task LaunchWelcomeWizardFlowAsync()
        {
            var canShowPwaInstallPrompt = await StartupExperienceWorkflow.RunWelcomeWizardAsync(
                Preferences?.Locale,
                CurrentBreakpoint == Breakpoint.None || CurrentBreakpoint <= Breakpoint.Sm,
                _timerCancellationToken.Token);
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

            var refreshResult = await ShellSessionWorkflow.RefreshAsync(_requestId, MainData, cancellationToken);
            if (refreshResult.Outcome == ShellSessionRefreshOutcome.AuthenticationRequired)
            {
                await HandleAuthenticationFailureAsync(cancellationToken);
                return ManagedTimerTickResult.Stop;
            }

            if (refreshResult.Outcome == ShellSessionRefreshOutcome.LostConnection)
            {
                await LostConnectionWorkflow.MarkLostConnectionAsync();
                _timerCancellationToken.CancelIfNotDisposed();
                await InvokeAsync(StateHasChanged);
                return ManagedTimerTickResult.Stop;
            }

            if (refreshResult.Outcome == ShellSessionRefreshOutcome.RetryableFailure)
            {
                return ManagedTimerTickResult.Continue;
            }

            MainData = refreshResult.MainData;
            _requestId = refreshResult.RequestId;

            if (MainData is not null)
            {
                await UpdateRefreshIntervalAsync(Preferences?.RefreshInterval ?? MainData.ServerState.RefreshInterval, cancellationToken);
            }

            if (refreshResult.TorrentsDirty)
            {
                MarkTorrentsDirty();
            }
            else if (refreshResult.ShouldRender)
            {
                IncrementTorrentsVersion();
            }

            if (refreshResult.ShouldRender)
            {
                await InvokeAsync(StateHasChanged);
            }

            return ManagedTimerTickResult.Continue;
        }

        private async Task HandleAuthenticationFailureAsync(CancellationToken cancellationToken)
        {
            var forceLoginReload = _authConfirmed;
            IsAuthenticated = false;
            _authConfirmed = false;
            _startupRecoveryPending = false;

            await PendingDownloadWorkflow.ClearAsync(cancellationToken);
            _timerCancellationToken.CancelIfNotDisposed();
            await InvokeAsync(() => NavigationManager.NavigateTo("login", forceLoad: forceLoginReload));
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

        private async Task HandleLocationChangedAsync(string? location, CancellationToken cancellationToken = default)
        {
            if (!_authConfirmed)
            {
                return;
            }

            await PendingDownloadWorkflow.CaptureFromUriAsync(location, cancellationToken);

            if (IsAuthenticated)
            {
                await PendingDownloadWorkflow.ProcessAsync(cancellationToken);
            }
        }

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

        protected async Task ToggleAlternativeSpeedLimits()
        {
            if (_toggleAltSpeedLimitsInProgress)
            {
                return;
            }

            _toggleAltSpeedLimitsInProgress = true;
            try
            {
                var isEnabled = await StatusBarWorkflow.ToggleAlternativeSpeedLimitsAsync(_timerCancellationToken.Token);
                if (isEnabled.HasValue && MainData is not null)
                {
                    MainData.ServerState.UseAltSpeedLimits = isEnabled.Value;
                }
            }
            finally
            {
                _toggleAltSpeedLimitsInProgress = false;

                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task ShowGlobalDownloadRateLimit()
        {
            var appliedRate = await StatusBarWorkflow.ShowGlobalDownloadRateLimitAsync(MainData?.ServerState.DownloadRateLimit ?? 0, _timerCancellationToken.Token);
            if (appliedRate.HasValue && MainData is not null)
            {
                MainData.ServerState.DownloadRateLimit = appliedRate.Value;
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task ShowGlobalUploadRateLimit()
        {
            var appliedRate = await StatusBarWorkflow.ShowGlobalUploadRateLimitAsync(MainData?.ServerState.UploadRateLimit ?? 0, _timerCancellationToken.Token);
            if (appliedRate.HasValue && MainData is not null)
            {
                MainData.ServerState.UploadRateLimit = appliedRate.Value;
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

        private void OnTorrentQueryStateChanged(object? sender, TorrentQueryStateChangedEventArgs args)
        {
            if (args.ChangeKind != TorrentQueryStateChangeKind.Filter || _disposedValue)
            {
                return;
            }

            MarkTorrentsDirty();
            StateHasChanged();
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

                TorrentQueryState.Changed -= OnTorrentQueryStateChanged;
                QBittorrentPreferencesStateService.Changed -= OnQBittorrentPreferencesChanged;
            }

            _disposedValue = true;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }

        private async Task<ManagedTimerTickResult> RetryStartupTickAsync(CancellationToken cancellationToken)
        {
            var loadResult = await ShellSessionWorkflow.RecoverAsync(_requestId, cancellationToken);
            if (loadResult.Outcome == ShellSessionLoadOutcome.AuthenticationRequired)
            {
                await HandleAuthenticationFailureAsync(cancellationToken);
                return ManagedTimerTickResult.Stop;
            }

            if (loadResult.Outcome == ShellSessionLoadOutcome.LostConnection)
            {
                _startupRecoveryPending = false;
                await LostConnectionWorkflow.MarkLostConnectionAsync();
                _timerCancellationToken.CancelIfNotDisposed();
                await InvokeAsync(StateHasChanged);
                return ManagedTimerTickResult.Stop;
            }

            if (loadResult.Outcome == ShellSessionLoadOutcome.RetryableFailure)
            {
                _startupRecoveryPending = true;
                return ManagedTimerTickResult.Continue;
            }

            _authConfirmed = true;
            await PendingDownloadWorkflow.CaptureFromUriAsync(NavigationManager.Uri, cancellationToken);
            await ApplyLoadResultAsync(loadResult, cancellationToken);
            await InvokeAsync(StateHasChanged);

            return ManagedTimerTickResult.Continue;
        }

        private async Task ApplyLoadResultAsync(ShellSessionLoadResult loadResult, CancellationToken cancellationToken)
        {
            var qBittorrentPreferences = loadResult.Preferences is not null
                ? PreferencesDataManager.CreateQBittorrentPreferences(loadResult.Preferences)
                : null;
            Preferences = qBittorrentPreferences;
            QBittorrentPreferencesStateService.SetPreferences(qBittorrentPreferences);
            Version = loadResult.Version ?? string.Empty;
            MainData = loadResult.MainData;
            _startupRecoveryPending = false;
            MarkTorrentsDirty();
            _requestId = loadResult.RequestId;
            _startupUpdateCheckReady = loadResult.AppSettings is not null;
            _startupUpdateChecksEnabled = loadResult.AppSettings?.UpdateChecksEnabled ?? false;
            _startupDismissedReleaseTag = loadResult.AppSettings?.DismissedReleaseTag;

            if (MainData is not null)
            {
                await UpdateRefreshIntervalAsync(MainData.ServerState.RefreshInterval, cancellationToken);
            }

            IsAuthenticated = true;
            Menu?.ShowMenu(Preferences);
            await PendingDownloadWorkflow.ProcessAsync(cancellationToken);
        }

        private void OnQBittorrentPreferencesChanged(object? sender, QBittorrentPreferencesChangedEventArgs args)
        {
            if (_disposedValue)
            {
                return;
            }

            _preferencesChangeTask = InvokeAsync(() => ApplyQBittorrentPreferencesChangedAsync(args));
        }

        private async Task ApplyQBittorrentPreferencesChangedAsync(QBittorrentPreferencesChangedEventArgs args)
        {
            Preferences = args.CurrentPreferences;

            if (args.PreviousPreferences?.UseSubcategories != args.CurrentPreferences?.UseSubcategories)
            {
                MarkTorrentsDirty();
            }

            if (Preferences is not null)
            {
                await UpdateRefreshIntervalAsync(Preferences.RefreshInterval, _timerCancellationToken.Token);
            }

            Menu?.ShowMenu(Preferences);
            StateHasChanged();
        }
    }
}
