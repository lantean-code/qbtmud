using Lantean.QBTMud.Services.Localization;
using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default implementation of <see cref="IApiFeedbackWorkflow"/>.
    /// </summary>
    public sealed class ApiFeedbackWorkflow : IApiFeedbackWorkflow
    {
        private readonly ILostConnectionWorkflow _lostConnectionWorkflow;
        private readonly ISnackbarWorkflow _snackbarWorkflow;
        private readonly ILanguageLocalizer _languageLocalizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiFeedbackWorkflow"/> class.
        /// </summary>
        /// <param name="lostConnectionWorkflow">The lost-connection workflow.</param>
        /// <param name="snackbarWorkflow">The snackbar workflow.</param>
        /// <param name="languageLocalizer">The language localizer.</param>
        public ApiFeedbackWorkflow(
            ILostConnectionWorkflow lostConnectionWorkflow,
            ISnackbarWorkflow snackbarWorkflow,
            ILanguageLocalizer languageLocalizer)
        {
            _lostConnectionWorkflow = lostConnectionWorkflow;
            _snackbarWorkflow = snackbarWorkflow;
            _languageLocalizer = languageLocalizer;
        }

        /// <inheritdoc />
        public async Task<bool> ProcessResultAsync(ApiResult result, Severity severity = Severity.Error, CancellationToken cancellationToken = default)
        {
            if (result.IsSuccess)
            {
                return true;
            }

            await HandleFailureCoreAsync(result.Failure, null, severity, cancellationToken);
            return false;
        }

        /// <inheritdoc />
        public async Task HandleFailureAsync(ApiResult result, Func<string?, string>? buildMessage = null, Severity severity = Severity.Error, CancellationToken cancellationToken = default)
        {
            if (result.IsSuccess)
            {
                throw new InvalidOperationException("HandleFailureAsync must only be used with failed ApiResult instances.");
            }

            await HandleFailureCoreAsync(result.Failure, buildMessage, severity, cancellationToken);
        }

        /// <inheritdoc />
        public async Task HandleFailureAsync<T>(ApiResult<T> result, Func<string?, string>? buildMessage = null, Severity severity = Severity.Error, CancellationToken cancellationToken = default)
        {
            if (result.IsSuccess)
            {
                throw new InvalidOperationException("HandleFailureAsync must only be used with failed ApiResult instances.");
            }

            await HandleFailureCoreAsync(result.Failure, buildMessage, severity, cancellationToken);
        }

        private async Task HandleFailureCoreAsync(ApiFailure? failure, Func<string?, string>? buildMessage, Severity severity, CancellationToken cancellationToken)
        {
            if (failure.IsConnectivityFailure())
            {
                await _lostConnectionWorkflow.MarkLostConnectionAsync();
                return;
            }

            var userMessage = failure?.UserMessage;
            var message = buildMessage is not null
                ? buildMessage(userMessage)
                : GetDefaultMessage(userMessage);

            _snackbarWorkflow.ShowTransientMessage(message, severity);
        }

        private string GetDefaultMessage(string? userMessage)
        {
            if (!string.IsNullOrWhiteSpace(userMessage))
            {
                return userMessage;
            }

            return _languageLocalizer.Translate("HttpServer", "qBittorrent returned an error. Please try again.");
        }
    }
}
