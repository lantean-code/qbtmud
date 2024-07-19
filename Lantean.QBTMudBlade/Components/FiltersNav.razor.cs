using Blazored.LocalStorage;
using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using System.Collections.Generic;

namespace Lantean.QBTMudBlade.Components
{
    public partial class FiltersNav
    {
        private const string _statusSelectionStorageKey = "FiltersNav.Selection.Status";
        private const string _categorySelectionStorageKey = "FiltersNav.Selection.Category";
        private const string _tagSelectionStorageKey = "FiltersNav.Selection.Tag";
        private const string _trackerSelectionStorageKey = "FiltersNav.Selection.Tracker";

        private const string _statusType = nameof(_statusType);
        private const string _categoryType = nameof(_categoryType);
        private const string _tagType = nameof(_tagType);
        private const string _trackerType = nameof(_trackerType);

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

        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        [Inject]
        public IApiClient ApiClient { get; set; } = default!;

        [CascadingParameter]
        public MainData? MainData { get; set; }

        [CascadingParameter]
        public QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        [Parameter]
        public EventCallback<string> CategoryChanged { get; set; }

        [Parameter]
        public EventCallback<Status> StatusChanged { get; set; }

        [Parameter]
        public EventCallback<string> TagChanged { get; set; }

        [Parameter]
        public EventCallback<string> TrackerChanged { get; set; }

        protected Dictionary<string, int> Tags => GetTags();

        protected Dictionary<string, int> Categories => GetCategories();

        protected Dictionary<string, int> Trackers => GetTrackers();

        protected Dictionary<string, int> Statuses => GetStatuses();

        protected ContextMenu? StatusContextMenu { get; set; }

        protected ContextMenu? CategoryContextMenu { get; set; }

        protected ContextMenu? TagContextMenu { get; set; }

        protected ContextMenu? TrackerContextMenu { get; set; }

        protected string? ContextMenuStatus { get; set; }

        protected bool IsCategoryTarget { get; set; }

        protected string? ContextMenuCategory { get; set; }

        protected bool IsTagTarget { get; set; }

        protected string? ContextMenuTag { get; set; }

        protected string? ContextMenuTracker { get; set; }

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

        protected Task StatusOnContextMenu(MouseEventArgs args, string value)
        {
            return ShowStatusContextMenu(args, value);
        }

        protected Task StatusOnLongPress(LongPressEventArgs args, string value)
        {
            return ShowStatusContextMenu(args, value);
        }

