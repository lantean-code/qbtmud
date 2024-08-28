using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Helpers;
using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class Rss
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [CascadingParameter]
        public MainData? MainData { get; set; }

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [Parameter]
        public string? Hash { get; set; }

        protected int ActiveTab { get; set; } = 0;

        protected int RefreshInterval => MainData?.ServerState.RefreshInterval ?? 1500;

        protected ServerState? ServerState => MainData?.ServerState;

        protected string? SelectedFeed { get; set; }

        protected string? SelectedArticle { get; set; }

        public IReadOnlyDictionary<string, QBitTorrentClient.Models.RssItem> Items { get; private set; } = new Dictionary<string, QBitTorrentClient.Models.RssItem>();

        protected IReadOnlyList<int> ColumnsSizes => GetColumnSizes();

        private IReadOnlyList<int> GetColumnSizes()
        {
            if (SelectedFeed is null)
            {
                return [12, 0, 0];
            }

            if (SelectedFeed is not null && SelectedArticle is null)
            {
                return [6, 6, 0];
            }

            return [4, 4, 4];
        }

        protected QBitTorrentClient.Models.RssItem? SelectedRssItem
        {
            get
            {
                if (SelectedFeed == null)
                {
                    return null;
                }

                Items.TryGetValue(SelectedFeed, out var feed);

                return feed;
            }
        }

        protected QBitTorrentClient.Models.RssArticle? SelectedRssArticle
        {
            get
            {
                return SelectedRssItem?.Articles?.FirstOrDefault(a => a.Id == SelectedArticle);
            }
        }

        protected void SelectedFeedChanged(string value)
        {
            SelectedFeed = value;
            SelectedArticle = null;
        }

        protected void SelectedArticleChanged(string value)
        {
            SelectedArticle = value;
        }

        protected override async Task OnInitializedAsync()
        {
            Items = await ApiClient.GetAllRssItems(true);
        }

        protected async Task DownloadItem(string? url)
        {
            await DialogService.InvokeAddTorrentLinkDialog(ApiClient, url);
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }

        protected async Task NewSubscription()
        {
            await Task.CompletedTask;
        }

        protected async Task MarkAsRead()
        {
            await Task.CompletedTask;
        }

        protected async Task UpdateAll()
        {
            await Task.CompletedTask;
        }

        protected async Task EditDownloadRules()
        {
            await Task.CompletedTask;
        }
    }
}