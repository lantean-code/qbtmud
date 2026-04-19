using MudBlazor;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides shared feedback handling for failed API results.
    /// </summary>
    public interface IApiFeedbackWorkflow
    {
        /// <summary>
        /// Processes a command-style API result and triggers failure feedback when needed.
        /// </summary>
        /// <param name="result">The result to inspect.</param>
        /// <param name="severity">The snackbar severity for non-connectivity failures.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns><see langword="true"/> when <paramref name="result"/> is successful; otherwise, <see langword="false"/> after failure feedback has been processed.</returns>
        Task<bool> ProcessResultAsync(ApiResult result, Severity severity = Severity.Error, CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles feedback for a failed command-style API result.
        /// </summary>
        /// <param name="result">The failed result.</param>
        /// <param name="buildMessage">An optional custom message builder that receives the API user message.</param>
        /// <param name="severity">The snackbar severity for non-connectivity failures.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task HandleFailureAsync(ApiResult result, Func<string?, string>? buildMessage = null, Severity severity = Severity.Error, CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles feedback for a failed value-returning API result.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="result">The failed result.</param>
        /// <param name="buildMessage">An optional custom message builder that receives the API user message.</param>
        /// <param name="severity">The snackbar severity for non-connectivity failures.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        Task HandleFailureAsync<T>(ApiResult<T> result, Func<string?, string>? buildMessage = null, Severity severity = Severity.Error, CancellationToken cancellationToken = default) where T : notnull;
    }
}
