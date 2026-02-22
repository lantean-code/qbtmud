using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class PathBrowserDialogTests : RazorComponentTestBase<PathBrowserDialog>
    {
        private readonly IApiClient _apiClient;
        private readonly PathBrowserDialogTestDriver _target;

        public PathBrowserDialogTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            TestContext.AddSingleton(_apiClient);
            _target = new PathBrowserDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_InitialPathProvided_WHEN_Rendered_THEN_ListsEntries()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("/root/", DirectoryContentMode.Directories))
                .ReturnsAsync(new[] { "/root/Folder", "/root/Alpha" });
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("/root/", DirectoryContentMode.Files))
                .ReturnsAsync(new[] { "/root/file.txt" });

            var dialog = await _target.RenderDialogAsync(initialPath: "/root/");

            dialog.Component.WaitForAssertion(() =>
            {
                FindComponentByTestId<MudListItem<string>>(dialog.Component, "PathBrowserEntry-Folder").Should().NotBeNull();
                FindComponentByTestId<MudListItem<string>>(dialog.Component, "PathBrowserEntry-file.txt").Should().NotBeNull();
            });
        }

        [Fact]
        public async Task GIVEN_DefaultPathEmpty_WHEN_Rendered_THEN_ShowsError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDefaultSavePath())
                .ReturnsAsync(string.Empty);

            var dialog = await _target.RenderDialogAsync();

            dialog.Component.WaitForAssertion(() =>
            {
                GetAlertText(FindComponentByTestId<MudAlert>(dialog.Component, "PathBrowserLoadError"))
                    .Should()
                    .Be("Enter a valid path.");
            });
        }

        [Fact]
        public async Task GIVEN_DefaultPathProvided_WHEN_Rendered_THEN_UsesDefaultPath()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDefaultSavePath())
                .ReturnsAsync("C:/");
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Files))
                .ReturnsAsync(new[] { "C:/file.txt" });

            var dialog = await _target.RenderDialogAsync();

            dialog.Component.WaitForAssertion(() =>
            {
                var pathField = dialog.Component.FindComponent<MudTextField<string>>();
                pathField.Instance.GetState(x => x.Value).Should().Be("C:/");
                FindComponentByTestId<MudListItem<string>>(dialog.Component, "PathBrowserEntry-file.txt").Should().NotBeNull();
            });
        }

        [Fact]
        public async Task GIVEN_DebouncedLoad_WHEN_Pending_THEN_ShowsLoadingIndicator()
        {
            var tcs = new TaskCompletionSource<IReadOnlyList<string>>();
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("D:/", DirectoryContentMode.Directories))
                .Returns(tcs.Task);
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("D:/", DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "C:/");
            var pathField = dialog.Component.FindComponent<MudTextField<string>>();

            var loadTask = dialog.Component.InvokeAsync(() => pathField.Instance.ValueChanged.InvokeAsync("D:/"));
            dialog.Component.WaitForAssertion(() =>
            {
                FindComponentByTestId<MudProgressLinear>(dialog.Component, "PathBrowserLoading").Should().NotBeNull();
            });

            tcs.SetResult(Array.Empty<string>());
            await loadTask;
        }

        [Fact]
        public async Task GIVEN_DefaultPathThrows_WHEN_Rendered_THEN_ShowsError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDefaultSavePath())
                .ThrowsAsync(new InvalidOperationException());

            var dialog = await _target.RenderDialogAsync();

            dialog.Component.WaitForAssertion(() =>
            {
                GetAlertText(FindComponentByTestId<MudAlert>(dialog.Component, "PathBrowserLoadError"))
                    .Should()
                    .Be("Enter a valid path.");
            });
        }

        [Fact]
        public async Task GIVEN_PathChangedTwice_WHEN_Debounced_THEN_UsesLatestPath()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "A/");
            var pathField = dialog.Component.FindComponent<MudTextField<string>>();

            await dialog.Component.InvokeAsync(() => pathField.Instance.ValueChanged.InvokeAsync("B/"));
            await dialog.Component.InvokeAsync(() => pathField.Instance.ValueChanged.InvokeAsync("C/"));

            dialog.Component.WaitForAssertion(() =>
            {
                Mock.Get(_apiClient).Verify(client => client.GetDirectoryContent("C/", DirectoryContentMode.Directories), Times.AtLeastOnce);
            });
        }

        [Fact]
        public async Task GIVEN_PathChangedTwiceQuickly_WHEN_Debounced_THEN_CancelsPreviousDelay()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "A/");
            var pathField = dialog.Component.FindComponent<MudTextField<string>>();

            var first = dialog.Component.InvokeAsync(() => pathField.Instance.ValueChanged.InvokeAsync("B/"));
            var second = dialog.Component.InvokeAsync(() => pathField.Instance.ValueChanged.InvokeAsync("C/"));

            await Task.WhenAll(first, second);
        }

        [Fact]
        public async Task GIVEN_NavigateUp_WHEN_ParentExists_THEN_LoadsParent()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "C:/Folder/Sub");
            var upButton = FindIconButton(dialog.Component, Icons.Material.Filled.ArrowUpward);

            await dialog.Component.InvokeAsync(() => upButton.Instance.OnClick.InvokeAsync());

            var pathField = dialog.Component.FindComponent<MudTextField<string>>();
            pathField.Instance.GetState(x => x.Value).Should().Be("C:/Folder/");

            Mock.Get(_apiClient).Verify(
                client => client.GetDirectoryContent("C:/Folder/", DirectoryContentMode.Directories),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task GIVEN_NavigateUp_WHEN_NoParent_THEN_DoesNothing()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "Folder");
            var upButton = FindIconButton(dialog.Component, Icons.Material.Filled.ArrowUpward);

            await dialog.Component.InvokeAsync(() => upButton.Instance.OnClick.InvokeAsync());

            var pathField = dialog.Component.FindComponent<MudTextField<string>>();
            pathField.Instance.GetState(x => x.Value).Should().Be("Folder");
        }

        [Fact]
        public async Task GIVEN_NoParent_WHEN_Rendered_THEN_UpDisabled()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "Folder");
            var upButton = FindIconButton(dialog.Component, Icons.Material.Filled.ArrowUpward);

            upButton.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_RootPath_WHEN_Rendered_THEN_UpEnabled()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "/");
            var upButton = FindIconButton(dialog.Component, Icons.Material.Filled.ArrowUpward);

            upButton.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_BackslashRoot_WHEN_Rendered_THEN_UpEnabled()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("\\", DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("\\", DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "\\");
            var upButton = FindIconButton(dialog.Component, Icons.Material.Filled.ArrowUpward);

            upButton.Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_OnlySeparators_WHEN_Rendered_THEN_UpDisabled()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "///");
            var upButton = FindIconButton(dialog.Component, Icons.Material.Filled.ArrowUpward);

            upButton.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_RefreshClicked_WHEN_Loaded_THEN_Reloads()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "C:/");
            var refreshButton = FindIconButton(dialog.Component, Icons.Material.Filled.Refresh);

            await dialog.Component.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(
                client => client.GetDirectoryContent("C:/", DirectoryContentMode.Directories),
                Times.AtLeast(2));
        }

        [Fact]
        public async Task GIVEN_SelectFolderNotAllowed_WHEN_Clicked_THEN_DoesNotClose()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "C:/Folder", allowFolderSelection: false);
            var selectButton = FindButton(dialog.Component, "SelectFolder");

            await dialog.Component.InvokeAsync(() => selectButton.Instance.OnClick.InvokeAsync());

            dialog.Reference.Result.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_SelectFolderAllowed_WHEN_Clicked_THEN_ClosesWithPath()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent(It.IsAny<string>(), DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "C:/Folder", allowFolderSelection: true);
            var selectButton = FindButton(dialog.Component, "SelectFolder");

            await dialog.Component.InvokeAsync(() => selectButton.Instance.OnClick.InvokeAsync());

            var result = await dialog.Reference.Result;
            result!.Data.Should().Be("C:/Folder");
        }

        [Fact]
        public async Task GIVEN_FileEntry_WHEN_ClickedWithFilesAllowed_THEN_ClosesWithFile()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Files))
                .ReturnsAsync(new[] { "C:/file.txt" });

            var dialog = await _target.RenderDialogAsync(initialPath: "C:/");
            dialog.Component.WaitForAssertion(() =>
            {
                FindComponentByTestId<MudListItem<string>>(dialog.Component, "PathBrowserEntry-file.txt").Should().NotBeNull();
            });

            var listItem = dialog.Component.FindComponents<MudListItem<string>>().Single();
            await dialog.Component.InvokeAsync(() => listItem.Instance.OnClick.InvokeAsync());

            var result = await dialog.Reference.Result;
            result!.Data.Should().Be("C:/file.txt");
        }

        [Fact]
        public async Task GIVEN_FileEntriesUnsorted_WHEN_Loaded_THEN_SortsByName()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Files))
                .ReturnsAsync(new[] { "C:/z.txt", "C:/a.txt" });

            var dialog = await _target.RenderDialogAsync(initialPath: "C:/");

            dialog.Component.WaitForAssertion(() =>
            {
                var listItems = dialog.Component.FindComponents<MudListItem<string>>();
                listItems.Select(item => item.Instance.Text).Should().ContainInOrder("a.txt", "z.txt");
            });
        }

        [Fact]
        public async Task GIVEN_EntryWithoutSeparators_WHEN_Loaded_THEN_ShowsEntry()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Directories))
                .ReturnsAsync(new[] { "Folder" });
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "C:/");

            dialog.Component.WaitForAssertion(() =>
            {
                FindComponentByTestId<MudListItem<string>>(dialog.Component, "PathBrowserEntry-Folder").Should().NotBeNull();
            });
        }

        [Fact]
        public async Task GIVEN_WhitespaceEntry_WHEN_Loaded_THEN_DoesNotError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Directories))
                .ReturnsAsync(new[] { " " });
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Files))
                .ReturnsAsync(new[] { "C:/file.txt" });

            var dialog = await _target.RenderDialogAsync(initialPath: "C:/");

            dialog.Component.WaitForAssertion(() =>
            {
                FindComponentByTestId<MudListItem<string>>(dialog.Component, "PathBrowserEntry-file.txt").Should().NotBeNull();
            });
        }

        [Fact]
        public async Task GIVEN_DirectoryEntry_WHEN_Clicked_THEN_UpdatesPath()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Directories))
                .ReturnsAsync(new[] { "C:/Folder" });
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/Folder", DirectoryContentMode.Directories))
                .ReturnsAsync(Array.Empty<string>());
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/Folder", DirectoryContentMode.Files))
                .ReturnsAsync(Array.Empty<string>());

            var dialog = await _target.RenderDialogAsync(initialPath: "C:/");
            var listItem = dialog.Component.FindComponents<MudListItem<string>>().Single();

            await dialog.Component.InvokeAsync(() => listItem.Instance.OnClick.InvokeAsync());

            var pathField = dialog.Component.FindComponent<MudTextField<string>>();
            pathField.Instance.GetState(x => x.Value).Should().Be("C:/Folder");
        }

        [Fact]
        public async Task GIVEN_ApiThrows_WHEN_LoadingEntries_THEN_ShowsError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetDirectoryContent("C:/", DirectoryContentMode.Directories))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            var dialog = await _target.RenderDialogAsync(initialPath: "C:/");

            dialog.Component.WaitForAssertion(() =>
            {
                GetAlertText(FindComponentByTestId<MudAlert>(dialog.Component, "PathBrowserLoadError"))
                    .Should()
                    .Be("Unable to load directory content: Failure");
            });
        }

        private string? GetAlertText(IRenderedComponent<MudAlert> component)
        {
            return GetChildContentText(component.Instance.ChildContent);
        }

        private sealed class PathBrowserDialogTestDriver
        {
            private readonly ComponentTestContext _testContext;

            public PathBrowserDialogTestDriver(ComponentTestContext testContext)
            {
                _testContext = testContext;
            }

            public async Task<PathBrowserDialogRenderContext> RenderDialogAsync(
                string? initialPath = null,
                DirectoryContentMode mode = DirectoryContentMode.All,
                bool allowFolderSelection = true)
            {
                var provider = _testContext.Render<MudDialogProvider>();
                var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

                var parameters = new DialogParameters
                {
                    { nameof(PathBrowserDialog.Mode), mode },
                    { nameof(PathBrowserDialog.AllowFolderSelection), allowFolderSelection },
                };

                if (initialPath is not null)
                {
                    parameters.Add(nameof(PathBrowserDialog.InitialPath), initialPath);
                }

                var reference = await dialogService.ShowAsync<PathBrowserDialog>("Browse", parameters);

                var dialog = provider.FindComponent<MudDialog>();
                var component = provider.FindComponent<PathBrowserDialog>();

                return new PathBrowserDialogRenderContext(provider, dialog, component, reference);
            }
        }

        private sealed class PathBrowserDialogRenderContext
        {
            public PathBrowserDialogRenderContext(
                IRenderedComponent<MudDialogProvider> provider,
                IRenderedComponent<MudDialog> dialog,
                IRenderedComponent<PathBrowserDialog> component,
                IDialogReference reference)
            {
                Provider = provider;
                Dialog = dialog;
                Component = component;
                Reference = reference;
            }

            public IRenderedComponent<MudDialogProvider> Provider { get; }

            public IRenderedComponent<MudDialog> Dialog { get; }

            public IRenderedComponent<PathBrowserDialog> Component { get; }

            public IDialogReference Reference { get; }
        }
    }
}
