using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides persistent qbtmud-specific application settings.
    /// </summary>
    public interface IAppSettingsService
    {
        /// <summary>
        /// Gets the current qbtmud app settings.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The current settings.</returns>
        Task<AppSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists qbtmud app settings.
        /// </summary>
        /// <param name="settings">The settings to persist.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The persisted settings.</returns>
        Task<AppSettings> SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists the dismissed release tag value.
        /// </summary>
        /// <param name="tagName">The dismissed release tag.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns>The updated settings.</returns>
        Task<AppSettings> SaveDismissedReleaseTagAsync(string? tagName, CancellationToken cancellationToken = default);
    }
}
