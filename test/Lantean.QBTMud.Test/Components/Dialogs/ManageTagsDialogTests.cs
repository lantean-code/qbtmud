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
    public sealed class ManageTagsDialogTests : RazorComponentTestBase<ManageTagsDialog>
    {
        private readonly IApiClient _apiClient;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly ManageTagsDialogTestDriver _target;

        public ManageTagsDialogTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _dialogWorkflow = Mock.Of<IDialogWorkflow>();

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.RemoveAll<IDialogWorkflow>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.AddSingleton(_dialogWorkflow);

            _target = new ManageTagsDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_NoHashes_WHEN_Rendered_THEN_RendersCheckedIcon()
        {
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllTags())
                .ReturnsAsync(new List<string> { "Tag" });
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

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Tag-Tag");
            listItem.Instance.Icon.Should().Be(Icons.Material.Filled.CheckBox);

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
        public async Task GIVEN_AllTorrentsHaveTag_WHEN_Clicked_THEN_RemovesTag()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllTags())
                .ReturnsAsync(new List<string> { "Tag" });
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
                .ReturnsAsync(new List<Torrent> { CreateTorrent(["Tag"]), CreateTorrent(["Tag"]) })
                .ReturnsAsync(new List<Torrent> { CreateTorrent(Array.Empty<string>()) });
            apiClientMock
                .Setup(client => client.RemoveTorrentTags(It.IsAny<IEnumerable<string>>(), It.IsAny<bool?>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(hashes);

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Tag-Tag");
            await listItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.RemoveTorrentTags(
                It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" })),
                It.IsAny<bool?>(),
                It.Is<string[]>(value => value.SequenceEqual(hashes))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_PartialTag_WHEN_Clicked_THEN_AddsTag()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllTags())
                .ReturnsAsync(new List<string> { "Tag" });
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
                .ReturnsAsync(new List<Torrent> { CreateTorrent(["Tag"]), CreateTorrent(Array.Empty<string>()) })
                .ReturnsAsync(new List<Torrent> { CreateTorrent(["Tag"]), CreateTorrent(["Tag"]) });
            apiClientMock
                .Setup(client => client.AddTorrentTags(It.IsAny<IEnumerable<string>>(), It.IsAny<bool?>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(hashes);

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Tag-Tag");
            listItem.Instance.Icon.Should().Be(Icons.Material.Filled.IndeterminateCheckBox);
            await listItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.AddTorrentTags(
                It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" })),
                It.IsAny<bool?>(),
                It.Is<string[]>(value => value.SequenceEqual(hashes))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_TagNotPresent_WHEN_Clicked_THEN_AddsTag()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllTags())
                .ReturnsAsync(new List<string> { "Tag" });
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
                .ReturnsAsync(new List<Torrent> { CreateTorrent(null) })
                .ReturnsAsync(new List<Torrent> { CreateTorrent(["Tag"]) });
            apiClientMock
                .Setup(client => client.AddTorrentTags(It.IsAny<IEnumerable<string>>(), It.IsAny<bool?>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(hashes);

            var listItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "Tag-Tag");
            await listItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.AddTorrentTags(
                It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" })),
                It.IsAny<bool?>(),
                It.Is<string[]>(value => value.SequenceEqual(hashes))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_AddTagsCanceled_WHEN_Clicked_THEN_NoTagsAdded()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllTags())
                .ReturnsAsync(new List<string> { "Tag" });
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
                .ReturnsAsync(new List<Torrent> { CreateTorrent(["Tag"]) });

            var dialogWorkflowMock = Mock.Get(_dialogWorkflow);
            dialogWorkflowMock
                .Setup(workflow => workflow.ShowAddTagsDialog())
                .ReturnsAsync((HashSet<string>?)null);

            var dialog = await _target.RenderDialogAsync(hashes);

            var addItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "TagAdd");
            await addItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.AddTorrentTags(It.IsAny<IEnumerable<string>>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AddTagsEmpty_WHEN_Clicked_THEN_NoTagsAdded()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllTags())
                .ReturnsAsync(new List<string> { "Tag" });
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
                .ReturnsAsync(new List<Torrent> { CreateTorrent(["Tag"]) });

            var dialogWorkflowMock = Mock.Get(_dialogWorkflow);
            dialogWorkflowMock
                .Setup(workflow => workflow.ShowAddTagsDialog())
                .ReturnsAsync(new HashSet<string>());

            var dialog = await _target.RenderDialogAsync(hashes);

            var addItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "TagAdd");
            await addItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.AddTorrentTags(It.IsAny<IEnumerable<string>>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_AddTagsProvided_WHEN_Clicked_THEN_TagsAdded()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllTags())
                .ReturnsAsync(new List<string> { "Tag" });
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
                .ReturnsAsync(new List<Torrent> { CreateTorrent(["Tag"]) })
                .ReturnsAsync(new List<Torrent> { CreateTorrent(["Tag", "NewTag"]) });
            apiClientMock
                .Setup(client => client.AddTorrentTags(It.IsAny<IEnumerable<string>>(), It.IsAny<bool?>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialogWorkflowMock = Mock.Get(_dialogWorkflow);
            dialogWorkflowMock
                .Setup(workflow => workflow.ShowAddTagsDialog())
                .ReturnsAsync(new HashSet<string> { "NewTag" });

            var dialog = await _target.RenderDialogAsync(hashes);

            var addItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "TagAdd");
            await addItem.Find("div").ClickAsync(new MouseEventArgs());

            dialog.Component.WaitForAssertion(() =>
                FindComponentByTestId<MudListItem<string>>(dialog.Component, "Tag-NewTag").Should().NotBeNull());

            apiClientMock.Verify(client => client.AddTorrentTags(
                It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "NewTag" })),
                It.IsAny<bool?>(),
                It.Is<string[]>(value => value.SequenceEqual(hashes))), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RemoveAllInvoked_WHEN_Clicked_THEN_RemovesTags()
        {
            var hashes = new[] { "Hash" };
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .Setup(client => client.GetAllTags())
                .ReturnsAsync(new List<string> { "Tag" });
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
                .ReturnsAsync(new List<Torrent> { CreateTorrent(["Tag"]) });
            apiClientMock
                .Setup(client => client.RemoveTorrentTags(It.IsAny<IEnumerable<string>>(), It.IsAny<bool?>(), It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync(hashes);

            var removeItem = FindComponentByTestId<MudListItem<string>>(dialog.Component, "TagRemoveAll");
            await removeItem.Find("div").ClickAsync(new MouseEventArgs());

            apiClientMock.Verify(client => client.RemoveTorrentTags(
                It.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "Tag" })),
                It.IsAny<bool?>(),
                It.Is<string[]>(value => value.SequenceEqual(hashes))), Times.Once);
        }

        private static Torrent CreateTorrent(IReadOnlyList<string>? tags)
        {
            return new Torrent
            {
                Tags = tags,
            };
        }
    }

    internal sealed class ManageTagsDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public ManageTagsDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<ManageTagsDialogRenderContext> RenderDialogAsync(IEnumerable<string> hashes)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ManageTagsDialog.Hashes), hashes },
            };

            var reference = await dialogService.ShowAsync<ManageTagsDialog>("Manage Tags", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<ManageTagsDialog>();

            return new ManageTagsDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class ManageTagsDialogRenderContext
    {
        public ManageTagsDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<ManageTagsDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<ManageTagsDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
