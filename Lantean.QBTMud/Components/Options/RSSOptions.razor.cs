using Lantean.QBTMud.Helpers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Options
{
    public partial class RSSOptions : Options
    {
        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        protected bool RssProcessingEnabled { get; private set; }
        protected int RssRefreshInterval { get; private set; }
        protected long RssFetchDelay { get; private set; }
        protected int RssMaxArticlesPerFeed { get; private set; }
        protected bool RssAutoDownloadingEnabled { get; private set; }
        protected bool RssDownloadRepackProperEpisodes { get; private set; }
        protected string? RssSmartEpisodeFilters { get; private set; }

        protected override bool SetOptions()
        {
            if (Preferences is null)
            {
                return false;
            }

            RssProcessingEnabled = Preferences.RssProcessingEnabled;
            RssRefreshInterval = Preferences.RssRefreshInterval;
            RssFetchDelay = Preferences.RssFetchDelay;
            RssMaxArticlesPerFeed = Preferences.RssMaxArticlesPerFeed;
            RssAutoDownloadingEnabled = Preferences.RssAutoDownloadingEnabled;
            RssDownloadRepackProperEpisodes = Preferences.RssDownloadRepackProperEpisodes;
            RssSmartEpisodeFilters = Preferences.RssSmartEpisodeFilters;

            return true;
        }

        protected async Task RssProcessingEnabledChanged(bool value)
        {
            RssProcessingEnabled = value;
            UpdatePreferences.RssProcessingEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task RssRefreshIntervalChanged(int value)
        {
            RssRefreshInterval = value;
            UpdatePreferences.RssRefreshInterval = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task RssFetchDelayChanged(int value)
        {
            RssFetchDelay = value;
            UpdatePreferences.RssFetchDelay = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task RssMaxArticlesPerFeedChanged(int value)
        {
            RssMaxArticlesPerFeed = value;
            UpdatePreferences.RssMaxArticlesPerFeed = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task RssAutoDownloadingEnabledChanged(bool value)
        {
            RssAutoDownloadingEnabled = value;
            UpdatePreferences.RssAutoDownloadingEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task RssDownloadRepackProperEpisodesChanged(bool value)
        {
            RssDownloadRepackProperEpisodes = value;
            UpdatePreferences.RssDownloadRepackProperEpisodes = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task RssSmartEpisodeFiltersChanged(string value)
        {
            RssSmartEpisodeFilters = value;
            UpdatePreferences.RssSmartEpisodeFilters = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task OpenRssRulesDialog()
        {
            await DialogService.InvokeRssRulesDialog();
        }
    }
}