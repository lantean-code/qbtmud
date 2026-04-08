using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;
using ClientCategory = QBittorrent.ApiClient.Models.Category;
using MudCategory = Lantean.QBTMud.Models.Category;
using MudMainData = Lantean.QBTMud.Models.MainData;
using MudServerState = Lantean.QBTMud.Models.ServerState;
using MudTorrent = Lantean.QBTMud.Models.Torrent;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class CategoriesTests : RazorComponentTestBase<Categories>
    {
        private readonly IApiClient _apiClient;
        private readonly IDialogWorkflow _dialogWorkflow;

        public CategoriesTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _dialogWorkflow = Mock.Of<IDialogWorkflow>();

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.RemoveAll<IDialogWorkflow>();
            TestContext.Services.AddSingleton(_dialogWorkflow);
        }

        [Fact]
        public async Task GIVEN_AddClicked_WHEN_Invoked_THEN_ShowsAddCategoryDialog()
        {
            var target = RenderPage();
            var addButton = FindIconButton(target, Icons.Material.Filled.PlaylistAdd);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.InvokeAddCategoryDialog(null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_BackClicked_WHEN_Invoked_THEN_NavigatesHome()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/other");
            var target = RenderPage();

            var backButton = FindIconButton(target, Icons.Material.Outlined.NavigateBefore);

            await target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public async Task GIVEN_RefreshClicked_WHEN_Invoked_THEN_ReloadsCategoriesFromApi()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsAsync(new Dictionary<string, ClientCategory>
                {
                    { "MudCategory", new ClientCategory("MudCategory", "SavePath", null) }
                });

            var target = RenderPage();
            var refreshButton = FindIconButton(target, Icons.Material.Filled.Refresh);

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.GetAllCategoriesAsync(), Times.Once);

            var table = target.FindComponent<DynamicTable<MudCategory>>();
            table.Instance.Items.Should().ContainSingle(category => category.Name == "MudCategory");
        }

        [Fact]
        public async Task GIVEN_RefreshResultWithNullSavePath_WHEN_Reloaded_THEN_UsesEmptySavePath()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsAsync(new Dictionary<string, ClientCategory>
                {
                    { "MudCategory", new ClientCategory("MudCategory", null, null) }
                });

            var target = RenderPage();
            var refreshButton = FindIconButton(target, Icons.Material.Filled.Refresh);

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            var table = target.FindComponent<DynamicTable<MudCategory>>();
            table.Instance.Items.Should().ContainSingle(category => category.Name == "MudCategory" && category.SavePath == string.Empty);
        }

        [Fact]
        public async Task GIVEN_RefreshInProgress_WHEN_RefreshClickedAgain_THEN_DoesNotReloadTwice()
        {
            var pendingLoad = new TaskCompletionSource<IReadOnlyDictionary<string, ClientCategory>>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_apiClient)
                .Setup(client => client.GetAllCategoriesAsync())
                .Returns(pendingLoad.Task);

            var target = RenderPage();
            var refreshButton = FindIconButton(target, Icons.Material.Filled.Refresh);

            var firstRefresh = target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());
            target.WaitForAssertion(() => Mock.Get(_apiClient).Verify(client => client.GetAllCategoriesAsync(), Times.Once));

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());
            Mock.Get(_apiClient).Verify(client => client.GetAllCategoriesAsync(), Times.Once);

            pendingLoad.SetResult(new Dictionary<string, ClientCategory>
            {
                { "MudCategory", new ClientCategory("MudCategory", "SavePath", null) }
            });

            await firstRefresh;
        }

        [Fact]
        public async Task GIVEN_InitialLoadFails_WHEN_Rendered_THEN_ShowsErrorAndLeavesTableEmpty()
        {
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            Mock.Get(_apiClient)
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsAsync(ApiResult<IReadOnlyDictionary<string, ClientCategory>>.FailureResult(new ApiFailure
                {
                    Kind = ApiFailureKind.ServerError,
                    Operation = "test",
                    UserMessage = "Failure",
                    Detail = "Failure",
                    ResponseBody = "Failure",
                    IsTransient = true
                }));

            var target = RenderPage();
            var refreshButton = FindIconButton(target, Icons.Material.Filled.Refresh);

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            snackbarMock.Verify(snackbar => snackbar.Add("Failure", Severity.Error, null, null), Times.Once);
            target.FindComponent<DynamicTable<MudCategory>>().Instance.Items.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_CategoryProvided_WHEN_Rendered_THEN_ShowsTableItem()
        {
            var target = RenderPage(new Dictionary<string, MudCategory>
            {
                { "MudCategory", new MudCategory("MudCategory", "SavePath") }
            });

            var table = target.FindComponent<DynamicTable<MudCategory>>();
            table.Instance.Items.Should().ContainSingle(category => category.Name == "MudCategory");
        }

        [Fact]
        public async Task GIVEN_CategoryProvided_WHEN_EditClicked_THEN_InvokesEditDialog()
        {
            var target = RenderPage(new Dictionary<string, MudCategory>
            {
                { "MudCategory", new MudCategory("MudCategory", "SavePath") }
            });

            var editButton = FindIconButton(target, Icons.Material.Filled.Edit);

            await target.InvokeAsync(() => editButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.InvokeEditCategoryDialog("MudCategory"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ActionColumnSortSelectorNull_WHEN_ActionClicked_THEN_UsesRowData()
        {
            IRenderedComponent<Categories>? target = null;
            Func<MudCategory, object?>? originalSelector = null;
            ColumnDefinition<MudCategory>? column = null;

            try
            {
                Mock.Get(_apiClient)
                    .Setup(client => client.RemoveCategoriesAsync(categories: new[] { "MudCategory" }))
                    .Returns(Task.CompletedTask);

                target = RenderPage(new Dictionary<string, MudCategory>
                {
                    { "MudCategory", new MudCategory("MudCategory", "SavePath") }
                });

                var table = target.FindComponent<DynamicTable<MudCategory>>();
                column = table.Instance.ColumnDefinitions.Single(definition => definition.Header == "Actions");
                originalSelector = column.SortSelector;
                column.SortSelector = _ => null;

                var editButton = FindIconButton(target, Icons.Material.Filled.Edit);
                await target.InvokeAsync(() => editButton.Instance.OnClick.InvokeAsync());

                var deleteButton = FindIconButton(target, Icons.Material.Filled.Delete);
                await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

                Mock.Get(_dialogWorkflow).Verify(workflow => workflow.InvokeEditCategoryDialog("MudCategory"), Times.Once);
                Mock.Get(_apiClient).Verify(client => client.RemoveCategoriesAsync(categories: new[] { "MudCategory" }), Times.Once);
            }
            finally
            {
                if (column is not null && originalSelector is not null)
                {
                    column.SortSelector = originalSelector;
                }
            }
        }

        [Fact]
        public async Task GIVEN_CategoryProvided_WHEN_DeleteClicked_THEN_RemovesCategory()
        {
            var target = RenderPage(new Dictionary<string, MudCategory>
            {
                { "MudCategory", new MudCategory("MudCategory", "SavePath") }
            });

            Mock.Get(_apiClient)
                .Setup(client => client.RemoveCategoriesAsync(categories: new[] { "MudCategory" }))
                .Returns(Task.CompletedTask);

            var deleteButton = FindIconButton(target, Icons.Material.Filled.Delete);

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.RemoveCategoriesAsync(categories: new[] { "MudCategory" }), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CategoryNameMissing_WHEN_DeleteClicked_THEN_SkipsRemoval()
        {
            var target = RenderPage(new Dictionary<string, MudCategory>
            {
                { "MudCategory", new MudCategory(null!, "SavePath") }
            });

            var deleteButton = FindIconButton(target, Icons.Material.Filled.Delete);

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.RemoveCategoriesAsync(It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_CategoryNameMissing_WHEN_EditClicked_THEN_SkipsEditDialog()
        {
            var target = RenderPage(new Dictionary<string, MudCategory>
            {
                { "MudCategory", new MudCategory(null!, "SavePath") }
            });

            var editButton = FindIconButton(target, Icons.Material.Filled.Edit);

            await target.InvokeAsync(() => editButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.InvokeEditCategoryDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_ColumnDefinitions_WHEN_Requested_THEN_ContainsExpectedColumns()
        {
            var target = RenderPage();
            var table = target.FindComponent<DynamicTable<MudCategory>>();
            var columns = table.Instance.ColumnDefinitions;

            columns.Should().ContainSingle(column => column.Header == "Name");
            columns.Should().ContainSingle(column => column.Header == "Save path");
            columns.Should().ContainSingle(column => column.Header == "Actions");
            columns.Should().ContainSingle(column => column.Id == "actions");
        }

        [Fact]
        public void GIVEN_DrawerOpen_WHEN_Rendered_THEN_HidesBackButton()
        {
            var target = RenderPage(drawerOpen: true);

            var buttons = target.FindComponents<MudIconButton>();

            buttons.Should().NotContain(button => button.Instance.Icon == Icons.Material.Outlined.NavigateBefore);
        }

        [Fact]
        public void GIVEN_MainDataMissing_WHEN_Rendered_THEN_TableHasNoItems()
        {
            var target = RenderPage(includeMainData: false);

            var table = target.FindComponent<DynamicTable<MudCategory>>();

            table.Instance.Items.Should().BeNull();
        }

        private IRenderedComponent<Categories> RenderPage(Dictionary<string, MudCategory>? categories = null, bool drawerOpen = false, bool includeMainData = true)
        {
            var mainData = new MudMainData(
                new Dictionary<string, MudTorrent>(),
                new List<string>(),
                categories ?? new Dictionary<string, MudCategory>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new MudServerState { ConnectionStatus = ConnectionStatus.Connected },
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());

            return TestContext.Render<Categories>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", drawerOpen);
                if (includeMainData)
                {
                    parameters.AddCascadingValue(mainData);
                }
            });
        }
    }
}
