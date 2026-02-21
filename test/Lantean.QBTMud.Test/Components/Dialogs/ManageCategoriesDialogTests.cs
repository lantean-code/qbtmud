using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class ManageCategoriesDialogTests : RazorComponentTestBase<ManageCategoriesDialog>
    {
        private readonly IApiClient _apiClient;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly ManageCategoriesDialogTestDriver _target;

        public ManageCategoriesDialogTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _dialogWorkflow = Mock.Of<IDialogWorkflow>();

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.RemoveAll<IDialogWorkflow>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.AddSingleton(_dialogWorkflow);

            _target = new ManageCategoriesDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_NoHashes_WHEN_Rendered_THEN_RendersUncheckedIcon()
        {
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategories())
                .ReturnsAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentList(
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
                    It.IsAny<string[]>()))
                .ReturnsAsync(new List<Torrent>());

            var dialog = await _target.RenderDialogAsync(Array.Empty<string>());

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-Movies");
            listItem.Instance.Icon.Should().Be(Icons.Material.Filled.RadioButtonUnchecked);

            apiClientMock.Verify(client => client.GetTorrentList(
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
                It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AllTorrentsHaveCategory_WHEN_Clicked_THEN_RemovesCategory()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategories())
                .ReturnsAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .SetupSequence(client => client.GetTorrentList(
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
                    It.IsAny<string[]>()))
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies"), CreateTorrent("Movies") })
                .ReturnsAsync(new List<Torrent> { CreateTorrent(string.Empty) });
            apiClientMock
                .Setup(client => client.SetTorrentCategory(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(hashes);

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-Movies");
            await listItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.SetTorrentCategory(
                string.Empty,
                It.IsAny<bool?>(),
                It.Is<string[]>(value => value.SequenceEqual(hashes))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_PartialCategory_WHEN_Clicked_THEN_SetsCategory()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategories())
                .ReturnsAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .SetupSequence(client => client.GetTorrentList(
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
                    It.IsAny<string[]>()))
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies"), CreateTorrent("Other") })
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies"), CreateTorrent("Movies") });
            apiClientMock
                .Setup(client => client.SetTorrentCategory(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(hashes);

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-Movies");
            listItem.Instance.Icon.Should().Be(CustomIcons.RadioIndeterminate);
            await listItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.SetTorrentCategory(
                "Movies",
                It.IsAny<bool?>(),
                It.Is<string[]>(value => value.SequenceEqual(hashes))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CategoryNotPresent_WHEN_Clicked_THEN_SetsCategory()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategories())
                .ReturnsAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .SetupSequence(client => client.GetTorrentList(
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
                    It.IsAny<string[]>()))
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Other") })
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies") });
            apiClientMock
                .Setup(client => client.SetTorrentCategory(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(hashes);

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-Movies");
            await listItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.SetTorrentCategory(
                "Movies",
                It.IsAny<bool?>(),
                It.Is<string[]>(value => value.SequenceEqual(hashes))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_EmptyCategory_WHEN_Rendered_THEN_RendersUncheckedIcon()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategories())
                .ReturnsAsync(new Dictionary<string, Category>
                {
                    { string.Empty, new Category(string.Empty, null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentList(
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
                    It.IsAny<string[]>()))
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies") });

            var dialog = await _target.RenderDialogAsync(hashes);

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-");
            listItem.Instance.Icon.Should().Be(Icons.Material.Filled.RadioButtonUnchecked);
        }

        [Fact]
        public async Task GIVEN_AddCategoryCanceled_WHEN_Clicked_THEN_NoCategoryAdded()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategories())
                .ReturnsAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentList(
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
                    It.IsAny<string[]>()))
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies") });

            var dialogWorkflowMock = Mock.Get(_dialogWorkflow);
            dialogWorkflowMock
                .Setup(workflow => workflow.InvokeAddCategoryDialog(It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync((string?)null);

            var dialog = await _target.RenderDialogAsync(hashes);

            var addItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "CategoryAdd");
            await addItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.SetTorrentCategory(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AddCategoryConfirmed_WHEN_Clicked_THEN_CategoryAdded()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategories())
                .ReturnsAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .SetupSequence(client => client.GetTorrentList(
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
                    It.IsAny<string[]>()))
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies") })
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies") });
            apiClientMock
                .Setup(client => client.SetTorrentCategory(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialogWorkflowMock = Mock.Get(_dialogWorkflow);
            dialogWorkflowMock
                .Setup(workflow => workflow.InvokeAddCategoryDialog(It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync("NewCategory");

            var dialog = await _target.RenderDialogAsync(hashes);

            var addItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "CategoryAdd");
            await addItem.Find("div").ClickAsync(new MouseEventArgs());

            dialog.Component.WaitForAssertion(() =>
                FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-NewCategory").Should().NotBeNull());

            apiClientMock.Verify(client => client.SetTorrentCategory(
                "NewCategory",
                It.IsAny<bool?>(),
                It.Is<string[]>(value => value.SequenceEqual(hashes))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RemoveInvoked_WHEN_Clicked_THEN_CategoryRemoved()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategories())
                .ReturnsAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentList(
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
                    It.IsAny<string[]>()))
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies") });
            apiClientMock
                .Setup(client => client.SetTorrentCategory(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(hashes);

            var removeItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "CategoryRemove");
            await removeItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.SetTorrentCategory(
                string.Empty,
                It.IsAny<bool?>(),
                It.Is<string[]>(value => value.SequenceEqual(hashes))), Times.Once);
        }

        private static Torrent CreateTorrent(string? category)
        {
            return new Torrent
            {
                Category = category,
            };
        }
    }

    internal sealed class ManageCategoriesDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public ManageCategoriesDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<ManageCategoriesDialogRenderContext> RenderDialogAsync(IEnumerable<string> hashes)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ManageCategoriesDialog.Hashes), hashes },
            };

            var reference = await dialogService.ShowAsync<ManageCategoriesDialog>("Manage Categories", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<ManageCategoriesDialog>();

            return new ManageCategoriesDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class ManageCategoriesDialogRenderContext
    {
        public ManageCategoriesDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<ManageCategoriesDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<ManageCategoriesDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
