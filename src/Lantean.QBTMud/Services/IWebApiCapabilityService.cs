using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides cached Web API capability information for the authenticated session.
    /// </summary>
    public interface IWebApiCapabilityService
    {
        /// <summary>
        /// Gets the cached capability state, loading it once when needed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The Web API capability state.</returns>
        Task<WebApiCapabilityState> GetCapabilityStateAsync(CancellationToken cancellationToken = default);
    }
}
