using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Components.Dialogs;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
        public MainData? MainData { get; set; }

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [Parameter]
        public string? Hash { get; set; }

        protected int ActiveTab { get; set; } = 0;

        protected int RefreshInterval => MainData?.ServerState.RefreshInterval ?? 1500;

        protected string Name => GetName();

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

        protected async Task PauseTorrent(MouseEventArgs eventArgs)
        {
            if (Hash is null)
            {
                return;
            }

            await ApiClient.PauseTorrent(Hash);
        }

        protected async Task ResumeTorrent(MouseEventArgs eventArgs)
        {
            if (Hash is null)
            {
                return;
            }

            await ApiClient.ResumeTorrent(Hash);
        }

        protected async Task RemoveTorrent(MouseEventArgs eventArgs)
        {
            if (Hash is null)
            {
                return;
            }

            var reference = await DialogService.ShowAsync<DeleteDialog>("Remove torrent(s)?");
            var result = await reference.Result;
            if (result.Canceled)
            {
                return;
            }

            await ApiClient.DeleteTorrent(Hash, (bool)result.Data);
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }
    }
}