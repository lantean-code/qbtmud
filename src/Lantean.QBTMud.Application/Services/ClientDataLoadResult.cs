using System.Text.Json;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Represents the outcome of a ClientData load operation.
    /// </summary>
    public sealed class ClientDataLoadResult
    {
        /// <summary>
        /// Gets a failed ClientData load result.
        /// </summary>
        public static ClientDataLoadResult Failure { get; } = new(false, null, null);

        private ClientDataLoadResult(bool succeeded, IReadOnlyDictionary<string, JsonElement>? entries, ApiResultBase? failureResult)
        {
            if (failureResult is not null && !failureResult.IsFailure)
            {
                throw new ArgumentException("FailureResult must be a failed API result.", nameof(failureResult));
            }

            Succeeded = succeeded;
            Entries = entries;
            FailureResult = failureResult;
        }

        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// Gets the loaded ClientData entries.
        /// </summary>
        public IReadOnlyDictionary<string, JsonElement>? Entries { get; }

        /// <summary>
        /// Gets the failed API result that caused the ClientData load operation to fail.
        /// </summary>
        public ApiResultBase? FailureResult { get; }

        /// <summary>
        /// Creates a successful ClientData load result.
        /// </summary>
        /// <param name="entries">The loaded ClientData entries.</param>
        /// <returns>The successful ClientData load result.</returns>
        public static ClientDataLoadResult FromEntries(IReadOnlyDictionary<string, JsonElement> entries)
        {
            return new ClientDataLoadResult(true, entries, null);
        }

        /// <summary>
        /// Creates a failed ClientData load result from a failed API result.
        /// </summary>
        /// <param name="failureResult">The failed API result.</param>
        /// <returns>The failed ClientData load result.</returns>
        public static ClientDataLoadResult FromFailure(ApiResultBase failureResult)
        {
            ArgumentNullException.ThrowIfNull(failureResult);

            return new ClientDataLoadResult(false, null, failureResult);
        }
    }
}
