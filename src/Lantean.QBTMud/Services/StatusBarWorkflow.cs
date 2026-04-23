using Lantean.QBTMud.Services.Localization;
using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="IStatusBarWorkflow"/>.
    /// </summary>
    public sealed class StatusBarWorkflow : IStatusBarWorkflow
    {
        private readonly IApiClient _apiClient;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly ISnackbarWorkflow _snackbarWorkflow;
        private readonly ILanguageLocalizer _languageLocalizer;
        private readonly IApiFeedbackWorkflow _apiFeedbackWorkflow;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusBarWorkflow"/> class.
        /// </summary>
        /// <param name="apiClient">The qBittorrent API client.</param>
        /// <param name="dialogWorkflow">The dialog workflow.</param>
        /// <param name="snackbarWorkflow">The snackbar workflow.</param>
        /// <param name="languageLocalizer">The language localizer.</param>
        /// <param name="apiFeedbackWorkflow">The API feedback workflow.</param>
        public StatusBarWorkflow(
            IApiClient apiClient,
            IDialogWorkflow dialogWorkflow,
            ISnackbarWorkflow snackbarWorkflow,
            ILanguageLocalizer languageLocalizer,
            IApiFeedbackWorkflow apiFeedbackWorkflow)
        {
            _apiClient = apiClient;
            _dialogWorkflow = dialogWorkflow;
            _snackbarWorkflow = snackbarWorkflow;
            _languageLocalizer = languageLocalizer;
            _apiFeedbackWorkflow = apiFeedbackWorkflow;
        }

        /// <inheritdoc />
        public async Task<bool?> ToggleAlternativeSpeedLimitsAsync(CancellationToken cancellationToken = default)
        {
            var toggleResult = await _apiClient.ToggleAlternativeSpeedLimitsAsync(cancellationToken);
            if (toggleResult.IsFailure)
            {
                await _apiFeedbackWorkflow.HandleFailureAsync(
                    toggleResult,
                    message => _languageLocalizer.Translate("AppLoggedInLayout", "Unable to toggle alternative speed limits: %1", message ?? string.Empty),
                    cancellationToken: cancellationToken);
                return null;
            }

            var isEnabledResult = await _apiClient.GetAlternativeSpeedLimitsStateAsync(cancellationToken);
            if (isEnabledResult.IsFailure)
            {
                await _apiFeedbackWorkflow.HandleFailureAsync(
                    isEnabledResult,
                    message => _languageLocalizer.Translate("AppLoggedInLayout", "Unable to toggle alternative speed limits: %1", message ?? string.Empty),
                    cancellationToken: cancellationToken);
                return null;
            }

            var isEnabled = isEnabledResult.Value;
            _snackbarWorkflow.ShowTransientMessage(BuildAlternativeSpeedLimitsStatusMessage(isEnabled), Severity.Info);
            return isEnabled;
        }

        /// <inheritdoc />
        public async Task<int?> ShowGlobalDownloadRateLimitAsync(int currentRateLimit, CancellationToken cancellationToken = default)
        {
            try
            {
                var appliedRate = await _dialogWorkflow.InvokeGlobalDownloadRateDialog(currentRateLimit);
                if (!appliedRate.HasValue)
                {
                    return null;
                }

                return checked((int)appliedRate.Value);
            }
            catch (HttpRequestException exception)
            {
                _snackbarWorkflow.ShowTransientMessage(
                    _languageLocalizer.Translate("AppLoggedInLayout", "Unable to set global download rate limit: %1", exception.Message),
                    Severity.Error);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<int?> ShowGlobalUploadRateLimitAsync(int currentRateLimit, CancellationToken cancellationToken = default)
        {
            try
            {
                var appliedRate = await _dialogWorkflow.InvokeGlobalUploadRateDialog(currentRateLimit);
                if (!appliedRate.HasValue)
                {
                    return null;
                }

                return checked((int)appliedRate.Value);
            }
            catch (HttpRequestException exception)
            {
                _snackbarWorkflow.ShowTransientMessage(
                    _languageLocalizer.Translate("AppLoggedInLayout", "Unable to set global upload rate limit: %1", exception.Message),
                    Severity.Error);
                return null;
            }
        }

        private string BuildAlternativeSpeedLimitsStatusMessage(bool isEnabled)
        {
            return _languageLocalizer.Translate(
                "MainWindow",
                isEnabled ? "Alternative speed limits: On" : "Alternative speed limits: Off");
        }
    }
}
