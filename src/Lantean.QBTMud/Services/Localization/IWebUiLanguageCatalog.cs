using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Provides access to available WebUI languages.
    /// </summary>
    public interface IWebUiLanguageCatalog
    {
        /// <summary>
        /// Gets the available WebUI languages.
        /// </summary>
        IReadOnlyList<WebUiLanguageCatalogItem> Languages { get; }

        /// <summary>
        /// Ensures the language catalog has been loaded.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
        Task EnsureInitialized(CancellationToken cancellationToken = default);
    }
}
