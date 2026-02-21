using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using ClientRssArticle = Lantean.QBitTorrentClient.Models.RssArticle;
using ClientRssItem = Lantean.QBitTorrentClient.Models.RssItem;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class RssTests : RazorComponentTestBase<Rss>
    {
        private readonly Mock<IApiClient> _apiClientMock;
        private readonly Mock<IDialogWorkflow> _dialogWorkflowMock;
        private readonly Mock<ISnackbar> _snackbarMock;
        private IRenderedComponent<MudPopoverProvider>? _popoverProvider;

        public RssTests()
        {
            _apiClientMock = TestContext.UseApiClientMock(MockBehavior.Loose);
            _dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            _snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            _snackbarMock.SetupGet(snackbar => snackbar.Configuration).Returns(new SnackbarConfiguration());
            _snackbarMock.SetupGet(snackbar => snackbar.ShownSnackbars).Returns(new List<Snackbar>());

            _apiClientMock
                .Setup(client => client.GetAllRssItems(true))
                .ReturnsAsync(CreateRssItems());
        }

        [Fact]
        public void GIVEN_DesktopBreakpoint_WHEN_Rendered_THEN_ShouldShowDesktopPanesAndLoadItems()
        {
            var target = RenderTarget(breakpoint: Breakpoint.Lg, orientation: Orientation.Landscape);

            FindByTestId<MudList<RssTreeNode>>(target, "RssFeedListDesktop").Should().NotBeNull();
            FindByTestId<MudList<string>>(target, "RssArticleListDesktop").Should().NotBeNull();
            FindByTestId<MudCard>(target, "RssArticleDetailsDesktop").Should().NotBeNull();

            _apiClientMock.Verify(client => client.GetAllRssItems(true), Times.AtLeastOnce);
        }

        [Fact]
        public void GIVEN_MobileBreakpoint_WHEN_Rendered_THEN_ShouldShowMobilePane()
        {
            var target = RenderTarget(breakpoint: Breakpoint.Xs, orientation: Orientation.Portrait);

            FindByTestId<MudList<RssTreeNode>>(target, "RssFeedListMobile").Should().NotBeNull();
        }

        [Fact]
        public void GIVEN_MdLandscapeBreakpoint_WHEN_Rendered_THEN_ShouldUseDesktopLayout()
        {
            var target = RenderTarget(breakpoint: Breakpoint.Md, orientation: Orientation.Landscape);

            FindByTestId<MudList<RssTreeNode>>(target, "RssFeedListDesktop").Should().NotBeNull();
            FindByTestId<MudCard>(target, "RssArticleDetailsDesktop").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_MobileDetailsPane_WHEN_BackButtonsClicked_THEN_ShouldReturnToFeedsPane()
        {
            var target = RenderTarget(breakpoint: Breakpoint.Sm, orientation: Orientation.Portrait);
            await SelectFeedNode(target, "RssFeedNode-Feed1");
            await SelectArticle(target, "Article1");

            var backToArticles = FindByTestId<MudButton>(target, "RssBackToArticles");
            await target.InvokeAsync(() => backToArticles.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            var backToFeeds = FindByTestId<MudButton>(target, "RssBackToFeeds");
            await target.InvokeAsync(() => backToFeeds.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            FindByTestId<MudList<RssTreeNode>>(target, "RssFeedListMobile").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_MobilePaneAnimationOverlap_WHEN_TokenAdvances_THEN_PreviousAnimationShouldExitEarly()
        {
            var target = RenderTarget(breakpoint: Breakpoint.Xs, orientation: Orientation.Portrait);
            await SelectFeedNode(target, "RssFeedNode-Feed1");
            await SelectArticle(target, "Article1");

            target.WaitForAssertion(
                () => target.FindAll(".rss-slider__pane--no-scroll").Should().BeEmpty(),
                TimeSpan.FromSeconds(3));
        }

        [Fact]
        public async Task GIVEN_MobileFeedNode_WHEN_ContextMenuOpened_THEN_ShouldShowFeedActions()
        {
            var target = RenderTarget(breakpoint: Breakpoint.Xs, orientation: Orientation.Portrait);
            var node = target.Find($"{ToDataTestIdSelector("RssFeedNode-Feed1")} .rss-feed-list__item-content");

            await target.InvokeAsync(() => node.TriggerEvent("oncontextmenu", new MouseEventArgs()));

            FindPopoverByTestId<MudMenuItem>("RssContextUpdate").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_MdPortraitBreakpoint_WHEN_NavigatingToDetails_THEN_ShouldShowBackToFeedsButton()
        {
            var target = RenderTarget(breakpoint: Breakpoint.Md, orientation: Orientation.Portrait);
            await SelectFeedNode(target, "RssFeedNode-Feed1");
            await SelectArticle(target, "Article1");

            FindByTestId<MudButton>(target, "RssBackToFeeds").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_MdPortraitDetails_WHEN_BackToFeedsClicked_THEN_ShouldReturnToFeedsStage()
        {
            var target = RenderTarget(breakpoint: Breakpoint.Md, orientation: Orientation.Portrait);
            await SelectFeedNode(target, "RssFeedNode-Feed1");
            await SelectArticle(target, "Article1");
            var backToFeeds = FindByTestId<MudButton>(target, "RssBackToFeeds");

            await target.InvokeAsync(() => backToFeeds.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            FindByTestId<MudList<RssTreeNode>>(target, "RssFeedListMobile").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_BackNavigationButton_WHEN_Clicked_THEN_ShouldNavigateHome()
        {
            var target = RenderTarget(drawerOpen: false);
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            var backButton = FindByTestId<MudIconButton>(target, "RssBackToList");

            await target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            navigationManager.Uri.Should().EndWith("/");
        }

        [Fact]
        public async Task GIVEN_NewSubscriptionUrl_WHEN_NewSubscriptionClicked_THEN_ShouldAddFeedAndRefresh()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please type a RSS feed URL", "Feed URL:", null))
                .ReturnsAsync("http://new-feed");
            _apiClientMock
                .Setup(client => client.AddRssFeed("http://new-feed", null))
                .Returns(Task.CompletedTask);

            var target = RenderTarget();
            var button = FindByTestId<MudIconButton>(target, "RssNewSubscription");

            await target.InvokeAsync(() => button.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            _apiClientMock.Verify(client => client.AddRssFeed("http://new-feed", null), Times.Once);
            _apiClientMock.Verify(client => client.GetAllRssItems(true), Times.AtLeast(2));
        }

        [Fact]
        public async Task GIVEN_NewSubscriptionUrlWhitespace_WHEN_NewSubscriptionClicked_THEN_ShouldNotAddFeed()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please type a RSS feed URL", "Feed URL:", null))
                .ReturnsAsync("   ");

            var target = RenderTarget();
            var button = FindByTestId<MudIconButton>(target, "RssNewSubscription");

            await target.InvokeAsync(() => button.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            _apiClientMock.Verify(client => client.AddRssFeed(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_SelectedUnreadNode_WHEN_MarkItemsReadClicked_THEN_ShouldMarkAllFeedsAsRead()
        {
            _apiClientMock
                .Setup(client => client.MarkRssItemAsRead(It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);

            var target = RenderTarget();
            var button = FindByTestId<MudIconButton>(target, "RssMarkItemsRead");

            await target.InvokeAsync(() => button.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            _apiClientMock.Verify(client => client.MarkRssItemAsRead("Feed1", null), Times.Once);
            _apiClientMock.Verify(client => client.MarkRssItemAsRead(@"Folder\Feed2", null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_NoSelectedNode_WHEN_MarkItemsReadInvoked_THEN_ShouldReturnWithoutApiCall()
        {
            _apiClientMock
                .Setup(client => client.MarkRssItemAsRead(It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);
            var rssDataManagerMock = TestContext.AddSingletonMock<IRssDataManager>(MockBehavior.Strict);
            rssDataManagerMock
                .Setup(manager => manager.CreateRssList(It.IsAny<IReadOnlyDictionary<string, ClientRssItem>>()))
                .Returns((RssList)null!);

            var target = RenderTarget();
            var button = FindByTestId<MudIconButton>(target, "RssMarkItemsRead");

            await target.InvokeAsync(() => button.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            _apiClientMock.Verify(client => client.MarkRssItemAsRead(It.IsAny<string>(), null), Times.Never);
        }

        [Fact]
        public async Task GIVEN_SelectedNodeAndDisconnected_WHEN_MarkItemsReadInvoked_THEN_ShouldReturnWithoutApiCall()
        {
            _apiClientMock
                .Setup(client => client.MarkRssItemAsRead(It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);

            var target = RenderTarget(disconnected: true);
            await SelectFeedNode(target, "RssFeedNode-Feed1");
            var button = FindByTestId<MudIconButton>(target, "RssMarkItemsRead");

            await target.InvokeAsync(() => button.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            _apiClientMock.Verify(client => client.MarkRssItemAsRead(It.IsAny<string>(), null), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DisconnectedClient_WHEN_UpdateAllInvoked_THEN_ShouldReturnWithoutRefresh()
        {
            _apiClientMock.Setup(client => client.RefreshRssItem(It.IsAny<string>())).Returns(Task.CompletedTask);

            var target = RenderTarget(disconnected: true);
            var button = FindByTestId<MudIconButton>(target, "RssUpdateAll");

            await target.InvokeAsync(() => button.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            _apiClientMock.Verify(client => client.RefreshRssItem(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_FeedContextCopyUrl_WHEN_Clicked_THEN_ShouldCopyUrlAndNotify()
        {
            var target = RenderTarget();
            var node = target.Find($"[data-test-id=\"{TestIdHelper.For("RssFeedNode-Feed1")}\"] .rss-feed-list__item-content");
            await target.InvokeAsync(() => node.TriggerEvent("oncontextmenu", new MouseEventArgs()));

            var copyItem = FindPopoverByTestId<MudMenuItem>("RssContextCopyFeedUrl");
            await target.InvokeAsync(() => copyItem.Instance.OnClick.InvokeAsync());

            TestContext.Clipboard.PeekLast().Should().Be("http://feed1");
            _snackbarMock.Verify(snackbar => snackbar.Add("Feed URL copied to clipboard.", Severity.Info, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FeedContextDeleteFails_WHEN_Clicked_THEN_ShouldShowErrorNotification()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _apiClientMock
                .Setup(client => client.RemoveRssItem(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Delete failed"));

            var target = RenderTarget();
            var node = target.Find($"[data-test-id=\"{TestIdHelper.For("RssFeedNode-Feed1")}\"] .rss-feed-list__item-content");
            await target.InvokeAsync(() => node.TriggerEvent("oncontextmenu", new MouseEventArgs()));

            var deleteItem = FindPopoverByTestId<MudMenuItem>("RssContextDelete");
            await target.InvokeAsync(() => deleteItem.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() =>
            {
                _apiClientMock.Verify(client => client.RemoveRssItem("Feed1"), Times.Once);
                _snackbarMock.Verify(snackbar =>
                    snackbar.Add(It.Is<string>(message => message.Contains("Unable to remove RSS item")), Severity.Error, null, null), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_EmptyAreaContextMenu_WHEN_UpdateAllSelected_THEN_ShouldRefreshAllFeeds()
        {
            _apiClientMock.Setup(client => client.RefreshRssItem(It.IsAny<string>())).Returns(Task.CompletedTask);

            var target = RenderTarget();
            var container = target.Find($"[data-test-id=\"{TestIdHelper.For("RssFeedListContainerDesktop")}\"]");
            await target.InvokeAsync(() => container.TriggerEvent("oncontextmenu", new MouseEventArgs()));

            var refreshAllItem = FindPopoverByTestId<MudMenuItem>("RssContextUpdateAllFeeds");
            await target.InvokeAsync(() => refreshAllItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.RefreshRssItem("Feed1"), Times.Once);
            _apiClientMock.Verify(client => client.RefreshRssItem(@"Folder\Feed2"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_EditDownloadRulesButton_WHEN_Clicked_THEN_ShouldOpenRulesDialog()
        {
            _dialogWorkflowMock.Setup(workflow => workflow.InvokeRssRulesDialog()).Returns(Task.CompletedTask);

            var target = RenderTarget();
            var button = FindByTestId<MudIconButton>(target, "RssEditDownloadRules");

            await target.InvokeAsync(() => button.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            _dialogWorkflowMock.Verify(workflow => workflow.InvokeRssRulesDialog(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_UnreadContextUpdate_WHEN_Clicked_THEN_ShouldRefreshAllFeeds()
        {
            _apiClientMock.Setup(client => client.RefreshRssItem(It.IsAny<string>())).Returns(Task.CompletedTask);
            var target = RenderTarget();

            await OpenFeedContextMenu(target, "RssFeedNode-__unread__");
            var updateItem = FindPopoverByTestId<MudMenuItem>("RssContextUpdate");
            await target.InvokeAsync(() => updateItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.RefreshRssItem("Feed1"), Times.Once);
            _apiClientMock.Verify(client => client.RefreshRssItem(@"Folder\Feed2"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FolderContextUpdate_WHEN_Clicked_THEN_ShouldRefreshOnlyFolderFeeds()
        {
            _apiClientMock.Setup(client => client.RefreshRssItem(It.IsAny<string>())).Returns(Task.CompletedTask);
            var target = RenderTarget();

            await OpenFeedContextMenu(target, "RssFeedNode-Folder");
            var updateItem = FindPopoverByTestId<MudMenuItem>("RssContextUpdate");
            await target.InvokeAsync(() => updateItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.RefreshRssItem(@"Folder\Feed2"), Times.Once);
            _apiClientMock.Verify(client => client.RefreshRssItem("Feed1"), Times.Never);
        }

        [Fact]
        public async Task GIVEN_FeedContextMarkRead_WHEN_Clicked_THEN_ShouldMarkSingleFeedRead()
        {
            _apiClientMock
                .Setup(client => client.MarkRssItemAsRead(It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);
            var target = RenderTarget();

            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");
            var markItem = FindPopoverByTestId<MudMenuItem>("RssContextMarkItemsRead");
            await target.InvokeAsync(() => markItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.MarkRssItemAsRead("Feed1", null), Times.Once);
            _apiClientMock.Verify(client => client.MarkRssItemAsRead(@"Folder\Feed2", null), Times.Never);
        }

        [Fact]
        public async Task GIVEN_FolderContextRename_WHEN_Submitted_THEN_ShouldMoveFolder()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please choose a folder name", "Folder name:", "Folder"))
                .ReturnsAsync("RenamedFolder");
            _apiClientMock.Setup(client => client.MoveRssItem("Folder", "RenamedFolder")).Returns(Task.CompletedTask);

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Folder");

            var renameItem = FindPopoverByTestId<MudMenuItem>("RssContextRename");
            await target.InvokeAsync(() => renameItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.MoveRssItem("Folder", "RenamedFolder"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FeedContextRenameFails_WHEN_Clicked_THEN_ShouldShowRenameError()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please choose a new name for this RSS feed", "New feed name:", "Feed1"))
                .ReturnsAsync("Feed1Renamed");
            _apiClientMock
                .Setup(client => client.MoveRssItem("Feed1", "Feed1Renamed"))
                .ThrowsAsync(new HttpRequestException("rename failure"));

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            var renameItem = FindPopoverByTestId<MudMenuItem>("RssContextRename");
            await target.InvokeAsync(() => renameItem.Instance.OnClick.InvokeAsync());

            _snackbarMock.Verify(snackbar =>
                snackbar.Add(It.Is<string>(message => message.Contains("Unable to rename RSS item")), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FeedContextRenameUnchanged_WHEN_Clicked_THEN_ShouldNotMoveItem()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please choose a new name for this RSS feed", "New feed name:", "Feed1"))
                .ReturnsAsync("Feed1");

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            var renameItem = FindPopoverByTestId<MudMenuItem>("RssContextRename");
            await target.InvokeAsync(() => renameItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.MoveRssItem(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_FeedContextRenameWhitespace_WHEN_Clicked_THEN_ShouldNotMoveItem()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please choose a new name for this RSS feed", "New feed name:", "Feed1"))
                .ReturnsAsync(" ");

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            var renameItem = FindPopoverByTestId<MudMenuItem>("RssContextRename");
            await target.InvokeAsync(() => renameItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.MoveRssItem(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_EditFeedUrlFails_WHEN_Clicked_THEN_ShouldShowUrlError()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Edit feed URL...", "Feed URL:", "http://feed1"))
                .ReturnsAsync("http://changed-url");
            _apiClientMock
                .Setup(client => client.SetRssFeedUrl("Feed1", "http://changed-url"))
                .ThrowsAsync(new HttpRequestException("url failure"));

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            var editUrlItem = FindPopoverByTestId<MudMenuItem>("RssContextEditFeedUrl");
            await target.InvokeAsync(() => editUrlItem.Instance.OnClick.InvokeAsync());

            _snackbarMock.Verify(snackbar =>
                snackbar.Add(It.Is<string>(message => message.Contains("Unable to update URL")), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_EditFeedUrlSuccess_WHEN_Clicked_THEN_ShouldUpdateUrlAndRefresh()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Edit feed URL...", "Feed URL:", "http://feed1"))
                .ReturnsAsync("http://changed-url");
            _apiClientMock.Setup(client => client.SetRssFeedUrl("Feed1", "http://changed-url")).Returns(Task.CompletedTask);

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            var editUrlItem = FindPopoverByTestId<MudMenuItem>("RssContextEditFeedUrl");
            await target.InvokeAsync(() => editUrlItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.SetRssFeedUrl("Feed1", "http://changed-url"), Times.Once);
            _apiClientMock.Verify(client => client.GetAllRssItems(true), Times.AtLeast(2));
        }

        [Fact]
        public async Task GIVEN_EditFeedUrlUnchanged_WHEN_Clicked_THEN_ShouldNotCallSetUrl()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Edit feed URL...", "Feed URL:", "http://feed1"))
                .ReturnsAsync("http://feed1");

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            var editUrlItem = FindPopoverByTestId<MudMenuItem>("RssContextEditFeedUrl");
            await target.InvokeAsync(() => editUrlItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.SetRssFeedUrl(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_EditFeedUrlWhitespace_WHEN_Clicked_THEN_ShouldNotCallSetUrl()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Edit feed URL...", "Feed URL:", "http://feed1"))
                .ReturnsAsync(" ");

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            var editUrlItem = FindPopoverByTestId<MudMenuItem>("RssContextEditFeedUrl");
            await target.InvokeAsync(() => editUrlItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.SetRssFeedUrl(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DeleteCancelled_WHEN_ContextDeleteClicked_THEN_ShouldNotRemove()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            var deleteItem = FindPopoverByTestId<MudMenuItem>("RssContextDelete");
            await target.InvokeAsync(() => deleteItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.RemoveRssItem(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DeleteConfirmed_WHEN_ContextDeleteClicked_THEN_ShouldRemoveAndRefresh()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            _apiClientMock.Setup(client => client.RemoveRssItem("Feed1")).Returns(Task.CompletedTask);

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            var deleteItem = FindPopoverByTestId<MudMenuItem>("RssContextDelete");
            await target.InvokeAsync(() => deleteItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.RemoveRssItem("Feed1"), Times.Once);
            _apiClientMock.Verify(client => client.GetAllRssItems(true), Times.AtLeast(2));
        }

        [Fact]
        public async Task GIVEN_ContextNewFolderOnFolder_WHEN_Submitted_THEN_ShouldCreateFolderUnderParent()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please choose a folder name", "Folder name:", null))
                .ReturnsAsync("CreatedFolder");
            _apiClientMock.Setup(client => client.AddRssFolder(@"Folder\CreatedFolder")).Returns(Task.CompletedTask);

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Folder");

            var newFolderItem = FindPopoverByTestId<MudMenuItem>("RssContextNewFolder");
            await target.InvokeAsync(() => newFolderItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.AddRssFolder(@"Folder\CreatedFolder"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ContextNewFolderBlankName_WHEN_Submitted_THEN_ShouldNotCreateFolder()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please choose a folder name", "Folder name:", null))
                .ReturnsAsync(" ");

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Folder");

            var newFolderItem = FindPopoverByTestId<MudMenuItem>("RssContextNewFolder");
            await target.InvokeAsync(() => newFolderItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.AddRssFolder(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_FeedContextCallbacks_WHEN_ContextCleared_THEN_ShouldReturnOnNullContext()
        {
            var clipboardMock = TestContext.AddSingletonMock<IClipboardService>(MockBehavior.Loose);
            clipboardMock.Setup(service => service.WriteToClipboard(It.IsAny<string>())).Returns(Task.CompletedTask);

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            var updateCallback = FindPopoverByTestId<MudMenuItem>("RssContextUpdate").Instance.OnClick;
            var markReadCallback = FindPopoverByTestId<MudMenuItem>("RssContextMarkItemsRead").Instance.OnClick;
            var renameCallback = FindPopoverByTestId<MudMenuItem>("RssContextRename").Instance.OnClick;
            var editUrlCallback = FindPopoverByTestId<MudMenuItem>("RssContextEditFeedUrl").Instance.OnClick;
            var deleteCallback = FindPopoverByTestId<MudMenuItem>("RssContextDelete").Instance.OnClick;
            var copyUrlCallback = FindPopoverByTestId<MudMenuItem>("RssContextCopyFeedUrl").Instance.OnClick;

            var container = target.Find(ToDataTestIdSelector("RssFeedListContainerDesktop"));
            await target.InvokeAsync(() => container.TriggerEvent("oncontextmenu", new MouseEventArgs()));

            await target.InvokeAsync(() => updateCallback.InvokeAsync());
            await target.InvokeAsync(() => markReadCallback.InvokeAsync());
            await target.InvokeAsync(() => renameCallback.InvokeAsync());
            await target.InvokeAsync(() => editUrlCallback.InvokeAsync());
            await target.InvokeAsync(() => deleteCallback.InvokeAsync());
            await target.InvokeAsync(() => copyUrlCallback.InvokeAsync());

            _apiClientMock.Verify(client => client.RefreshRssItem(It.IsAny<string>()), Times.Never);
            _apiClientMock.Verify(client => client.MarkRssItemAsRead(It.IsAny<string>(), null), Times.Never);
            _apiClientMock.Verify(client => client.MoveRssItem(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _apiClientMock.Verify(client => client.SetRssFeedUrl(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _apiClientMock.Verify(client => client.RemoveRssItem(It.IsAny<string>()), Times.Never);
            clipboardMock.Verify(service => service.WriteToClipboard(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_FeedContextCallbacks_WHEN_DisconnectedAfterCapture_THEN_ShouldReturnOnDisconnectedGuards()
        {
            var mainData = CreateMainData(disconnected: false);
            var target = RenderTarget(mainData);
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            var updateCallback = FindPopoverByTestId<MudMenuItem>("RssContextUpdate").Instance.OnClick;
            var markReadCallback = FindPopoverByTestId<MudMenuItem>("RssContextMarkItemsRead").Instance.OnClick;
            var renameCallback = FindPopoverByTestId<MudMenuItem>("RssContextRename").Instance.OnClick;
            var editUrlCallback = FindPopoverByTestId<MudMenuItem>("RssContextEditFeedUrl").Instance.OnClick;
            var deleteCallback = FindPopoverByTestId<MudMenuItem>("RssContextDelete").Instance.OnClick;
            var addSubscriptionCallback = FindPopoverByTestId<MudMenuItem>("RssContextNewSubscription").Instance.OnClick;

            mainData.LostConnection = true;
            target.Render();

            await target.InvokeAsync(() => updateCallback.InvokeAsync());
            await target.InvokeAsync(() => markReadCallback.InvokeAsync());
            await target.InvokeAsync(() => renameCallback.InvokeAsync());
            await target.InvokeAsync(() => editUrlCallback.InvokeAsync());
            await target.InvokeAsync(() => deleteCallback.InvokeAsync());
            await target.InvokeAsync(() => addSubscriptionCallback.InvokeAsync());

            _apiClientMock.Verify(client => client.RefreshRssItem(It.IsAny<string>()), Times.Never);
            _apiClientMock.Verify(client => client.MarkRssItemAsRead(It.IsAny<string>(), null), Times.Never);
            _apiClientMock.Verify(client => client.MoveRssItem(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _apiClientMock.Verify(client => client.SetRssFeedUrl(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _apiClientMock.Verify(client => client.RemoveRssItem(It.IsAny<string>()), Times.Never);
            _apiClientMock.Verify(client => client.AddRssFeed(It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
            _dialogWorkflowMock.Verify(workflow => workflow.ShowStringFieldDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
            _dialogWorkflowMock.Verify(workflow => workflow.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_FolderNewFolderCallback_WHEN_DisconnectedAfterCapture_THEN_ShouldReturnWithoutDialogOrApiCall()
        {
            var mainData = CreateMainData(disconnected: false);
            var target = RenderTarget(mainData);
            await OpenFeedContextMenu(target, "RssFeedNode-Folder");

            var addFolderCallback = FindPopoverByTestId<MudMenuItem>("RssContextNewFolder").Instance.OnClick;

            mainData.LostConnection = true;
            target.Render();

            await target.InvokeAsync(() => addFolderCallback.InvokeAsync());

            _apiClientMock.Verify(client => client.AddRssFolder(It.IsAny<string>()), Times.Never);
            _dialogWorkflowMock.Verify(workflow => workflow.ShowStringFieldDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ContextNewFolderFails_WHEN_ClickedFromEmptyArea_THEN_ShouldShowError()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please choose a folder name", "Folder name:", null))
                .ReturnsAsync("TopFolder");
            _apiClientMock
                .Setup(client => client.AddRssFolder("TopFolder"))
                .ThrowsAsync(new HttpRequestException("add folder failed"));

            var target = RenderTarget();
            var container = target.Find($"[data-test-id=\"{TestIdHelper.For("RssFeedListContainerDesktop")}\"]");
            await target.InvokeAsync(() => container.TriggerEvent("oncontextmenu", new MouseEventArgs()));

            var newFolderItem = FindPopoverByTestId<MudMenuItem>("RssContextNewFolder");
            await target.InvokeAsync(() => newFolderItem.Instance.OnClick.InvokeAsync());

            _snackbarMock.Verify(snackbar =>
                snackbar.Add(It.Is<string>(message => message.Contains("Unable to add folder")), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ContextNewSubscriptionOnFolder_WHEN_Submitted_THEN_ShouldUseFolderParent()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please type a RSS feed URL", "Feed URL:", null))
                .ReturnsAsync("http://folder-feed");
            _apiClientMock.Setup(client => client.AddRssFeed("http://folder-feed", "Folder")).Returns(Task.CompletedTask);

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Folder");

            var newSubscriptionItem = FindPopoverByTestId<MudMenuItem>("RssContextNewSubscription");
            await target.InvokeAsync(() => newSubscriptionItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.AddRssFeed("http://folder-feed", "Folder"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ContextNewSubscriptionOnFeed_WHEN_Submitted_THEN_ShouldUseFeedParent()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please type a RSS feed URL", "Feed URL:", null))
                .ReturnsAsync("http://feed-child");
            _apiClientMock.Setup(client => client.AddRssFeed("http://feed-child", null)).Returns(Task.CompletedTask);

            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            var newSubscriptionItem = FindPopoverByTestId<MudMenuItem>("RssContextNewSubscription");
            await target.InvokeAsync(() => newSubscriptionItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.AddRssFeed("http://feed-child", null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ContextNewSubscriptionOnNestedFeed_WHEN_Submitted_THEN_ShouldUseFolderParent()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please type a RSS feed URL", "Feed URL:", null))
                .ReturnsAsync("http://nested-feed");
            _apiClientMock.Setup(client => client.AddRssFeed("http://nested-feed", "Folder")).Returns(Task.CompletedTask);

            var target = RenderTarget();
            await OpenFeedContextMenu(target, @"RssFeedNode-Folder\Feed2");

            var newSubscriptionItem = FindPopoverByTestId<MudMenuItem>("RssContextNewSubscription");
            await target.InvokeAsync(() => newSubscriptionItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.AddRssFeed("http://nested-feed", "Folder"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ContextNewSubscriptionFromEmptyArea_WHEN_Submitted_THEN_ShouldUseRootParent()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please type a RSS feed URL", "Feed URL:", null))
                .ReturnsAsync("http://root-feed");
            _apiClientMock.Setup(client => client.AddRssFeed("http://root-feed", null)).Returns(Task.CompletedTask);

            var target = RenderTarget();
            var container = target.Find(ToDataTestIdSelector("RssFeedListContainerDesktop"));
            await target.InvokeAsync(() => container.TriggerEvent("oncontextmenu", new MouseEventArgs()));

            var newSubscriptionItem = FindPopoverByTestId<MudMenuItem>("RssContextNewSubscription");
            await target.InvokeAsync(() => newSubscriptionItem.Instance.OnClick.InvokeAsync());

            _apiClientMock.Verify(client => client.AddRssFeed("http://root-feed", null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_NewSubscriptionFails_WHEN_Clicked_THEN_ShouldShowAddFeedError()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowStringFieldDialog("Please type a RSS feed URL", "Feed URL:", null))
                .ReturnsAsync("http://failed-feed");
            _apiClientMock
                .Setup(client => client.AddRssFeed("http://failed-feed", null))
                .ThrowsAsync(new HttpRequestException("add feed failure"));

            var target = RenderTarget();
            var button = FindByTestId<MudIconButton>(target, "RssNewSubscription");
            await target.InvokeAsync(() => button.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            _snackbarMock.Verify(snackbar =>
                snackbar.Add(It.Is<string>(message => message.Contains("Unable to add feed")), Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_DownloadArticleAction_WHEN_Clicked_THEN_ShouldOpenAddTorrentLinkDialog()
        {
            _dialogWorkflowMock.Setup(workflow => workflow.InvokeAddTorrentLinkDialog("http://torrent1")).Returns(Task.CompletedTask);

            var target = RenderTarget();
            var actionsMenu = FindByTestId<MudMenu>(target, "RssArticleActionsDesktop");

            await target.InvokeAsync(() => actionsMenu.Instance.OpenMenuAsync(new MouseEventArgs()));
            var downloadItem = FindPopoverByTestId<MudMenuItem>("RssDownloadTorrentDesktop");
            await target.InvokeAsync(() => downloadItem.Instance.OnClick.InvokeAsync());

            _dialogWorkflowMock.Verify(workflow => workflow.InvokeAddTorrentLinkDialog("http://torrent1"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_DownloadArticleActionWithMissingUrl_WHEN_Clicked_THEN_ShouldNotOpenAddTorrentLinkDialog()
        {
            var items = CreateRssItems(article1TorrentUrl: null);
            var target = RenderTarget(rssItems: items);
            var actionsMenu = FindByTestId<MudMenu>(target, "RssArticleActionsDesktop");

            await target.InvokeAsync(() => actionsMenu.Instance.OpenMenuAsync(new MouseEventArgs()));
            var downloadItem = FindPopoverByTestId<MudMenuItem>("RssDownloadTorrentDesktop");
            await target.InvokeAsync(() => downloadItem.Instance.OnClick.InvokeAsync());

            _dialogWorkflowMock.Verify(workflow => workflow.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DesktopArticleActions_WHEN_Opened_THEN_ShouldRenderNewsLinkAction()
        {
            var target = RenderTarget();
            var actionsMenu = FindByTestId<MudMenu>(target, "RssArticleActionsDesktop");

            await target.InvokeAsync(() => actionsMenu.Instance.OpenMenuAsync(new MouseEventArgs()));

            FindPopoverByTestId<MudMenuItem>("RssOpenNewsUrlDesktop").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_DesktopArticleWithoutNewsLink_WHEN_Opened_THEN_ShouldNotRenderNewsLinkAction()
        {
            var items = CreateRssItems(includeArticleLink: false);
            var target = RenderTarget(rssItems: items);
            var actionsMenu = FindByTestId<MudMenu>(target, "RssArticleActionsDesktop");

            await target.InvokeAsync(() => actionsMenu.Instance.OpenMenuAsync(new MouseEventArgs()));

            FindPopoverByTestIdOrDefault<MudMenuItem>("RssOpenNewsUrlDesktop").Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_MobileArticleActions_WHEN_Opened_THEN_ShouldRenderNewsLinkAction()
        {
            var target = RenderTarget(breakpoint: Breakpoint.Xs, orientation: Orientation.Portrait);
            await SelectFeedNode(target, "RssFeedNode-Feed1");
            await SelectArticle(target, "Article1");

            var actionsMenu = FindByTestId<MudMenu>(target, "RssArticleActionsMobile");
            await target.InvokeAsync(() => actionsMenu.Instance.OpenMenuAsync(new MouseEventArgs()));

            FindPopoverByTestId<MudMenuItem>("RssOpenNewsUrlMobile").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_ReadArticleSelected_WHEN_Clicked_THEN_ShouldNotMarkReadAgain()
        {
            _apiClientMock
                .Setup(client => client.MarkRssItemAsRead(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            var target = RenderTarget();
            await SelectFeedNode(target, "RssFeedNode-Feed1");
            await SelectArticle(target, "Article2");

            _apiClientMock.Verify(client => client.MarkRssItemAsRead("Feed1", "Article2"), Times.Never);
        }

        [Fact]
        public async Task GIVEN_UnreadArticleSelected_WHEN_Clicked_THEN_ShouldMarkAsReadWithArticleId()
        {
            _apiClientMock
                .Setup(client => client.MarkRssItemAsRead(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            var target = RenderTarget();
            await SelectFeedNode(target, "RssFeedNode-Feed1");
            await SelectArticle(target, "Article1");

            _apiClientMock.Verify(client => client.MarkRssItemAsRead("Feed1", "Article1"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_UnknownSelectedArticle_WHEN_SelectedValueChanged_THEN_ShouldIgnore()
        {
            var target = RenderTarget();
            var articleList = FindByTestId<MudList<string>>(target, "RssArticleListDesktop");

            await target.InvokeAsync(() => articleList.Instance.SelectedValueChanged.InvokeAsync("missing-id"));

            _apiClientMock.Verify(client => client.MarkRssItemAsRead(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ArticleListCallbackAfterRssListCleared_WHEN_SelectedValueChanged_THEN_ShouldReturn()
        {
            var rssDataManager = new RssDataManager();
            var rssDataManagerMock = TestContext.AddSingletonMock<IRssDataManager>(MockBehavior.Strict);
            rssDataManagerMock
                .SetupSequence(manager => manager.CreateRssList(It.IsAny<IReadOnlyDictionary<string, ClientRssItem>>()))
                .Returns(rssDataManager.CreateRssList(CreateRssItems()))
                .Returns((RssList)null!);
            _apiClientMock.Setup(client => client.RefreshRssItem(It.IsAny<string>())).Returns(Task.CompletedTask);

            var target = RenderTarget();
            var articleList = FindByTestId<MudList<string>>(target, "RssArticleListDesktop");
            var selectedValueChanged = articleList.Instance.SelectedValueChanged;
            var refreshButton = FindByTestId<MudIconButton>(target, "RssUpdateAll");

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            await target.InvokeAsync(() => selectedValueChanged.InvokeAsync("Article1"));

            _apiClientMock.Verify(client => client.MarkRssItemAsRead(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NullFeedNodeValue_WHEN_SelectedValueChanged_THEN_ShouldIgnore()
        {
            var target = RenderTarget();
            var feedList = FindByTestId<MudList<RssTreeNode>>(target, "RssFeedListDesktop");

            await target.InvokeAsync(() => feedList.Instance.SelectedValueChanged.InvokeAsync((RssTreeNode?)null));

            target.Find(ToDataTestIdSelector("RssFeedListDesktop")).Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_SameFeedNodeValue_WHEN_SelectedValueChanged_THEN_ShouldReturnWithoutStateReset()
        {
            var target = RenderTarget();
            await SelectFeedNode(target, "RssFeedNode-Feed1");
            await SelectArticle(target, "Article1");
            var feedList = FindByTestId<MudList<RssTreeNode>>(target, "RssFeedListDesktop");
            var selectedNode = feedList.Instance.GetState(x => x.SelectedValue);

            await target.InvokeAsync(() => feedList.Instance.SelectedValueChanged.InvokeAsync(selectedNode));

            target.Find(ToDataTestIdSelector("RssArticle-Article1")).Should().NotBeNull();
        }

        [Fact]
        public void GIVEN_FeedStatusVariants_WHEN_Rendered_THEN_ShouldEvaluateLoadingAndErrorBranches()
        {
            var items = CreateRssItems(feed1Loading: true, feed2HasError: true);
            var target = RenderTarget(rssItems: items);

            target.Find(ToDataTestIdSelector("RssFeedNode-Feed1")).Should().NotBeNull();
            target.Find(ToDataTestIdSelector(@"RssFeedNode-Folder\Feed2")).Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_DisconnectedFeedContext_WHEN_Opened_THEN_ShouldOnlyExposeCopyAction()
        {
            var target = RenderTarget(disconnected: true);
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            FindPopoverByTestId<MudMenuItem>("RssContextCopyFeedUrl").Should().NotBeNull();
            FindPopoverByTestIdOrDefault<MudMenuItem>("RssContextUpdate").Should().BeNull();
            FindPopoverByTestIdOrDefault<MudMenuItem>("RssContextMarkItemsRead").Should().BeNull();
            FindPopoverByTestIdOrDefault<MudMenuItem>("RssContextNewSubscription").Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ConnectedFeedContext_WHEN_Opened_THEN_ShouldNotExposeNewFolderAction()
        {
            var target = RenderTarget();
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");

            FindPopoverByTestIdOrDefault<MudMenuItem>("RssContextNewFolder").Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ContextMenuOpenedWithoutSelection_WHEN_Opened_THEN_ShouldNotExposeNewFolderAction()
        {
            var target = RenderTarget();
            var menu = FindByTestId<MudMenu>(target, "RssFeedContextMenu");

            await target.InvokeAsync(() => menu.Instance.OpenMenuAsync(new MouseEventArgs()));

            FindPopoverByTestIdOrDefault<MudMenuItem>("RssContextNewFolder").Should().BeNull();
        }

        [Fact]
        public void GIVEN_AllArticlesRead_WHEN_Rendered_THEN_ShouldShowSkeletonBranches()
        {
            var items = CreateRssItems(article1Read: true, article2Read: true, article3Read: true);
            var target = RenderTarget(rssItems: items);

            target.FindComponents<MudSkeleton>().Should().NotBeEmpty();
        }

        [Fact]
        public void GIVEN_AllArticlesReadOnMobile_WHEN_Rendered_THEN_ShouldShowMobileSkeletons()
        {
            var items = CreateRssItems(article1Read: true, article2Read: true, article3Read: true);
            var target = RenderTarget(breakpoint: Breakpoint.Xs, orientation: Orientation.Portrait, rssItems: items);

            target.FindComponents<MudSkeleton>().Count.Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public void GIVEN_SmLandscapeBreakpoint_WHEN_Rendered_THEN_ShouldUseTwoColumnMobileLayout()
        {
            var target = RenderTarget(breakpoint: Breakpoint.Sm, orientation: Orientation.Landscape);

            FindByTestId<MudList<RssTreeNode>>(target, "RssFeedListMobile").Should().NotBeNull();
        }

        [Fact]
        public void GIVEN_MainDataMissing_WHEN_Rendered_THEN_ShouldTreatAsConnectedState()
        {
            var target = TestContext.Render<Rss>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue(Breakpoint.Lg);
                parameters.AddCascadingValue(Orientation.Landscape);
            });

            FindByTestId<MudIconButton>(target, "RssNewSubscription").Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_DetailsPaneAndNoUnreadArticles_WHEN_UnreadNodeSelected_THEN_ShouldCloseDetailsPane()
        {
            var items = CreateRssItems(article1Read: false, article2Read: true, article3Read: true);
            _apiClientMock
                .Setup(client => client.MarkRssItemAsRead(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            var target = RenderTarget(breakpoint: Breakpoint.Xs, orientation: Orientation.Portrait, rssItems: items);

            await SelectFeedNode(target, "RssFeedNode-Feed1");
            await SelectArticle(target, "Article1");
            await SelectFeedNode(target, "RssFeedNode-__unread__");

            FindByTestId<MudButton>(target, "RssBackToFeeds").Should().NotBeNull();
            HasComponentByTestId<MudButton>(target, "RssBackToArticles").Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_SelectedArticleChangedFromDifferentFeed_WHEN_SelectedValueChanged_THEN_ShouldMarkRead()
        {
            _apiClientMock
                .Setup(client => client.MarkRssItemAsRead(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            var target = RenderTarget();
            await SelectFeedNode(target, "RssFeedNode-Feed1");
            var articleList = FindByTestId<MudList<string>>(target, "RssArticleListDesktop");

            await target.InvokeAsync(() => articleList.Instance.SelectedValueChanged.InvokeAsync("Article3"));

            _apiClientMock.Verify(client => client.MarkRssItemAsRead(@"Folder\Feed2", "Article3"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_DetailsPaneVisible_WHEN_RefreshRemovesArticles_THEN_ShouldExitDetailsPane()
        {
            var initialItems = CreateRssItems(article1Read: false, article2Read: true, article3Read: true);
            var refreshedItems = CreateRssItemsWithoutFeed1Articles();
            _apiClientMock
                .SetupSequence(client => client.GetAllRssItems(true))
                .ReturnsAsync(initialItems)
                .ReturnsAsync(refreshedItems);
            _apiClientMock.Setup(client => client.RefreshRssItem(It.IsAny<string>())).Returns(Task.CompletedTask);
            _apiClientMock
                .Setup(client => client.MarkRssItemAsRead(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var target = RenderTarget(breakpoint: Breakpoint.Xs, orientation: Orientation.Portrait);
            await SelectFeedNode(target, "RssFeedNode-Feed1");
            await SelectArticle(target, "Article1");
            var refreshButton = FindByTestId<MudIconButton>(target, "RssUpdateAll");

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            HasComponentByTestId<MudButton>(target, "RssBackToArticles").Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_SelectedFeedRemovedOnRefresh_WHEN_Updated_THEN_ShouldFallbackToFirstTreeNode()
        {
            _apiClientMock
                .SetupSequence(client => client.GetAllRssItems(true))
                .ReturnsAsync(CreateRssItems())
                .ReturnsAsync(CreateRssItemsOnlyFeed2());
            _apiClientMock.Setup(client => client.RefreshRssItem(It.IsAny<string>())).Returns(Task.CompletedTask);

            var target = RenderTarget();
            await SelectFeedNode(target, "RssFeedNode-Feed1");
            var refreshButton = FindByTestId<MudIconButton>(target, "RssUpdateAll");

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            target.Find(ToDataTestIdSelector("RssArticle-Article3")).Should().NotBeNull();
            target.FindAll(ToDataTestIdSelector("RssArticle-Article1")).Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_RefreshInProgress_WHEN_AdditionalRefreshRequestsArrive_THEN_ShouldQueueAndProcessPendingRefresh()
        {
            var refreshGate = new TaskCompletionSource<IReadOnlyDictionary<string, ClientRssItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callCount = 0;
            _apiClientMock
                .Setup(client => client.GetAllRssItems(true))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return Task.FromResult((IReadOnlyDictionary<string, ClientRssItem>)CreateRssItems());
                    }

                    if (callCount == 2)
                    {
                        return refreshGate.Task;
                    }

                    return Task.FromResult((IReadOnlyDictionary<string, ClientRssItem>)CreateRssItems());
                });
            _apiClientMock.Setup(client => client.RefreshRssItem(It.IsAny<string>())).Returns(Task.CompletedTask);
            _apiClientMock.Setup(client => client.MarkRssItemAsRead(It.IsAny<string>(), null)).Returns(Task.CompletedTask);

            var target = RenderTarget();
            var refreshButton = FindByTestId<MudIconButton>(target, "RssUpdateAll");
            var refreshTask = target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            target.WaitForState(() => callCount >= 2);

            var markReadButton = FindByTestId<MudIconButton>(target, "RssMarkItemsRead");
            await target.InvokeAsync(() => markReadButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            await OpenFeedContextMenu(target, "RssFeedNode-Feed1");
            var contextUpdate = FindPopoverByTestId<MudMenuItem>("RssContextUpdate");
            await target.InvokeAsync(() => contextUpdate.Instance.OnClick.InvokeAsync());

            refreshGate.SetResult(CreateRssItems());
            await refreshTask;
            target.WaitForState(() => callCount >= 3);

            _apiClientMock.Verify(client => client.GetAllRssItems(true), Times.AtLeast(3));
        }

        [Fact]
        public async Task GIVEN_NoFeedsAvailable_WHEN_MarkItemsReadInvokedOnUnreadNode_THEN_ShouldNotCallMarkAsRead()
        {
            _apiClientMock
                .Setup(client => client.MarkRssItemAsRead(It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);

            var target = RenderTarget(rssItems: CreateEmptyRssItems());
            await SelectFeedNode(target, "RssFeedNode-__unread__");
            var markReadButton = FindByTestId<MudIconButton>(target, "RssMarkItemsRead");

            await target.InvokeAsync(() => markReadButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            _apiClientMock.Verify(client => client.MarkRssItemAsRead(It.IsAny<string>(), null), Times.Never);
        }

        [Fact]
        public void GIVEN_RssDataManagerReturnsNull_WHEN_Rendered_THEN_ShouldRenderWithEmptyTree()
        {
            var rssDataManagerMock = TestContext.AddSingletonMock<IRssDataManager>(MockBehavior.Strict);
            rssDataManagerMock
                .Setup(manager => manager.CreateRssList(It.IsAny<IReadOnlyDictionary<string, ClientRssItem>>()))
                .Returns((RssList)null!);

            var target = RenderTarget();

            FindByTestId<MudList<RssTreeNode>>(target, "RssFeedListDesktop").Should().NotBeNull();
        }

        private IRenderedComponent<Rss> RenderTarget(
            bool drawerOpen = false,
            bool disconnected = false,
            Breakpoint breakpoint = Breakpoint.Lg,
            Orientation orientation = Orientation.Landscape,
            IReadOnlyDictionary<string, ClientRssItem>? rssItems = null)
        {
            if (rssItems is not null)
            {
                _apiClientMock
                    .Setup(client => client.GetAllRssItems(true))
                    .ReturnsAsync(rssItems);
            }

            var mainData = CreateMainData(disconnected);

            return RenderTarget(mainData, drawerOpen, breakpoint, orientation);
        }

        private IRenderedComponent<Rss> RenderTarget(
            MainData mainData,
            bool drawerOpen = false,
            Breakpoint breakpoint = Breakpoint.Lg,
            Orientation orientation = Orientation.Landscape)
        {
            return TestContext.Render<Rss>(parameters =>
            {
                parameters.AddCascadingValue(mainData);
                parameters.AddCascadingValue("DrawerOpen", drawerOpen);
                parameters.AddCascadingValue(breakpoint);
                parameters.AddCascadingValue(orientation);
            });
        }

        private static MainData CreateMainData(bool disconnected)
        {
            return new MainData(
                new Dictionary<string, Torrent>(),
                new List<string>(),
                new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new ServerState(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>())
            {
                LostConnection = disconnected
            };
        }

        private static IReadOnlyDictionary<string, ClientRssItem> CreateRssItems(
            bool includeArticleLink = true,
            bool feed1Loading = false,
            bool feed2HasError = false,
            bool article1Read = false,
            bool article2Read = true,
            bool article3Read = false,
            string? article1TorrentUrl = "http://torrent1")
        {
            var article1Link = includeArticleLink ? "http://news1" : null;
            var article3Link = includeArticleLink ? "http://news3" : null;

            return new Dictionary<string, ClientRssItem>(StringComparer.Ordinal)
            {
                ["Feed1"] = new ClientRssItem(
                    new List<ClientRssArticle>
                    {
                        new ClientRssArticle("Category", "Comments", "2020-01-01", "Description", "Article1", article1Link, null, "Article 1", article1TorrentUrl, article1Read),
                        new ClientRssArticle("Category", "Comments", "2020-01-02", "Description", "Article2", "http://news2", null, "Article 2", "http://torrent2", article2Read)
                    },
                    false,
                    feed1Loading,
                    "2020-01-01",
                    "Feed 1",
                    "Feed1Uid",
                    "http://feed1"),
                [@"Folder\Feed2"] = new ClientRssItem(
                    new List<ClientRssArticle>
                    {
                        new ClientRssArticle("Category", "Comments", "2020-01-03", "Description", "Article3", article3Link, null, "Article 3", "http://torrent3", article3Read)
                    },
                    feed2HasError,
                    false,
                    "2020-01-01",
                    "Feed 2",
                    "Feed2Uid",
                    "http://feed2")
            };
        }

        private static IReadOnlyDictionary<string, ClientRssItem> CreateRssItemsOnlyFeed2()
        {
            return new Dictionary<string, ClientRssItem>(StringComparer.Ordinal)
            {
                [@"Folder\Feed2"] = new ClientRssItem(
                    new List<ClientRssArticle>
                    {
                        new ClientRssArticle("Category", "Comments", "2020-01-03", "Description", "Article3", "http://news3", null, "Article 3", "http://torrent3", false)
                    },
                    false,
                    false,
                    "2020-01-01",
                    "Feed 2",
                    "Feed2Uid",
                    "http://feed2")
            };
        }

        private static IReadOnlyDictionary<string, ClientRssItem> CreateEmptyRssItems()
        {
            return new Dictionary<string, ClientRssItem>(StringComparer.Ordinal);
        }

        private static IReadOnlyDictionary<string, ClientRssItem> CreateRssItemsWithoutFeed1Articles()
        {
            return new Dictionary<string, ClientRssItem>(StringComparer.Ordinal)
            {
                ["Feed1"] = new ClientRssItem(
                    new List<ClientRssArticle>(),
                    false,
                    false,
                    "2020-01-01",
                    "Feed 1",
                    "Feed1Uid",
                    "http://feed1"),
                [@"Folder\Feed2"] = new ClientRssItem(
                    new List<ClientRssArticle>
                    {
                        new ClientRssArticle("Category", "Comments", "2020-01-03", "Description", "Article3", "http://news3", null, "Article 3", "http://torrent3", true)
                    },
                    false,
                    false,
                    "2020-01-01",
                    "Feed 2",
                    "Feed2Uid",
                    "http://feed2")
            };
        }

        private static IRenderedComponent<TComponent> FindByTestId<TComponent>(IRenderedComponent<Rss> target, string testId)
            where TComponent : IComponent
        {
            return target.FindComponents<TComponent>().First(component =>
            {
                var element = component.FindAll($"[data-test-id='{TestIdHelper.For(testId)}']");
                return element.Count > 0;
            });
        }

        private IRenderedComponent<TComponent> FindPopoverByTestId<TComponent>(string testId)
            where TComponent : IComponent
        {
            return EnsurePopoverProvider().FindComponents<TComponent>().First(component =>
            {
                var element = component.FindAll($"[data-test-id='{TestIdHelper.For(testId)}']");
                return element.Count > 0;
            });
        }

        private IRenderedComponent<TComponent>? FindPopoverByTestIdOrDefault<TComponent>(string testId)
            where TComponent : IComponent
        {
            return EnsurePopoverProvider().FindComponents<TComponent>().FirstOrDefault(component =>
            {
                var element = component.FindAll($"[data-test-id='{TestIdHelper.For(testId)}']");
                return element.Count > 0;
            });
        }

        private static bool HasComponentByTestId<TComponent>(IRenderedComponent<Rss> target, string testId)
            where TComponent : IComponent
        {
            return target.FindComponents<TComponent>().Any(component => component.FindAll(ToDataTestIdSelector(testId)).Count > 0);
        }

        private async Task OpenFeedContextMenu(IRenderedComponent<Rss> target, string nodeTestId)
        {
            var node = target.Find($"{ToDataTestIdSelector(nodeTestId)} .rss-feed-list__item-content");
            await target.InvokeAsync(() => node.TriggerEvent("oncontextmenu", new MouseEventArgs()));
        }

        private async Task SelectFeedNode(IRenderedComponent<Rss> target, string nodeTestId)
        {
            var node = target.Find(ToDataTestIdSelector(nodeTestId));
            await target.InvokeAsync(() => node.Click());
        }

        private async Task SelectArticle(IRenderedComponent<Rss> target, string articleId)
        {
            var article = target.Find(ToDataTestIdSelector($"RssArticle-{articleId}"));
            await target.InvokeAsync(() => article.Click());
        }

        private static string ToDataTestIdSelector(string testId)
        {
            var testIdValue = TestIdHelper.For(testId) ?? string.Empty;
            return $"[data-test-id=\"{testIdValue.Replace("\\", "\\\\", StringComparison.Ordinal)}\"]";
        }

        private IRenderedComponent<MudPopoverProvider> EnsurePopoverProvider()
        {
            if (_popoverProvider is null)
            {
                _popoverProvider = TestContext.Render<MudPopoverProvider>();
            }

            return _popoverProvider;
        }
    }
}
