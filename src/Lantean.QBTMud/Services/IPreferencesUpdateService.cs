using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Publishes updated application preferences to in-app subscribers.
    /// </summary>
    public interface IPreferencesUpdateService
    {
        /// <summary>
        /// Occurs when application preferences are updated.
        /// </summary>
        event PreferencesUpdatedAsyncHandler? PreferencesUpdated;

        /// <summary>
        /// Publishes updated application preferences.
        /// </summary>
        /// <param name="preferences">The updated preferences snapshot.</param>
        /// <returns>A task representing the asynchronous publish operation.</returns>
        ValueTask PublishAsync(Preferences preferences);
    }
}
