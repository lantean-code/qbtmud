using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Filter;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Net;
using ClientPriority = Lantean.QBitTorrentClient.Models.Priority;
using ContentItem = Lantean.QBTMud.Models.ContentItem;
using FileData = Lantean.QBitTorrentClient.Models.FileData;
using FilterOperator = Lantean.QBTMud.Filter.FilterOperator;
using UiPriority = Lantean.QBTMud.Models.Priority;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class FilesTabTests : RazorComponentTestBase
    {
        private readonly IApiClient _apiClient = Mock.Of<IApiClient>();
        private readonly IManagedTimer _timer;
        private readonly IManagedTimerFactory _timerFactory;
        private readonly IDialogWorkflow _dialogWorkflow = Mock.Of<IDialogWorkflow>();
        private Func<CancellationToken, Task<ManagedTimerTickResult>>? _tickHandler;
        private IRenderedComponent<MudPopoverProvider>? _popoverProvider;

        public FilesTabTests()
        {
            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.RemoveAll<IDialogWorkflow>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.AddSingleton(_dialogWorkflow);
            TestContext.UseSnackbarMock(MockBehavior.Loose);

            _timer = Mock.Of<IManagedTimer>();
            _timerFactory = Mock.Of<IManagedTimerFactory>();
            Mock.Get(_timerFactory)
                .Setup(factory => factory.Create(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(_timer);
            Mock.Get(_timer)
                .Setup(timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((handler, _) => _tickHandler = handler)
                .ReturnsAsync(true);
            TestContext.Services.RemoveAll<IManagedTimerFactory>();
            TestContext.Services.AddSingleton(_timerFactory);
        }

        private Mock<IApiClient> ApiClientMock
        {
            get
            {
                return Mock.Get(_apiClient);
            }
        }

        private Mock<IDialogWorkflow> DialogWorkflowMock
        {
            get
            {
                return Mock.Get(_dialogWorkflow);
            }
        }

        [Fact]
        public async Task GIVEN_TimerTick_WHEN_ContentFetched_THEN_FolderExpandsToShowFiles()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("Folder/file1.txt", "Folder/file2.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() => HasElementByTestId(target, "FolderToggle-Folder").Should().BeTrue());

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-Folder");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            target.WaitForAssertion(() => HasElementByTestId(target, "Priority-Folder_file1.txt").Should().BeTrue());
        }

        [Fact]
        public async Task GIVEN_FileRow_WHEN_PriorityChanged_THEN_ApiCalledWithIndexes()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("Root/file1.txt"));
            ApiClientMock.Setup(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.Single() == 1), ClientPriority.High)).Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-Root");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            target.WaitForAssertion(() => HasElementByTestId(target, "Priority-Root_file1.txt").Should().BeTrue());

            var prioritySelect = FindComponentByTestId<MudSelect<UiPriority>>(target, "Priority-Root_file1.txt");
            await target.InvokeAsync(() => prioritySelect.Instance.ValueChanged.InvokeAsync(UiPriority.High));

            ApiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.Single() == 1), ClientPriority.High), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Files_WHEN_DoNotDownloadAvailabilityInvoked_THEN_LowAvailabilityFilesUpdated()
        {
            var files = new[]
            {
                new FileData(1, "root/low.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.5f),
                new FileData(2, "root/high.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.9f),
            };
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            ApiClientMock
                .Setup(c => c.SetFilePriority("Hash", It.IsAny<IEnumerable<int>>(), ClientPriority.DoNotDownload))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() => HasElementByTestId(target, "FolderToggle-root").Should().BeTrue());

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(async () => await toggle.Find("button").ClickAsync());
            target.WaitForAssertion(() => HasElementByTestId(target, "Priority-root_low.txt").Should().BeTrue());

            var menu = FindComponentByTestId<MudMenu>(target, "DoNotDownloadMenu");
            var menuActivator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(async () => await menuActivator.Find("button").ClickAsync());

            var availabilityItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("DoNotDownloadLessThan80")}\"]");
            await _popoverProvider!.InvokeAsync(() => availabilityItem.Click());

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(client => client.SetFilePriority(
                    "Hash",
                    It.Is<IEnumerable<int>>(indexes => indexes.SequenceEqual(new[] { 1 })),
                    ClientPriority.DoNotDownload), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_SearchText_WHEN_Filtered_THEN_OnlyMatchingFilesRemain()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt", "folder/file2.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-folder");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            var search = FindComponentByTestId<MudTextField<string>>(target, "FilesTabSearch");
            search.Find("input").Input("file2");

            target.WaitForAssertion(() =>
            {
                HasElementByTestId(target, "Priority-folder_file2.txt").Should().BeTrue();
                HasElementByTestId(target, "Priority-folder_file1.txt").Should().BeFalse();
            });
        }

        [Fact]
        public async Task GIVEN_NoFilesLoaded_WHEN_DoNotDownloadMenuInvoked_THEN_NoApiCalls()
        {
            _popoverProvider = TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<FilesTab>(parameters =>
            {
                parameters.AddCascadingValue("RefreshInterval", 10);
                parameters.Add(p => p.Active, false);
                parameters.Add(p => p.Hash, "Hash");
            });

            var menu = FindComponentByTestId<MudMenu>(target, "DoNotDownloadMenu");
            var activator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(() => activator.Find("button").Click());

            var filteredItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("DoNotDownloadFiltered")}\"]");
            await _popoverProvider!.InvokeAsync(async () => await filteredItem.ClickAsync());

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(client => client.SetFilePriority(It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), It.IsAny<ClientPriority>()), Times.Never);
            });
        }

        [Fact]
        public async Task GIVEN_NoSelection_WHEN_RenameToolbarClicked_THEN_MultiRenameDialogInvoked()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("root/file1.txt"));
            DialogWorkflowMock.Setup(d => d.InvokeRenameFilesDialog("Hash")).Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var renameButton = FindComponentByTestId<MudIconButton>(target, "RenameToolbar");
            await target.InvokeAsync(() => renameButton.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                DialogWorkflowMock.Verify(d => d.InvokeRenameFilesDialog("Hash"), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_FileSelected_WHEN_RenameToolbarClicked_THEN_StringDialogShown()
        {
            var files = CreateFiles("root/file1.txt");
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            DialogWorkflowMock
                .Setup(d => d.InvokeStringFieldDialog("Renaming", "New name:", "file1.txt", It.IsAny<Func<string, Task>>()))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => toggle.Find("button").ClickAsync());

            target.WaitForAssertion(() => HasElementByTestId(target, "Priority-root_file1.txt").Should().BeTrue());

            var row = target.WaitForElement($"[data-test-id=\"{TestIdHelper.For("Row-Files-root_file1.txt")}\"]");
            await target.InvokeAsync(() => row.Click());

            var toolbarRename = FindComponentByTestId<MudIconButton>(target, "RenameToolbar");
            await target.InvokeAsync(() => toolbarRename.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                DialogWorkflowMock.Verify(d => d.InvokeStringFieldDialog("Renaming", "New name:", "file1.txt", It.IsAny<Func<string, Task>>()), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_FileContextMenu_WHEN_RenameClicked_THEN_StringDialogShown()
        {
            var files = CreateFiles("root/file1.txt");
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            DialogWorkflowMock
                .Setup(d => d.InvokeStringFieldDialog("Renaming", "New name:", "file1.txt", It.IsAny<Func<string, Task>>()))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            target.WaitForAssertion(() => HasElementByTestId(target, "Priority-root_file1.txt").Should().BeTrue());

            var row = target.WaitForElement($"[data-test-id=\"{TestIdHelper.For("Row-Files-root_file1.txt")}\"]");
            await target.InvokeAsync(() => row.TriggerEvent("oncontextmenu", new MouseEventArgs()));

            var contextRename = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("ContextMenuRename")}\"]");
            await target.InvokeAsync(() => contextRename.Click());

            target.WaitForAssertion(() =>
            {
                DialogWorkflowMock.Verify(d => d.InvokeStringFieldDialog("Renaming", "New name:", "file1.txt", It.IsAny<Func<string, Task>>()), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_TimerTick_WHEN_FileListExists_THEN_MergeApplied()
        {
            var initial = CreateFiles("root/file1.txt");
            var updated = CreateFiles("root/file1.txt", "root/file2.txt");
            ApiClientMock
                .SetupSequence(c => c.GetTorrentContents("Hash"))
                .ReturnsAsync(initial)
                .ReturnsAsync(updated);

            var dataManagerMock = new Mock<ITorrentDataManager>();
            TestContext.Services.RemoveAll<ITorrentDataManager>();
            TestContext.Services.AddSingleton(dataManagerMock.Object);

            dataManagerMock.Setup(m => m.CreateContentsList(initial)).Returns(new Dictionary<string, ContentItem>());
            dataManagerMock.Setup(m => m.MergeContentsList(updated, It.IsAny<Dictionary<string, ContentItem>>())).Returns(true);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() =>
            {
                dataManagerMock.Verify(m => m.MergeContentsList(updated, It.IsAny<Dictionary<string, ContentItem>>()), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_FileListMissing_WHEN_TimerRuns_THEN_ContentListInitialized()
        {
            var files = CreateFiles("root/file1.txt");
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);

            var dataManagerMock = new Mock<ITorrentDataManager>();
            TestContext.Services.RemoveAll<ITorrentDataManager>();
            TestContext.Services.AddSingleton(dataManagerMock.Object);

            dataManagerMock
                .SetupSequence(m => m.CreateContentsList(files))
                .Returns((Dictionary<string, ContentItem>?)null!)
                .Returns(new Dictionary<string, ContentItem>());

            var target = RenderFilesTab();

            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() =>
            {
                dataManagerMock.Verify(m => m.CreateContentsList(files), Times.Exactly(2));
            });
        }

        [Fact]
        public async Task GIVEN_TimerTick_WHEN_ComponentInactive_THEN_NoRefreshPerformed()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("root/file1.txt"));

            var target = TestContext.Render<FilesTab>(parameters =>
            {
                parameters.AddCascadingValue("RefreshInterval", 10);
                parameters.Add(p => p.Active, false);
                parameters.Add(p => p.Hash, "Hash");
            });

            await TriggerTimerTickAsync(target);

            ApiClientMock.Verify(c => c.GetTorrentContents(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_TimerTick_WHEN_ApiReturnsForbidden_THEN_RefreshStops()
        {
            ApiClientMock
                .SetupSequence(c => c.GetTorrentContents("Hash"))
                .ReturnsAsync(CreateFiles("root/file1.txt"))
                .ThrowsAsync(new HttpRequestException(null, null, HttpStatusCode.Forbidden));

            var target = RenderFilesTab();

            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(c => c.GetTorrentContents("Hash"), Times.Exactly(2));
            });
        }

        [Fact]
        public async Task GIVEN_ExpandedFolder_WHEN_ToggledAgain_THEN_FolderCollapses()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-folder");
            await target.InvokeAsync(() => toggle.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() => HasElementByTestId(target, "Priority-folder_file1.txt").Should().BeTrue());

            await target.InvokeAsync(() => toggle.Instance.OnClick.InvokeAsync());

            target.WaitForAssertion(() => HasElementByTestId(target, "Priority-folder_file1.txt").Should().BeFalse());
        }

        [Fact]
        public async Task GIVEN_TimerStops_WHEN_NoFurtherTicks_THEN_NoAdditionalWork()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("root/file1.txt"));

            var target = RenderFilesTab();

            await TriggerTimerTickAsync(target);
            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(client => client.GetTorrentContents("Hash"), Times.AtLeastOnce);
            });
            ApiClientMock.ClearInvocations();

            target.Render();

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(client => client.GetTorrentContents("Hash"), Times.Never);
            });
        }

        [Fact]
        public async Task GIVEN_NoUpdatesFromTimer_WHEN_RenderedTwice_THEN_SecondRenderSkipsLoop()
        {
            var dataManagerMock = new Mock<ITorrentDataManager>();
            dataManagerMock.Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>())).Returns(new Dictionary<string, ContentItem>());
            dataManagerMock.Setup(m => m.MergeContentsList(It.IsAny<IReadOnlyList<FileData>>(), It.IsAny<Dictionary<string, ContentItem>>())).Returns(false);
            TestContext.Services.RemoveAll<ITorrentDataManager>();
            TestContext.Services.AddSingleton(dataManagerMock.Object);

            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("root/file1.txt"));

            var target = TestContext.Render<FilesTab>(parameters =>
            {
                parameters.AddCascadingValue("RefreshInterval", 10);
                parameters.Add(p => p.Active, true);
                parameters.Add(p => p.Hash, "Hash");
            });

            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(client => client.GetTorrentContents("Hash"), Times.AtLeastOnce);
            });

            ApiClientMock.ClearInvocations();

            target.Render();

            ApiClientMock.Verify(client => client.GetTorrentContents("Hash"), Times.Never);
        }

        [Fact]
        public void GIVEN_SubsequentRender_WHEN_FirstRenderComplete_THEN_NoAdditionalInitialization()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();

            target.Render();

            ApiClientMock.Verify(c => c.GetTorrentContents("Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FilterDialogCancelled_WHEN_ShowFilterInvoked_THEN_FiltersCleared()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));
            DialogWorkflowMock.Setup(d => d.ShowFilterOptionsDialog(It.IsAny<List<PropertyFilterDefinition<ContentItem>>?>())).ReturnsAsync((List<PropertyFilterDefinition<ContentItem>>?)null);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var filterButton = FindComponentByTestId<MudIconButton>(target, "ShowFilterDialog");
            await target.InvokeAsync(() => filterButton.Find("button").Click());

            target.WaitForAssertion(() => target.Instance.Filters.Should().BeNull());
        }

        [Fact]
        public async Task GIVEN_FilterDialogWithDefinition_WHEN_Applied_THEN_FiltersStoredAndRendered()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt", "folder/file2.txt"));
            DialogWorkflowMock
                .Setup(d => d.ShowFilterOptionsDialog(It.IsAny<List<PropertyFilterDefinition<ContentItem>>?>()))
                .ReturnsAsync(new List<PropertyFilterDefinition<ContentItem>>
                {
                    new PropertyFilterDefinition<ContentItem>("Name", FilterOperator.String.Contains, "file2"),
                });

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-folder");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            var filterButton = FindComponentByTestId<MudIconButton>(target, "ShowFilterDialog");
            await target.InvokeAsync(() => filterButton.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                HasElementByTestId(target, "Priority-folder_file2.txt").Should().BeTrue();
                HasElementByTestId(target, "Priority-folder_file1.txt").Should().BeFalse();
                target.Instance.Filters.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task GIVEN_FilterApplied_WHEN_RemoveFilterClicked_THEN_AllFilesVisible()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt", "folder/file2.txt"));
            DialogWorkflowMock
                .Setup(d => d.ShowFilterOptionsDialog(It.IsAny<List<PropertyFilterDefinition<ContentItem>>?>()))
                .ReturnsAsync(new List<PropertyFilterDefinition<ContentItem>>
                {
                    new PropertyFilterDefinition<ContentItem>("Name", FilterOperator.String.Contains, "file2"),
                });

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-folder");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            var filterButton = FindComponentByTestId<MudIconButton>(target, "ShowFilterDialog");
            await target.InvokeAsync(() => filterButton.Find("button").Click());

            var removeFilterButton = FindComponentByTestId<MudIconButton>(target, "RemoveFilter");
            await target.InvokeAsync(() => removeFilterButton.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                HasElementByTestId(target, "Priority-folder_file1.txt").Should().BeTrue();
                HasElementByTestId(target, "Priority-folder_file2.txt").Should().BeTrue();
                target.Instance.Filters.Should().BeNull();
            });
        }

        [Fact]
        public async Task GIVEN_HashNull_WHEN_ParametersSet_THEN_NoLoadOccurs()
        {
            var target = TestContext.Render<FilesTab>(parameters =>
            {
                parameters.AddCascadingValue("RefreshInterval", 10);
                parameters.Add(p => p.Active, true);
                parameters.Add(p => p.Hash, null);
            });

            await TriggerTimerTickAsync(target);

            ApiClientMock.Verify(c => c.GetTorrentContents(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ExpandedNodesInStorage_WHEN_Rendered_THEN_NodesRestored()
        {
            await TestContext.SessionStorage.SetItemAsync("FilesTab.ExpandedNodes.Hash", new HashSet<string>(new[] { "folder" }), Xunit.TestContext.Current.CancellationToken);
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();

            target.WaitForAssertion(() =>
            {
                HasElementByTestId(target, "Priority-folder_file1.txt").Should().BeTrue();
            });
        }

        [Fact]
        public async Task GIVEN_RefreshActive_WHEN_SameHashProvided_THEN_LoadNotRepeated()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();

            target.Render();

            ApiClientMock.Verify(c => c.GetTorrentContents("Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Folder_WHEN_PriorityChanged_THEN_AllDescendantsUpdated()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("Folder/file1.txt", "Folder/file2.txt"));
            ApiClientMock
                .Setup(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1, 2 })), ClientPriority.Maximum))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var folderPriority = FindComponentByTestId<MudSelect<UiPriority>>(target, "Priority-Folder");
            await target.InvokeAsync(async () => await folderPriority.Instance.ValueChanged.InvokeAsync(UiPriority.Maximum));

            ApiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1, 2 })), ClientPriority.Maximum), Times.Once);
        }

        [Fact]
        public async Task GIVEN_MenuAction_WHEN_DoNotDownloadLessThan100Invoked_THEN_AllFilesUpdated()
        {
            var files = new[]
            {
                new FileData(1, "root/low.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.5f),
                new FileData(2, "root/high.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.9f),
            };
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            ApiClientMock
                .Setup(c => c.SetFilePriority("Hash", It.IsAny<IEnumerable<int>>(), ClientPriority.DoNotDownload))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            var menu = FindComponentByTestId<MudMenu>(target, "DoNotDownloadMenu");
            var menuActivator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(() => menuActivator.Find("button").Click());

            var availabilityItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("DoNotDownloadLessThan100")}\"]");
            await _popoverProvider!.InvokeAsync(() => availabilityItem.Click());

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1, 2 })), ClientPriority.DoNotDownload), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_MenuAction_WHEN_NormalPriorityLessThan80Invoked_THEN_LowAvailabilityFilesUpdated()
        {
            var files = new[]
            {
                new FileData(1, "root/low.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.5f),
                new FileData(2, "root/high.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.9f),
            };
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            ApiClientMock
                .Setup(c => c.SetFilePriority("Hash", It.IsAny<IEnumerable<int>>(), ClientPriority.Normal))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            var menu = FindComponentByTestId<MudMenu>(target, "NormalPriorityMenu");
            var menuActivator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(() => menuActivator.Find("button").ClickAsync());

            var availabilityItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("NormalPriorityLessThan80")}\"]");
            await _popoverProvider!.InvokeAsync(() => availabilityItem.Click());

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1 })), ClientPriority.Normal), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_FilteredFiles_WHEN_DoNotDownloadFilteredInvoked_THEN_VisibleFilesUpdated()
        {
            var files = new[]
            {
                new FileData(1, "root/only.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.5f),
            };
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            ApiClientMock
                .Setup(c => c.SetFilePriority("Hash", It.IsAny<IEnumerable<int>>(), ClientPriority.DoNotDownload))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            var menu = FindComponentByTestId<MudMenu>(target, "DoNotDownloadMenu");
            var menuActivator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(() => menuActivator.Find("button").Click());

            var filteredItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("DoNotDownloadFiltered")}\"]");
            await _popoverProvider!.InvokeAsync(() => filteredItem.Click());

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1 })), ClientPriority.DoNotDownload), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_FilteredFiles_WHEN_NormalPriorityFilteredInvoked_THEN_VisibleFilesUpdated()
        {
            var files = new[]
            {
                new FileData(1, "root/only.txt", 100, 0.5f, ClientPriority.DoNotDownload, false, new[] { 0 }, 0.5f),
            };
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            ApiClientMock
                .Setup(c => c.SetFilePriority("Hash", It.IsAny<IEnumerable<int>>(), ClientPriority.Normal))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            var menu = FindComponentByTestId<MudMenu>(target, "NormalPriorityMenu");
            var menuActivator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(() => menuActivator.Find("button").Click());

            var filteredItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("NormalPriorityFiltered")}\"]");
            await _popoverProvider!.InvokeAsync(() => filteredItem.Click());

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1 })), ClientPriority.Normal), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_MenuAction_WHEN_NormalPriorityLessThan100Invoked_THEN_AllFilesUpdated()
        {
            var files = new[]
            {
                new FileData(1, "root/low.txt", 100, 0.5f, ClientPriority.DoNotDownload, false, new[] { 0 }, 0.5f),
                new FileData(2, "root/high.txt", 100, 0.5f, ClientPriority.DoNotDownload, false, new[] { 0 }, 0.9f),
            };
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            ApiClientMock
                .Setup(c => c.SetFilePriority("Hash", It.IsAny<IEnumerable<int>>(), ClientPriority.Normal))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            var menu = FindComponentByTestId<MudMenu>(target, "NormalPriorityMenu");
            var menuActivator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(() => menuActivator.Find("button").Click());

            var availabilityItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("NormalPriorityLessThan100")}\"]");
            await _popoverProvider!.InvokeAsync(() => availabilityItem.Click());

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1, 2 })), ClientPriority.Normal), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_MenuAction_WHEN_NormalPriorityLessThan100WithNoMatches_THEN_NoApiCall()
        {
            var files = new[]
            {
                new FileData(1, "root/high.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 1.0f),
            };
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);
            ApiClientMock.ClearInvocations();

            var menu = FindComponentByTestId<MudMenu>(target, "NormalPriorityMenu");
            var menuActivator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(() => menuActivator.Find("button").Click());

            var availabilityItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("NormalPriorityLessThan100")}\"]");
            await _popoverProvider!.InvokeAsync(() => availabilityItem.Click());

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(client => client.SetFilePriority(It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), It.IsAny<ClientPriority>()), Times.Never);
            });
        }

        [Fact]
        public async Task GIVEN_FileListWithoutRoots_WHEN_Refreshed_THEN_NoVisibleItems()
        {
            var dataManagerMock = new Mock<ITorrentDataManager>();
            var content = new ContentItem("folder/file1.txt", "file1.txt", 1, UiPriority.Normal, 0.5f, 100, 1.0f, false, 1);
            var fileList = new Dictionary<string, ContentItem> { { content.Name, content } };
            dataManagerMock.Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>())).Returns(fileList);

            TestContext.Services.RemoveAll<ITorrentDataManager>();
            TestContext.Services.AddSingleton(dataManagerMock.Object);

            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            HasElementByTestId(target, "Priority-folder_file1.txt").Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ActiveFalse_WHEN_ParametersSet_THEN_ApiNotInvoked()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            TestContext.Render<FilesTab>(parameters =>
            {
                parameters.AddCascadingValue("RefreshInterval", 10);
                parameters.Add(p => p.Active, false);
                parameters.Add(p => p.Hash, "Hash");
            });

            ApiClientMock.Verify(c => c.GetTorrentContents(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_SameHash_WHEN_ParametersUpdated_THEN_InitialLoadNotRepeated()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();

            target.Render(parameters =>
            {
                parameters.Add(p => p.Active, true);
                parameters.Add(p => p.Hash, "Hash");
            });

            ApiClientMock.Verify(c => c.GetTorrentContents("Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_HttpForbidden_WHEN_Refreshed_THEN_CancellationRequested()
        {
            ApiClientMock
                .SetupSequence(c => c.GetTorrentContents("Hash"))
                .ReturnsAsync(CreateFiles("folder/file1.txt"))
                .ThrowsAsync(new HttpRequestException(null, null, HttpStatusCode.Forbidden));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(c => c.GetTorrentContents("Hash"), Times.Exactly(2));
            });
        }

        [Fact]
        public async Task GIVEN_TableRendered_WHEN_ColumnOptionsClicked_THEN_NoErrors()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));
            DialogWorkflowMock
                .Setup(d => d.ShowColumnsOptionsDialog(
                    It.IsAny<List<ColumnDefinition<ContentItem>>>(),
                    It.IsAny<HashSet<string>>(),
                    It.IsAny<Dictionary<string, int?>>(),
                    It.IsAny<Dictionary<string, int>>()))
                .ReturnsAsync((new HashSet<string>(), new Dictionary<string, int?>(), new Dictionary<string, int>()));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var columnButton = FindComponentByTestId<MudIconButton>(target, "ColumnOptions");
            await target.InvokeAsync(() => columnButton.Find("button").Click());
        }

        [Fact]
        public async Task GIVEN_Component_WHEN_Disposed_THEN_TimerCancelled()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();
            await target.Instance.DisposeAsync();
        }

        [Fact]
        public async Task GIVEN_ContextMenuWithoutItem_WHEN_RenameClicked_THEN_NoRenameWorkflowsInvoked()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("file1.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var table = target.FindComponent<DynamicTable<ContentItem>>();
            var contextArgs = new TableDataContextMenuEventArgs<ContentItem>(new MouseEventArgs(), new MudTd(), null);
            await target.InvokeAsync(() => table.Instance.OnTableDataContextMenu.InvokeAsync(contextArgs));

            var contextRename = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("ContextMenuRename")}\"]");
            await target.InvokeAsync(() => contextRename.Click());

            DialogWorkflowMock.Verify(d => d.InvokeStringFieldDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, Task>>()), Times.Never);
            DialogWorkflowMock.Verify(d => d.InvokeRenameFilesDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_FileLongPress_WHEN_ContextRenameClicked_THEN_StringDialogInvoked()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("file1.txt"));
            DialogWorkflowMock
                .Setup(d => d.InvokeStringFieldDialog("Renaming", "New name:", "file1.txt", It.IsAny<Func<string, Task>>()))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var table = target.FindComponent<DynamicTable<ContentItem>>();
            target.WaitForAssertion(() => table.Instance.Items.Should().NotBeNull());
            var file = table.Instance.Items!.Single(item => !item.IsFolder);
            var longPressArgs = new LongPressEventArgs
            {
                ClientX = 1,
                ClientY = 2,
                OffsetX = 1,
                OffsetY = 2,
                PageX = 3,
                PageY = 4,
                ScreenX = 5,
                ScreenY = 6,
                Type = "contextmenu",
            };

            var args = new TableDataLongPressEventArgs<ContentItem>(longPressArgs, new MudTd(), file);
            await target.InvokeAsync(() => table.Instance.OnTableDataLongPress.InvokeAsync(args));

            var contextRename = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("ContextMenuRename")}\"]");
            await _popoverProvider!.InvokeAsync(async () => await contextRename.ClickAsync());

            target.WaitForAssertion(() =>
            {
                DialogWorkflowMock.Verify(d => d.InvokeStringFieldDialog("Renaming", "New name:", "file1.txt", It.IsAny<Func<string, Task>>()), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_CancelledTickToken_WHEN_RefreshRuns_THEN_StopIsReturned()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();
            var handler = GetTickHandler(target);
            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.Cancel();

            ManagedTimerTickResult result = ManagedTimerTickResult.Continue;
            await target.InvokeAsync(async () =>
            {
                result = await handler(cancellationSource.Token);
            });

            result.Should().Be(ManagedTimerTickResult.Stop);
        }

        [Fact]
        public async Task GIVEN_RootFile_WHEN_SearchExcludesFile_THEN_FileRowIsHidden()
        {
            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("single.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() => HasElementByTestId(target, "Priority-single.txt").Should().BeTrue());

            var search = FindComponentByTestId<MudTextField<string>>(target, "FilesTabSearch");
            search.Find("input").Input("not-matching");

            target.WaitForAssertion(() => HasElementByTestId(target, "Priority-single.txt").Should().BeFalse());
        }

        [Fact]
        public async Task GIVEN_ExpandedFolderWithoutChildren_WHEN_Rendered_THEN_NoDescendantRowsShown()
        {
            var rootFolder = new ContentItem("root", "root", -1, UiPriority.Normal, 0, 0, 0, true, 0);
            var fileList = CreateContentMap(rootFolder);
            var dataManagerMock = new Mock<ITorrentDataManager>();
            dataManagerMock.Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>())).Returns(fileList);
            dataManagerMock.Setup(m => m.MergeContentsList(It.IsAny<IReadOnlyList<FileData>>(), It.IsAny<Dictionary<string, ContentItem>>())).Returns(false);
            TestContext.Services.RemoveAll<ITorrentDataManager>();
            TestContext.Services.AddSingleton(dataManagerMock.Object);

            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("root/file.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                HasElementByTestId(target, "Priority-root").Should().BeTrue();
                HasElementByTestId(target, "Priority-root_file.txt").Should().BeFalse();
            });
        }

        [Fact]
        public async Task GIVEN_NestedEmptyFolder_WHEN_RootExpanded_THEN_EmptyFolderIsHidden()
        {
            var rootFolder = new ContentItem("root", "root", -1, UiPriority.Normal, 0, 0, 0, true, 0);
            var childFolder = new ContentItem("root/child", "child", -2, UiPriority.Normal, 0, 0, 0, true, 1);
            var fileList = CreateContentMap(rootFolder, childFolder);
            var dataManagerMock = new Mock<ITorrentDataManager>();
            dataManagerMock.Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>())).Returns(fileList);
            dataManagerMock.Setup(m => m.MergeContentsList(It.IsAny<IReadOnlyList<FileData>>(), It.IsAny<Dictionary<string, ContentItem>>())).Returns(false);
            TestContext.Services.RemoveAll<ITorrentDataManager>();
            TestContext.Services.AddSingleton(dataManagerMock.Object);

            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("root/placeholder.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var rootToggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => rootToggle.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                HasElementByTestId(target, "Priority-root_child").Should().BeFalse();
            });
        }

        [Fact]
        public async Task GIVEN_NestedFolderWithFile_WHEN_ChildExpanded_THEN_FileBecomesVisible()
        {
            var rootFolder = new ContentItem("root", "root", -1, UiPriority.Normal, 0, 0, 0, true, 0);
            var childFolder = new ContentItem("root/child", "child", -2, UiPriority.Normal, 0, 0, 0, true, 1);
            var file = new ContentItem("root/child/file.txt", "file.txt", 1, UiPriority.Normal, 0.5f, 100, 0.9f, false, 2);
            var fileList = CreateContentMap(rootFolder, childFolder, file);
            var dataManagerMock = new Mock<ITorrentDataManager>();
            dataManagerMock.Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>())).Returns(fileList);
            dataManagerMock.Setup(m => m.MergeContentsList(It.IsAny<IReadOnlyList<FileData>>(), It.IsAny<Dictionary<string, ContentItem>>())).Returns(false);
            TestContext.Services.RemoveAll<ITorrentDataManager>();
            TestContext.Services.AddSingleton(dataManagerMock.Object);

            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("root/child/file.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var rootToggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => rootToggle.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                HasElementByTestId(target, "Priority-root_child").Should().BeTrue();
                HasElementByTestId(target, "Priority-root_child_file.txt").Should().BeFalse();
            });

            var childToggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root_child");
            await target.InvokeAsync(() => childToggle.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                HasElementByTestId(target, "Priority-root_child_file.txt").Should().BeTrue();
            });
        }

        [Fact]
        public async Task GIVEN_SelectedAndContextItemsRemovedOnRefresh_WHEN_RenameToolbarClicked_THEN_MultiRenameDialogIsUsed()
        {
            var file1 = new ContentItem("file1.txt", "file1.txt", 1, UiPriority.Normal, 0.5f, 100, 0.9f, false, 0);
            var file2 = new ContentItem("file2.txt", "file2.txt", 2, UiPriority.Normal, 0.5f, 100, 0.9f, false, 0);
            var fileList = CreateContentMap(file1);
            var dataManagerMock = new Mock<ITorrentDataManager>();
            dataManagerMock.Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>())).Returns(fileList);
            dataManagerMock
                .Setup(m => m.MergeContentsList(It.IsAny<IReadOnlyList<FileData>>(), It.IsAny<Dictionary<string, ContentItem>>()))
                .Callback<IReadOnlyList<FileData>, Dictionary<string, ContentItem>>((_, current) =>
                {
                    current.Clear();
                    current[file2.Name] = file2;
                })
                .Returns(true);
            TestContext.Services.RemoveAll<ITorrentDataManager>();
            TestContext.Services.AddSingleton(dataManagerMock.Object);

            ApiClientMock
                .SetupSequence(c => c.GetTorrentContents("Hash"))
                .ReturnsAsync(CreateFiles("file1.txt"))
                .ReturnsAsync(CreateFiles("file2.txt"));
            DialogWorkflowMock.Setup(d => d.InvokeRenameFilesDialog("Hash")).Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            target.WaitForAssertion(() => HasElementByTestId(target, "Priority-file1.txt").Should().BeTrue());

            var table = target.FindComponent<DynamicTable<ContentItem>>();
            var selected = table.Instance.Items!.Single(item => item.Name == "file1.txt");
            await target.InvokeAsync(() => table.Instance.SelectedItemChanged.InvokeAsync(selected));

            var contextArgs = new TableDataContextMenuEventArgs<ContentItem>(new MouseEventArgs(), new MudTd(), selected);
            await target.InvokeAsync(() => table.Instance.OnTableDataContextMenu.InvokeAsync(contextArgs));

            await TriggerTimerTickAsync(target);

            var toolbarRename = FindComponentByTestId<MudIconButton>(target, "RenameToolbar");
            await target.InvokeAsync(() => toolbarRename.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                DialogWorkflowMock.Verify(d => d.InvokeRenameFilesDialog("Hash"), Times.Once);
                DialogWorkflowMock.Verify(d => d.InvokeStringFieldDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, Task>>()), Times.Never);
            });
        }

        [Fact]
        public async Task GIVEN_NoFileList_WHEN_DoNotDownloadLessThan100Clicked_THEN_NoPriorityChangeRequested()
        {
            _popoverProvider = TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<FilesTab>(parameters =>
            {
                parameters.AddCascadingValue("RefreshInterval", 10);
                parameters.Add(p => p.Active, false);
                parameters.Add(p => p.Hash, "Hash");
            });

            var menu = FindComponentByTestId<MudMenu>(target, "DoNotDownloadMenu");
            var menuActivator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(() => menuActivator.Find("button").Click());

            var availabilityItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("DoNotDownloadLessThan100")}\"]");
            await _popoverProvider!.InvokeAsync(() => availabilityItem.Click());

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(client => client.SetFilePriority(It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), It.IsAny<ClientPriority>()), Times.Never);
            });
        }

        [Fact]
        public async Task GIVEN_FolderOnlyList_WHEN_DoNotDownloadFilteredClicked_THEN_NoPriorityChangeRequested()
        {
            var rootFolder = new ContentItem("root", "root", -1, UiPriority.Normal, 0, 0, 0, true, 0);
            rootFolder.Equals(null).Should().BeFalse();
            var fileList = CreateContentMap(rootFolder!);
            var dataManagerMock = new Mock<ITorrentDataManager>();
            dataManagerMock.Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>())).Returns(fileList);
            dataManagerMock.Setup(m => m.MergeContentsList(It.IsAny<IReadOnlyList<FileData>>(), It.IsAny<Dictionary<string, ContentItem>>())).Returns(false);
            TestContext.Services.RemoveAll<ITorrentDataManager>();
            TestContext.Services.AddSingleton(dataManagerMock.Object);

            ApiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("root/placeholder.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var menu = FindComponentByTestId<MudMenu>(target, "DoNotDownloadMenu");
            var menuActivator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(() => menuActivator.Find("button").Click());

            var filteredItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("DoNotDownloadFiltered")}\"]");
            await _popoverProvider!.InvokeAsync(() => filteredItem.Click());

            target.WaitForAssertion(() =>
            {
                ApiClientMock.Verify(client => client.SetFilePriority(It.IsAny<string>(), It.IsAny<IEnumerable<int>>(), It.IsAny<ClientPriority>()), Times.Never);
            });
        }

        private IRenderedComponent<FilesTab> RenderFilesTab()
        {
            _popoverProvider = TestContext.Render<MudPopoverProvider>();

            return TestContext.Render<FilesTab>(parameters =>
            {
                parameters.AddCascadingValue("RefreshInterval", 10);
                parameters.Add(p => p.Active, true);
                parameters.Add(p => p.Hash, "Hash");
            });
        }

        private static IReadOnlyList<FileData> CreateFiles(params string[] names)
        {
            return names.Select((name, index) => new FileData(index + 1, name, 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.9f)).ToList();
        }

        private static Dictionary<string, ContentItem> CreateContentMap(params ContentItem[] items)
        {
            return items.ToDictionary(item => item.Name, item => item);
        }

        private async Task TriggerTimerTickAsync(IRenderedComponent<FilesTab> target)
        {
            var handler = GetTickHandler(target);
            await target.InvokeAsync(() => handler(CancellationToken.None));
        }

        private Func<CancellationToken, Task<ManagedTimerTickResult>> GetTickHandler(IRenderedComponent<FilesTab> target)
        {
            target.WaitForAssertion(() =>
            {
                Mock.Get(_timer).Verify(
                    timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            _tickHandler.Should().NotBeNull();
            return _tickHandler!;
        }

        private static IRenderedComponent<TComponent> FindComponentByTestId<TComponent>(IRenderedComponent<FilesTab> target, string testId) where TComponent : IComponent
        {
            return target.FindComponents<TComponent>().First(component => component.Markup.Contains($"data-test-id=\"{testId}\"", StringComparison.Ordinal));
        }

        private static bool HasElementByTestId(IRenderedComponent<FilesTab> target, string testId)
        {
            return target.FindAll($"[data-test-id=\"{testId}\"]").Count > 0;
        }
    }
}
