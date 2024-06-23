using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class Rss
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

        protected ServerState? ServerState => MainData?.ServerState;

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }
    }
}