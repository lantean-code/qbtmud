using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Helpers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class RssRulesDialog
    {
        private readonly List<string> _unsavedRuleNames = [];

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        protected string? SelectedRuleName { get; set; }

        protected Dictionary<string, QBitTorrentClient.Models.AutoDownloadingRule?> Rules { get; set; } = [];

        protected IEnumerable<string> Categories { get; set; } = [];

        protected Dictionary<string, string> Feeds { get; set; } = [];

        protected IReadOnlyDictionary<string, IReadOnlyList<string>>? MatchingArticles { get; set; }

        private QBitTorrentClient.Models.AutoDownloadingRule SelectedRule { get; set; } = default!;

        protected bool UseRegex { get; set; }

        protected void UseRegexChanged(bool value)
        {
            UseRegex = value;
            SelectedRule.UseRegex = value;
        }

        protected string? MustContain { get; set; }

        protected void MustContainChanged(string value)
        {
            MustContain = value;
            SelectedRule.MustContain = value;
        }

        protected string? MustNotContain { get; set; }

        protected void MustNotContainChanged(string value)
        {
            MustNotContain = value;
            SelectedRule.MustNotContain = value;
        }

        protected string? EpisodeFilter { get; set; }

        protected void EpisodeFilterChanged(string value)
        {
            EpisodeFilter = value;
            SelectedRule.EpisodeFilter = value;
        }

        protected bool SmartFilter { get; set; }

        protected void SmartFilterChanged(bool value)
        {
            SmartFilter = value;
            SelectedRule.SmartFilter = value;
        }

        protected string? Category { get; set; }

        protected void CategoryChanged(string value)
        {
            Category = value;
            SelectedRule.TorrentParams.Category = value;
        }

        protected string? Tags { get; set; }

        protected void TagsChanged(string value)
        {
            Tags = value;
            SelectedRule.TorrentParams.Tags = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }

        protected bool SaveToDifferentDirectory { get; set; }

        protected void SaveToDifferentDirectoryChanged(bool value)
        {
            SaveToDifferentDirectory = value;
            if (!value)
            {
                SelectedRule.TorrentParams.SavePath = "";
            }
        }

        protected string? SaveTo { get; set; }

        protected void SaveToChanged(string value)
        {
            SaveTo = value;
            SelectedRule.TorrentParams.SavePath = value;
            SelectedRule.TorrentParams.UseAutoTmm = false;
        }

        protected int IgnoreDays { get; set; }

        protected void IgnoreDaysChanged(int value)
        {
            IgnoreDays = value;
            SelectedRule.IgnoreDays = value;
        }

        protected string? AddStopped { get; set; }

        protected void AddStoppedChanged(string value)
        {
            AddStopped = value;
            switch (value)
            {
                case "default":
                    SelectedRule.TorrentParams.Stopped = null;
                    break;

                case "always":
                    SelectedRule.TorrentParams.Stopped = true;
                    break;

                case "never":
                    SelectedRule.TorrentParams.Stopped = false;
                    break;
            }
        }

        protected string? ContentLayout { get; set; }

        protected void ContentLayoutChanged(string value)
        {
            ContentLayout = value;

            switch (value)
            {
                case "Default":
                    SelectedRule.TorrentParams.ContentLayout = null;
                    break;

                case "Original":
                    SelectedRule.TorrentParams.ContentLayout = "Original";
                    break;

                case "Subfolder":
                    SelectedRule.TorrentParams.ContentLayout = "Subfolder";
                    break;

                case "NoSubfolder":
                    SelectedRule.TorrentParams.ContentLayout = "NoSubfolder";
                    break;
            }
        }

        protected IReadOnlyCollection<string>? SelectedFeeds { get; set; }

        protected void SelectedFeedsChanged(IReadOnlyCollection<string> value)
        {
            SelectedFeeds = value;

            var feeds = new List<string>();
            foreach (var feed in SelectedFeeds)
            {
                if (Feeds.TryGetValue(feed, out var url))
                {
                    feeds.Add(url);
                }
            }

            SelectedRule.AffectedFeeds = feeds;
        }

        protected override async Task OnInitializedAsync()
        {
            var rules = await ApiClient.GetAllRssAutoDownloadingRules();
            foreach (var kvp in rules)
            {
                Rules.Add(kvp.Key, kvp.Value);
            }

            Categories = (await ApiClient.GetAllCategories()).Keys;

            Feeds = (await ApiClient.GetAllRssItems(false)).ToDictionary(f => f.Key, f => f.Value.Url);
        }

        protected async Task AddRule()
        {
            var ruleName = await DialogWorkflow.ShowStringFieldDialog("Add Rule", "Name", null);
            if (ruleName is null)
            {
                return;
            }

            if (Rules.ContainsKey(ruleName))
            {
                SelectedRuleName = ruleName;
                return;
            }

            Rules.Add(ruleName, null);
            _unsavedRuleNames.Add(ruleName);

            await InvokeAsync(StateHasChanged);
        }

        protected async Task RemoveRule()
        {
            if (SelectedRuleName is null)
            {
                return;
            }

            if (_unsavedRuleNames.Contains(SelectedRuleName))
            {
                _unsavedRuleNames.Remove(SelectedRuleName);
            }
            else
            {
                await ApiClient.RemoveRssAutoDownloadingRule(SelectedRuleName);
            }

            Rules.Remove(SelectedRuleName);
            SelectedRuleName = null;

            await InvokeAsync(StateHasChanged);
        }

        protected async Task SelectedRuleChanged(string value)
        {
            SelectedRuleName = value;

            if (!Rules.TryGetValue(SelectedRuleName, out var rule))
            {
                return;
            }

            if (!_unsavedRuleNames.Contains(SelectedRuleName))
            {
                MatchingArticles = await ApiClient.GetRssMatchingArticles(SelectedRuleName);
            }
            else
            {
                MatchingArticles = null;
            }

            if (rule is null)
            {
                rule = new QBitTorrentClient.Models.AutoDownloadingRule();

                Rules[SelectedRuleName] = rule;
            }
            SelectedRule = rule;

            UseRegex = SelectedRule.UseRegex ?? false;
            MustContain = SelectedRule.MustContain;
            MustNotContain = SelectedRule.MustNotContain;
            EpisodeFilter = SelectedRule.EpisodeFilter;
            SmartFilter = SelectedRule.SmartFilter ?? false;
            Category = SelectedRule.TorrentParams.Category;
            Tags = string.Join(' ', SelectedRule.TorrentParams.Tags);
            SaveToDifferentDirectory = !string.IsNullOrEmpty(SelectedRule.TorrentParams.SavePath);
            SaveTo = SelectedRule.TorrentParams.SavePath;
            IgnoreDays = SelectedRule.IgnoreDays ?? 0;
            switch (SelectedRule.TorrentParams.Stopped)
            {
                case null:
                    AddStopped = "default";
                    break;

                case true:
                    AddStopped = "always";
                    break;

                case false:
                    AddStopped = "never";
                    break;
            }

            switch (SelectedRule.TorrentParams.ContentLayout)
            {
                case "Default":
                    ContentLayout = null;
                    break;

                case "Original":
                    ContentLayout = "Original";
                    break;

                case "Subfolder":
                    ContentLayout = "Subfolder";
                    break;

                case "NoSubfolder":
                    ContentLayout = "NoSubfolder";
                    break;
            }

            var feeds = new List<string>();
            foreach (var feed in SelectedRule.AffectedFeeds)
            {
                foreach (var key in Feeds.Keys)
                {
                    if (Feeds[key] == feed)
                    {
                        feeds.Add(key);
                    }
                }
            }
            SelectedFeeds = feeds;
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected async Task Submit()
        {
            if (SelectedRuleName is null)
            {
                return;
            }

            await ApiClient.SetRssAutoDownloadingRule(SelectedRuleName, SelectedRule);

            MatchingArticles = await ApiClient.GetRssMatchingArticles(SelectedRuleName);

            _unsavedRuleNames.Remove(SelectedRuleName);
        }
    }
}