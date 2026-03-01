using Lantean.QBitTorrentClient.Models;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Components
{
    public partial class Menu
    {
        private bool _isVisible = false;

        private Preferences? _preferences;

        protected Preferences? Preferences => _preferences;

        public void ShowMenu(Preferences? preferences = null)
        {
            _isVisible = true;
            _preferences = preferences;

            StateHasChanged();
        }
    }
}
