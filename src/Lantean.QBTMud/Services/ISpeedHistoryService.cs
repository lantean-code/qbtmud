using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides access to historical transfer speed data with local persistence.
    /// </summary>
    public interface ISpeedHistoryService
    {
        /// <summary>
        /// Loads persisted history into memory.
        /// </summary>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Records a new transfer speed sample.
        /// </summary>
        /// <param name="timestampUtc">The sample timestamp in UTC.</param>
        /// <param name="downloadBytesPerSecond">Download rate in bytes per second.</param>
        /// <param name="uploadBytesPerSecond">Upload rate in bytes per second.</param>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        Task PushSampleAsync(DateTime timestampUtc, long downloadBytesPerSecond, long uploadBytesPerSecond, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists the current in-memory history to local storage.
        /// </summary>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        /// <param name="timestampUtc">Optional timestamp to record as the persistence time.</param>
        Task PersistAsync(CancellationToken cancellationToken = default, DateTime? timestampUtc = null);

        /// <summary>
        /// Clears all in-memory and persisted history.
        /// </summary>
        /// <param name="cancellationToken">Token that signals cancellation.</param>
        Task ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the aggregated series for the given period and direction.
        /// </summary>
        /// <param name="period">The time window to return.</param>
        /// <param name="direction">Whether to return download or upload values.</param>
        /// <returns>Ordered speed points for the requested series.</returns>
        IReadOnlyList<SpeedPoint> GetSeries(SpeedPeriod period, SpeedDirection direction);

        /// <summary>
        /// The timestamp of the most recent recorded sample.
        /// </summary>
        DateTime? LastUpdatedUtc { get; }
    }
}
