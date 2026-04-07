namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Coordinates pending download capture, persistence, and processing.
    /// </summary>
    public interface IPendingDownloadWorkflow
    {
        /// <summary>
        /// Restores persisted pending download state.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        Task RestoreAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Captures a supported pending download from a location URI.
        /// </summary>
        /// <param name="uri">The location URI.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        Task CaptureFromUriAsync(string? uri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes any captured pending download.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        Task ProcessAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears any captured pending download.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        Task ClearAsync(CancellationToken cancellationToken = default);
    }
}
