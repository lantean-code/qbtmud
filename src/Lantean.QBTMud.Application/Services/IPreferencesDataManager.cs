using Lantean.QBTMud.Core.Models;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Application.Services
{
    public interface IPreferencesDataManager
    {
        /// <summary>
        /// Creates runtime qBittorrent preferences from qBittorrent preferences.
        /// </summary>
        /// <param name="preferences">The qBittorrent preferences.</param>
        /// <returns>The runtime qBittorrent preferences.</returns>
        QBittorrentPreferences CreateQBittorrentPreferences(Preferences preferences);

        UpdatePreferences MergePreferences(UpdatePreferences? original, UpdatePreferences changed);
    }
}
