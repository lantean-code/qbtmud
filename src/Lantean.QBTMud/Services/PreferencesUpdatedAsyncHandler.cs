using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Represents an asynchronous callback invoked when application preferences are updated.
    /// </summary>
    /// <param name="preferences">The updated preferences snapshot.</param>
    /// <returns>A task representing the asynchronous callback operation.</returns>
    public delegate ValueTask PreferencesUpdatedAsyncHandler(Preferences preferences);
}
