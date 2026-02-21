using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using System.Globalization;

namespace Lantean.QBTMud.Pages
{
    public partial class Rss
    {
        private const char PathSeparator = '\\';

        private RssTreeNode? _selectedNode;
        private RssTreeNode? _contextNode;
        private bool _contextIsEmptyArea;
        private bool _refreshInProgress;
        private bool _pendingRefresh;
        private string? _pendingPreferredPath;
        private bool _pendingPreferUnread;
        private bool _hasNavigatedForward;
        private bool _showDetailsPane;
        private bool _isAnimating;
        private int _animationToken;
        private bool _pendingAnimation;
        private int _previousStartPaneForAnimation;
        private int ColumnCount => DetermineColumnCount();

        private int ColumnSpan => 4;

        private int CurrentStage
        {
            get
            {
                if (ColumnCount == 1 && !_hasNavigatedForward && !_showDetailsPane)
                {
                    return 0;
                }

                if (_showDetailsPane)
                {
                    return 2;
                }

                if (_selectedNode is not null)
                {
                    return 1;
                }

                return 0;
            }
        }

        private int StartPane => Math.Max(0, CurrentStage - (ColumnCount - 1));

        private double SlideOffsetPercent => ColumnCount == 1 ? StartPane * 100 : StartPane * 50;

        private string SliderContainerClass => ColumnCount == 1 ? "rss-slider rss-slider--one" : "rss-slider rss-slider--two";

        private string SliderTrackClass => ColumnCount == 1 ? "rss-slider__track rss-slider__track--one" : "rss-slider__track rss-slider__track--two";

        private string SliderTrackStyle => $"transform: translateX(-{SlideOffsetPercent.ToString(CultureInfo.InvariantCulture)}%);";

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IRssDataManager RssDataManager { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected IClipboardService ClipboardService { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [CascadingParameter]
        public MainData? MainData { get; set; }

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter]
        public Breakpoint CurrentBreakpoint { get; set; }

        [CascadingParameter]
        public Orientation CurrentOrientation { get; set; }

        protected MudMenu? FeedContextMenu { get; set; }

        protected RssList? RssList { get; set; }

        protected IReadOnlyList<RssTreeItem> FeedItems => RssList?.TreeItems ?? Array.Empty<RssTreeItem>();

        protected List<RssArticle> Articles { get; } = [];

        protected RssArticle? Article { get; set; }

        protected string? SelectedArticle { get; set; }

        protected bool CanMarkSelectionAsRead => _selectedNode is not null && !IsClientDisconnected;

        private bool IsClientDisconnected => MainData is not null && MainData.LostConnection;

        protected override async Task OnInitializedAsync()
        {
            await RefreshRssList(preferUnread: true);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_pendingAnimation)
            {
                return;
            }

            _pendingAnimation = false;
            var token = _animationToken;

            await Task.Delay(700);

            if (token != _animationToken)
            {
                return;
            }

            _isAnimating = false;
            await InvokeAsync(StateHasChanged);
        }

        protected IReadOnlyList<RssTreeItem> TreeItems => FeedItems;

        protected bool ContextCanUpdate => _contextNode is not null && !IsClientDisconnected;

        protected bool ContextCanMarkRead => _contextNode is not null && !IsClientDisconnected;

        protected bool ContextCanRename => _contextNode is not null && !_contextNode.IsUnread && !IsClientDisconnected;

        protected bool ContextCanEditUrl => _contextNode?.Feed is not null && !IsClientDisconnected;

        protected bool ContextCanDelete => _contextNode is not null && !_contextNode.IsUnread && !string.IsNullOrEmpty(_contextNode.Path) && !IsClientDisconnected;

        protected bool ContextCanAddSubscription => !IsClientDisconnected;

        protected bool ContextCanAddFolder
        {
            get
            {
                if (IsClientDisconnected)
                {
                    return false;
                }

                if (_contextIsEmptyArea)
                {
                    return true;
                }

                return _contextNode is not null && _contextNode.IsFolder;
            }
        }

        protected bool ContextCanUpdateAll => _contextIsEmptyArea && !IsClientDisconnected;

        protected bool ContextCanCopyUrl => _contextNode?.Feed is not null;

        protected bool ShowBackToFeedsFromArticles => ColumnCount == 1
            ? StartPane > 0 || (_isAnimating && _previousStartPaneForAnimation > 0)
            : StartPane > 0;

        protected bool ShowBackToArticlesFromDetails => ColumnCount == 1 && Article is not null;

