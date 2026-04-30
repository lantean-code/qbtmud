namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Coordinates status-bar command behavior.
    /// </summary>
    public interface IStatusBarWorkflow
    {
        /// <summary>
        /// Toggles alternative speed limits.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns>The resulting enabled state when known; otherwise, <see langword="null"/>.</returns>
        Task<bool?> ToggleAlternativeSpeedLimitsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Shows the global download rate dialog and applies any confirmed value.
        /// </summary>
        /// <param name="currentRateLimit">The current global download rate limit.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns>The applied rate limit, or <see langword="null"/> when no value should be applied.</returns>
        Task<int?> ShowGlobalDownloadRateLimitAsync(int currentRateLimit, CancellationToken cancellationToken = default);

        /// <summary>
        /// Shows the global upload rate dialog and applies any confirmed value.
        /// </summary>
        /// <param name="currentRateLimit">The current global upload rate limit.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns>The applied rate limit, or <see langword="null"/> when no value should be applied.</returns>
        Task<int?> ShowGlobalUploadRateLimitAsync(int currentRateLimit, CancellationToken cancellationToken = default);
    }
}
