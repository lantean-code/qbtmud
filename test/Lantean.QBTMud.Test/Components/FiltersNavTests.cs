using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;
using System.Net;
using ClientCategory = QBittorrent.ApiClient.Models.Category;
using ClientTorrent = QBittorrent.ApiClient.Models.Torrent;
using MudCategory = Lantean.QBTMud.Models.Category;
using MudMainData = Lantean.QBTMud.Models.MainData;
using MudServerState = Lantean.QBTMud.Models.ServerState;
using MudTorrent = Lantean.QBTMud.Models.Torrent;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class FiltersNavTests : RazorComponentTestBase
    {
        private const string _statusStorageKey = "FiltersNav.Selection.Status";
        private const string _categoryStorageKey = "FiltersNav.Selection.Category";
        private const string _tagStorageKey = "FiltersNav.Selection.Tag";
        private const string _trackerStorageKey = "FiltersNav.Selection.Tracker";

        private IRenderedComponent<MudPopoverProvider>? _popoverProvider;

        [Fact]
        public async Task GIVEN_LocalStorageSelections_WHEN_Initialized_THEN_CallbacksInvokedAndLinksActive()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();
            var queryState = TestContext.Services.GetRequiredService<ITorrentQueryState>();

            await TestContext.LocalStorage.SetItemAsStringAsync(_statusStorageKey, Status.Downloading.ToString(), Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsStringAsync(_categoryStorageKey, "Movies", Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsStringAsync(_tagStorageKey, "Tag1", Xunit.TestContext.Current.CancellationToken);
            await TestContext.LocalStorage.SetItemAsStringAsync(_trackerStorageKey, "tracker.example.com", Xunit.TestContext.Current.CancellationToken);

            var target = RenderFiltersNav(mainData, CreatePreferences(useSubcategories: true, confirmDeletion: true));

            queryState.Status.Should().Be(Status.Downloading);
            queryState.Category.Should().Be("Movies");
            queryState.Tag.Should().Be("Tag1");
            queryState.Tracker.Should().Be("tracker.example.com");

            FindComponentByTestId<CustomNavLink>(target, "Status-Downloading").Instance.Active.Should().BeTrue();
            FindComponentByTestId<CustomNavLink>(target, "Category-Movies").Instance.Active.Should().BeTrue();
            FindComponentByTestId<CustomNavLink>(target, "Tag-Tag1").Instance.Active.Should().BeTrue();
            FindComponentByTestId<CustomNavLink>(target, "Tracker-tracker.example.com").Instance.Active.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_NoLocalStorage_WHEN_Initialized_THEN_DefaultsActive()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();
            var queryState = TestContext.Services.GetRequiredService<ITorrentQueryState>();

            var target = RenderFiltersNav(mainData, null);

            queryState.Status.Should().Be(Status.All);
            queryState.Category.Should().Be(FilterHelper.CATEGORY_ALL);
            queryState.Tag.Should().Be(FilterHelper.TAG_ALL);
            queryState.Tracker.Should().Be(FilterHelper.TRACKER_ALL);

            FindComponentByTestId<CustomNavLink>(target, "Status-All").Instance.Active.Should().BeTrue();
            FindComponentByTestId<CustomNavLink>(target, "Category-All").Instance.Active.Should().BeTrue();
            FindComponentByTestId<CustomNavLink>(target, "Tag-All").Instance.Active.Should().BeTrue();
            FindComponentByTestId<CustomNavLink>(target, "Tracker-All").Instance.Active.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_StatusSelection_WHEN_Clicked_THEN_StorageUpdated()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();
            var queryState = TestContext.Services.GetRequiredService<ITorrentQueryState>();

            var target = RenderFiltersNav(mainData, null);

            var downloading = FindComponentByTestId<CustomNavLink>(target, "Status-Downloading");
            await target.InvokeAsync(() => downloading.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            queryState.Status.Should().Be(Status.Downloading);
            (await TestContext.LocalStorage.GetItemAsStringAsync(_statusStorageKey, Xunit.TestContext.Current.CancellationToken)).Should().Be(Status.Downloading.ToString());

            var all = FindComponentByTestId<CustomNavLink>(target, "Status-All");
            await target.InvokeAsync(() => all.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            queryState.Status.Should().Be(Status.All);
            (await TestContext.LocalStorage.GetItemAsStringAsync(_statusStorageKey, Xunit.TestContext.Current.CancellationToken)).Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_CategorySelection_WHEN_Clicked_THEN_StorageUpdated()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();
            var queryState = TestContext.Services.GetRequiredService<ITorrentQueryState>();

            var target = RenderFiltersNav(mainData, null);

            var movies = FindComponentByTestId<CustomNavLink>(target, "Category-Movies");
            await target.InvokeAsync(() => movies.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            queryState.Category.Should().Be("Movies");
            (await TestContext.LocalStorage.GetItemAsStringAsync(_categoryStorageKey, Xunit.TestContext.Current.CancellationToken)).Should().Be("Movies");

            var all = FindComponentByTestId<CustomNavLink>(target, "Category-All");
            await target.InvokeAsync(() => all.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            queryState.Category.Should().Be(FilterHelper.CATEGORY_ALL);
            (await TestContext.LocalStorage.GetItemAsStringAsync(_categoryStorageKey, Xunit.TestContext.Current.CancellationToken)).Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_TagSelection_WHEN_Clicked_THEN_StorageUpdated()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();
            var queryState = TestContext.Services.GetRequiredService<ITorrentQueryState>();

            var target = RenderFiltersNav(mainData, null);

            var tagLink = FindComponentByTestId<CustomNavLink>(target, "Tag-Tag1");
            await target.InvokeAsync(() => tagLink.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            queryState.Tag.Should().Be("Tag1");
            (await TestContext.LocalStorage.GetItemAsStringAsync(_tagStorageKey, Xunit.TestContext.Current.CancellationToken)).Should().Be("Tag1");

            var all = FindComponentByTestId<CustomNavLink>(target, "Tag-All");
            await target.InvokeAsync(() => all.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            queryState.Tag.Should().Be(FilterHelper.TAG_ALL);
            (await TestContext.LocalStorage.GetItemAsStringAsync(_tagStorageKey, Xunit.TestContext.Current.CancellationToken)).Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_TrackerSelection_WHEN_Clicked_THEN_StorageUpdated()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();
            var queryState = TestContext.Services.GetRequiredService<ITorrentQueryState>();

            var target = RenderFiltersNav(mainData, null);

            var trackerLink = FindComponentByTestId<CustomNavLink>(target, "Tracker-tracker.example.com");
            await target.InvokeAsync(() => trackerLink.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            queryState.Tracker.Should().Be("tracker.example.com");
            (await TestContext.LocalStorage.GetItemAsStringAsync(_trackerStorageKey, Xunit.TestContext.Current.CancellationToken)).Should().Be("tracker.example.com");

            var all = FindComponentByTestId<CustomNavLink>(target, "Tracker-All");
            await target.InvokeAsync(() => all.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            queryState.Tracker.Should().Be(FilterHelper.TRACKER_ALL);
            (await TestContext.LocalStorage.GetItemAsStringAsync(_trackerStorageKey, Xunit.TestContext.Current.CancellationToken)).Should().BeNull();
        }

        [Fact]
        public void GIVEN_AllStatusValues_WHEN_Rendered_THEN_DisplaysExpectedStatusLabels()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();
            mainData.StatusState[Status.Completed.ToString()] = new HashSet<string> { "Hash1" };
            mainData.StatusState[Status.Stopped.ToString()] = new HashSet<string> { "Hash1" };
            mainData.StatusState[Status.Active.ToString()] = new HashSet<string> { "Hash1" };
            mainData.StatusState[Status.Inactive.ToString()] = new HashSet<string> { "Hash1" };
            mainData.StatusState[Status.StalledUploading.ToString()] = new HashSet<string> { "Hash1" };
            mainData.StatusState[Status.StalledDownloading.ToString()] = new HashSet<string> { "Hash1" };
            mainData.StatusState[Status.Checking.ToString()] = new HashSet<string> { "Hash1" };
            mainData.StatusState[Status.Errored.ToString()] = new HashSet<string> { "Hash1" };

            var target = RenderFiltersNav(mainData);

            GetChildContentText(FindComponentByTestId<CustomNavLink>(target, "Status-Completed").Instance.ChildContent).Should().Contain("Completed");
            GetChildContentText(FindComponentByTestId<CustomNavLink>(target, "Status-Stopped").Instance.ChildContent).Should().Contain("Stopped");
            GetChildContentText(FindComponentByTestId<CustomNavLink>(target, "Status-Active").Instance.ChildContent).Should().Contain("Active");
            GetChildContentText(FindComponentByTestId<CustomNavLink>(target, "Status-Inactive").Instance.ChildContent).Should().Contain("Inactive");
            GetChildContentText(FindComponentByTestId<CustomNavLink>(target, "Status-StalledUploading").Instance.ChildContent).Should().Contain("Stalled Uploading");
            GetChildContentText(FindComponentByTestId<CustomNavLink>(target, "Status-StalledDownloading").Instance.ChildContent).Should().Contain("Stalled Downloading");
            GetChildContentText(FindComponentByTestId<CustomNavLink>(target, "Status-Checking").Instance.ChildContent).Should().Contain("Checking");
            GetChildContentText(FindComponentByTestId<CustomNavLink>(target, "Status-Errored").Instance.ChildContent).Should().Contain("Errored");
        }

        [Fact]
        public async Task GIVEN_CategoryContextMenu_WHEN_TargetAllAndCustom_THEN_MenuItemsToggle()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();

            var target = RenderFiltersNav(mainData);

            var allLink = FindComponentByTestId<CustomNavLink>(target, "Category-All");
            await target.InvokeAsync(() => allLink.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            _popoverProvider!.FindComponents<MudMenuItem>()
                .Any(item => HasTestId(item, "CategoryEdit"))
                .Should().BeFalse();

            var moviesLink = FindComponentByTestId<CustomNavLink>(target, "Category-Movies");
            await target.InvokeAsync(() => moviesLink.Instance.OnLongPress.InvokeAsync(new LongPressEventArgs()));

            WaitForMenuItemByTestId("CategoryEdit").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_TagContextMenu_WHEN_TargetAllAndCustom_THEN_MenuItemsToggle()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();

            var target = RenderFiltersNav(mainData);

            var allLink = FindComponentByTestId<CustomNavLink>(target, "Tag-All");
            await target.InvokeAsync(() => allLink.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            _popoverProvider!.FindComponents<MudMenuItem>()
                .Any(item => HasTestId(item, "TagRemove"))
                .Should().BeFalse();

            var tagLink = FindComponentByTestId<CustomNavLink>(target, "Tag-Tag1");
            await target.InvokeAsync(() => tagLink.Instance.OnLongPress.InvokeAsync(new LongPressEventArgs()));

            WaitForMenuItemByTestId("TagRemove").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_StatusAndTrackerContextMenus_WHEN_Invoked_THEN_RemoveTrackerDisabledToggles()
        {
            TestContext.UseApiClientMock();
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();

            var target = RenderFiltersNav(mainData);

            var downloading = FindComponentByTestId<CustomNavLink>(target, "Status-Downloading");
            await target.InvokeAsync(() => downloading.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var seeding = FindComponentByTestId<CustomNavLink>(target, "Status-Seeding");
            await target.InvokeAsync(() => seeding.Instance.OnLongPress.InvokeAsync(new LongPressEventArgs()));

            var allTracker = FindComponentByTestId<CustomNavLink>(target, "Tracker-All");
            await target.InvokeAsync(() => allTracker.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var removeTracker = WaitForMenuItemByTestId("TrackerRemove");
            removeTracker.Instance.Disabled.Should().BeTrue();

            var hostTracker = FindComponentByTestId<CustomNavLink>(target, "Tracker-tracker.example.com");
            await target.InvokeAsync(() => hostTracker.Instance.OnLongPress.InvokeAsync(new LongPressEventArgs()));

            removeTracker = WaitForMenuItemByTestId("TrackerRemove");
            removeTracker.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_CategoryMenuActions_WHEN_Clicked_THEN_DialogsAndApiInvoked()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var mainData = CreateMainData();

            dialogMock.Setup(d => d.InvokeAddCategoryDialog(null, null)).ReturnsAsync("Category");
            dialogMock.Setup(d => d.InvokeEditCategoryDialog("Movies")).ReturnsAsync("Movies");
            dialogMock.Setup(d => d.InvokeAddCategoryDialog("Movies\\", "C:\\Movies")).ReturnsAsync("Movies\\Sub");
            apiClientMock.Setup(c => c.RemoveCategoriesAsync(categories: new[] { "Movies" })).Returns(Task.CompletedTask);

            var target = RenderFiltersNav(mainData);

            var movies = FindComponentByTestId<CustomNavLink>(target, "Category-Movies");
            await target.InvokeAsync(() => movies.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var addCategory = WaitForMenuItemByTestId("CategoryAdd");
            await target.InvokeAsync(() => addCategory.Instance.OnClick.InvokeAsync());

            var editCategory = WaitForMenuItemByTestId("CategoryEdit");
            await target.InvokeAsync(() => editCategory.Instance.OnClick.InvokeAsync());

            var addSubcategory = WaitForMenuItemByTestId("CategoryAddSubcategory");
            await target.InvokeAsync(() => addSubcategory.Instance.OnClick.InvokeAsync());

            var removeCategory = WaitForMenuItemByTestId("CategoryRemove");
            await target.InvokeAsync(() => removeCategory.Instance.OnClick.InvokeAsync());

            dialogMock.Verify(d => d.InvokeAddCategoryDialog(null, null), Times.Once);
            dialogMock.Verify(d => d.InvokeEditCategoryDialog("Movies"), Times.Once);
            dialogMock.Verify(d => d.InvokeAddCategoryDialog("Movies\\", "C:\\Movies"), Times.Once);
            apiClientMock.Verify(c => c.RemoveCategoriesAsync(categories: new[] { "Movies" }), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CategorySubcategoryWithTrailingSlash_WHEN_Clicked_THEN_PrefixPreserved()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var mainData = CreateMainData(includeGamesCategory: true, includeGamesDefinition: false);

            dialogMock.Setup(d => d.InvokeAddCategoryDialog("Games\\", null)).ReturnsAsync("Games\\Sub");

            var target = RenderFiltersNav(mainData);

            var games = FindComponentByTestId<CustomNavLink>(target, "Category-Games\\");
            await target.InvokeAsync(() => games.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var addSubcategory = WaitForMenuItemByTestId("CategoryAddSubcategory");
            await target.InvokeAsync(() => addSubcategory.Instance.OnClick.InvokeAsync());

            apiClientMock.Invocations.Should().BeEmpty();
            dialogMock.Verify(d => d.InvokeAddCategoryDialog("Games\\", null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RemoveUnusedCategories_WHEN_Clicked_THEN_RemovesFromApi()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();

            apiClientMock
                .Setup(c => c.GetTorrentListAsync(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<TorrentSelector>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ClientTorrent> { ClientTorrentFactory.Create(category: "Movies") });
            apiClientMock.Setup(c => c.GetAllCategoriesAsync()).ReturnsAsync(new Dictionary<string, ClientCategory>
            {
                ["Movies"] = new ClientCategory("Movies", "C:\\Movies", null),
                ["Games"] = new ClientCategory("Games", "C:\\Games", null)
            });
            apiClientMock.Setup(c => c.RemoveCategoriesAsync(categories: new[] { "Games" })).Returns(Task.CompletedTask);

            var target = RenderFiltersNav(mainData);

            await OpenMenuAsync(target, 1);

            var removeUnused = WaitForMenuItemByTestId("CategoryRemoveUnused");
            await target.InvokeAsync(() => removeUnused.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.RemoveCategoriesAsync(categories: new[] { "Games" }), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RemoveUnusedCategoriesFails_WHEN_Clicked_THEN_ErrorShownAndCategoriesRemain()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();

            apiClientMock
                .Setup(c => c.GetTorrentListAsync(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<TorrentSelector?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ClientTorrent> { ClientTorrentFactory.Create(category: "Movies") });
            apiClientMock
                .Setup(c => c.GetAllCategoriesAsync())
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure", HttpStatusCode.InternalServerError);

            var target = RenderFiltersNav(mainData);

            await OpenMenuAsync(target, 1);

            var removeUnused = WaitForMenuItemByTestId("CategoryRemoveUnused");
            await target.InvokeAsync(() => removeUnused.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add("Failure", Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_TagMenuActions_WHEN_RemoveInvoked_THEN_ApiCalled()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var mainData = CreateMainData();

            apiClientMock.Setup(c => c.DeleteTagsAsync(tags: new[] { "Tag1" })).Returns(Task.CompletedTask);

            var target = RenderFiltersNav(mainData);

            var tagLink = FindComponentByTestId<CustomNavLink>(target, "Tag-Tag1");
            await target.InvokeAsync(() => tagLink.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var removeTag = WaitForMenuItemByTestId("TagRemove");
            await target.InvokeAsync(() => removeTag.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.DeleteTagsAsync(tags: new[] { "Tag1" }), Times.Once);
            dialogMock.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_RemoveUnusedTags_WHEN_Clicked_THEN_RemovesFromApi()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();

            apiClientMock
                .Setup(c => c.GetTorrentListAsync(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<TorrentSelector?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ClientTorrent>
                {
                    ClientTorrentFactory.Create(tags: new List<string> { "Tag1" })
                });
            apiClientMock.Setup(c => c.GetAllTagsAsync()).ReturnsAsync(new List<string> { "Tag1", "Tag2" });
            apiClientMock.Setup(c => c.DeleteTagsAsync(tags: new[] { "Tag2" })).Returns(Task.CompletedTask);

            var target = RenderFiltersNav(mainData);

            await OpenMenuAsync(target, 2);

            var removeUnused = WaitForMenuItemByTestId("TagRemoveUnused");
            await target.InvokeAsync(() => removeUnused.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.DeleteTagsAsync(tags: new[] { "Tag2" }), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RemoveUnusedTagsFails_WHEN_Clicked_THEN_ErrorShownAndTagsRemain()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();

            apiClientMock
                .Setup(c => c.GetTorrentListAsync(
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<string?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>(),
                    It.IsAny<TorrentSelector?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ClientTorrent>
                {
                    ClientTorrentFactory.Create(tags: new List<string> { "Tag1" })
                });
            apiClientMock
                .Setup(c => c.GetAllTagsAsync())
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure", HttpStatusCode.InternalServerError);

            var target = RenderFiltersNav(mainData);

            await OpenMenuAsync(target, 2);

            var removeUnused = WaitForMenuItemByTestId("TagRemoveUnused");
            await target.InvokeAsync(() => removeUnused.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add("Failure", Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_AddTagWithoutContext_WHEN_Clicked_THEN_DialogNotInvoked()
        {
            TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var mainData = CreateMainData();

            var target = RenderFiltersNav(mainData);

            await OpenMenuAsync(target, 2);

            var addTag = WaitForMenuItemByTestId("TagAdd");
            await target.InvokeAsync(() => addTag.Instance.OnClick.InvokeAsync());

            dialogMock.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_AddTagWithEmptySelection_WHEN_Clicked_THEN_NoApiCall()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var mainData = CreateMainData();

            dialogMock.Setup(d => d.ShowAddTagsDialog()).ReturnsAsync(new HashSet<string>());

            var target = RenderFiltersNav(mainData);

            var tagLink = FindComponentByTestId<CustomNavLink>(target, "Tag-Tag1");
            await target.InvokeAsync(() => tagLink.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var addTag = WaitForMenuItemByTestId("TagAdd");
            await target.InvokeAsync(() => addTag.Instance.OnClick.InvokeAsync());

            apiClientMock.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_AddTagWithSelection_WHEN_Clicked_THEN_CreatesTags()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var mainData = CreateMainData();

            dialogMock.Setup(d => d.ShowAddTagsDialog()).ReturnsAsync(new HashSet<string> { "Tag2" });
            apiClientMock.Setup(c => c.CreateTagsAsync(It.Is<IEnumerable<string>>(t => t.SequenceEqual(new[] { "Tag2" })))).Returns(Task.CompletedTask);

            var target = RenderFiltersNav(mainData);

            var tagLink = FindComponentByTestId<CustomNavLink>(target, "Tag-Tag1");
            await target.InvokeAsync(() => tagLink.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var addTag = WaitForMenuItemByTestId("TagAdd");
            await target.InvokeAsync(() => addTag.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(c => c.CreateTagsAsync(It.Is<IEnumerable<string>>(t => t.SequenceEqual(new[] { "Tag2" }))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RemoveTrackerWithoutContext_WHEN_Clicked_THEN_NoApiCall()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();

            var target = RenderFiltersNav(mainData);

            await OpenMenuAsync(target, 3);

            var removeTracker = WaitForMenuItemByTestId("TrackerRemove");
            await target.InvokeAsync(() => removeTracker.Instance.OnClick.InvokeAsync());

            apiClientMock.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_RemoveTrackerSynthetic_WHEN_Clicked_THEN_NoApiCall()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();

            var target = RenderFiltersNav(mainData);

            var tracker = FindComponentByTestId<CustomNavLink>(target, "Tracker-All");
            await target.InvokeAsync(() => tracker.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var removeTracker = WaitForMenuItemByTestId("TrackerRemove");
            await target.InvokeAsync(() => removeTracker.Instance.OnClick.InvokeAsync());

            apiClientMock.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_RemoveTrackerWithoutHashes_WHEN_Clicked_THEN_NoApiCall()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();

            var target = RenderFiltersNav(mainData);

            var tracker = FindComponentByTestId<CustomNavLink>(target, "Tracker-unused.example.com");
            await target.InvokeAsync(() => tracker.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var removeTracker = WaitForMenuItemByTestId("TrackerRemove");
            await target.InvokeAsync(() => removeTracker.Instance.OnClick.InvokeAsync());

            apiClientMock.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_RemoveTrackerWithHashes_WHEN_Clicked_THEN_ApiCalled()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            var mainData = CreateMainData();

            apiClientMock
                .Setup(c => c.RemoveTrackersAsync(
                    TorrentSelectorTestHelper.FromHash("Hash1"),
                    It.Is<IEnumerable<string>>(urls => urls.SequenceEqual(new[] { "http://tracker.example.com/announce" })),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var target = RenderFiltersNav(mainData);

            var tracker = FindComponentByTestId<CustomNavLink>(target, "Tracker-tracker.example.com");
            await target.InvokeAsync(() => tracker.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var removeTracker = WaitForMenuItemByTestId("TrackerRemove");
            await target.InvokeAsync(() => removeTracker.Instance.OnClick.InvokeAsync());

            apiClientMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_TorrentControlsWithContext_WHEN_Clicked_THEN_FiltersApplied()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var mainData = CreateMainData();
            var preferences = CreatePreferences(useSubcategories: true, confirmDeletion: true);

            apiClientMock
                .Setup(c => c.StartTorrentsAsync(It.Is<TorrentSelector>(s => s.Hashes!.SequenceEqual(new[] { "Hash1", "Hash3" })), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            apiClientMock
                .Setup(c => c.StopTorrentsAsync(It.Is<TorrentSelector>(s => s.Hashes!.SequenceEqual(new[] { "Hash1", "Hash2" })), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            dialogMock
                .Setup(d => d.InvokeDeleteTorrentDialog(true, false, "Hash1"))
                .ReturnsAsync(true);

            var target = RenderFiltersNav(mainData, preferences);

            var downloading = FindComponentByTestId<CustomNavLink>(target, "Status-Downloading");
            await target.InvokeAsync(() => downloading.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var start = WaitForMenuItemByTestId("_statusType-Start");
            await target.InvokeAsync(() => start.Instance.OnClick.InvokeAsync());

            var movies = FindComponentByTestId<CustomNavLink>(target, "Category-Movies");
            await target.InvokeAsync(() => movies.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var stop = WaitForMenuItemByTestId("_categoryType-Stop");
            await target.InvokeAsync(() => stop.Instance.OnClick.InvokeAsync());

            var tag = FindComponentByTestId<CustomNavLink>(target, "Tag-Tag1");
            await target.InvokeAsync(() => tag.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var remove = WaitForMenuItemByTestId("_tagType-Remove");
            await target.InvokeAsync(() => remove.Instance.OnClick.InvokeAsync());

            apiClientMock.VerifyAll();
            dialogMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_TorrentControlsWithContext_WHEN_StartAndStopFail_THEN_ErrorShownForEachFailure()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var mainData = CreateMainData();
            var preferences = CreatePreferences(useSubcategories: true, confirmDeletion: true);

            apiClientMock
                .Setup(c => c.StartTorrentsAsync(It.Is<TorrentSelector>(s => s.Hashes!.SequenceEqual(new[] { "Hash1", "Hash3" })), It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure", System.Net.HttpStatusCode.InternalServerError);
            apiClientMock
                .Setup(c => c.StopTorrentsAsync(It.Is<TorrentSelector>(s => s.Hashes!.SequenceEqual(new[] { "Hash1", "Hash2" })), It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure", System.Net.HttpStatusCode.InternalServerError);

            var target = RenderFiltersNav(mainData, preferences);

            var downloading = FindComponentByTestId<CustomNavLink>(target, "Status-Downloading");
            await target.InvokeAsync(() => downloading.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var start = WaitForMenuItemByTestId("_statusType-Start");
            await target.InvokeAsync(() => start.Instance.OnClick.InvokeAsync());

            var movies = FindComponentByTestId<CustomNavLink>(target, "Category-Movies");
            await target.InvokeAsync(() => movies.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var stop = WaitForMenuItemByTestId("_categoryType-Stop");
            await target.InvokeAsync(() => stop.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(s => s.Add("Failure", Severity.Error, null, null), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_TorrentControlsWithoutContext_WHEN_Clicked_THEN_EmptyHashes()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var mainData = CreateMainData();

            apiClientMock.Setup(c => c.StartTorrentsAsync(It.IsAny<TorrentSelector>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            apiClientMock.Setup(c => c.StopTorrentsAsync(It.IsAny<TorrentSelector>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            dialogMock.Setup(d => d.InvokeDeleteTorrentDialog(false, false, It.IsAny<string[]>())).ReturnsAsync(true);

            var target = RenderFiltersNav(mainData);

            await OpenMenuAsync(target, 0);

            var statusStart = WaitForMenuItemByTestId("_statusType-Start");
            await target.InvokeAsync(() => statusStart.Instance.OnClick.InvokeAsync());

            await OpenMenuAsync(target, 1);

            var categoryStop = WaitForMenuItemByTestId("_categoryType-Stop");
            await target.InvokeAsync(() => categoryStop.Instance.OnClick.InvokeAsync());

            await OpenMenuAsync(target, 2);

            var tagRemove = WaitForMenuItemByTestId("_tagType-Remove");
            await target.InvokeAsync(() => tagRemove.Instance.OnClick.InvokeAsync());

            await OpenMenuAsync(target, 3);

            var trackerStart = WaitForMenuItemByTestId("_trackerType-Start");
            await target.InvokeAsync(() => trackerStart.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(client => client.StartTorrentsAsync(It.IsAny<TorrentSelector>(), It.IsAny<CancellationToken>()), Times.Never);
            apiClientMock.Verify(client => client.StopTorrentsAsync(It.IsAny<TorrentSelector>(), It.IsAny<CancellationToken>()), Times.Never);
            dialogMock.Verify(workflow => workflow.InvokeDeleteTorrentDialog(false, false, It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NoMainData_WHEN_Invoked_THEN_EmptyCollections()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);

            apiClientMock.Setup(c => c.StartTorrentsAsync(It.IsAny<TorrentSelector>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var target = RenderFiltersNav(mainData: null);

            target.FindComponents<CustomNavLink>().Should().BeEmpty();

            await OpenMenuAsync(target, 0);

            var statusStart = WaitForMenuItemByTestId("_statusType-Start");
            await target.InvokeAsync(() => statusStart.Instance.OnClick.InvokeAsync());

            apiClientMock.Verify(client => client.StartTorrentsAsync(It.IsAny<TorrentSelector>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AddTagWithNullSelection_WHEN_Clicked_THEN_NoApiCall()
        {
            var apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            var dialogMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var mainData = CreateMainData();

            dialogMock.Setup(d => d.ShowAddTagsDialog()).ReturnsAsync((HashSet<string>?)null);

            var target = RenderFiltersNav(mainData);

            var tagLink = FindComponentByTestId<CustomNavLink>(target, "Tag-Tag1");
            await target.InvokeAsync(() => tagLink.Instance.OnContextMenu.InvokeAsync(new MouseEventArgs()));

            var addTag = WaitForMenuItemByTestId("TagAdd");
            await target.InvokeAsync(() => addTag.Instance.OnClick.InvokeAsync());

            apiClientMock.Invocations.Should().BeEmpty();
        }

        private IRenderedComponent<FiltersNav> RenderFiltersNav(
            MudMainData? mainData = null,
            Preferences? preferences = null)
        {
            _popoverProvider = TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<FiltersNav>(parameters =>
            {
                if (mainData is not null)
                {
                    parameters.AddCascadingValue(mainData);
                }

                if (preferences is not null)
                {
                    parameters.AddCascadingValue(preferences);
                }
            });

            return target;
        }

        private async Task OpenMenuAsync(IRenderedComponent<FiltersNav> target, int index)
        {
            var menus = target.FindComponents<MudMenu>();
            var menu = menus[index];
            await target.InvokeAsync(() => menu.Instance.OpenMenuAsync(new MouseEventArgs()));
        }

        private IRenderedComponent<MudMenuItem> WaitForMenuItemByTestId(string testId, int occurrenceFromEnd = 0)
        {
            if (_popoverProvider is null)
            {
                throw new InvalidOperationException("Popover provider not initialized.");
            }

            _popoverProvider.WaitForState(() =>
                _popoverProvider.FindComponents<MudMenuItem>().Count(component => HasTestId(component, testId)) > occurrenceFromEnd);

            var matches = _popoverProvider.FindComponents<MudMenuItem>()
                .Where(component => HasTestId(component, testId))
                .ToList();

            return matches[matches.Count - 1 - occurrenceFromEnd];
        }

        private static MudMainData CreateMainData(bool includeGamesCategory = true, bool includeGamesDefinition = true)
        {
            var torrents = new Dictionary<string, MudTorrent>
            {
                ["Hash1"] = CreateTorrent(
                    hash: "Hash1",
                    category: "Movies",
                    tags: new[] { "Tag1" },
                    tracker: "http://tracker.example.com/announce",
                    trackersCount: 1,
                    state: TorrentState.Downloading),
                ["Hash2"] = CreateTorrent(
                    hash: "Hash2",
                    category: "Movies/Sub",
                    tags: new[] { "Tag2" },
                    tracker: "::::",
                    trackersCount: 1,
                    state: TorrentState.Uploading,
                    hasTrackerWarning: true),
                ["Hash3"] = CreateTorrent(
                    hash: "Hash3",
                    category: string.Empty,
                    tags: Array.Empty<string>(),
                    tracker: string.Empty,
                    trackersCount: 0,
                    state: TorrentState.StalledDownloading,
                    hasTrackerError: true,
                    hasOtherAnnounceError: true),
            };

            var categories = new Dictionary<string, MudCategory>
            {
                ["Movies"] = new MudCategory("Movies", "C:\\Movies")
            };

            if (includeGamesDefinition)
            {
                categories["Games\\"] = new MudCategory("Games\\", "C:\\Games");
            }

            var tags = new[] { "Tag1", "Tag2" };

            var trackers = new Dictionary<string, IReadOnlyList<string>>
            {
                ["http://tracker.example.com/announce"] = new[] { "Hash1" },
                ["::::"] = new[] { "Hash2" },
                ["http://unused.example.com/announce"] = new[] { "MissingHash" }
            };

            var tagState = new Dictionary<string, HashSet<string>>
            {
                [FilterHelper.TAG_ALL] = new HashSet<string> { "Hash1", "Hash2", "Hash3" },
                [FilterHelper.TAG_UNTAGGED] = new HashSet<string> { "Hash3" },
                ["Tag1"] = new HashSet<string> { "Hash1" },
                ["Tag2"] = new HashSet<string> { "Hash2" }
            };

            var categoriesState = new Dictionary<string, HashSet<string>>
            {
                [FilterHelper.CATEGORY_ALL] = new HashSet<string> { "Hash1", "Hash2", "Hash3" },
                [FilterHelper.CATEGORY_UNCATEGORIZED] = new HashSet<string> { "Hash3" },
                ["Movies"] = new HashSet<string> { "Hash1", "Hash2" }
            };

            if (includeGamesCategory)
            {
                categoriesState["Games\\"] = new HashSet<string> { "Hash2" };
            }

            var statusState = new Dictionary<string, HashSet<string>>
            {
                [Status.All.ToString()] = new HashSet<string> { "Hash1", "Hash2", "Hash3" },
                [Status.Downloading.ToString()] = new HashSet<string> { "Hash1" },
                [Status.Seeding.ToString()] = new HashSet<string> { "Hash2" },
                [Status.Stalled.ToString()] = new HashSet<string> { "Hash3" }
            };

            var trackersState = new Dictionary<string, HashSet<string>>
            {
                [FilterHelper.TRACKER_ALL] = new HashSet<string> { "Hash1", "Hash2", "Hash3" },
                [FilterHelper.TRACKER_TRACKERLESS] = new HashSet<string> { "Hash3" },
                [FilterHelper.TRACKER_ERROR] = new HashSet<string> { "Hash3" },
                [FilterHelper.TRACKER_WARNING] = new HashSet<string> { "Hash2" },
                [FilterHelper.TRACKER_ANNOUNCE_ERROR] = new HashSet<string> { "Hash3" }
            };

            var serverState = new MudServerState();

            return new MudMainData(
                torrents,
                tags,
                categories,
                trackers,
                serverState,
                tagState,
                categoriesState,
                statusState,
                trackersState);
        }

        private static Preferences CreatePreferences(bool useSubcategories, bool confirmDeletion)
        {
            return PreferencesFactory.CreatePreferences(spec =>
            {
                spec.ConfirmTorrentDeletion = confirmDeletion;
                spec.UseSubcategories = useSubcategories;
            });
        }

        private static MudTorrent CreateTorrent(
            string hash,
            string category,
            IEnumerable<string> tags,
            string tracker,
            int trackersCount,
            TorrentState? state,
            bool hasTrackerError = false,
            bool hasTrackerWarning = false,
            bool hasOtherAnnounceError = false)
        {
            return new MudTorrent(
                hash,
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                availability: 1,
                category,
                completed: 0,
                completionOn: 0,
                contentPath: string.Empty,
                downloadLimit: 0,
                downloadSpeed: 0,
                downloaded: 0,
                downloadedSession: 0,
                estimatedTimeOfArrival: 0,
                firstLastPiecePriority: false,
                forceStart: false,
                infoHashV1: string.Empty,
                infoHashV2: string.Empty,
                lastActivity: 0,
                magnetUri: string.Empty,
                maxRatio: 1,
                maxSeedingTime: 0,
                name: hash,
                numberComplete: 0,
                numberIncomplete: 0,
                numberLeeches: 0,
                numberSeeds: 0,
                priority: 0,
                progress: 0,
                ratio: 0,
                ratioLimit: 0,
                savePath: string.Empty,
                seedingTime: 0,
                seedingTimeLimit: 0,
                seenComplete: 0,
                sequentialDownload: false,
                size: 0,
                state,
                superSeeding: false,
                tags,
                timeActive: 0,
                totalSize: 0,
                tracker,
                trackersCount,
                hasTrackerError,
                hasTrackerWarning,
                hasOtherAnnounceError,
                uploadLimit: 0,
                uploaded: 0,
                uploadedSession: 0,
                uploadSpeed: 0,
                reannounce: 0,
                inactiveSeedingTimeLimit: 0,
                maxInactiveSeedingTime: 0,
                popularity: 0,
                downloadPath: string.Empty,
                rootPath: string.Empty,
                isPrivate: false,
                ShareLimitAction.Default,
                comment: string.Empty);
        }
    }
}
