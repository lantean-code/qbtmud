using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components
{
    public partial class FiltersNav
    {
        private bool _statusExpanded = true;
        private bool _categoriesExpanded = true;
        private bool _tagsExpanded = true;
        private bool _trackersExpanded = true;

        protected string Status { get; set; } = Models.Status.All.ToString();

        protected string Category { get; set; } = FilterHelper.CATEGORY_ALL;

        protected string Tag { get; set; } = FilterHelper.TAG_ALL;

        protected string Tracker { get; set; } = FilterHelper.TRACKER_ALL;

        [CascadingParameter]
        public MainData? MainData { get; set; }

        [Parameter]
        public EventCallback<string> CategoryChanged { get; set; }

        [Parameter]
        public EventCallback<Status> StatusChanged { get; set; }

        [Parameter]
        public EventCallback<string> TagChanged { get; set; }

        [Parameter]
        public EventCallback<string> TrackerChanged { get; set; }

        public Dictionary<string, int> Tags => MainData?.TagState.ToDictionary(d => d.Key, d => d.Value.Count) ?? [];

        public Dictionary<string, int> Categories => MainData?.CategoriesState.ToDictionary(d => d.Key, d => d.Value.Count) ?? [];

        public Dictionary<string, int> Trackers => MainData?.TrackersState.ToDictionary(d => d.Key, d => d.Value.Count) ?? [];

        public Dictionary<string, int> Statuses => MainData?.StatusState.ToDictionary(d => d.Key, d => d.Value.Count) ?? [];

        protected async Task StatusValueChanged(string value)
        {
            Status = value;
            await StatusChanged.InvokeAsync(Enum.Parse<Status>(value));
        }

        protected async Task CategoryValueChanged(string value)
        {
            Category = value;
            await CategoryChanged.InvokeAsync(value);
        }

        protected async Task TagValueChanged(string value)
        {
            Tag = value;
            await TagChanged.InvokeAsync(value);
        }

        protected async Task TrackerValueChanged(string value)
        {
            Tracker = value;
            await TrackerChanged.InvokeAsync(value);
        }

        protected static string GetHostName(string tracker)
        {
            try
            {
                var uri = new Uri(tracker);
                return uri.Host;
            }
            catch
            {
                return tracker;
            }
        }

        
    }
}