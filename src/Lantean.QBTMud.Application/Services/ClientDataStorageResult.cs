using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Represents the outcome of a ClientData storage operation.
    /// </summary>
    public sealed class ClientDataStorageResult
    {
        /// <summary>
        /// Gets a successful ClientData storage result.
        /// </summary>
        public static ClientDataStorageResult Success { get; } = new(true, null);

        /// <summary>
        /// Gets a failed ClientData storage result.
        /// </summary>
        public static ClientDataStorageResult Failure { get; } = new(false, null);

        private ClientDataStorageResult(bool succeeded, ApiResultBase? failureResult)
        {
            if (failureResult is not null && !failureResult.IsFailure)
            {
                throw new ArgumentException("FailureResult must be a failed API result.", nameof(failureResult));
            }

            Succeeded = succeeded;
            FailureResult = failureResult;
        }

        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Gets the failed API result that caused the ClientData storage operation to fail.
        /// </summary>
        public ApiResultBase? FailureResult { get; }

        /// <summary>
        /// Creates a failed ClientData storage result from a failed API result.
        /// </summary>
        /// <param name="failureResult">The failed API result.</param>
        /// <returns>The failed ClientData storage result.</returns>
        public static ClientDataStorageResult FromFailure(ApiResultBase failureResult)
        {
            ArgumentNullException.ThrowIfNull(failureResult);

            return new ClientDataStorageResult(false, failureResult);
        }
    }
}
