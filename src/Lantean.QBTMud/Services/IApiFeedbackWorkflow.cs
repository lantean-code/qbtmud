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
        /// Handles feedback for a command-style API result when it has failed.
        /// </summary>
        /// <param name="result">The result to inspect.</param>
        /// <param name="severity">The snackbar severity for non-connectivity failures.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns><see langword="true"/> when the failure was handled; otherwise, <see langword="false"/>.</returns>
        Task<bool> HandleIfFailureAsync(ApiResult result, Severity severity = Severity.Error, CancellationToken cancellationToken = default);

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
        Task HandleFailureAsync<T>(ApiResult<T> result, Func<string?, string>? buildMessage = null, Severity severity = Severity.Error, CancellationToken cancellationToken = default);
    }
}
