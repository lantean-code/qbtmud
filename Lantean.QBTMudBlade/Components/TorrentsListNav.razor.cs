using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMudBlade.Components
{
    public partial class TorrentsListNav
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Parameter]
        public IEnumerable<Torrent>? Torrents { get; set; }

        [Parameter]
        public string? SelectedTorrent { get; set; }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }
    }
}