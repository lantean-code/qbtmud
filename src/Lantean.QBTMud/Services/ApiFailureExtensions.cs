using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides helper methods for broad UI decisions based on structured API failures.
    /// </summary>
    public static class ApiFailureExtensions
    {
        /// <summary>
        /// Gets a value indicating whether the failure represents authentication loss.
        /// </summary>
        /// <param name="failure">The failure to inspect.</param>
        /// <returns><see langword="true" /> when the failure represents authentication loss; otherwise, <see langword="false" />.</returns>
        public static bool IsAuthenticationFailure(this ApiFailure? failure)
        {
            return failure?.Kind == ApiFailureKind.AuthenticationRequired;
        }

        /// <summary>
        /// Gets a value indicating whether the failure represents lost connectivity to qBittorrent.
        /// </summary>
        /// <param name="failure">The failure to inspect.</param>
        /// <returns><see langword="true" /> when the failure represents lost connectivity; otherwise, <see langword="false" />.</returns>
        public static bool IsConnectivityFailure(this ApiFailure? failure)
        {
            return failure?.Kind is ApiFailureKind.NoResponse or ApiFailureKind.Timeout;
        }
    }
}
