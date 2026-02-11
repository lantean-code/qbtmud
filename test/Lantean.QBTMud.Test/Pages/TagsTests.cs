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
    public sealed class TagsTests : RazorComponentTestBase<Tags>
    {
        private readonly IApiClient _apiClient;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly IRenderedComponent<Tags> _target;

        public TagsTests()
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
        public async Task GIVEN_AddClicked_WHEN_DialogCanceled_THEN_SkipsApiCalls()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog("New Tag", "Tag:", null))
                .ReturnsAsync((string?)null);

            var addButton = FindIconButton(_target, Icons.Material.Filled.NewLabel);

            await _target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.GetAllTags(), Times.Never);
            Mock.Get(_apiClient).Verify(client => client.CreateTags(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AddClicked_WHEN_TagExists_THEN_SkipsCreate()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog("New Tag", "Tag:", null))
                .ReturnsAsync("Tag");
            Mock.Get(_apiClient)
                .Setup(client => client.GetAllTags())
                .ReturnsAsync(new[] { "Tag" });

            var addButton = FindIconButton(_target, Icons.Material.Filled.NewLabel);

            await _target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.GetAllTags(), Times.Once);
            Mock.Get(_apiClient).Verify(client => client.CreateTags(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AddClicked_WHEN_NewTag_THEN_CreatesTag()
        {
            Mock.Get(_dialogWorkflow)
                .Setup(workflow => workflow.ShowStringFieldDialog("New Tag", "Tag:", null))
                .ReturnsAsync("Tag");
            Mock.Get(_apiClient)
                .Setup(client => client.GetAllTags())
                .ReturnsAsync(new[] { "Other" });

            var addButton = FindIconButton(_target, Icons.Material.Filled.NewLabel);

            await _target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.GetAllTags(), Times.Once);
            Mock.Get(_apiClient).Verify(
                client => client.CreateTags(It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" }))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_TagProvided_WHEN_DeleteClicked_THEN_DeletesTag()
        {
            var target = RenderPage(new List<string> { "Tag" });

            var deleteButton = FindIconButton(target, Icons.Material.Filled.Delete);

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.DeleteTags("Tag"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_TagMissing_WHEN_DeleteClicked_THEN_SkipsDelete()
        {
            var target = RenderPage(new List<string> { null! });

            var deleteButton = FindIconButton(target, Icons.Material.Filled.Delete);

            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.DeleteTags(It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ActionColumnSortSelectorNull_WHEN_DeleteClicked_THEN_UsesRowData()
        {
            IRenderedComponent<Tags>? target = null;
            Func<string, object?>? originalSelector = null;
            ColumnDefinition<string>? column = null;
            try
            {
                target = RenderPage(new List<string> { "Tag" });
                var table = target.FindComponent<DynamicTable<string>>();
                column = table.Instance.ColumnDefinitions.Single(definition => definition.Header == "Actions");
                originalSelector = column.SortSelector;
                column.SortSelector = _ => null;

                var deleteButton = FindIconButton(target, Icons.Material.Filled.Delete);

                await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

                Mock.Get(_apiClient).Verify(client => client.DeleteTags("Tag"), Times.Once);
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
        public void GIVEN_ColumnDefinitions_WHEN_Requested_THEN_ContainsExpectedColumns()
        {
            var target = RenderPage();
            var table = target.FindComponent<DynamicTable<string>>();
            var columns = table.Instance.ColumnDefinitions;

            columns.Should().ContainSingle(column => column.Header == "Name");
            columns.Should().ContainSingle(column => column.Header == "Actions");
        }

        [Fact]
        public void GIVEN_ColumnSortSelectors_WHEN_Invoked_THEN_ReturnExpectedValues()
        {
            var target = RenderPage();
            var table = target.FindComponent<DynamicTable<string>>();
            var columns = table.Instance.ColumnDefinitions;

            foreach (var column in columns)
            {
                var value = column.SortSelector("Tag");
                value.Should().Be("Tag");
            }
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
        public async Task GIVEN_RefreshClicked_WHEN_Invoked_THEN_ReloadsCurrentPage()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            navigationManager.NavigateTo("http://localhost/tags");

            var refreshButton = FindIconButton(_target, Icons.Material.Filled.Refresh);

            await _target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().Be("http://localhost/tags");
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

            var table = target.FindComponent<DynamicTable<string>>();

            table.Instance.Items.Should().BeNull();
        }

        private IRenderedComponent<Tags> RenderPage(IEnumerable<string>? tags = null, bool drawerOpen = false, bool includeMainData = true)
        {
            var mainData = new MainData(
                new Dictionary<string, Torrent>(),
                tags ?? new List<string>(),
                new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new ServerState { ConnectionStatus = "Connected" },
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());

            return TestContext.Render<Tags>(parameters =>
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
