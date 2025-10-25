using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Layout
{
    public partial class DetailsLayout
    {
        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter]
        public IEnumerable<Torrent>? Torrents { get; set; }

        [CascadingParameter(Name = "SortColumn")]
        public string? SortColumn { get; set; }

        [CascadingParameter(Name = "SortDirection")]
        public SortDirection SortDirection { get; set; }

        protected string? SelectedTorrent { get; set; }

        protected override void OnParametersSet()
        {
            if (Body?.Target is not RouteView routeView || routeView.RouteData.RouteValues is null)
            {
                return;
            }

            if (routeView.RouteData.RouteValues.TryGetValue("hash", out var hash))
            {
                SelectedTorrent = hash?.ToString();
            }
        }
    }
}