        protected string GetNodeDisplay(RssTreeNode node)
        {
            var count = node.UnreadCount;
            if (node.IsUnread)
            {
                return $"{LanguageLocalizer.Translate("FeedListWidget", "Unread")} ({count})";
            }

            return count > 0
                ? $"{node.Name} ({count})"
                : node.Name;
        }

        protected string GetNodeIcon(RssTreeNode node)
        {
            if (node.IsUnread)
            {
                return Icons.Material.Filled.MarkEmailUnread;
            }

            if (node.IsFolder)
            {
                return Icons.Material.Filled.Folder;
            }

            var feed = node.Feed!;
            if (feed.IsLoading)
            {
                return Icons.Material.Filled.Sync;
            }

            if (feed.HasError)
            {
                return Icons.Material.Filled.Error;
            }

            return Icons.Material.Filled.RssFeed;
        }

        protected Color GetNodeIconColor(RssTreeNode node)
        {
            if (node.IsUnread)
            {
                return node.UnreadCount > 0 ? Color.Info : Color.Default;
            }

            if (node.IsFolder)
            {
                return Color.Default;
            }

            var feed = node.Feed!;
            if (feed.HasError)
            {
                return Color.Error;
            }

            return feed.IsLoading ? Color.Info : Color.Default;
        }

        protected string GetNodeCssClass(RssTreeNode node)
        {
            if (node.Feed?.IsLoading == true)
            {
                return "spin-animation";
            }

            return string.Empty;
        }

        private static string GetNodeKey(RssTreeNode node)
        {
            return node.IsUnread ? "__unread__" : node.Path;
        }

        protected async Task SelectNode(RssTreeNode? node)
        {
            if (node is null)
            {
                return;
            }

            var previousStartPane = StartPane;
            if (_selectedNode == node)
            {
                return;
            }

            _selectedNode = node;
            _hasNavigatedForward = true;
            _showDetailsPane = false;
            SelectedArticle = null;
            Article = null;
            UpdateArticlesForSelection();
            TriggerSlideAnimation(previousStartPane);
            await InvokeAsync(StateHasChanged);
        }

        protected async Task ShowFeedContextMenu(MouseEventArgs args, RssTreeNode node)
        {
            _contextNode = node;
            _contextIsEmptyArea = false;
            await InvokeAsync(StateHasChanged);

            if (FeedContextMenu is not null)
            {
                await FeedContextMenu.OpenMenuAsync(args.NormalizeForContextMenu());
            }
        }

        protected async Task ShowFeedContextMenuFromContainer(MouseEventArgs args)
        {
            _contextNode = null;
            _contextIsEmptyArea = true;
            await InvokeAsync(StateHasChanged);

            if (FeedContextMenu is not null)
            {
                await FeedContextMenu.OpenMenuAsync(args.NormalizeForContextMenu());
            }
        }

        protected async Task NewSubscription()
        {
            await AddSubscriptionAtNode(_selectedNode);
        }

        protected async Task MarkAsRead()
        {
            if (_selectedNode is null || IsClientDisconnected)
            {
                return;
            }

            await MarkNodeAsRead(_selectedNode);
        }

        protected async Task RefreshAllFeeds()
        {
            if (IsClientDisconnected)
            {
                return;
            }

            await RefreshFeedsForNode(null);
        }

        protected async Task EditDownloadRules()
        {
            await DialogWorkflow.InvokeRssRulesDialog();
        }

        protected async Task SelectArticle(RssArticle article)
        {
            var previousStartPane = StartPane;
            _hasNavigatedForward = true;
            _showDetailsPane = true;
            await HandleArticleSelection(article);
            TriggerSlideAnimation(previousStartPane);
        }

        protected async Task SelectedArticleChanged(string value)
        {
            if (RssList is null)
            {
                return;
            }

            var article = RssList.Articles.Find(a => a.Id == value);
            if (article is null)
            {
                return;
            }

            var previousStartPane = StartPane;
            _hasNavigatedForward = true;
            _showDetailsPane = true;
            await HandleArticleSelection(article);
            TriggerSlideAnimation(previousStartPane);
        }

        private async Task HandleArticleSelection(RssArticle article)
        {
            Article = article;
            SelectedArticle = article.Id;

            if (article.IsRead)
            {
                return;
            }

            article.IsRead = true;
            var localArticle = Articles.FirstOrDefault(a => a.Id == article.Id);
            if (localArticle is not null)
            {
                localArticle.IsRead = true;
            }

            await ApiClient.MarkRssItemAsRead(article.Feed, article.Id);
            UpdateUnreadCountsAfterRead(article);
            await InvokeAsync(StateHasChanged);
        }

