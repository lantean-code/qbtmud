using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Presentation.Test.Components.Dialogs
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
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentListAsync(
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
                .ReturnsSuccessAsync(new List<Torrent>());

            var dialog = await _target.RenderDialogAsync(Array.Empty<string>());

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-Movies");
            listItem.Instance.Icon.Should().Be(Icons.Material.Filled.RadioButtonUnchecked);

            apiClientMock.Verify(client => client.GetTorrentListAsync(
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
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_GetAllCategoriesFails_WHEN_Rendered_THEN_ErrorShownAndTorrentListNotRequested()
        {
            var apiClientMock = Mock.Get(_apiClient);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure", System.Net.HttpStatusCode.InternalServerError);

            await _target.RenderDialogAsync(new[] { "Hash" });

            snackbarMock.Verify(snackbar => snackbar.Add("Failure", Severity.Error, null, null), Times.Once);
            apiClientMock.Verify(client => client.GetTorrentListAsync(
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
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_GetTorrentCategoriesFails_WHEN_Rendered_THEN_ErrorShown()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentListAsync(
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
                    It.Is<TorrentSelector?>(selector => selector != null && TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                    It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure", System.Net.HttpStatusCode.InternalServerError);

            var dialog = await _target.RenderDialogAsync(hashes);

            FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-Movies").Instance.Icon.Should().Be(Icons.Material.Filled.RadioButtonUnchecked);
            snackbarMock.Verify(snackbar => snackbar.Add("Failure", Severity.Error, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_AllTorrentsHaveCategory_WHEN_Clicked_THEN_RemovesCategory()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .SetupSequence(client => client.GetTorrentListAsync(
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
                    It.Is<TorrentSelector?>(selector => selector != null && TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies"), CreateTorrent("Movies") })
                .ReturnsAsync(new List<Torrent> { CreateTorrent(string.Empty) });
            apiClientMock
                .Setup(client => client.SetTorrentCategoryAsync(It.IsAny<TorrentSelector>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsSuccess(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(hashes);

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-Movies");
            await listItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.SetTorrentCategoryAsync(
                It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                string.Empty,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_PartialCategory_WHEN_Clicked_THEN_SetsCategory()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .SetupSequence(client => client.GetTorrentListAsync(
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
                    It.Is<TorrentSelector?>(selector => selector != null && TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies"), CreateTorrent("Other") })
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies"), CreateTorrent("Movies") });
            apiClientMock
                .Setup(client => client.SetTorrentCategoryAsync(It.IsAny<TorrentSelector>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsSuccess(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(hashes);

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-Movies");
            listItem.Instance.Icon.Should().Be(CustomIcons.RadioIndeterminate);
            await listItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.SetTorrentCategoryAsync(
                It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                "Movies",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CategoryNotPresent_WHEN_Clicked_THEN_SetsCategory()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .SetupSequence(client => client.GetTorrentListAsync(
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
                    It.Is<TorrentSelector?>(selector => selector != null && TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Other") })
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies") });
            apiClientMock
                .Setup(client => client.SetTorrentCategoryAsync(It.IsAny<TorrentSelector>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsSuccess(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(hashes);

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-Movies");
            await listItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.SetTorrentCategoryAsync(
                It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                "Movies",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CategoryNotPresent_WHEN_SetCategoryFails_THEN_ErrorShownAndTorrentCategoriesNotReloaded()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentListAsync(
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
                    It.Is<TorrentSelector?>(selector => selector != null && TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                    It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(new List<Torrent> { CreateTorrent("Other") });
            apiClientMock
                .Setup(client => client.SetTorrentCategoryAsync(It.IsAny<TorrentSelector>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure", System.Net.HttpStatusCode.InternalServerError);

            var dialog = await _target.RenderDialogAsync(hashes);

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-Movies");
            await listItem.Find("div").ClickAsync(new MouseEventArgs());

            snackbarMock.Verify(snackbar => snackbar.Add("Failure", Severity.Error, null, null), Times.Once);
            apiClientMock.Verify(client => client.GetTorrentListAsync(
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
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_EmptyCategory_WHEN_Rendered_THEN_RendersUncheckedIcon()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { string.Empty, new Category(string.Empty, null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentListAsync(
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
                    It.Is<TorrentSelector?>(selector => selector != null && TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                    It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(new List<Torrent> { CreateTorrent("Movies") });

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
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentListAsync(
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
                    It.Is<TorrentSelector?>(selector => selector != null && TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                    It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(new List<Torrent> { CreateTorrent("Movies") });

            var dialogWorkflowMock = Mock.Get(_dialogWorkflow);
            dialogWorkflowMock
                .Setup(workflow => workflow.InvokeAddCategoryDialog(It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync((string?)null);

            var dialog = await _target.RenderDialogAsync(hashes);

            var addItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "CategoryAdd");
            await addItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.SetTorrentCategoryAsync(It.IsAny<TorrentSelector>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AllTorrentsHaveCategory_WHEN_ClearCategoryFails_THEN_ErrorShownAndTorrentCategoriesNotReloaded()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentListAsync(
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
                    It.Is<TorrentSelector?>(selector => selector != null && TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                    It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(new List<Torrent> { CreateTorrent("Movies"), CreateTorrent("Movies") });
            apiClientMock
                .Setup(client => client.SetTorrentCategoryAsync(It.IsAny<TorrentSelector>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure", System.Net.HttpStatusCode.InternalServerError);

            var dialog = await _target.RenderDialogAsync(hashes);

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-Movies");
            await listItem.Find("div").ClickAsync(new MouseEventArgs());

            snackbarMock.Verify(snackbar => snackbar.Add("Failure", Severity.Error, null, null), Times.Once);
            apiClientMock.Verify(client => client.GetTorrentListAsync(
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
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_AddCategoryConfirmed_WHEN_Clicked_THEN_CategoryAdded()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .SetupSequence(client => client.GetTorrentListAsync(
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
                    It.Is<TorrentSelector?>(selector => selector != null && TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies") })
                .ReturnsAsync(new List<Torrent> { CreateTorrent("Movies") });
            apiClientMock
                .Setup(client => client.SetTorrentCategoryAsync(It.IsAny<TorrentSelector>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsSuccess(Task.CompletedTask);

            var dialogWorkflowMock = Mock.Get(_dialogWorkflow);
            dialogWorkflowMock
                .Setup(workflow => workflow.InvokeAddCategoryDialog(It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync("NewCategory");

            var dialog = await _target.RenderDialogAsync(hashes);

            var addItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "CategoryAdd");
            await addItem.Find("div").ClickAsync(new MouseEventArgs());

            dialog.Component.WaitForAssertion(() =>
                FindComponentByTestId<MudListItem<string>>(dialog.Component, "Category-NewCategory").Should().NotBeNull());

            apiClientMock.Verify(client => client.SetTorrentCategoryAsync(
                It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                "NewCategory",
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_AddCategoryConfirmed_WHEN_SetCategoryFails_THEN_ErrorShownAndCategoryNotAdded()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentListAsync(
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
                    It.Is<TorrentSelector?>(selector => selector != null && TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                    It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(new List<Torrent> { CreateTorrent("Movies") });
            apiClientMock
                .Setup(client => client.SetTorrentCategoryAsync(It.IsAny<TorrentSelector>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure", System.Net.HttpStatusCode.InternalServerError);

            var dialogWorkflowMock = Mock.Get(_dialogWorkflow);
            dialogWorkflowMock
                .Setup(workflow => workflow.InvokeAddCategoryDialog(It.IsAny<string?>(), It.IsAny<string?>()))
                .ReturnsAsync("NewCategory");

            var dialog = await _target.RenderDialogAsync(hashes);

            var addItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "CategoryAdd");
            await addItem.Find("div").ClickAsync(new MouseEventArgs());

            snackbarMock.Verify(snackbar => snackbar.Add("Failure", Severity.Error, null, null), Times.Once);
            apiClientMock.Verify(client => client.GetTorrentListAsync(
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
                It.IsAny<CancellationToken>()), Times.Once);
            dialog.Component.FindComponents<MudListItem<string>>()
                .Any(item => item.Instance.Text == "NewCategory")
                .Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_RemoveInvoked_WHEN_Clicked_THEN_CategoryRemoved()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentListAsync(
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
                    It.Is<TorrentSelector?>(selector => selector != null && TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                    It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(new List<Torrent> { CreateTorrent("Movies") });
            apiClientMock
                .Setup(client => client.SetTorrentCategoryAsync(It.IsAny<TorrentSelector>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsSuccess(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(hashes);

            var removeItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "CategoryRemove");
            await removeItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.SetTorrentCategoryAsync(
                It.Is<TorrentSelector>(selector => TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                string.Empty,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RemoveInvoked_WHEN_ClearCategoryFails_THEN_ErrorShownAndTorrentCategoriesNotReloaded()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            var snackbarMock = TestContext.UseSnackbarMock(MockBehavior.Loose);
            apiClientMock
                .Setup(client => client.GetAllCategoriesAsync())
                .ReturnsSuccessAsync(new Dictionary<string, Category>
                {
                    { "Movies", new Category("Movies", null, null) },
                });
            apiClientMock
                .Setup(client => client.GetTorrentListAsync(
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
                    It.Is<TorrentSelector?>(selector => selector != null && TorrentSelectorTestHelper.HasHashes(selector, hashes)),
                    It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(new List<Torrent> { CreateTorrent("Movies") });
            apiClientMock
                .Setup(client => client.SetTorrentCategoryAsync(It.IsAny<TorrentSelector>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure", System.Net.HttpStatusCode.InternalServerError);

            var dialog = await _target.RenderDialogAsync(hashes);

            var removeItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "CategoryRemove");
            await removeItem.Find("div").ClickAsync(new MouseEventArgs());

            snackbarMock.Verify(snackbar => snackbar.Add("Failure", Severity.Error, null, null), Times.Once);
            apiClientMock.Verify(client => client.GetTorrentListAsync(
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
                It.IsAny<CancellationToken>()), Times.Once);
        }

        private static Torrent CreateTorrent(string? category)
        {
            return ClientTorrentFactory.Create(category: category);
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
