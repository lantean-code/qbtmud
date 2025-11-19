using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Helpers;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Components
{
    public partial class Menu
    {
        private bool _isVisible = false;

        private Preferences? _preferences;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        protected Preferences? Preferences => _preferences;

        public void ShowMenu(Preferences? preferences = null)
        {
            _isVisible = true;
            _preferences = preferences;

            StateHasChanged();
        }

        protected async Task ResetWebUI()
        {
            var preferences = new UpdatePreferences
            {
                AlternativeWebuiEnabled = false,
            };

            await ApiClient.SetApplicationPreferences(preferences);

            NavigationManager.NavigateTo("./", true);
        }

        protected async Task Logout()
        {
            await DialogWorkflow.ShowConfirmDialog("Logout?", "Are you sure you want to logout?", async () =>
            {
                await ApiClient.Logout();

                NavigationManager.NavigateTo("./login", true);
            });
        }

        protected async Task Exit()
        {
            await DialogWorkflow.ShowConfirmDialog("Quit?", "Are you sure you want to exit qBittorrent?", ApiClient.Shutdown);
        }
    }
}
