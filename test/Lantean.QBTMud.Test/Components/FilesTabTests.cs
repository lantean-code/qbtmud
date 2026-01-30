using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components;
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
        private readonly Mock<IApiClient> _apiClientMock;
        private readonly IManagedTimer _timer;
        private readonly IManagedTimerFactory _timerFactory;
        private readonly Mock<IDialogWorkflow> _dialogWorkflowMock;
        private IRenderedComponent<MudPopoverProvider>? _popoverProvider;

        public FilesTabTests()
        {
            _apiClientMock = TestContext.UseApiClientMock(MockBehavior.Strict);
            _dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>();
            TestContext.UseSnackbarMock(MockBehavior.Loose);

            _timer = Mock.Of<IManagedTimer>();
            _timerFactory = Mock.Of<IManagedTimerFactory>();
            Mock.Get(_timerFactory)
                .Setup(factory => factory.Create(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(_timer);
            Mock.Get(_timer)
                .Setup(timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            TestContext.Services.RemoveAll(typeof(IManagedTimerFactory));
            TestContext.Services.AddSingleton(_timerFactory);
        }

        [Fact]
        public async Task GIVEN_TimerTick_WHEN_ContentFetched_THEN_FolderExpandsToShowFiles()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("Folder/file1.txt", "Folder/file2.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() => target.Markup.Should().Contain("Folder"));

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-Folder");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            target.WaitForAssertion(() => target.Markup.Should().Contain("file1.txt"));
        }

        [Fact]
        public async Task GIVEN_FileRow_WHEN_PriorityChanged_THEN_ApiCalledWithIndexes()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("Root/file1.txt"));
            _apiClientMock.Setup(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.Single() == 1), ClientPriority.High)).Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-Root");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            target.WaitForAssertion(() => target.Markup.Should().Contain("file1.txt"));

            var prioritySelect = FindComponentByTestId<MudSelect<UiPriority>>(target, "Priority-Root_file1.txt");
            await target.InvokeAsync(() => prioritySelect.Instance.ValueChanged.InvokeAsync(UiPriority.High));

            _apiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.Single() == 1), ClientPriority.High), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Files_WHEN_DoNotDownloadAvailabilityInvoked_THEN_LowAvailabilityFilesUpdated()
        {
            var files = new[]
            {
                new FileData(1, "root/low.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.5f),
                new FileData(2, "root/high.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.9f),
            };
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            _apiClientMock
                .Setup(c => c.SetFilePriority("Hash", It.IsAny<IEnumerable<int>>(), ClientPriority.DoNotDownload))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() => target.Markup.Should().Contain("root"));

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(async () => await toggle.Find("button").ClickAsync());
            target.WaitForAssertion(() => target.Markup.Should().Contain("low.txt"));

            var menu = FindComponentByTestId<MudMenu>(target, "DoNotDownloadMenu");
            var menuActivator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(async () => await menuActivator.Find("button").ClickAsync());

            var availabilityItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("DoNotDownloadLessThan80")}\"]");
            await _popoverProvider!.InvokeAsync(() => availabilityItem.Click());

            target.WaitForAssertion(() =>
            {
                var setPriorityInvocation = _apiClientMock.Invocations.SingleOrDefault(invocation => invocation.Method.Name == nameof(IApiClient.SetFilePriority));
                setPriorityInvocation.Should().NotBeNull();
                var indexes = (IEnumerable<int>)setPriorityInvocation!.Arguments[1];
                indexes.Should().Equal(new[] { 1 });
                setPriorityInvocation.Arguments[2].Should().Be(ClientPriority.DoNotDownload);
            });
        }

        [Fact]
        public async Task GIVEN_SearchText_WHEN_Filtered_THEN_OnlyMatchingFilesRemain()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt", "folder/file2.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-folder");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            var search = FindComponentByTestId<MudTextField<string>>(target, "FilesTabSearch");
            search.Find("input").Input("file2");

            target.WaitForAssertion(() =>
            {
                target.Markup.Should().Contain("file2.txt");
                target.Markup.Should().NotContain("file1.txt");
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
                _apiClientMock.Invocations.Should().BeEmpty();
            });
        }

        [Fact]
        public async Task GIVEN_NoSelection_WHEN_RenameToolbarClicked_THEN_MultiRenameDialogInvoked()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("root/file1.txt"));
            _dialogWorkflowMock.Setup(d => d.InvokeRenameFilesDialog("Hash")).Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var renameButton = FindComponentByTestId<MudIconButton>(target, "RenameToolbar");
            await target.InvokeAsync(() => renameButton.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                _dialogWorkflowMock.Verify(d => d.InvokeRenameFilesDialog("Hash"), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_FileSelected_WHEN_RenameToolbarClicked_THEN_StringDialogShown()
        {
            var files = CreateFiles("root/file1.txt");
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            _dialogWorkflowMock
                .Setup(d => d.InvokeStringFieldDialog("Rename", "New name", "file1.txt", It.IsAny<Func<string, Task>>()))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            target.WaitForAssertion(() => target.Markup.Should().Contain("file1.txt"));

            var row = target.WaitForElement($"[data-test-id=\"{TestIdHelper.For("Row-Files-root_file1.txt")}\"]");
            await target.InvokeAsync(() => row.Click());

            var toolbarRename = FindComponentByTestId<MudIconButton>(target, "RenameToolbar");
            await target.InvokeAsync(() => toolbarRename.Find("button").Click());

            target.WaitForAssertion(() =>
            {
                _dialogWorkflowMock.Verify(d => d.InvokeStringFieldDialog("Rename", "New name", "file1.txt", It.IsAny<Func<string, Task>>()), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_FileContextMenu_WHEN_RenameClicked_THEN_StringDialogShown()
        {
            var files = CreateFiles("root/file1.txt");
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            _dialogWorkflowMock
                .Setup(d => d.InvokeStringFieldDialog("Rename", "New name", "file1.txt", It.IsAny<Func<string, Task>>()))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            target.WaitForAssertion(() => target.Markup.Should().Contain("file1.txt"));

            var row = target.WaitForElement($"[data-test-id=\"{TestIdHelper.For("Row-Files-root_file1.txt")}\"]");
            row.TriggerEvent("oncontextmenu", new MouseEventArgs());

            var contextRename = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("ContextMenuRename")}\"]");
            await _popoverProvider!.InvokeAsync(() => contextRename.Click());

            target.WaitForAssertion(() =>
            {
                _dialogWorkflowMock.Verify(d => d.InvokeStringFieldDialog("Rename", "New name", "file1.txt", It.IsAny<Func<string, Task>>()), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_TimerTick_WHEN_FileListExists_THEN_MergeApplied()
        {
            var initial = CreateFiles("root/file1.txt");
            var updated = CreateFiles("root/file1.txt", "root/file2.txt");
            _apiClientMock
                .SetupSequence(c => c.GetTorrentContents("Hash"))
                .ReturnsAsync(initial)
                .ReturnsAsync(updated);

            var dataManagerMock = new Mock<ITorrentDataManager>();
            TestContext.Services.RemoveAll(typeof(ITorrentDataManager));
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
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);

            var dataManagerMock = new Mock<ITorrentDataManager>();
            TestContext.Services.RemoveAll(typeof(ITorrentDataManager));
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
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("root/file1.txt"));

            var target = TestContext.Render<FilesTab>(parameters =>
            {
                parameters.AddCascadingValue("RefreshInterval", 10);
                parameters.Add(p => p.Active, false);
                parameters.Add(p => p.Hash, "Hash");
            });

            await TriggerTimerTickAsync(target);

            _apiClientMock.Verify(c => c.GetTorrentContents(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_TimerTick_WHEN_ApiReturnsForbidden_THEN_RefreshStops()
        {
            _apiClientMock
                .SetupSequence(c => c.GetTorrentContents("Hash"))
                .ReturnsAsync(CreateFiles("root/file1.txt"))
                .ThrowsAsync(new HttpRequestException(null, null, HttpStatusCode.Forbidden));

            var target = RenderFilesTab();

            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() =>
            {
                _apiClientMock.Verify(c => c.GetTorrentContents("Hash"), Times.Exactly(2));
            });
        }

        [Fact]
        public async Task GIVEN_ExpandedFolder_WHEN_ToggledAgain_THEN_FolderCollapses()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-folder");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            target.WaitForAssertion(() => target.Markup.Should().Contain("file1.txt"));

            await target.InvokeAsync(() => toggle.Find("button").Click());

            target.WaitForAssertion(() => target.Markup.Should().NotContain("file1.txt"));
        }

        [Fact]
        public async Task GIVEN_TimerStops_WHEN_NoFurtherTicks_THEN_NoAdditionalWork()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("root/file1.txt"));

            var target = RenderFilesTab();

            await TriggerTimerTickAsync(target);
            target.WaitForAssertion(() =>
            {
                _apiClientMock.Invocations.Count(invocation => invocation.Method.Name == nameof(IApiClient.GetTorrentContents)).Should().BeGreaterThanOrEqualTo(1);
            });
            _apiClientMock.Invocations.Clear();

            target.Render();

            target.WaitForAssertion(() =>
            {
                _apiClientMock.Invocations.Count(invocation => invocation.Method.Name == nameof(IApiClient.GetTorrentContents)).Should().Be(0);
            });
        }

        [Fact]
        public async Task GIVEN_NoUpdatesFromTimer_WHEN_RenderedTwice_THEN_SecondRenderSkipsLoop()
        {
            var dataManagerMock = new Mock<ITorrentDataManager>();
            dataManagerMock.Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>())).Returns(new Dictionary<string, ContentItem>());
            dataManagerMock.Setup(m => m.MergeContentsList(It.IsAny<IReadOnlyList<FileData>>(), It.IsAny<Dictionary<string, ContentItem>>())).Returns(false);
            TestContext.Services.RemoveAll(typeof(ITorrentDataManager));
            TestContext.Services.AddSingleton(dataManagerMock.Object);

            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("root/file1.txt"));

            var target = TestContext.Render<FilesTab>(parameters =>
            {
                parameters.AddCascadingValue("RefreshInterval", 10);
                parameters.Add(p => p.Active, true);
                parameters.Add(p => p.Hash, "Hash");
            });

            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() =>
            {
                _apiClientMock.Invocations.Count(i => i.Method.Name == nameof(IApiClient.GetTorrentContents)).Should().BeGreaterThanOrEqualTo(1);
            });

            _apiClientMock.Invocations.Clear();

            target.Render();

            _apiClientMock.Invocations.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_SubsequentRender_WHEN_FirstRenderComplete_THEN_NoAdditionalInitialization()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();

            target.Render();

            _apiClientMock.Verify(c => c.GetTorrentContents("Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_FilterDialogCancelled_WHEN_ShowFilterInvoked_THEN_FiltersCleared()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));
            _dialogWorkflowMock.Setup(d => d.ShowFilterOptionsDialog(It.IsAny<List<PropertyFilterDefinition<ContentItem>>?>())).ReturnsAsync((List<PropertyFilterDefinition<ContentItem>>?)null);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var filterButton = FindComponentByTestId<MudIconButton>(target, "ShowFilterDialog");
            await target.InvokeAsync(() => filterButton.Find("button").Click());

            target.WaitForAssertion(() => target.Instance.Filters.Should().BeNull());
        }

        [Fact]
        public async Task GIVEN_FilterDialogWithDefinition_WHEN_Applied_THEN_FiltersStoredAndRendered()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt", "folder/file2.txt"));
            _dialogWorkflowMock
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
                target.Markup.Should().Contain("file2.txt");
                target.Markup.Should().NotContain("file1.txt");
                target.Instance.Filters.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task GIVEN_FilterApplied_WHEN_RemoveFilterClicked_THEN_AllFilesVisible()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt", "folder/file2.txt"));
            _dialogWorkflowMock
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
                target.Markup.Should().Contain("file1.txt");
                target.Markup.Should().Contain("file2.txt");
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

            _apiClientMock.Verify(c => c.GetTorrentContents(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ExpandedNodesInStorage_WHEN_Rendered_THEN_NodesRestored()
        {
            await TestContext.SessionStorage.SetItemAsync("FilesTab.ExpandedNodes.Hash", new HashSet<string>(new[] { "folder" }));
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();

            target.WaitForAssertion(() =>
            {
                target.Markup.Should().Contain("file1.txt");
            });
        }

        [Fact]
        public async Task GIVEN_RefreshActive_WHEN_SameHashProvided_THEN_LoadNotRepeated()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();

            target.Render();

            _apiClientMock.Verify(c => c.GetTorrentContents("Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_Folder_WHEN_PriorityChanged_THEN_AllDescendantsUpdated()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("Folder/file1.txt", "Folder/file2.txt"));
            _apiClientMock
                .Setup(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1, 2 })), ClientPriority.Maximum))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var folderPriority = FindComponentByTestId<MudSelect<UiPriority>>(target, "Priority-Folder");
            await target.InvokeAsync(() => folderPriority.Instance.ValueChanged.InvokeAsync(UiPriority.Maximum));

            _apiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1, 2 })), ClientPriority.Maximum), Times.Once);
        }

        [Fact]
        public async Task GIVEN_MenuAction_WHEN_DoNotDownloadLessThan100Invoked_THEN_AllFilesUpdated()
        {
            var files = new[]
            {
                new FileData(1, "root/low.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.5f),
                new FileData(2, "root/high.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.9f),
            };
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            _apiClientMock
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
                _apiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1, 2 })), ClientPriority.DoNotDownload), Times.Once);
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
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            _apiClientMock
                .Setup(c => c.SetFilePriority("Hash", It.IsAny<IEnumerable<int>>(), ClientPriority.Normal))
                .Returns(Task.CompletedTask);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            var toggle = FindComponentByTestId<MudIconButton>(target, "FolderToggle-root");
            await target.InvokeAsync(() => toggle.Find("button").Click());

            var menu = FindComponentByTestId<MudMenu>(target, "NormalPriorityMenu");
            var menuActivator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(() => menuActivator.Find("button").Click());

            var availabilityItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("NormalPriorityLessThan80")}\"]");
            await _popoverProvider!.InvokeAsync(() => availabilityItem.Click());

            target.WaitForAssertion(() =>
            {
                _apiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1 })), ClientPriority.Normal), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_FilteredFiles_WHEN_DoNotDownloadFilteredInvoked_THEN_VisibleFilesUpdated()
        {
            var files = new[]
            {
                new FileData(1, "root/only.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 0.5f),
            };
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            _apiClientMock
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
                _apiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1 })), ClientPriority.DoNotDownload), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_FilteredFiles_WHEN_NormalPriorityFilteredInvoked_THEN_VisibleFilesUpdated()
        {
            var files = new[]
            {
                new FileData(1, "root/only.txt", 100, 0.5f, ClientPriority.DoNotDownload, false, new[] { 0 }, 0.5f),
            };
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            _apiClientMock
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
                _apiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1 })), ClientPriority.Normal), Times.Once);
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
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);
            _apiClientMock
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
                _apiClientMock.Verify(c => c.SetFilePriority("Hash", It.Is<IEnumerable<int>>(i => i.SequenceEqual(new[] { 1, 2 })), ClientPriority.Normal), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_MenuAction_WHEN_NormalPriorityLessThan100WithNoMatches_THEN_NoApiCall()
        {
            var files = new[]
            {
                new FileData(1, "root/high.txt", 100, 0.5f, ClientPriority.Normal, false, new[] { 0 }, 1.0f),
            };
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(files);

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);
            _apiClientMock.Invocations.Clear();

            var menu = FindComponentByTestId<MudMenu>(target, "NormalPriorityMenu");
            var menuActivator = menu.FindComponent<MudIconButton>();
            await target.InvokeAsync(() => menuActivator.Find("button").Click());

            var availabilityItem = _popoverProvider!.WaitForElement($"[data-test-id=\"{TestIdHelper.For("NormalPriorityLessThan100")}\"]");
            await _popoverProvider!.InvokeAsync(() => availabilityItem.Click());

            target.WaitForAssertion(() =>
            {
                _apiClientMock.Invocations.Count(invocation => invocation.Method.Name == nameof(IApiClient.SetFilePriority)).Should().Be(0);
            });
        }

        [Fact]
        public async Task GIVEN_FileListWithoutRoots_WHEN_Refreshed_THEN_NoVisibleItems()
        {
            var dataManagerMock = new Mock<ITorrentDataManager>();
            var content = new ContentItem("folder/file1.txt", "file1.txt", 1, UiPriority.Normal, 0.5f, 100, 1.0f, false, 1);
            var fileList = new Dictionary<string, ContentItem> { { content.Name, content } };
            dataManagerMock.Setup(m => m.CreateContentsList(It.IsAny<IReadOnlyList<FileData>>())).Returns(fileList);

            TestContext.Services.RemoveAll(typeof(ITorrentDataManager));
            TestContext.Services.AddSingleton(dataManagerMock.Object);

            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            target.Markup.Should().NotContain("file1.txt");
        }

        [Fact]
        public async Task GIVEN_ActiveFalse_WHEN_ParametersSet_THEN_ApiNotInvoked()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            TestContext.Render<FilesTab>(parameters =>
            {
                parameters.AddCascadingValue("RefreshInterval", 10);
                parameters.Add(p => p.Active, false);
                parameters.Add(p => p.Hash, "Hash");
            });

            _apiClientMock.Verify(c => c.GetTorrentContents(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_SameHash_WHEN_ParametersUpdated_THEN_InitialLoadNotRepeated()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();

            target.Render(parameters =>
            {
                parameters.Add(p => p.Active, true);
                parameters.Add(p => p.Hash, "Hash");
            });

            _apiClientMock.Verify(c => c.GetTorrentContents("Hash"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_HttpForbidden_WHEN_Refreshed_THEN_CancellationRequested()
        {
            _apiClientMock
                .SetupSequence(c => c.GetTorrentContents("Hash"))
                .ReturnsAsync(CreateFiles("folder/file1.txt"))
                .ThrowsAsync(new HttpRequestException(null, null, HttpStatusCode.Forbidden));

            var target = RenderFilesTab();
            await TriggerTimerTickAsync(target);

            target.WaitForAssertion(() =>
            {
                _apiClientMock.Verify(c => c.GetTorrentContents("Hash"), Times.Exactly(2));
            });
        }

        [Fact]
        public async Task GIVEN_TableRendered_WHEN_ColumnOptionsClicked_THEN_NoErrors()
        {
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));
            _dialogWorkflowMock
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
            _apiClientMock.Setup(c => c.GetTorrentContents("Hash")).ReturnsAsync(CreateFiles("folder/file1.txt"));

            var target = RenderFilesTab();
            await target.Instance.DisposeAsync();
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

            var invocation = Mock.Get(_timer).Invocations.Single(invocation => invocation.Method.Name == nameof(IManagedTimer.StartAsync));
            return (Func<CancellationToken, Task<ManagedTimerTickResult>>)invocation.Arguments[0];
        }

        private static IRenderedComponent<TComponent> FindComponentByTestId<TComponent>(IRenderedComponent<FilesTab> target, string testId) where TComponent : IComponent
        {
            return target.FindComponents<TComponent>().First(component => component.Markup.Contains($"data-test-id=\"{testId}\"", StringComparison.Ordinal));
        }
    }
}
