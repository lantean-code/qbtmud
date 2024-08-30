using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Helpers;
using Lantean.QBTMudBlade.Models;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net;
using System;
using System.Collections.ObjectModel;
using static MudBlazor.CategoryTypes;
using Lantean.QBTMudBlade.Components.Dialogs;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class Rss : IAsyncDisposable
    {
        private readonly bool _refreshEnabled = true;

        private readonly CancellationTokenSource _timerCancellationToken = new();
        private bool _disposedValue;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IDataManager DataManager { get; set; } = default!;

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
        
        protected RssList? RssList { get; set; }

        protected Dictionary<string, RssFeed> Feeds => RssList?.Feeds ?? [];

        protected List<RssArticle> Articles { get; } = [];

        protected int UnreadCount => RssList?.UnreadCount ?? 0;

        protected RssArticle? Article { get; set; }

        protected void SelectedFeedChanged(string value)
        {
            SelectedFeed = value;
            SelectedArticle = null;

            Articles.Clear();

            if (RssList is null)
            {
                return;
            }

            IEnumerable<RssArticle> articles;

            if (value == "unread")
            {
                articles = RssList.Articles.Where(a => !a.IsRead);
            }
            else
            {
                articles = RssList.Articles.Where(a => a.Feed == value);
            }

            foreach (var article in articles)
            {
                Articles.Add(article);
            }
        }

        protected async Task SelectedArticleChanged(string value)
        {
            Article = null;
            SelectedArticle = value;

            if (RssList is null)
            {
                return;
            }

            var article = RssList.Articles.Find(a => a.Id == value);
            if (article is null)
            {
                return;
            }

            article.IsRead = true;
            Articles.First(a => a.Id == value).IsRead = true;
            Article = article;

            await ApiClient.MarkRssItemAsRead(article.Feed, article.Id);
        }

        protected override async Task OnInitializedAsync()
        {
            await GetRssList();
        }

        private async Task GetRssList()
        {
            var items = await ApiClient.GetAllRssItems(true);
            RssList = DataManager.CreateRssList(items);
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
            var url = await DialogService.ShowStringFieldDialog("RSS Feed URL", "Feed URL", null);
            if (url is not null)
            {
                await ApiClient.AddRssFeed(url);

                await GetRssList();

                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task MarkAsRead()
        {
            if (SelectedFeed is null)
            {
                return;
            }

            if (SelectedFeed == "unread")
            {
                if (RssList is null)
                {
                    return;
                }

                var articles = RssList.Articles.Where(a => !a.IsRead);
                foreach (var article in articles)
                {
                    await ApiClient.MarkRssItemAsRead(article.Feed, article.Id);
                }

                RssList.MarkAllUnreadAsRead();
            }
            else
            {
                await ApiClient.MarkRssItemAsRead(SelectedFeed);

                RssList?.MarkAsUnread(SelectedFeed);
            }

            foreach (var article in Articles)
            {
                article.IsRead = true;
            }
        }

        protected async Task UpdateAll()
        {
            foreach (var (path, feed) in Feeds)
            {
                feed.IsLoading = true;
                await ApiClient.RefreshRssItem(path);
            }
        }

        protected async Task EditDownloadRules()
        {
            await DialogService.InvokeRssRulesDialog();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_refreshEnabled)
            {
                return;
            }

            if (!firstRender)
            {
                return;
            }

            using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(RefreshInterval)))
            {
                while (!_timerCancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync())
                {
                    await GetRssList();

                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        protected virtual Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _timerCancellationToken.Cancel();
                    _timerCancellationToken.Dispose();
                }

                _disposedValue = true;
            }

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}