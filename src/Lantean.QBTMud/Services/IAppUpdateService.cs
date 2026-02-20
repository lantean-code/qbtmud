using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides qbtmud update information based on GitHub releases.
    /// </summary>
    public interface IAppUpdateService
    {
        /// <summary>
        /// Gets current update status for qbtmud.
        /// </summary>
        /// <param name="forceRefresh">A value indicating whether cached status should be bypassed.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The current update status.</returns>
        Task<AppUpdateStatus> GetUpdateStatusAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
    }
}
