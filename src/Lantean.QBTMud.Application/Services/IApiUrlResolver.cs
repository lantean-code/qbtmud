namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Resolves URLs for the configured qBittorrent Web API endpoint.
    /// </summary>
    public interface IApiUrlResolver
    {
        /// <summary>
        /// Gets the normalized qBittorrent Web API base address.
        /// </summary>
        Uri ApiBaseAddress { get; }

        /// <summary>
        /// Builds an absolute URL for a path relative to the qBittorrent Web API base address.
        /// </summary>
        /// <param name="relativePath">The relative API path and optional query string.</param>
        /// <returns>The absolute API URL.</returns>
        string BuildAbsoluteUrl(string relativePath);
    }
}
