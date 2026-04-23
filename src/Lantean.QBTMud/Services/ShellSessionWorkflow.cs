using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;
using ClientMainData = QBittorrent.ApiClient.Models.MainData;
using MudMainData = Lantean.QBTMud.Models.MainData;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="IShellSessionWorkflow"/>.
    /// </summary>
    public sealed class ShellSessionWorkflow : IShellSessionWorkflow
    {
        private const string _startupApiErrorSnackbarKey = "logged-in-layout-startup-api-error";
        private const string _refreshApiErrorSnackbarKey = "logged-in-layout-refresh-api-error";

        private readonly IApiClient _apiClient;
        private readonly ITorrentDataManager _dataManager;
        private readonly ISnackbarWorkflow _snackbarWorkflow;
        private readonly ISettingsStorageService _settingsStorageService;
        private readonly ILanguageLocalizer _languageLocalizer;
        private readonly ISpeedHistoryService _speedHistoryService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly ITorrentCompletionNotificationService _torrentCompletionNotificationService;
        private readonly NavigationManager _navigationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellSessionWorkflow"/> class.
        /// </summary>
        /// <param name="apiClient">The qBittorrent API client.</param>
        /// <param name="dataManager">The torrent data manager.</param>
        /// <param name="snackbarWorkflow">The snackbar workflow.</param>
        /// <param name="settingsStorageService">The settings storage service.</param>
        /// <param name="languageLocalizer">The language localizer.</param>
        /// <param name="speedHistoryService">The speed history service.</param>
        /// <param name="appSettingsService">The app settings service.</param>
        /// <param name="torrentCompletionNotificationService">The torrent completion notification service.</param>
        /// <param name="navigationManager">The navigation manager.</param>
        public ShellSessionWorkflow(
            IApiClient apiClient,
            ITorrentDataManager dataManager,
            ISnackbarWorkflow snackbarWorkflow,
            ISettingsStorageService settingsStorageService,
            ILanguageLocalizer languageLocalizer,
            ISpeedHistoryService speedHistoryService,
            IAppSettingsService appSettingsService,
            ITorrentCompletionNotificationService torrentCompletionNotificationService,
            NavigationManager navigationManager)
        {
            _apiClient = apiClient;
            _dataManager = dataManager;
            _snackbarWorkflow = snackbarWorkflow;
            _settingsStorageService = settingsStorageService;
            _languageLocalizer = languageLocalizer;
            _speedHistoryService = speedHistoryService;
            _appSettingsService = appSettingsService;
            _torrentCompletionNotificationService = torrentCompletionNotificationService;
            _navigationManager = navigationManager;
        }

        /// <inheritdoc />
        public Task<ShellSessionLoadResult> LoadAsync(CancellationToken cancellationToken = default)
        {
            return LoadCoreAsync(0, "startup", cancellationToken);
        }

        /// <inheritdoc />
        public Task<ShellSessionLoadResult> RecoverAsync(int requestId, CancellationToken cancellationToken = default)
        {
            return LoadCoreAsync(requestId, "startup-recovery", cancellationToken);
        }

        /// <inheritdoc />
        public async Task<ShellSessionRefreshResult> RefreshAsync(int requestId, MudMainData? currentMainData, CancellationToken cancellationToken = default)
        {
            var dataResult = await _apiClient.GetMainDataAsync(requestId, cancellationToken);
            if (dataResult.IsFailure)
            {
                return HandleRefreshFailure(dataResult.Failure!);
            }

            var data = dataResult.Value;
            var shouldRender = false;
            var torrentsDirty = false;
            IReadOnlyList<TorrentTransition> transitionBatch = Array.Empty<TorrentTransition>();
            MudMainData? mainData = currentMainData;

            if (mainData is null || data.FullUpdate)
            {
                mainData = _dataManager.CreateMainData(data);
                shouldRender = true;
                torrentsDirty = true;
            }
            else
            {
                var dataChanged = _dataManager.MergeMainData(data, mainData, out var filterChanged, out transitionBatch);
                torrentsDirty = filterChanged;
                shouldRender = dataChanged;
            }

            if (mainData is not null)
            {
                await _speedHistoryService.PushSampleAsync(
                    DateTime.UtcNow,
                    mainData.ServerState.DownloadInfoSpeed,
                    mainData.ServerState.UploadInfoSpeed,
                    cancellationToken);
                await TryProcessTorrentNotificationsAsync(transitionBatch, cancellationToken);
            }

            return new ShellSessionRefreshResult(
                shouldRender || torrentsDirty ? ShellSessionRefreshOutcome.Updated : ShellSessionRefreshOutcome.NoChange,
                mainData,
                data.ResponseId,
                shouldRender,
                torrentsDirty);
        }

        private async Task<ShellSessionLoadResult> LoadCoreAsync(int requestId, string operation, CancellationToken cancellationToken)
        {
            var authStateResult = await _apiClient.CheckAuthStateAsync(cancellationToken);
            if (authStateResult.IsFailure)
            {
                return HandleLoadFailure(authStateResult.Failure!);
            }

            var isAuthenticated = authStateResult.Value;
            if (!isAuthenticated)
            {
                return new ShellSessionLoadResult(ShellSessionLoadOutcome.AuthenticationRequired);
            }

            var appSettingsTask = _appSettingsService.RefreshSettingsAsync(cancellationToken);
            var preferencesResultTask = _apiClient.GetApplicationPreferencesAsync(cancellationToken);
            var versionResultTask = _apiClient.GetApplicationVersionAsync(cancellationToken);
            var mainDataResultTask = _apiClient.GetMainDataAsync(requestId, cancellationToken);

            try
            {
                await Task.WhenAll(appSettingsTask, preferencesResultTask, versionResultTask, mainDataResultTask);
            }
            catch (Exception exception)
            {
                return HandleLoadFailure(
                    new ApiFailure
                    {
                        Kind = ApiFailureKind.UnexpectedResponse,
                        Operation = operation,
                        UserMessage = exception.Message,
                        Detail = exception.Message
                    });
            }

            Preferences? preferences = null;
            string? version = null;
            ClientMainData? data = null;
            if (preferencesResultTask.Result.IsFailure
                || versionResultTask.Result.IsFailure
                || mainDataResultTask.Result.IsFailure)
            {
                var failure = preferencesResultTask.Result.Failure ?? versionResultTask.Result.Failure ?? mainDataResultTask.Result.Failure;
                if (failure is not null)
                {
                    return HandleLoadFailure(failure);
                }
            }

            preferences = preferencesResultTask.Result.Value;
            version = versionResultTask.Result.Value;
            data = mainDataResultTask.Result.Value;

            await SynchronizeLocalePreferenceAsync(preferences);

            var mainData = _dataManager.CreateMainData(data!);
            await _speedHistoryService.InitializeAsync(cancellationToken);
            await _speedHistoryService.PushSampleAsync(
                DateTime.UtcNow,
                mainData.ServerState.DownloadInfoSpeed,
                mainData.ServerState.UploadInfoSpeed,
                cancellationToken);

            var appSettings = await appSettingsTask ?? await _appSettingsService.GetSettingsAsync(cancellationToken);

            return new ShellSessionLoadResult(
                ShellSessionLoadOutcome.Ready,
                appSettings,
                preferences,
                version ?? string.Empty,
                mainData,
                data!.ResponseId);
        }

        private ShellSessionLoadResult HandleLoadFailure(ApiFailure failure)
        {
            if (failure.IsAuthenticationFailure())
            {
                return new ShellSessionLoadResult(ShellSessionLoadOutcome.AuthenticationRequired);
            }

            if (failure.IsConnectivityFailure())
            {
                return new ShellSessionLoadResult(ShellSessionLoadOutcome.LostConnection);
            }

            _snackbarWorkflow.ShowLocalizedMessage(
                "AppConnectivity",
                "qBittorrent returned an error. Please try again.",
                Severity.Error,
                configure: null,
                key: _startupApiErrorSnackbarKey);

            return new ShellSessionLoadResult(ShellSessionLoadOutcome.RetryableFailure);
        }

        private ShellSessionRefreshResult HandleRefreshFailure(ApiFailure failure)
        {
            if (failure.IsAuthenticationFailure())
            {
                return new ShellSessionRefreshResult(ShellSessionRefreshOutcome.AuthenticationRequired);
            }

            if (failure.IsConnectivityFailure())
            {
                return new ShellSessionRefreshResult(ShellSessionRefreshOutcome.LostConnection);
            }

            _snackbarWorkflow.ShowLocalizedMessage(
                "AppConnectivity",
                "qBittorrent returned an error. Please try again.",
                Severity.Error,
                configure: null,
                key: _refreshApiErrorSnackbarKey);

            return new ShellSessionRefreshResult(ShellSessionRefreshOutcome.RetryableFailure);
        }

        private async Task SynchronizeLocalePreferenceAsync(Preferences? preferences)
        {
            if (preferences is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(preferences.Locale))
            {
                var storedLocaleWhenApiMissing = await _settingsStorageService.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale);
                if (!string.IsNullOrWhiteSpace(storedLocaleWhenApiMissing))
                {
                    await _settingsStorageService.RemoveItemAsync(LanguageStorageKeys.PreferredLocale);
                }

                return;
            }

            var apiLocale = WebUiLocaleNormalizer.Normalize(preferences.Locale);
            var storedLocale = await _settingsStorageService.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale);

            if (string.IsNullOrWhiteSpace(storedLocale))
            {
                await _settingsStorageService.SetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, apiLocale);
                return;
            }

            var normalizedStoredLocale = WebUiLocaleNormalizer.Normalize(storedLocale);
            if (!string.Equals(storedLocale.Trim(), normalizedStoredLocale, StringComparison.Ordinal))
            {
                await _settingsStorageService.SetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, normalizedStoredLocale);
            }

            if (string.Equals(normalizedStoredLocale, apiLocale, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await _settingsStorageService.SetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, apiLocale);
            _snackbarWorkflow.ShowActionMessage(
                _languageLocalizer.Translate("AppLocalization", "Language preference changed on server. Click Reload to apply it."),
                Severity.Warning,
                _languageLocalizer.Translate("AppLocalization", "Reload"),
                _ =>
                {
                    _navigationManager.NavigateToHome(forceLoad: true);
                    return Task.CompletedTask;
                },
                configure: options =>
                {
                    options.CloseAfterNavigation = true;
                });
        }

        private async Task TryProcessTorrentNotificationsAsync(IReadOnlyList<TorrentTransition> transitions, CancellationToken cancellationToken)
        {
            if (transitions.Count == 0)
            {
                return;
            }

            try
            {
                await _torrentCompletionNotificationService.ProcessTransitionsAsync(transitions, cancellationToken);
            }
            catch (OperationCanceledException exception) when (exception.CancellationToken == cancellationToken)
            {
                throw;
            }
            catch (Exception)
            {
            }
        }
    }
}
