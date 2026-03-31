namespace Lantean.QBTMud.Services
{
    public interface IPreferencesDataManager
    {
        QBittorrent.ApiClient.Models.UpdatePreferences MergePreferences(QBittorrent.ApiClient.Models.UpdatePreferences? original, QBittorrent.ApiClient.Models.UpdatePreferences changed);
    }
}