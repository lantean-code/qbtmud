using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class Details
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [CascadingParameter]
        public MainData MainData { get; set; } = default!;

        [CascadingParameter]
        public QBitTorrentClient.Models.Preferences Preferences { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [Parameter]
        public string? Hash { get; set; }

        protected int ActiveTab { get; set; } = 0;

        protected int RefreshInterval => MainData?.ServerState.RefreshInterval ?? 1500;

        protected string Name => GetName();

        protected bool ShowTabs { get; set; } = true;

        private string GetName()
        {
            if (Hash is null || MainData is null)
            {
                return "";
            }

            if (!MainData.Torrents.TryGetValue(Hash, out var torrent))
            {
                return "";
            }

            return torrent.Name;
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }
    }
}