        private void UpdateUnreadCountsAfterRead(RssArticle article)
        {
            var rssList = RssList!;

            if (rssList.Feeds.TryGetValue(article.Feed, out var feed))
            {
                feed.UnreadCount = Math.Max(0, feed.UnreadCount - 1);
            }

            rssList.RecalculateCounts();
        }

        protected async Task UpdateContextUpdate()
        {
            if (IsClientDisconnected)
            {
                return;
            }

            if (_contextNode is null)
            {
                return;
            }

            if (_contextNode.IsUnread)
            {
                await RefreshFeedsForNode(null);
            }
            else
            {
                await RefreshFeedsForNode(_contextNode);
            }
        }

        protected async Task UpdateContextMarkRead()
        {
            if (_contextNode is null || IsClientDisconnected)
            {
                return;
            }

            await MarkNodeAsRead(_contextNode);
        }

        protected async Task UpdateContextRename()
        {
            if (_contextNode is null || _contextNode.IsUnread || IsClientDisconnected)
            {
                return;
            }

            var title = _contextNode.IsFolder
                ? LanguageLocalizer.Translate("RSSWidget", "Please choose a folder name")
                : LanguageLocalizer.Translate("RSSWidget", "Please choose a new name for this RSS feed");
            var label = _contextNode.IsFolder
                ? LanguageLocalizer.Translate("RSSWidget", "Folder name:")
                : LanguageLocalizer.Translate("RSSWidget", "New feed name:");
            var newPath = await DialogWorkflow.ShowStringFieldDialog(title, label, _contextNode.Path);
            if (string.IsNullOrWhiteSpace(newPath) || newPath == _contextNode.Path)
            {
                return;
            }

            try
            {
                await ApiClient.MoveRssItem(_contextNode.Path, newPath);
                var parentPath = GetParentPath(newPath);
                await RefreshRssList(preferredPath: newPath, preferUnread: false);
            }
            catch (HttpRequestException exception)
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateRss("Unable to rename RSS item: %1", exception.Message), Severity.Error);
            }
        }

        protected async Task UpdateContextEditUrl()
        {
            if (_contextNode?.Feed is null || IsClientDisconnected)
            {
                return;
            }

            var newUrl = await DialogWorkflow.ShowStringFieldDialog(
                LanguageLocalizer.Translate("RSSWidget", "Edit feed URL..."),
                LanguageLocalizer.Translate("RSSWidget", "Feed URL:"),
                _contextNode.Feed.Url);
            if (string.IsNullOrWhiteSpace(newUrl) || string.Equals(newUrl, _contextNode.Feed.Url, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                await ApiClient.SetRssFeedUrl(_contextNode.Path, newUrl);
                await RefreshRssList(preferredPath: _contextNode.Path, preferUnread: false);
            }
            catch (HttpRequestException exception)
            {
                SnackbarWorkflow.ShowTransientMessage($"{LanguageLocalizer.Translate("RSSWidget", "Unable to update URL")}: {exception.Message}", Severity.Error);
            }
        }

        protected async Task UpdateContextDelete()
        {
            if (_contextNode is null || IsClientDisconnected)
            {
                return;
            }

            var confirmed = await DialogWorkflow.ShowConfirmDialog(
                LanguageLocalizer.Translate("RSSWidget", "Deletion confirmation"),
                TranslateRss("Remove \"%1\"?", _contextNode.Name));
            if (!confirmed)
            {
                return;
            }

            try
            {
                await ApiClient.RemoveRssItem(_contextNode.Path);
                var parentPath = GetParentPath(_contextNode.Path);
                await RefreshRssList(preferredPath: parentPath, preferUnread: false);
            }
            catch (HttpRequestException exception)
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateRss("Unable to remove RSS item: %1", exception.Message), Severity.Error);
            }
        }

        protected async Task UpdateContextAddSubscription()
        {
            if (IsClientDisconnected)
            {
                return;
            }

            await AddSubscriptionAtNode(_contextNode);
        }

        protected async Task UpdateContextAddFolder()
        {
            if (IsClientDisconnected)
            {
                return;
            }

            var parentPath = DetermineParentPathForNewFolder(_contextNode);
            var folderName = await DialogWorkflow.ShowStringFieldDialog(
                LanguageLocalizer.Translate("RSSWidget", "Please choose a folder name"),
                LanguageLocalizer.Translate("RSSWidget", "Folder name:"),
                null);
            if (string.IsNullOrWhiteSpace(folderName))
            {
                return;
            }

            var newPath = string.IsNullOrEmpty(parentPath) ? folderName : $"{parentPath}{PathSeparator}{folderName}";

            try
            {
                await ApiClient.AddRssFolder(newPath);
                await RefreshRssList(preferredPath: newPath, preferUnread: false);
            }
            catch (HttpRequestException exception)
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateRss("Unable to add folder: %1", exception.Message), Severity.Error);
            }
        }

        protected async Task UpdateContextCopyUrl()
        {
            if (_contextNode?.Feed is null)
            {
                return;
            }

            await ClipboardService.WriteToClipboard(_contextNode.Feed.Url);
            SnackbarWorkflow.ShowTransientMessage(TranslateRss("Feed URL copied to clipboard."), Severity.Info);
        }

        protected async Task DownloadItem(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            await DialogWorkflow.InvokeAddTorrentLinkDialog(url);
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected Task ShowFeedsPanel()
        {
            var previousStartPane = StartPane;
            _selectedNode = null;
            _hasNavigatedForward = false;
            _showDetailsPane = false;
            TriggerSlideAnimation(previousStartPane);
            return InvokeAsync(StateHasChanged);
        }

        protected Task ShowArticlesPanel()
        {
            var previousStartPane = StartPane;
            _showDetailsPane = false;
            TriggerSlideAnimation(previousStartPane);
            return InvokeAsync(StateHasChanged);
        }

        private async Task AddSubscriptionAtNode(RssTreeNode? node)
        {
            var url = await DialogWorkflow.ShowStringFieldDialog(
                LanguageLocalizer.Translate("RSSWidget", "Please type a RSS feed URL"),
                LanguageLocalizer.Translate("RSSWidget", "Feed URL:"),
                null);
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            var parentPath = DetermineParentPathForNewFeed(node);
            try
            {
                await ApiClient.AddRssFeed(url, string.IsNullOrEmpty(parentPath) ? null : parentPath);
                await RefreshRssList(preferredPath: parentPath, preferUnread: false);
            }
            catch (HttpRequestException exception)
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateRss("Unable to add feed: %1", exception.Message), Severity.Error);
            }
        }

        private async Task MarkNodeAsRead(RssTreeNode node)
        {
            var feeds = EnumerateFeedNodes(node).Select(n => n.Path).Distinct(StringComparer.Ordinal).ToList();
            if (feeds.Count == 0)
            {
                return;
            }

            foreach (var feedPath in feeds)
            {
                await ApiClient.MarkRssItemAsRead(feedPath);
            }

            await RefreshRssList(preferredPath: node.IsUnread ? null : node.Path, preferUnread: node.IsUnread);
        }

        private async Task RefreshFeedsForNode(RssTreeNode? node)
        {
            IEnumerable<string> feedPaths;
            if (node is null)
            {
                feedPaths = RssList?.Feeds.Values.Select(f => f.Path) ?? Enumerable.Empty<string>();
            }
            else
            {
                feedPaths = EnumerateFeedNodes(node).Select(n => n.Path);
            }

            foreach (var feedPath in feedPaths.Distinct(StringComparer.Ordinal))
            {
                await ApiClient.RefreshRssItem(feedPath);
            }

            await RefreshRssList(preferredPath: node?.Path, preferUnread: node?.IsUnread == true);
        }

        private async Task RefreshRssList(string? preferredPath = null, bool preferUnread = false)
        {
            if (_refreshInProgress)
            {
                _pendingRefresh = true;
                if (preferUnread)
                {
                    _pendingPreferredPath = null;
                }
                else if (!string.IsNullOrEmpty(preferredPath))
                {
                    _pendingPreferredPath = preferredPath;
                }

                _pendingPreferUnread = preferUnread;
                return;
            }

            _refreshInProgress = true;

            try
            {
                await GetRssList();

                if (RssList is null)
                {
                    _selectedNode = null;
                    Articles.Clear();
                    Article = null;
                    SelectedArticle = null;
                }
                else
                {
                    RssTreeNode? nodeToSelect = null;
                    if (preferUnread)
                    {
                        nodeToSelect = RssList.UnreadNode;
                    }
                    else if (!string.IsNullOrEmpty(preferredPath) && RssList.TryGetNode(preferredPath, out var preferredNode))
                    {
                        nodeToSelect = preferredNode;
                    }
                    else if (_selectedNode is not null)
                    {
                        if (_selectedNode.IsUnread)
                        {
                            nodeToSelect = RssList.UnreadNode;
                        }
                        else if (RssList.TryGetNode(_selectedNode.Path, out var existing))
                        {
                            nodeToSelect = existing;
                        }
                    }

                    if (nodeToSelect is null && RssList.TreeItems.Count > 0)
                    {
                        nodeToSelect = RssList.TreeItems[0].Node;
                    }

                    if (nodeToSelect is not null)
                    {
                        _selectedNode = nodeToSelect;
                        UpdateArticlesForSelection();
                    }
                    else
                    {
                        _selectedNode = null;
                        Articles.Clear();
                        Article = null;
                        SelectedArticle = null;
                    }
                }
            }
            finally
            {
                _refreshInProgress = false;
            }

            if (_pendingRefresh)
            {
                var pendingPreferredPath = _pendingPreferredPath;
                var pendingPreferUnread = _pendingPreferUnread;
                _pendingRefresh = false;
                _pendingPreferredPath = null;
                _pendingPreferUnread = false;

                await RefreshRssList(pendingPreferredPath, pendingPreferUnread);
            }
        }

        private void UpdateArticlesForSelection()
        {
            var previousStartPane = StartPane;
            var previousSelectedArticle = SelectedArticle;
            Articles.Clear();

            var rssList = RssList!;
            var selectedNode = _selectedNode!;

            IEnumerable<RssArticle> source;

            if (selectedNode.IsUnread)
            {
                source = rssList.Articles.Where(a => !a.IsRead);
            }
            else if (selectedNode.IsFolder)
            {
                source = rssList.Articles.Where(a => IsDescendantPath(a.Feed, selectedNode.Path));
            }
            else
            {
                source = rssList.Articles.Where(a => string.Equals(a.Feed, selectedNode.Path, StringComparison.Ordinal));
            }

            Articles.AddRange(source);

            Article = Articles.FirstOrDefault(a => a.Id == previousSelectedArticle) ?? Articles.FirstOrDefault();
            SelectedArticle = Article?.Id;

            if (_showDetailsPane && Article is null)
            {
                _showDetailsPane = false;
            }

            TriggerSlideAnimation(previousStartPane);
        }

        protected string GetArticleCssClass(RssArticle article)
        {
            var classes = new List<string> { "rss-article-item" };
            if (string.Equals(SelectedArticle, article.Id, StringComparison.Ordinal))
            {
                classes.Add("mud-selected-item");
                classes.Add("rss-article-item--selected");
            }

            return string.Join(" ", classes);
        }

        private async Task GetRssList()
        {
            var items = await ApiClient.GetAllRssItems(true);
            RssList = RssDataManager.CreateRssList(items);
        }

        private IEnumerable<RssTreeNode> EnumerateFeedNodes(RssTreeNode node)
        {
            if (node.IsUnread)
            {
                var rssList = RssList!;
                return rssList.TreeItems
                    .Select(t => t.Node)
                    .Where(n => n.Feed is not null);
            }

            if (node.Feed is not null)
            {
                return new[] { node };
            }

            return node.Children.SelectMany(EnumerateFeedNodes);
        }

        private static string DetermineParentPathForNewFeed(RssTreeNode? node)
        {
            if (node is null)
            {
                return string.Empty;
            }

            if (node.IsUnread)
            {
                return string.Empty;
            }

            if (node.IsFolder)
            {
                return node.Path;
            }

            return GetParentPath(node.Path);
        }

        private static string DetermineParentPathForNewFolder(RssTreeNode? node)
        {
            if (node is null || node.IsUnread || !node.IsFolder)
            {
                return string.Empty;
            }

            return node.Path;
        }

        private static string GetParentPath(string path)
        {
            var lastSeparator = path.LastIndexOf(PathSeparator);
            return lastSeparator == -1 ? string.Empty : path[..lastSeparator];
        }

        private static bool IsDescendantPath(string feedPath, string ancestorPath)
        {
            return feedPath.StartsWith($"{ancestorPath}{PathSeparator}", StringComparison.Ordinal);
        }

        private string TranslateRss(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppRss", source, arguments);
        }

        private int DetermineColumnCount()
        {
            if (CurrentBreakpoint >= Breakpoint.Lg)
            {
                return 3;
            }

            if (CurrentBreakpoint == Breakpoint.Md)
            {
                return CurrentOrientation == Orientation.Landscape ? 3 : 2;
            }

            if (CurrentBreakpoint == Breakpoint.Sm)
            {
                return CurrentOrientation == Orientation.Landscape ? 2 : 1;
            }

            return 1;
        }

        private void TriggerSlideAnimation(int previousStartPane)
        {
            if (ColumnCount >= 3)
            {
                return;
            }

            var currentStartPane = StartPane;
            if (currentStartPane == previousStartPane)
            {
                return;
            }

            _isAnimating = true;
            _previousStartPaneForAnimation = previousStartPane;
            _animationToken++;
            _pendingAnimation = true;
        }
    }
}
