using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

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

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Inject]
        protected IApiFeedbackWorkflow ApiFeedbackWorkflow { get; set; } = default!;

        protected string? SelectedRuleName { get; set; }

        protected Dictionary<string, AutoDownloadingRule?> Rules { get; set; } = [];

        protected IEnumerable<string> Categories { get; set; } = [];

        protected Dictionary<string, string> Feeds { get; set; } = [];

        protected IReadOnlyDictionary<string, IReadOnlyList<string>>? MatchingArticles { get; set; }

        private AutoDownloadingRule SelectedRule { get; set; } = default!;

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

        protected TorrentContentLayout? ContentLayout { get; set; }

        protected void ContentLayoutChanged(TorrentContentLayout? value)
        {
            ContentLayout = value;
            SelectedRule.TorrentParams.ContentLayout = value;
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
            var rulesResult = await ApiClient.GetAllRssAutoDownloadingRulesAsync();
            if (rulesResult.IsFailure)
            {
                await ApiFeedbackWorkflow.HandleFailureAsync(rulesResult);
            }
            else
            {
                foreach (var kvp in rulesResult.Value)
                {
                    Rules.Add(kvp.Key, kvp.Value);
                }
            }

            var categoriesResult = await ApiClient.GetAllCategoriesAsync();
            if (categoriesResult.IsFailure)
            {
                Categories = [];
                await ApiFeedbackWorkflow.HandleFailureAsync(categoriesResult);
            }
            else
            {
                Categories = categoriesResult.Value.Keys;
            }

            var feedsResult = await ApiClient.GetAllRssItemsAsync(false);
            if (feedsResult.IsFailure)
            {
                Feeds = new Dictionary<string, string>();
                await ApiFeedbackWorkflow.HandleFailureAsync(feedsResult);
            }
            else
            {
                Feeds = RssItemTreeHelper.EnumerateFeeds(feedsResult.Value).ToDictionary(f => f.Key, f => f.Value.Url, StringComparer.Ordinal);
            }
        }

        protected async Task AddRule()
        {
            var ruleName = await DialogWorkflow.ShowStringFieldDialog(
                LanguageLocalizer.Translate("AutomatedRssDownloader", "New rule name"),
                LanguageLocalizer.Translate("AutomatedRssDownloader", "Please type the name of the new download rule."),
                null);
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
                var removeResult = await ApiClient.RemoveRssAutoDownloadingRuleAsync(SelectedRuleName);
                if (!await ApiFeedbackWorkflow.ProcessResultAsync(removeResult))
                {
                    return;
                }
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
                var matchingArticlesResult = await ApiClient.GetRssMatchingArticlesAsync(SelectedRuleName);
                if (matchingArticlesResult.IsFailure)
                {
                    MatchingArticles = null;
                    await ApiFeedbackWorkflow.HandleFailureAsync(matchingArticlesResult);
                }
                else
                {
                    MatchingArticles = matchingArticlesResult.Value;
                }
            }
            else
            {
                MatchingArticles = null;
            }

            if (rule is null)
            {
                rule = new AutoDownloadingRule();

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

            ContentLayout = SelectedRule.TorrentParams.ContentLayout;

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

            var setRuleResult = await ApiClient.SetRssAutoDownloadingRuleAsync(SelectedRuleName, SelectedRule);
            if (!await ApiFeedbackWorkflow.ProcessResultAsync(setRuleResult))
            {
                return;
            }

            var matchingArticlesResult = await ApiClient.GetRssMatchingArticlesAsync(SelectedRuleName);
            if (matchingArticlesResult.IsFailure)
            {
                await ApiFeedbackWorkflow.HandleFailureAsync(matchingArticlesResult);
                return;
            }

            MatchingArticles = matchingArticlesResult.Value;

            _unsavedRuleNames.Remove(SelectedRuleName);
        }
    }
}