        protected Task ShowStatusContextMenu(EventArgs args, string value)
        {
            if (StatusContextMenu is null)
            {
                return Task.CompletedTask;
            }

            ContextMenuStatus = value;

            return StatusContextMenu.OpenMenuAsync(args);
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

        protected Task CategoryOnContextMenu(MouseEventArgs args, string value)
        {
            return ShowCategoryContextMenu(args, value);
        }

        protected Task CategoryOnLongPress(LongPressEventArgs args, string value)
        {
            return ShowCategoryContextMenu(args, value);
        }

        protected Task ShowCategoryContextMenu(EventArgs args, string value)
        {
            if (CategoryContextMenu is null)
            {
                return Task.CompletedTask;
            }

            IsCategoryTarget = value != FilterHelper.CATEGORY_ALL && value != FilterHelper.CATEGORY_UNCATEGORIZED;
            ContextMenuCategory = value;

            return CategoryContextMenu.OpenMenuAsync(args);
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

        protected Task TagOnContextMenu(MouseEventArgs args, string value)
        {
            return ShowTagContextMenu(args, value);
        }

        protected Task TagOnLongPress(LongPressEventArgs args, string value)
        {
            return ShowTagContextMenu(args, value);
        }

        protected Task ShowTagContextMenu(EventArgs args, string value)
        {
            if (TagContextMenu is null)
            {
                return Task.CompletedTask;
            }

            IsTagTarget = value != FilterHelper.TAG_ALL && value != FilterHelper.TAG_UNTAGGED;
            ContextMenuTag = value;

            return TagContextMenu.OpenMenuAsync(args);
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

        protected Task TrackerOnContextMenu(MouseEventArgs args, string value)
        {
            return ShowTrackerContextMenu(args, value);
        }

        protected Task TrackerOnLongPress(LongPressEventArgs args, string value)
        {
            return ShowTrackerContextMenu(args, value);
        }

        protected Task ShowTrackerContextMenu(EventArgs args, string value)
        {
            if (TrackerContextMenu is null)
            {
                return Task.CompletedTask;
            }

            ContextMenuTracker = value;

            return TrackerContextMenu.OpenMenuAsync(args);
        }

        protected async Task AddCategory()
        {
            await DialogService.ShowAddCategoryDialog(ApiClient);
        }

        protected async Task EditCategory()
        {
            if (ContextMenuCategory is null)
            {
                return;
            }

            await DialogService.ShowEditCategoryDialog(ApiClient, ContextMenuCategory);
        }

        protected async Task RemoveCategory()
        {
            if (ContextMenuCategory is null)
            {
                return;
            }

            await ApiClient.RemoveCategories(ContextMenuCategory);

            Categories.Remove(ContextMenuCategory);
        }

        protected async Task RemoveUnusedCategories()
        {
            var removedCategories = await ApiClient.RemoveUnusedCategories();

            foreach (var removedCategory in removedCategories)
            {
                Categories.Remove(removedCategory);
            }
        }

        protected async Task AddTag()
        {
            if (ContextMenuTag is null)
            {
                return;
            }

            var tags = await DialogService.ShowAddTagsDialog();
            if (tags is null || tags.Count == 0)
            {
                return;
            }

            await ApiClient.CreateTags(tags);
        }

        protected async Task RemoveTag()
        {
            if (ContextMenuTag is null)
            {
                return;
            }

            await ApiClient.DeleteTags(ContextMenuTag);

            Tags.Remove(ContextMenuTag);
        }

        protected async Task RemoveUnusedTags()
        {
            var removedTags = await ApiClient.RemoveUnusedTags();

            foreach (var removedTag in removedTags)
            {
                Tags.Remove(removedTag);
            }
        }

        protected async Task ResumeTorrents(string type)
        {
            var torrents = GetAffectedTorrentHashes(type);

            await ApiClient.ResumeTorrents(torrents);
        }

        protected async Task PauseTorrents(string type)
        {
            var torrents = GetAffectedTorrentHashes(type);

            await ApiClient.PauseTorrents(torrents);
        }

        protected async Task RemoveTorrents(string type)
        {
            var torrents = GetAffectedTorrentHashes(type);

            await DialogService.InvokeDeleteTorrentDialog(ApiClient, [.. torrents]);
        }

        private Dictionary<string, int> GetTags()
        {
            if (MainData is null)
            {
                return [];
            }

            return MainData.TagState.ToDictionary(d => d.Key, d => d.Value.Count);
        }

        private Dictionary<string, int> GetCategories()
        {
            if (MainData is null)
            {
                return [];
            }

            return MainData.CategoriesState.ToDictionary(d => d.Key, d => d.Value.Count);
        }

        private Dictionary<string, int> GetTrackers()
        {
            if (MainData is null)
            {
                return [];
            }

            return MainData.TrackersState
                .GroupBy(d => GetHostName(d.Key))
                .Select(l => new KeyValuePair<string, int>(GetHostName(l.First().Key), l.Sum(i => i.Value.Count)))
                .ToDictionary(d => d.Key, d => d.Value);
        }

        private Dictionary<string, int> GetStatuses()
        {
            if (MainData is null)
            {
                return [];
            }

            return MainData.StatusState.ToDictionary(d => d.Key, d => d.Value.Count);
        }

        private List<string> GetAffectedTorrentHashes(string type)
        {
            if (MainData is null)
            {
                return [];
            }

            switch (type)
            {
                case _statusType:
                    if (ContextMenuStatus is null)
                    {
                        return [];
                    }

                    var status = Enum.Parse<Status>(ContextMenuStatus);

                    return MainData.Torrents.Where(t => FilterHelper.FilterStatus(t.Value, status)).Select(t => t.Value.Hash).ToList();

                case _categoryType:
                    if (ContextMenuCategory is null)
                    {
                        return [];
                    }

                    return MainData.Torrents.Where(t => FilterHelper.FilterCategory(t.Value, ContextMenuCategory, Preferences?.UseSubcategories ?? false)).Select(t => t.Value.Hash).ToList();

                case _tagType:
                    if (ContextMenuTag is null)
                    {
                        return [];
                    }

                    return MainData.Torrents.Where(t => FilterHelper.FilterTag(t.Value, ContextMenuTag)).Select(t => t.Value.Hash).ToList();

                case _trackerType:
                    if (ContextMenuTracker is null)
                    {
                        return [];
                    }

                    return MainData.Torrents.Where(t => FilterHelper.FilterTracker(t.Value, ContextMenuTracker)).Select(t => t.Value.Hash).ToList();

                default:
                    return [];
            }
        }

        private static string GetHostName(string tracker)
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