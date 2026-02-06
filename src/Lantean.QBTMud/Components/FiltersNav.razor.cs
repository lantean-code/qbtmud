using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Components
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
        private readonly Dictionary<string, TrackerFilterItem> _trackerItems = new(StringComparer.Ordinal);

        protected string Status { get; set; } = Models.Status.All.ToString();

        protected string Category { get; set; } = FilterHelper.CATEGORY_ALL;

        protected string Tag { get; set; } = FilterHelper.TAG_ALL;

        protected string Tracker { get; set; } = FilterHelper.TRACKER_ALL;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

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

        private IReadOnlyList<TrackerFilterItem> Trackers => GetTrackers();

        protected Dictionary<string, int> Statuses => GetStatuses();

        protected MudMenu? StatusContextMenu { get; set; }

        protected MudMenu? CategoryContextMenu { get; set; }

        protected MudMenu? TagContextMenu { get; set; }

        protected MudMenu? TrackerContextMenu { get; set; }

        protected string? ContextMenuStatus { get; set; }

        protected bool IsCategoryTarget { get; set; }

        protected string? ContextMenuCategory { get; set; }

        protected bool IsTagTarget { get; set; }

        protected string? ContextMenuTag { get; set; }

        protected string? ContextMenuTracker { get; set; }

        private bool CanRemoveTracker => ContextMenuTracker is not null
            && _trackerItems.TryGetValue(ContextMenuTracker, out var trackerItem)
            && !trackerItem.IsSynthetic
            && trackerItem.Urls.Count > 0;

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

            var normalizedArgs = args.NormalizeForContextMenu();

            return StatusContextMenu.OpenMenuAsync(normalizedArgs);
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

            var normalizedArgs = args.NormalizeForContextMenu();

            return CategoryContextMenu.OpenMenuAsync(normalizedArgs);
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

            var normalizedArgs = args.NormalizeForContextMenu();

            return TagContextMenu.OpenMenuAsync(normalizedArgs);
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

            var normalizedArgs = args.NormalizeForContextMenu();

            return TrackerContextMenu.OpenMenuAsync(normalizedArgs);
        }

        protected async Task AddCategory()
        {
            await DialogWorkflow.InvokeAddCategoryDialog();
        }

        protected async Task AddSubcategory()
        {
            if (ContextMenuCategory is null || ContextMenuCategory == FilterHelper.CATEGORY_ALL || ContextMenuCategory == FilterHelper.CATEGORY_UNCATEGORIZED)
            {
                return;
            }

            var prefix = ContextMenuCategory.EndsWith("\\", StringComparison.Ordinal)
                ? ContextMenuCategory
                : $"{ContextMenuCategory}\\";

            string? initialSavePath = null;
            if (MainData?.Categories.TryGetValue(ContextMenuCategory, out var category) == true)
            {
                initialSavePath = category.SavePath;
            }

            await DialogWorkflow.InvokeAddCategoryDialog(prefix, initialSavePath);
        }

        protected async Task EditCategory()
        {
            if (ContextMenuCategory is null)
            {
                return;
            }

            await DialogWorkflow.InvokeEditCategoryDialog(ContextMenuCategory);
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

        private async Task RemoveTracker()
        {
            if (ContextMenuTracker is null)
            {
                return;
            }

            if (!_trackerItems.TryGetValue(ContextMenuTracker, out var trackerItem) || trackerItem.IsSynthetic)
            {
                return;
            }

            if (trackerItem.Urls.Count == 0)
            {
                return;
            }

            var hashes = GetAffectedTorrentHashes(_trackerType);
            if (hashes.Count == 0)
            {
                return;
            }

            await ApiClient.RemoveTrackers(trackerItem.Urls, hashes: hashes.ToArray());
        }

        protected async Task AddTag()
        {
            if (ContextMenuTag is null)
            {
                return;
            }

            var tags = await DialogWorkflow.ShowAddTagsDialog();
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

        protected async Task StartTorrents(string type)
        {
            var torrents = GetAffectedTorrentHashes(type);

            await ApiClient.StartTorrents(hashes: torrents.ToArray());
        }

        protected async Task StopTorrents(string type)
        {
            var torrents = GetAffectedTorrentHashes(type);

            await ApiClient.StopTorrents(hashes: torrents.ToArray());
        }

        protected async Task RemoveTorrents(string type)
        {
            var torrents = GetAffectedTorrentHashes(type);

            await DialogWorkflow.InvokeDeleteTorrentDialog(Preferences?.ConfirmTorrentDeletion == true, [.. torrents]);
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

        private IReadOnlyList<TrackerFilterItem> GetTrackers()
        {
            if (MainData is null)
            {
                _trackerItems.Clear();
                return Array.Empty<TrackerFilterItem>();
            }

            var items = new List<TrackerFilterItem>();
            _trackerItems.Clear();

            AppendSpecialTrackerItem(FilterHelper.TRACKER_ALL, items);
            AppendSpecialTrackerItem(FilterHelper.TRACKER_TRACKERLESS, items);
            AppendSpecialTrackerItem(FilterHelper.TRACKER_ERROR, items);
            AppendSpecialTrackerItem(FilterHelper.TRACKER_WARNING, items);
            AppendSpecialTrackerItem(FilterHelper.TRACKER_ANNOUNCE_ERROR, items);

            if (MainData.Trackers.Count > 0)
            {
                var hostGroups = new Dictionary<string, TrackerHostGroup>(StringComparer.OrdinalIgnoreCase);
                foreach (var (url, hashes) in MainData.Trackers)
                {
                    var host = GetHostName(url);
                    if (!hostGroups.TryGetValue(host, out var group))
                    {
                        group = new TrackerHostGroup(host);
                        hostGroups[host] = group;
                    }

                    if (hashes is not null)
                    {
                        foreach (var hash in hashes)
                        {
                            if (MainData.Torrents.ContainsKey(hash))
                            {
                                group.Hashes.Add(hash);
                            }
                        }
                    }

                    group.Urls.Add(url);
                }

                foreach (var group in hostGroups.Values.OrderBy(g => g.Host, StringComparer.OrdinalIgnoreCase))
                {
                    var urls = group.Urls.Distinct(StringComparer.Ordinal).ToArray();
                    var item = new TrackerFilterItem(group.Host, group.Host, group.Hashes.Count, false, urls);
                    items.Add(item);
                    _trackerItems[item.Key] = item;
                }
            }

            return items;
        }

        private Dictionary<string, int> GetStatuses()
        {
            if (MainData is null)
            {
                return [];
            }

            return MainData.StatusState.ToDictionary(d => d.Key, d => d.Value.Count);
        }

        private string GetStatusDisplayName(string status, int count)
        {
            if (!Enum.TryParse<Lantean.QBTMud.Models.Status>(status, out var parsed))
            {
                return status;
            }

            return parsed switch
            {
                Lantean.QBTMud.Models.Status.All => WebUiLocalizer.Translate("StatusFilterWidget", "All (%1)", count),
                Lantean.QBTMud.Models.Status.Downloading => WebUiLocalizer.Translate("StatusFilterWidget", "Downloading (%1)", count),
                Lantean.QBTMud.Models.Status.Seeding => WebUiLocalizer.Translate("StatusFilterWidget", "Seeding (%1)", count),
                Lantean.QBTMud.Models.Status.Completed => WebUiLocalizer.Translate("StatusFilterWidget", "Completed (%1)", count),
                Lantean.QBTMud.Models.Status.Stopped => WebUiLocalizer.Translate("StatusFilterWidget", "Stopped (%1)", count),
                Lantean.QBTMud.Models.Status.Active => WebUiLocalizer.Translate("StatusFilterWidget", "Active (%1)", count),
                Lantean.QBTMud.Models.Status.Inactive => WebUiLocalizer.Translate("StatusFilterWidget", "Inactive (%1)", count),
                Lantean.QBTMud.Models.Status.Stalled => WebUiLocalizer.Translate("StatusFilterWidget", "Stalled (%1)", count),
                Lantean.QBTMud.Models.Status.StalledUploading => WebUiLocalizer.Translate("StatusFilterWidget", "Stalled Uploading (%1)", count),
                Lantean.QBTMud.Models.Status.StalledDownloading => WebUiLocalizer.Translate("StatusFilterWidget", "Stalled Downloading (%1)", count),
                Lantean.QBTMud.Models.Status.Checking => WebUiLocalizer.Translate("StatusFilterWidget", "Checking (%1)", count),
                Lantean.QBTMud.Models.Status.Errored => WebUiLocalizer.Translate("StatusFilterWidget", "Errored (%1)", count),
                _ => status
            };
        }

        private string GetCategoryDisplayName(string category)
        {
            if (string.Equals(category, FilterHelper.CATEGORY_ALL, StringComparison.Ordinal))
            {
                return WebUiLocalizer.Translate("CategoryFilterModel", "All");
            }

            if (string.Equals(category, FilterHelper.CATEGORY_UNCATEGORIZED, StringComparison.Ordinal))
            {
                return WebUiLocalizer.Translate("CategoryFilterModel", "Uncategorized");
            }

            return category;
        }

        private string GetTagDisplayName(string tag)
        {
            if (string.Equals(tag, FilterHelper.TAG_ALL, StringComparison.Ordinal))
            {
                return WebUiLocalizer.Translate("TagFilterModel", "All");
            }

            if (string.Equals(tag, FilterHelper.TAG_UNTAGGED, StringComparison.Ordinal))
            {
                return WebUiLocalizer.Translate("TagFilterModel", "Untagged");
            }

            return tag;
        }

        private string GetTrackerDisplayName(TrackerFilterItem tracker)
        {
            if (!tracker.IsSynthetic)
            {
                return tracker.DisplayName;
            }

            return tracker.Key switch
            {
                FilterHelper.TRACKER_ALL => WebUiLocalizer.Translate("TrackerFiltersList", "All"),
                FilterHelper.TRACKER_TRACKERLESS => WebUiLocalizer.Translate("TrackerFiltersList", "Trackerless"),
                FilterHelper.TRACKER_ERROR => WebUiLocalizer.Translate("TrackerFiltersList", "Tracker error"),
                FilterHelper.TRACKER_WARNING => WebUiLocalizer.Translate("TrackerFiltersList", "Warning"),
                FilterHelper.TRACKER_ANNOUNCE_ERROR => WebUiLocalizer.Translate("TrackerFiltersList", "Other error"),
                _ => tracker.DisplayName
            };
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

        private void AppendSpecialTrackerItem(string key, List<TrackerFilterItem> items)
        {
            if (MainData is null)
            {
                return;
            }

            var count = MainData.TrackersState.TryGetValue(key, out var set) ? set.Count : 0;
            var item = new TrackerFilterItem(key, key, count, true, Array.Empty<string>());
            items.Add(item);
            _trackerItems[key] = item;
        }

        private sealed class TrackerHostGroup
        {
            public TrackerHostGroup(string host)
            {
                Host = host;
                Hashes = new HashSet<string>(StringComparer.Ordinal);
                Urls = new List<string>();
            }

            public string Host { get; }

            public HashSet<string> Hashes { get; }

            public List<string> Urls { get; }
        }

        private sealed record TrackerFilterItem(string Key, string DisplayName, int Count, bool IsSynthetic, IReadOnlyList<string> Urls);

        private static string GetFilterContext(string type)
        {
            return type switch
            {
                _statusType => "StatusFilterWidget",
                _categoryType => "CategoryFilterWidget",
                _tagType => "TagFilterWidget",
                _trackerType => "TrackerFiltersList",
                _ => string.Empty
            };
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
