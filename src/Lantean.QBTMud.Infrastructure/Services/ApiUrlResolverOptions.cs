namespace Lantean.QBTMud.Infrastructure.Services
{
    /// <summary>
    /// Provides configuration for <see cref="ApiUrlResolver"/>.
    /// </summary>
    public sealed class ApiUrlResolverOptions
    {
        /// <summary>
        /// Gets the qBittorrent Web API base address.
        /// </summary>
        public Uri? ApiBaseAddress { get; set; }
    }
}
