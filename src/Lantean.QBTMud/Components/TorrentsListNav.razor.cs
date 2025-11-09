using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components
{
    public partial class TorrentsListNav
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Parameter]
        public IEnumerable<Torrent>? Torrents { get; set; }

        [Parameter]
        public string? SelectedTorrent { get; set; }

        [Parameter]
        public SortDirection SortDirection { get; set; }

        [Parameter]
        public string? SortColumn { get; set; }

        protected IEnumerable<Torrent>? OrderedTorrents => GetOrderedTorrents();

        private IEnumerable<Torrent>? GetOrderedTorrents()
        {
            if (Torrents is null)
            {
                return null;
            }

            var sortSelector = TorrentList.ColumnsDefinitions.Find(t => t.Id == SortColumn)?.SortSelector ?? (t => t.Name);

            return Torrents.OrderByDirection(SortDirection, sortSelector);
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }
    }
}
