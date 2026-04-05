using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Services
{
    public interface IPreferencesDataManager
    {
        UpdatePreferences MergePreferences(UpdatePreferences? original, UpdatePreferences changed);
    }
}
