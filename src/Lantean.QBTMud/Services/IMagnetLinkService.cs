namespace Lantean.QBTMud.Services
{
    public interface IMagnetLinkService
    {
        /// <summary>
        /// Registers qBittorrent WebUI as the browser magnet handler.
        /// </summary>
        /// <param name="handlerName">The handler name shown by the browser.</param>
        /// <returns>The browser registration result.</returns>
        Task<MagnetHandlerRegistrationResult> RegisterHandler(string handlerName);

        /// <summary>
        /// Extracts a supported download link from an application URL.
        /// </summary>
        /// <param name="uri">The absolute application URL.</param>
        /// <returns>The decoded download link when present and supported; otherwise <see langword="null"/>.</returns>
        string? ExtractDownloadLink(string? uri);

        /// <summary>
        /// Determines whether the provided download link is supported.
        /// </summary>
        /// <param name="value">The download link to validate.</param>
        /// <returns><see langword="true"/> when the link is supported; otherwise <see langword="false"/>.</returns>
        bool IsSupportedDownloadLink(string? value);
    }
}
