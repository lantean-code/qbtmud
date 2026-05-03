using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Microsoft.AspNetCore.Components;
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
        private readonly NavigationManager _navigationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiFeedbackWorkflow"/> class.
        /// </summary>
        /// <param name="lostConnectionWorkflow">The lost-connection workflow.</param>
        /// <param name="snackbarWorkflow">The snackbar workflow.</param>
        /// <param name="languageLocalizer">The language localizer.</param>
        /// <param name="navigationManager">The navigation manager.</param>
        public ApiFeedbackWorkflow(
            ILostConnectionWorkflow lostConnectionWorkflow,
            ISnackbarWorkflow snackbarWorkflow,
            ILanguageLocalizer languageLocalizer,
            NavigationManager navigationManager)
        {
            _lostConnectionWorkflow = lostConnectionWorkflow;
            _snackbarWorkflow = snackbarWorkflow;
            _languageLocalizer = languageLocalizer;
            _navigationManager = navigationManager;
        }

        /// <inheritdoc />
        public async Task<bool> ProcessResultAsync(ApiResultBase result, Severity severity = Severity.Error, CancellationToken cancellationToken = default)
        {
            if (!result.IsFailure)
            {
                return true;
            }

            await HandleFailureCoreAsync(result.Failure, null, null, severity, cancellationToken);
            return false;
        }

        /// <inheritdoc />
        public Task HandleFailureAsync(ApiResultBase result, Func<ApiFailure, ApiFeedbackCustomFailureResult> handleCustomFailure, Func<string?, string>? buildMessage = null, Severity severity = Severity.Error, CancellationToken cancellationToken = default)
        {
            return HandleFailureAsync(
                result,
                (failure, _) => Task.FromResult(handleCustomFailure(failure)),
                buildMessage,
                severity,
                cancellationToken);
        }

        /// <inheritdoc />
        public async Task HandleFailureAsync(ApiResultBase result, Func<ApiFailure, CancellationToken, Task<ApiFeedbackCustomFailureResult>> handleCustomFailure, Func<string?, string>? buildMessage = null, Severity severity = Severity.Error, CancellationToken cancellationToken = default)
        {
            if (!result.IsFailure)
            {
                throw new InvalidOperationException("HandleFailureAsync must only be used with failed ApiResultBase instances.");
            }

            await HandleFailureCoreAsync(result.Failure, handleCustomFailure, buildMessage, severity, cancellationToken);
        }

        /// <inheritdoc />
        public async Task HandleFailureAsync(ApiResultBase result, Func<string?, string>? buildMessage = null, Severity severity = Severity.Error, CancellationToken cancellationToken = default)
        {
            if (!result.IsFailure)
            {
                throw new InvalidOperationException("HandleFailureAsync must only be used with failed ApiResultBase instances.");
            }

            await HandleFailureCoreAsync(result.Failure, null, buildMessage, severity, cancellationToken);
        }

        private async Task HandleFailureCoreAsync(ApiFailure failure, Func<ApiFailure, CancellationToken, Task<ApiFeedbackCustomFailureResult>>? handleCustomFailure, Func<string?, string>? buildMessage, Severity severity, CancellationToken cancellationToken)
        {
            if (handleCustomFailure is not null)
            {
                var customFailureResult = await handleCustomFailure(failure, cancellationToken);
                if (customFailureResult == ApiFeedbackCustomFailureResult.StopHandling)
                {
                    return;
                }
            }

            if (failure.IsAuthenticationFailure())
            {
                _navigationManager.NavigateTo("login", forceLoad: true);
                return;
            }

            if (failure.IsConnectivityFailure())
            {
                await _lostConnectionWorkflow.MarkLostConnectionAsync();
                return;
            }

            var userMessage = failure.UserMessage;
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
