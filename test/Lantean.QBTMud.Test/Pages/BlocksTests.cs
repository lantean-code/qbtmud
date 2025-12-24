using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class BlocksTests : RazorComponentTestBase
    {
        private const string SelectedTypesStorageKey = "Blocks.SelectedTypes";
        private readonly IApiClient _apiClient;
        private readonly ISnackbar _snackbar;
        private readonly FakePeriodicTimer _timer;
        private readonly IRenderedComponent<BlocksHarness> _target;

        public BlocksTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _snackbar = Mock.Of<ISnackbar>();
            _timer = new FakePeriodicTimer();

            TestContext.Services.RemoveAll(typeof(IApiClient));
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.RemoveAll(typeof(ISnackbar));
            TestContext.Services.AddSingleton(_snackbar);
            TestContext.Services.RemoveAll(typeof(IPeriodicTimerFactory));
            TestContext.Services.AddSingleton<IPeriodicTimerFactory>(new FakePeriodicTimerFactory(_timer));

            TestContext.Render<MudPopoverProvider>();

            Mock.Get(_apiClient)
                .Setup(c => c.GetPeerLog(It.IsAny<int?>()))
                .ReturnsAsync(new List<PeerLog>());

            _target = TestContext.Render<BlocksHarness>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", false);
            });
        }

        [Fact]
        public void GIVEN_DefaultLoad_WHEN_Rendered_THEN_PeerLogRequested()
        {
            Mock.Get(_apiClient).Verify(c => c.GetPeerLog(It.IsAny<int?>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_BackNavigation_WHEN_Clicked_THEN_NavigatesHome()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            var backButton = _target.FindComponents<MudIconButton>()
                .Single(button => button.Instance.Icon == Icons.Material.Outlined.NavigateBefore);

            await _target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().EndWith("/");
        }

        [Fact]
        public async Task GIVEN_TimerTick_WHEN_ResultsReturned_THEN_TableUpdated()
        {
            var results = new List<PeerLog> { CreatePeerLog(1, "IPAddress", true) };
            Mock.Get(_apiClient)
                .Setup(c => c.GetPeerLog(It.IsAny<int?>()))
                .ReturnsAsync(results);

            await _timer.TriggerTickAsync();

            var table = _target.FindComponent<DynamicTable<PeerLog>>();
            table.WaitForAssertion(() =>
            {
                var items = table.Instance.Items.Should().NotBeNull().And.Subject;
                items.Count().Should().Be(1);
            });
        }

        [Fact]
        public async Task GIVEN_ContextMenuCopy_WHEN_AddressPresent_THEN_CopiesAndNotifies()
        {
            var item = CreatePeerLog(1, "IPAddress", true);
            _target.Instance.SetContextMenuItem(item);

            await _target.InvokeAsync(() => _target.Instance.InvokeCopyContextMenuItem());

            TestContext.Clipboard.PeekLast().Should().Be("IPAddress");
            Mock.Get(_snackbar).Verify(s => s.Add("Address copied to clipboard.", Severity.Info, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ContextMenuCopy_WHEN_AddressMissing_THEN_DoesNotCopy()
        {
            var item = CreatePeerLog(1, string.Empty, true);
            _target.Instance.SetContextMenuItem(item);

            await _target.InvokeAsync(() => _target.Instance.InvokeCopyContextMenuItem());

            TestContext.Clipboard.PeekLast().Should().BeNull();
            Mock.Get(_snackbar).Verify(s => s.Add(It.IsAny<string>(), It.IsAny<Severity>(), null, null), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ContextMenuReferenceMissing_WHEN_TableDataContextMenuInvoked_THEN_ContextItemSet()
        {
            var item = CreatePeerLog(1, "IPAddress", true);
            _target.Instance.ClearContextMenuReference();

            await _target.InvokeAsync(() => _target.Instance.InvokeTableDataContextMenu(item));

            _target.Instance.CurrentContextMenuItem.Should().Be(item);
        }

        [Fact]
        public async Task GIVEN_ContextMenuReferenceMissing_WHEN_TableDataLongPressInvoked_THEN_ContextItemSet()
        {
            var item = CreatePeerLog(1, "IPAddress", true);
            _target.Instance.ClearContextMenuReference();

            await _target.InvokeAsync(() => _target.Instance.InvokeTableDataLongPress(item));

            _target.Instance.CurrentContextMenuItem.Should().Be(item);
        }

        [Fact]
        public async Task GIVEN_ContextMenuReferenceAvailable_WHEN_TableDataContextMenuInvoked_THEN_ContextItemSet()
        {
            var item = CreatePeerLog(1, "IPAddress", true);

            _target.WaitForAssertion(() =>
            {
                _target.Instance.HasContextMenuReference.Should().BeTrue();
            });

            await _target.InvokeAsync(() => _target.Instance.InvokeTableDataContextMenu(item));

            _target.Instance.CurrentContextMenuItem.Should().Be(item);
        }

        [Fact]
        public async Task GIVEN_NoResults_WHEN_ClearInvoked_THEN_NoNotification()
        {
            await _target.InvokeAsync(() => _target.Instance.InvokeClearResults());

            Mock.Get(_snackbar).Verify(s => s.Add(It.IsAny<string>(), It.IsAny<Severity>(), null, null), Times.Never);
        }

        [Fact]
        public async Task GIVEN_Results_WHEN_ClearInvoked_THEN_TableCleared()
        {
            var results = new List<PeerLog> { CreatePeerLog(1, "IPAddress", false) };
            Mock.Get(_apiClient)
                .Setup(c => c.GetPeerLog(It.IsAny<int?>()))
                .ReturnsAsync(results);

            await _target.InvokeAsync(() => _target.Instance.InvokeSubmit(new EditContext(new LogForm())));

            _target.WaitForAssertion(() =>
            {
                var items = _target.Instance.CurrentResults.Should().NotBeNull().And.Subject;
                items.Count().Should().Be(1);
            });

            _target.Instance.SetContextMenuItem(results[0]);
            await _target.InvokeAsync(() => _target.Instance.InvokeClearResults());

            var clearedItems = _target.Instance.CurrentResults.Should().NotBeNull().And.Subject;
            clearedItems.Should().BeEmpty();
            Mock.Get(_snackbar).Verify(s => s.Add("Blocked IP list cleared.", Severity.Info, null, null), Times.Once);
        }

        [Fact]
        public void GIVEN_RowClassFunc_WHEN_BlockedAndNormal_THEN_ReturnsExpectedClasses()
        {
            var table = _target.FindComponent<DynamicTable<PeerLog>>();
            var func = table.Instance.RowClassFunc;
            func.Should().NotBeNull();

            func!.Invoke(new PeerLog(1, "IPAddress", 1, true, "Reason"), 0).Should().Be("log-critical");
            func!.Invoke(new PeerLog(2, "IPAddress", 1, false, "Reason"), 0).Should().Be("log-normal");
        }

        [Fact]
        public async Task GIVEN_MoreThanMaxResults_WHEN_Fetched_THEN_TrimsOldest()
        {
            var results = CreatePeerLogs(501, true);
            Mock.Get(_apiClient)
                .Setup(c => c.GetPeerLog(It.IsAny<int?>()))
                .ReturnsAsync(results);

            await _timer.TriggerTickAsync();

            _target.WaitForAssertion(() =>
            {
                var items = _target.Instance.CurrentResults.Should().NotBeNull().And.Subject.ToList();
                items.Count.Should().Be(500);
                items[0].Id.Should().Be(2);
            });
        }

        [Fact]
        public async Task GIVEN_TimerTick_WHEN_Forbidden_THEN_NoCrash()
        {
            var exception = new HttpRequestException("Message", null, System.Net.HttpStatusCode.Forbidden);
            Mock.Get(_apiClient)
                .Setup(c => c.GetPeerLog(It.IsAny<int?>()))
                .ThrowsAsync(exception);

            await _timer.TriggerTickAsync();

            Mock.Get(_apiClient).Verify(c => c.GetPeerLog(It.IsAny<int?>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GIVEN_SelectedValuesChanged_WHEN_Invoked_THEN_Persisted()
        {
            var values = new[] { "Normal", "Warning" };

            await _target.InvokeAsync(() => _target.Instance.InvokeSelectedValuesChanged(values));

            var stored = await TestContext.LocalStorage.GetItemAsync<IEnumerable<string>>(SelectedTypesStorageKey);
            stored.Should().BeEquivalentTo(values);
        }

        [Fact]
        public async Task GIVEN_SubmitInvoked_WHEN_ApiReturnsData_THEN_TableUpdated()
        {
            var results = new List<PeerLog> { CreatePeerLog(1, "IPAddress", true) };
            Mock.Get(_apiClient)
                .Setup(c => c.GetPeerLog(It.IsAny<int?>()))
                .ReturnsAsync(results);

            await _target.InvokeAsync(() => _target.Instance.InvokeSubmit(new EditContext(new LogForm())));

            _target.WaitForAssertion(() =>
            {
                var items = _target.Instance.CurrentResults.Should().NotBeNull().And.Subject;
                items.Count().Should().Be(1);
            });
        }

        [Fact]
        public void GIVEN_GenerateSelectedText_WHEN_AllSelected_THEN_ReturnsAll()
        {
            var text = _target.Instance.InvokeGenerateSelectedText(new List<string> { "Normal", "Info", "Warning", "Critical" });

            text.Should().Be("All");
        }

        [Fact]
        public void GIVEN_GenerateSelectedText_WHEN_NotAllSelected_THEN_ReturnsCount()
        {
            var text = _target.Instance.InvokeGenerateSelectedText(new List<string> { "Normal", "Warning" });

            text.Should().Be("2 selected");
        }

        [Fact]
        public async Task GIVEN_StoredTypes_WHEN_Rendered_THEN_PersistsSelection()
        {
            var values = new[] { "Info", "Critical" };

            await using var localContext = new ComponentTestContext();
            await localContext.LocalStorage.SetItemAsync(SelectedTypesStorageKey, values);

            var apiClientMock = new Mock<IApiClient>();
            apiClientMock.Setup(c => c.GetPeerLog(It.IsAny<int?>())).ReturnsAsync(new List<PeerLog>());
            localContext.Services.RemoveAll(typeof(IApiClient));
            localContext.Services.AddSingleton(apiClientMock.Object);
            localContext.Services.RemoveAll(typeof(IPeriodicTimerFactory));
            localContext.Services.AddSingleton<IPeriodicTimerFactory>(new FakePeriodicTimerFactory(new FakePeriodicTimer()));

            var localTarget = localContext.Render<BlocksHarness>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", false);
            });

            localTarget.WaitForAssertion(() =>
            {
                var stored = localTarget.Instance.CurrentSelectedTypes;
                stored.Should().BeEquivalentTo(values);
            });
        }

        private static PeerLog CreatePeerLog(int id, string address, bool blocked)
        {
            return new PeerLog(id, address, id, blocked, "Reason");
        }

        private static List<PeerLog> CreatePeerLogs(int count, bool blocked)
        {
            var results = new List<PeerLog>(count);
            for (var i = 1; i <= count; i++)
            {
                results.Add(CreatePeerLog(i, $"IPAddress{i}", blocked));
            }

            return results;
        }

        private sealed class BlocksHarness : Blocks
        {
            public IEnumerable<string> CurrentSelectedTypes
            {
                get { return Model.SelectedTypes; }
            }

            public List<PeerLog>? CurrentResults
            {
                get { return Results; }
            }

            public PeerLog? CurrentContextMenuItem
            {
                get { return ContextMenuItem; }
            }

            public bool HasContextMenuReference
            {
                get { return ContextMenu is not null; }
            }

            public void SetContextMenuItem(PeerLog? item)
            {
                ContextMenuItem = item;
            }

            public void ClearContextMenuReference()
            {
                ContextMenu = null;
            }

            public Task InvokeTableDataContextMenu(PeerLog? item)
            {
                var args = new TableDataContextMenuEventArgs<PeerLog>(new MouseEventArgs(), new MudTd(), item);
                return TableDataContextMenu(args);
            }

            public Task InvokeTableDataLongPress(PeerLog? item)
            {
                var args = new TableDataLongPressEventArgs<PeerLog>(new LongPressEventArgs(), new MudTd(), item);
                return TableDataLongPress(args);
            }

            public Task InvokeCopyContextMenuItem()
            {
                return CopyContextMenuItem();
            }

            public Task InvokeClearResults()
            {
                return ClearResults();
            }

            public Task InvokeSelectedValuesChanged(IEnumerable<string> values)
            {
                return SelectedValuesChanged(values);
            }

            public string InvokeGenerateSelectedText(List<string> values)
            {
                return GenerateSelectedText(values);
            }

            public Task InvokeSubmit(EditContext editContext)
            {
                return Submit(editContext);
            }
        }
    }
}
