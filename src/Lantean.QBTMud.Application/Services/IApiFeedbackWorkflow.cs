using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides shared feedback handling for failed API results.
    /// </summary>
    public interface IApiFeedbackWorkflow
    {
        /// <summary>
        /// Processes an API result and triggers failure feedback when needed.
        /// </summary>
        /// <param name="result">The result to inspect.</param>
        /// <param name="severity">The snackbar severity for non-connectivity failures.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns><see langword="true"/> when <paramref name="result"/> is successful; otherwise, <see langword="false"/> after failure feedback has been processed.</returns>
        Task<bool> ProcessResultAsync(ApiResultBase result, Severity severity = Severity.Error, CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles feedback for a failed API result and optionally lets the caller intercept non-standard failures.
        /// </summary>
        /// <param name="result">The failed result.</param>
        /// <param name="handleCustomFailure">A synchronous callback that runs before the shared workflow handling and indicates whether workflow processing should continue.</param>
        /// <param name="buildMessage">An optional custom message builder that receives the API user message when generic feedback is used.</param>
        /// <param name="severity">The snackbar severity for non-connectivity failures.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task HandleFailureAsync(ApiResultBase result, Func<ApiFailure, ApiFeedbackCustomFailureResult> handleCustomFailure, Func<string?, string>? buildMessage = null, Severity severity = Severity.Error, CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles feedback for a failed API result and optionally lets the caller intercept non-standard failures.
        /// </summary>
        /// <param name="result">The failed result.</param>
        /// <param name="handleCustomFailure">A callback that runs before the shared workflow handling and indicates whether workflow processing should continue.</param>
        /// <param name="buildMessage">An optional custom message builder that receives the API user message when generic feedback is used.</param>
        /// <param name="severity">The snackbar severity for non-connectivity failures.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task HandleFailureAsync(ApiResultBase result, Func<ApiFailure, CancellationToken, Task<ApiFeedbackCustomFailureResult>> handleCustomFailure, Func<string?, string>? buildMessage = null, Severity severity = Severity.Error, CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles feedback for a failed API result.
        /// </summary>
        /// <param name="result">The failed result.</param>
        /// <param name="buildMessage">An optional custom message builder that receives the API user message.</param>
        /// <param name="severity">The snackbar severity for non-connectivity failures.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task HandleFailureAsync(ApiResultBase result, Func<string?, string>? buildMessage = null, Severity severity = Severity.Error, CancellationToken cancellationToken = default);
    }
}
