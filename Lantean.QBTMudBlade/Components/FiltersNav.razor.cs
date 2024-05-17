using Blazored.LocalStorage;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using static MudBlazor.Colors;

namespace Lantean.QBTMudBlade.Components
{
    public partial class FiltersNav
    {
        private const string _statusSelectionStorageKey = "FiltersNav.Selection.Status";
        private const string _categorySelectionStorageKey = "FiltersNav.Selection.Category";
        private const string _tagSelectionStorageKey = "FiltersNav.Selection.Tag";
        private const string _trackerSelectionStorageKey = "FiltersNav.Selection.Tracker";

        private bool _statusExpanded = true;
        private bool _categoriesExpanded = true;
        private bool _tagsExpanded = true;
        private bool _trackersExpanded = true;

        protected string Status { get; set; } = Models.Status.All.ToString();

        protected string Category { get; set; } = FilterHelper.CATEGORY_ALL;

        protected string Tag { get; set; } = FilterHelper.TAG_ALL;

        protected string Tracker { get; set; } = FilterHelper.TRACKER_ALL;

        [Inject]
        public ILocalStorageService LocalStorage { get; set; } = default!;

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

        protected override async Task OnInitializedAsync()
        {
            var status = await LocalStorage.GetItemAsStringAsync(_statusSelectionStorageKey);
            if (status is not null)
            {
                Status = status;
                await StatusChanged.InvokeAsync(Enum.Parse<Status>(status));
            }

            var category = await LocalStorage.GetItemAsStringAsync(_categorySelectionStorageKey);
            if (category is not null)
            {
                Category = category;
                await CategoryChanged.InvokeAsync(category);
            }


            var tag = await LocalStorage.GetItemAsStringAsync(_tagSelectionStorageKey);
            if (tag is not null)
            {
                Tag = tag;
                await TagChanged.InvokeAsync(tag);
            }
            
            var tracker = await LocalStorage.GetItemAsStringAsync(_trackerSelectionStorageKey);
            if (tracker is not null)
            {
                Tracker = tracker;
                await TrackerChanged.InvokeAsync(tracker);
            }
        }

        protected async Task StatusValueChanged(string value)
        {
            Status = value;
            await StatusChanged.InvokeAsync(Enum.Parse<Status>(value));

            if (value != Models.Status.All.ToString())
            {
                await LocalStorage.SetItemAsStringAsync(_statusSelectionStorageKey, value);
            }
            else
            {
                await LocalStorage.RemoveItemAsync(_statusSelectionStorageKey);
            }
        }

        protected async Task CategoryValueChanged(string value)
        {
            Category = value;
            await CategoryChanged.InvokeAsync(value);

            if (value != FilterHelper.CATEGORY_ALL)
            {
                await LocalStorage.SetItemAsStringAsync(_categorySelectionStorageKey, value);
            }
            else
            {
                await LocalStorage.RemoveItemAsync(_categorySelectionStorageKey);
            }
        }

        protected async Task TagValueChanged(string value)
        {
            Tag = value;
            await TagChanged.InvokeAsync(value);

            if (value != FilterHelper.TAG_ALL)
            {
                await LocalStorage.SetItemAsStringAsync(_tagSelectionStorageKey, value);
            }
            else
            {
                await LocalStorage.RemoveItemAsync(_tagSelectionStorageKey);
            }
        }

        protected async Task TrackerValueChanged(string value)
        {
            Tracker = value;
            await TrackerChanged.InvokeAsync(value);

            if (value != FilterHelper.TRACKER_ALL)
            {
                await LocalStorage.SetItemAsStringAsync(_trackerSelectionStorageKey, value);
            }
            else
            {
                await LocalStorage.RemoveItemAsync(_trackerSelectionStorageKey);
            }
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