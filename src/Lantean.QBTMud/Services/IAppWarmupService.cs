using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Coordinates application warmup tasks (loading catalogs and cached state) and captures any startup failures.
    /// </summary>
    public interface IAppWarmupService
    {
        /// <summary>
        /// Gets a value indicating whether warmup has completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Gets the warmup failures captured during the most recent run.
        /// </summary>
        IReadOnlyList<AppWarmupFailure> Failures { get; }

        /// <summary>
        /// Runs warmup tasks. The method is idempotent and safe to call concurrently.
        /// </summary>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task WarmupAsync(CancellationToken cancellationToken = default);
    }
}
