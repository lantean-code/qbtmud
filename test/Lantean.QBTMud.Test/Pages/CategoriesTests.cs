using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class CategoriesTests : RazorComponentTestBase<Categories>
    {
        private readonly IApiClient _apiClient;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly IRenderedComponent<Categories> _target;

        public CategoriesTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _dialogWorkflow = Mock.Of<IDialogWorkflow>();

            TestContext.Services.RemoveAll(typeof(IApiClient));
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.RemoveAll(typeof(IDialogWorkflow));
            TestContext.Services.AddSingleton(_dialogWorkflow);

            _target = RenderPage();
        }

        [Fact]
        public async Task GIVEN_AddClicked_WHEN_Invoked_THEN_ShowsAddCategoryDialog()
        {
            var addButton = FindIconButton(_target, Icons.Material.Filled.PlaylistAdd);

            await _target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.InvokeAddCategoryDialog(null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_BackClicked_WHEN_Invoked_THEN_NavigatesHome()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/other");

            var backButton = FindIconButton(_target, Icons.Material.Outlined.NavigateBefore);

            await _target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public void GIVEN_CategoryProvided_WHEN_Rendered_THEN_ShowsTableItem()
        {
            var target = RenderPage(new Dictionary<string, Category>
            {
                { "Category", new Category("Category", "SavePath") }
            });

            var table = target.FindComponent<DynamicTable<Category>>();
            table.Instance.Items.Should().ContainSingle(category => category.Name == "Category");
        }

        [Fact]
        public async Task GIVEN_CategoryProvided_WHEN_EditClicked_THEN_InvokesEditDialog()
        {
            var target = RenderPage(new Dictionary<string, Category>
            {
                { "Category", new Category("Category", "SavePath") }
            });

            var editButton = FindIconButton(target, Icons.Material.Filled.Edit);

            await target.InvokeAsync(() => editButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.InvokeEditCategoryDialog("Category"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ActionColumnSortSelectorNull_WHEN_ActionClicked_THEN_UsesRowData()
        {
            var column = Categories.ColumnsDefinitions.Single(definition => definition.Header == "Actions");
            var originalSelector = column.SortSelector;
            column.SortSelector = _ => null;

            try
            {
                Mock.Get(_apiClient)
                    .Setup(client => client.RemoveCategories("Category"))
                    .Returns(Task.CompletedTask);

                var target = RenderPage(new Dictionary<string, Category>
                {
                    { "Category", new Category("Category", "SavePath") }
                });

                var editButton = FindIconButton(target, Icons.Material.Filled.Edit);
                await target.InvokeAsync(() => editButton.Instance.OnClick.InvokeAsync());

                var deleteButton = FindIconButton(target, Icons.Material.Filled.Delete);
                await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

                Mock.Get(_dialogWorkflow).Verify(workflow => workflow.InvokeEditCategoryDialog("Category"), Times.Once);
                Mock.Get(_apiClient).Verify(client => client.RemoveCategories("Category"), Times.Once);
            }
            finally
            {
                column.SortSelector = originalSelector;
            }
        }

        [Fact]
        public async Task GIVEN_CategoryProvided_WHEN_DeleteClicked_THEN_RemovesCategory()
        {
            var target = RenderPage(new Dictionary<string, Category>
            {
                { "Category", new Category("Category", "SavePath") }
            });

            Mock.Get(_apiClient)
                .Setup(client => client.RemoveCategories("Category"))
                .Returns(Task.CompletedTask);

            var deleteButton = FindIconButton(target, Icons.Material.Filled.Delete);

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.RemoveCategories("Category"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CategoryNameMissing_WHEN_DeleteClicked_THEN_SkipsRemoval()
        {
            var target = RenderPage(new Dictionary<string, Category>
            {
                { "Category", new Category(null!, "SavePath") }
            });

            var deleteButton = FindIconButton(target, Icons.Material.Filled.Delete);

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.RemoveCategories(It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_CategoryNameMissing_WHEN_EditClicked_THEN_SkipsEditDialog()
        {
            var target = RenderPage(new Dictionary<string, Category>
            {
                { "Category", new Category(null!, "SavePath") }
            });

            var editButton = FindIconButton(target, Icons.Material.Filled.Edit);

            await target.InvokeAsync(() => editButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_dialogWorkflow).Verify(workflow => workflow.InvokeEditCategoryDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_ColumnDefinitions_WHEN_Requested_THEN_ContainsExpectedColumns()
        {
            var columns = Categories.ColumnsDefinitions;

            columns.Should().ContainSingle(column => column.Header == "Name");
            columns.Should().ContainSingle(column => column.Header == "Save path");
            columns.Should().ContainSingle(column => column.Header == "Actions");
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

            var table = target.FindComponent<DynamicTable<Category>>();

            table.Instance.Items.Should().BeNull();
        }

        private IRenderedComponent<Categories> RenderPage(Dictionary<string, Category>? categories = null, bool drawerOpen = false, bool includeMainData = true)
        {
            var mainData = new MainData(
                new Dictionary<string, Torrent>(),
                new List<string>(),
                categories ?? new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new ServerState { ConnectionStatus = "Connected" },
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
