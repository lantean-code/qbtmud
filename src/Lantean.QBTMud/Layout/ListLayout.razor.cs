using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Layout
{
    public partial class ListLayout
    {
        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter(Name = "StatusChanged")]
        public EventCallback<Status> StatusChanged { get; set; }

        [CascadingParameter(Name = "CategoryChanged")]
        public EventCallback<string> CategoryChanged { get; set; }

        [CascadingParameter(Name = "TagChanged")]
        public EventCallback<string> TagChanged { get; set; }

        [CascadingParameter(Name = "TrackerChanged")]
        public EventCallback<string> TrackerChanged { get; set; }

        [CascadingParameter(Name = "SearchTermChanged")]
        public EventCallback<FilterSearchState> SearchTermChanged { get; set; }
    }
}