using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Components
{
    public partial class Menu
    {
        private bool _isVisible = false;

        private QBittorrentPreferences? _preferences;

        protected QBittorrentPreferences? Preferences => _preferences;

        public void ShowMenu(QBittorrentPreferences? preferences = null)
        {
            _isVisible = true;
            _preferences = preferences;

            StateHasChanged();
        }
    }
}
