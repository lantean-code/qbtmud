using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Net;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class TrackersTabTests : RazorComponentTestBase
    {
        private readonly IApiClient _apiClient = Mock.Of<IApiClient>();
        private readonly Mock<IDialogWorkflow> _dialogWorkflowMock;
        private readonly IManagedTimer _timer;
        private readonly IManagedTimerFactory _timerFactory;
        private readonly IRenderedComponent<MudPopoverProvider> _popoverProvider;
        private Func<CancellationToken, Task<ManagedTimerTickResult>>? _tickHandler;

        public TrackersTabTests()
        {
            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(_apiClient);
            _dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);

            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers(It.IsAny<string>()))
                .ReturnsAsync(Array.Empty<TorrentTracker>());

            _timer = Mock.Of<IManagedTimer>();
            _timerFactory = Mock.Of<IManagedTimerFactory>();
            Mock.Get(_timerFactory)
                .Setup(factory => factory.Create(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(_timer);
            Mock.Get(_timer)
                .Setup(timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((handler, _) => _tickHandler = handler)
                .ReturnsAsync(true);
            Mock.Get(_timer)
                .Setup(timer => timer.DisposeAsync())
                .Returns(ValueTask.CompletedTask);

            TestContext.Services.RemoveAll<IManagedTimerFactory>();
            TestContext.Services.AddSingleton(_timerFactory);

            _popoverProvider = TestContext.Render<MudPopoverProvider>();
        }

        [Fact]
        public void GIVEN_NullHash_WHEN_ActiveTabIsRendered_THEN_TrackersAreNotLoaded()
        {
            RenderTrackersTab(active: true, hash: null);

            Mock.Get(_apiClient).Verify(client => client.GetTorrentTrackers(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_InactiveTab_WHEN_Rendered_THEN_TrackersAreNotLoaded()
        {
            RenderTrackersTab(active: false);

            Mock.Get(_apiClient).Verify(client => client.GetTorrentTrackers(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_ActiveTab_WHEN_Rendered_THEN_TrackersAreLoadedAndSorted()
        {
            var trackers = new[]
            {
                CreateTracker("udp://b.example", tier: 2),
                CreateTracker("** [DHT]", tier: -1),
                CreateTracker("udp://a.example", tier: 1),
            };
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(trackers);

            var target = RenderTrackersTab(active: true);

            target.WaitForAssertion(() =>
            {
                Mock.Get(_apiClient).Verify(client => client.GetTorrentTrackers("Hash"), Times.Once);
                GetTrackerUrls(target).Should().Equal("** [DHT]", "udp://a.example", "udp://b.example");
            });
        }

        [Fact]
        public async Task GIVEN_SortCallbacks_WHEN_ColumnAndDirectionChange_THEN_RealTrackersAreResorted()
        {
            var trackers = new[]
            {
                CreateTracker("udp://a.example", tier: 1),
                CreateTracker("udp://b.example", tier: 2),
                CreateTracker("** [DHT]", tier: -1),
            };
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(trackers);

            var target = RenderTrackersTab(active: true);
            var table = target.FindComponent<DynamicTable<TorrentTracker>>();

            await target.InvokeAsync(() => table.Instance.SortColumnChanged.InvokeAsync("tier"));
            await target.InvokeAsync(() => table.Instance.SortDirectionChanged.InvokeAsync(SortDirection.Descending));

            target.WaitForAssertion(() =>
            {
                GetTrackerUrls(target).Should().Equal("** [DHT]", "udp://b.example", "udp://a.example");
            });
        }

        [Fact]
        public void GIVEN_RenderedToolbar_WHEN_IconButtonsLoaded_THEN_ButtonsExposeExpectedTitles()
        {
            var target = RenderTrackersTab(active: false);

            var addButton = FindIconButton(target, Icons.Material.Filled.AddCircle);
            var editButton = FindIconButton(target, Icons.Material.Filled.Edit);
            var removeButton = FindIconButton(target, Icons.Material.Filled.Delete);
            var copyButton = FindIconButton(target, Icons.Material.Filled.FolderCopy);
            var columnsButton = FindIconButton(target, Icons.Material.Outlined.ViewColumn);

            GetUserAttributeValue(addButton, "title").Should().Be("Add trackers");
            GetUserAttributeValue(editButton, "title").Should().Be("Edit tracker URL...");
            GetUserAttributeValue(removeButton, "title").Should().Be("Remove tracker");
            GetUserAttributeValue(copyButton, "title").Should().Be("Copy tracker URL");
            GetUserAttributeValue(columnsButton, "title").Should().Be("Choose Columns");
        }

        [Fact]
        public async Task GIVEN_NullHash_WHEN_AddTrackerClicked_THEN_NoDialogOrApiCallsOccur()
        {
            var target = RenderTrackersTab(active: false, hash: null);
            var addButton = FindIconButton(target, Icons.Material.Filled.AddCircle);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            _dialogWorkflowMock.Verify(workflow => workflow.ShowAddTrackersDialog(), Times.Never);
            Mock.Get(_apiClient).Verify(client => client.AddTrackersToTorrent(It.IsAny<IEnumerable<string>>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NullHash_WHEN_CopyToolbarClicked_THEN_ClipboardInteropIsNotCalled()
        {
            var copyInvocation = TestContext.JSInterop.SetupVoid("qbt.copyTextToClipboard", _ => true);
            copyInvocation.SetVoidResult();

            var target = RenderTrackersTab(active: false, hash: null);
            var copyButton = FindIconButton(target, Icons.Material.Filled.FolderCopy);

            await target.InvokeAsync(() => copyButton.Instance.OnClick.InvokeAsync());

            copyInvocation.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_DialogReturnsNullTrackers_WHEN_AddTrackerClicked_THEN_ApiIsNotCalled()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowAddTrackersDialog())
                .ReturnsAsync((HashSet<string>?)null);

            var target = RenderTrackersTab(active: false);
            var addButton = FindIconButton(target, Icons.Material.Filled.AddCircle);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.AddTrackersToTorrent(It.IsAny<IEnumerable<string>>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DialogReturnsEmptyTrackers_WHEN_AddTrackerClicked_THEN_ApiIsNotCalled()
        {
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowAddTrackersDialog())
                .ReturnsAsync(new HashSet<string>());

            var target = RenderTrackersTab(active: false);
            var addButton = FindIconButton(target, Icons.Material.Filled.AddCircle);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.AddTrackersToTorrent(It.IsAny<IEnumerable<string>>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DialogReturnsTrackers_WHEN_AddTrackerClicked_THEN_ApiIsCalled()
        {
            var addedTrackers = new HashSet<string> { "udp://a.example", "udp://b.example" };
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowAddTrackersDialog())
                .ReturnsAsync(addedTrackers);
            Mock.Get(_apiClient)
                .Setup(client => client.AddTrackersToTorrent(It.IsAny<IEnumerable<string>>(), null, It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var target = RenderTrackersTab(active: false);
            var addButton = FindIconButton(target, Icons.Material.Filled.AddCircle);

            await target.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(
                client => client.AddTrackersToTorrent(
                    It.Is<IEnumerable<string>>(urls => urls.OrderBy(url => url).SequenceEqual(addedTrackers.OrderBy(url => url))),
                    null,
                    It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash" }))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NoSelectedTracker_WHEN_EditToolbarClicked_THEN_DialogIsNotOpened()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(new[] { CreateTracker("udp://a.example") });

            var target = RenderTrackersTab(active: true);
            var editButton = FindIconButton(target, Icons.Material.Filled.Edit);

            await target.InvokeAsync(() => editButton.Instance.OnClick.InvokeAsync());

            _dialogWorkflowMock.Verify(
                workflow => workflow.InvokeStringFieldDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<Func<string, Task>>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_SelectedTracker_WHEN_EditToolbarClicked_THEN_TrackerIsEdited()
        {
            var tracker = CreateTracker("udp://a.example");
            var updatedTrackerUrl = "udp://edited.example";
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(new[] { tracker });
            Mock.Get(_apiClient)
                .Setup(client => client.EditTracker("Hash", tracker.Url, updatedTrackerUrl, null))
                .Returns(Task.CompletedTask);
            _dialogWorkflowMock
                .Setup(workflow => workflow.InvokeStringFieldDialog("Tracker editing", "Tracker URL:", tracker.Url, It.IsAny<Func<string, Task>>()))
                .Returns<string, string, string?, Func<string, Task>>(async (_, _, _, onSuccess) => await onSuccess(updatedTrackerUrl));

            var target = RenderTrackersTab(active: true);
            var table = target.FindComponent<DynamicTable<TorrentTracker>>();
            await target.InvokeAsync(() => table.Instance.SelectedItemChanged.InvokeAsync(tracker));

            var editButton = FindIconButton(target, Icons.Material.Filled.Edit);
            await target.InvokeAsync(() => editButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.EditTracker("Hash", tracker.Url, updatedTrackerUrl, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_NoSelectedTracker_WHEN_RemoveToolbarClicked_THEN_ApiIsNotCalled()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(new[] { CreateTracker("udp://a.example") });

            var target = RenderTrackersTab(active: true);
            var removeButton = FindIconButton(target, Icons.Material.Filled.Delete);

            await target.InvokeAsync(() => removeButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.RemoveTrackers(It.IsAny<IEnumerable<string>>(), It.IsAny<bool?>(), It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_SelectedTracker_WHEN_RemoveToolbarClicked_THEN_ApiIsCalled()
        {
            var tracker = CreateTracker("udp://a.example");
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(new[] { tracker });
            Mock.Get(_apiClient)
                .Setup(client => client.RemoveTrackers(It.IsAny<IEnumerable<string>>(), null, It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var target = RenderTrackersTab(active: true);
            var table = target.FindComponent<DynamicTable<TorrentTracker>>();
            await target.InvokeAsync(() => table.Instance.SelectedItemChanged.InvokeAsync(tracker));

            var removeButton = FindIconButton(target, Icons.Material.Filled.Delete);
            await target.InvokeAsync(() => removeButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(
                client => client.RemoveTrackers(
                    It.Is<IEnumerable<string>>(urls => urls.SequenceEqual(new[] { tracker.Url })),
                    null,
                    It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash" }))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NoSelectedTracker_WHEN_CopyToolbarClicked_THEN_ClipboardInteropIsNotCalled()
        {
            var copyInvocation = TestContext.JSInterop.SetupVoid("qbt.copyTextToClipboard", _ => true);
            copyInvocation.SetVoidResult();

            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(new[] { CreateTracker("udp://a.example") });

            var target = RenderTrackersTab(active: true);
            var copyButton = FindIconButton(target, Icons.Material.Filled.FolderCopy);

            await target.InvokeAsync(() => copyButton.Instance.OnClick.InvokeAsync());

            copyInvocation.Invocations.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ContextMenuOnSyntheticTracker_WHEN_Opened_THEN_OnlyAddMenuItemIsShown()
        {
            var syntheticTracker = CreateTracker("** [DHT]", tier: -1);
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(new[] { syntheticTracker, CreateTracker("udp://a.example") });

            var target = RenderTrackersTab(active: true);
            await TriggerContextMenuAsync(target, syntheticTracker);
            await OpenMenuAsync(target);

            target.WaitForAssertion(() =>
            {
                var icons = _popoverProvider.FindComponents<MudMenuItem>().Select(item => item.Instance.Icon).ToList();
                icons.Should().ContainSingle().Which.Should().Be(Icons.Material.Filled.AddCircle);
            });
        }

        [Fact]
        public async Task GIVEN_ContextMenuOnRealTracker_WHEN_Opened_THEN_ContextActionsAreShown()
        {
            var tracker = CreateTracker("udp://a.example");
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(new[] { tracker });

            var target = RenderTrackersTab(active: true);
            await TriggerContextMenuAsync(target, tracker);
            await OpenMenuAsync(target);

            target.WaitForAssertion(() =>
            {
                var icons = _popoverProvider.FindComponents<MudMenuItem>().Select(item => item.Instance.Icon).ToList();
                icons.Should().Contain(Icons.Material.Filled.AddCircle);
                icons.Should().Contain(Icons.Material.Filled.Edit);
                icons.Should().Contain(Icons.Material.Filled.Delete);
                icons.Should().Contain(Icons.Material.Filled.FolderCopy);
            });
        }

        [Fact]
        public async Task GIVEN_ContextMenuTracker_WHEN_RemoveMenuClicked_THEN_TrackerIsRemoved()
        {
            var tracker = CreateTracker("udp://a.example");
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(new[] { tracker });
            Mock.Get(_apiClient)
                .Setup(client => client.RemoveTrackers(It.IsAny<IEnumerable<string>>(), null, It.IsAny<string[]>()))
                .Returns(Task.CompletedTask);

            var target = RenderTrackersTab(active: true);
            await TriggerLongPressAsync(target, tracker);
            await OpenMenuAsync(target);

            var removeItem = FindMenuItem(Icons.Material.Filled.Delete);
            await target.InvokeAsync(() => removeItem.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(
                client => client.RemoveTrackers(
                    It.Is<IEnumerable<string>>(urls => urls.SequenceEqual(new[] { tracker.Url })),
                    null,
                    It.Is<string[]>(hashes => hashes.SequenceEqual(new[] { "Hash" }))),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ContextMenuTracker_WHEN_CopyMenuClicked_THEN_TrackerUrlIsCopied()
        {
            var copyInvocation = TestContext.JSInterop.SetupVoid("qbt.copyTextToClipboard", _ => true);
            copyInvocation.SetVoidResult();

            var tracker = CreateTracker("udp://a.example");
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(new[] { tracker });

            var target = RenderTrackersTab(active: true);
            await TriggerContextMenuAsync(target, tracker);
            await OpenMenuAsync(target);

            var copyItem = FindMenuItem(Icons.Material.Filled.FolderCopy);
            await target.InvokeAsync(() => copyItem.Instance.OnClick.InvokeAsync());

            copyInvocation.Invocations.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_ColumnOptionsClicked_WHEN_TableExists_THEN_ColumnDialogIsShown()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(new[] { CreateTracker("udp://a.example") });
            _dialogWorkflowMock
                .Setup(workflow => workflow.ShowColumnsOptionsDialog<TorrentTracker>(
                    It.IsAny<List<ColumnDefinition<TorrentTracker>>>(),
                    It.IsAny<HashSet<string>>(),
                    It.IsAny<Dictionary<string, int?>>(),
                    It.IsAny<Dictionary<string, int>>()))
                .ReturnsAsync((new HashSet<string>(), new Dictionary<string, int?>(), new Dictionary<string, int>()));

            var target = RenderTrackersTab(active: true);
            var columnsButton = FindIconButton(target, Icons.Material.Outlined.ViewColumn);

            await target.InvokeAsync(() => columnsButton.Instance.OnClick.InvokeAsync());

            _dialogWorkflowMock.Verify(
                workflow => workflow.ShowColumnsOptionsDialog<TorrentTracker>(
                    It.IsAny<List<ColumnDefinition<TorrentTracker>>>(),
                    It.IsAny<HashSet<string>>(),
                    It.IsAny<Dictionary<string, int?>>(),
                    It.IsAny<Dictionary<string, int>>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTickSucceeds_THEN_ContinueIsReturned()
        {
            var initialTrackers = new[] { CreateTracker("udp://a.example") };
            var updatedTrackers = new[] { CreateTracker("udp://b.example") };
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(initialTrackers)
                .ReturnsAsync(updatedTrackers);

            var target = RenderTrackersTab(active: true);

            var result = await TriggerTimerTickAsync(target, global::Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(ManagedTimerTickResult.Continue);
            Mock.Get(_apiClient).Verify(client => client.GetTorrentTrackers("Hash"), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTickGetsForbidden_THEN_StopIsReturned()
        {
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(new[] { CreateTracker("udp://a.example") })
                .ThrowsAsync(new HttpRequestException("Forbidden", null, HttpStatusCode.Forbidden));

            var target = RenderTrackersTab(active: true);

            var result = await TriggerTimerTickAsync(target, global::Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(ManagedTimerTickResult.Stop);
        }

        [Fact]
        public async Task GIVEN_ActiveTab_WHEN_TimerTickGetsNotFound_THEN_StopIsReturned()
        {
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetTorrentTrackers("Hash"))
                .ReturnsAsync(new[] { CreateTracker("udp://a.example") })
                .ThrowsAsync(new HttpRequestException("Not Found", null, HttpStatusCode.NotFound));

            var target = RenderTrackersTab(active: true);

            var result = await TriggerTimerTickAsync(target, global::Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(ManagedTimerTickResult.Stop);
        }

        [Fact]
        public async Task GIVEN_CancelledToken_WHEN_TimerTickInvoked_THEN_StopIsReturned()
        {
            var target = RenderTrackersTab(active: false);
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var result = await TriggerTimerTickAsync(target, cancellationTokenSource.Token);

            result.Should().Be(ManagedTimerTickResult.Stop);
        }

        [Fact]
        public void GIVEN_RerenderAfterFirstRender_WHEN_TimerInitialized_THEN_StartHappensOnce()
        {
            var target = RenderTrackersTab(active: false);

            target.Render();

            Mock.Get(_timerFactory).Verify(
                factory => factory.Create("TrackersTabRefresh", TimeSpan.FromMilliseconds(10)),
                Times.Once);
            Mock.Get(_timer).Verify(
                timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_ComponentDisposedTwice_WHEN_DisposeInvoked_THEN_TimerDisposedOnlyOnce()
        {
            var target = RenderTrackersTab(active: false);

            await target.Instance.DisposeAsync();
            await target.Instance.DisposeAsync();

            Mock.Get(_timer).Verify(timer => timer.DisposeAsync(), Times.Once);
        }

        private IRenderedComponent<TrackersTab> RenderTrackersTab(bool active, string? hash = "Hash")
        {
            return TestContext.Render<TrackersTab>(parameters =>
            {
                parameters.Add(p => p.Active, active);
                parameters.Add(p => p.Hash, hash);
                parameters.AddCascadingValue("RefreshInterval", 10);
            });
        }

        private async Task<ManagedTimerTickResult> TriggerTimerTickAsync(IRenderedComponent<TrackersTab> target, CancellationToken cancellationToken = default)
        {
            var handler = GetTickHandler(target);
            return await target.InvokeAsync(() => handler(cancellationToken));
        }

        private Func<CancellationToken, Task<ManagedTimerTickResult>> GetTickHandler(IRenderedComponent<TrackersTab> target)
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

        private async Task TriggerContextMenuAsync(IRenderedComponent<TrackersTab> target, TorrentTracker? tracker)
        {
            var table = target.FindComponent<DynamicTable<TorrentTracker>>();
            var args = new TableDataContextMenuEventArgs<TorrentTracker>(new MouseEventArgs(), new MudTd(), tracker);
            await target.InvokeAsync(() => table.Instance.OnTableDataContextMenu.InvokeAsync(args));
        }

        private async Task TriggerLongPressAsync(IRenderedComponent<TrackersTab> target, TorrentTracker? tracker)
        {
            var table = target.FindComponent<DynamicTable<TorrentTracker>>();
            var args = new TableDataLongPressEventArgs<TorrentTracker>(new LongPressEventArgs(), new MudTd(), tracker);
            await target.InvokeAsync(() => table.Instance.OnTableDataLongPress.InvokeAsync(args));
        }

        private static IReadOnlyList<string> GetTrackerUrls(IRenderedComponent<TrackersTab> target)
        {
            var table = target.FindComponent<DynamicTable<TorrentTracker>>();
            return table.Instance.Items?.Select(tracker => tracker.Url).ToList() ?? new List<string>();
        }

        private async Task OpenMenuAsync(IRenderedComponent<TrackersTab> target)
        {
            var menu = target.FindComponent<MudMenu>();
            await target.InvokeAsync(() => menu.Instance.OpenMenuAsync(new MouseEventArgs()));
        }

        private IRenderedComponent<MudMenuItem> FindMenuItem(string icon)
        {
            return _popoverProvider.FindComponents<MudMenuItem>()
                .Single(item => item.Instance.Icon == icon);
        }

        private static TorrentTracker CreateTracker(string url, int tier = 1)
        {
            return new TorrentTracker(
                url,
                TrackerStatus.Working,
                tier,
                peers: 1,
                seeds: 2,
                leeches: 3,
                downloads: 4,
                message: "Message",
                nextAnnounce: null,
                minAnnounce: null,
                endpoints: null);
        }

        private static string? GetUserAttributeValue(IRenderedComponent<MudIconButton> component, string attribute)
        {
            if (component.Instance.UserAttributes.TryGetValue(attribute, out var value))
            {
                return value?.ToString();
            }

            return null;
        }
    }
}
