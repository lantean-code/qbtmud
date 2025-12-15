using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Pages
{
    public partial class Statistics
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

        [CascadingParameter(Name = "RefreshInterval")]
        public int RefreshInterval { get; set; }

        [Parameter]
        public string? Hash { get; set; }

        protected int ActiveTab { get; set; } = 0;

        protected ServerState? ServerState => MainData?.ServerState;

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }
    }
}