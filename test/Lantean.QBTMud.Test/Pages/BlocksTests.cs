using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
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
        private readonly IApiClient _apiClient;
        private readonly ISnackbar _snackbar;
        private readonly IManagedTimer _timer;
        private readonly IManagedTimerFactory _timerFactory;
        private Func<CancellationToken, Task<ManagedTimerTickResult>>? _tickHandler;
        private readonly IRenderedComponent<MudPopoverProvider> _popoverProvider;
        private readonly IRenderedComponent<Blocks> _target;

        public BlocksTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _snackbar = Mock.Of<ISnackbar>();
            _timer = Mock.Of<IManagedTimer>();
            _timerFactory = Mock.Of<IManagedTimerFactory>();
            Mock.Get(_timerFactory)
                .Setup(factory => factory.Create(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(_timer);
            Mock.Get(_timer)
                .Setup(timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((handler, _) => _tickHandler = handler)
                .ReturnsAsync(true);

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.RemoveAll<ISnackbar>();
            TestContext.Services.AddSingleton(_snackbar);
            TestContext.Services.RemoveAll<IManagedTimerFactory>();
            TestContext.Services.AddSingleton(_timerFactory);

            _popoverProvider = TestContext.Render<MudPopoverProvider>();

            Mock.Get(_apiClient)
                .Setup(c => c.GetPeerLog(It.IsAny<int?>()))
                .ReturnsAsync(new List<PeerLog>());

            _target = TestContext.Render<Blocks>(parameters =>
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

            await TriggerTimerTickAsync();

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

            await TriggerContextMenuAsync(item);
            await OpenMenuAsync();

            _target.WaitForAssertion(() =>
            {
                var menuItem = FindMenuItem(Icons.Material.Filled.ContentCopy);
                menuItem.Instance.Disabled.Should().BeFalse();
            });

            var copyItem = FindMenuItem(Icons.Material.Filled.ContentCopy);
            await _target.InvokeAsync(() => copyItem.Instance.OnClick.InvokeAsync());

            TestContext.Clipboard.PeekLast().Should().Be("IPAddress");
            Mock.Get(_snackbar).Verify(s => s.Add("Address copied to clipboard.", Severity.Info, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ContextMenuCopy_WHEN_AddressMissing_THEN_DoesNotCopy()
        {
            var item = CreatePeerLog(1, string.Empty, true);

            await TriggerLongPressAsync(item);
            await OpenMenuAsync();

            var copyItem = FindMenuItem(Icons.Material.Filled.ContentCopy);
            await _target.InvokeAsync(() => copyItem.Instance.OnClick.InvokeAsync());

            TestContext.Clipboard.PeekLast().Should().BeNull();
            Mock.Get(_snackbar).Verify(s => s.Add(It.IsAny<string>(), It.IsAny<Severity>(), null, null), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NoResults_WHEN_ClearInvoked_THEN_NoNotification()
        {
            await OpenMenuAsync();

            var clearItem = FindMenuItem(Icons.Material.Filled.Clear);

            await _target.InvokeAsync(() => clearItem.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(s => s.Add(It.IsAny<string>(), It.IsAny<Severity>(), null, null), Times.Never);
        }

        [Fact]
        public async Task GIVEN_Results_WHEN_ClearInvoked_THEN_TableCleared()
        {
            var results = new List<PeerLog> { CreatePeerLog(1, "IPAddress", false) };
            Mock.Get(_apiClient)
                .Setup(c => c.GetPeerLog(It.IsAny<int?>()))
                .ReturnsAsync(results);

            await InvokeSubmitAsync();

            var table = _target.FindComponent<DynamicTable<PeerLog>>();
            table.WaitForAssertion(() =>
            {
                var items = table.Instance.Items.Should().NotBeNull().And.Subject;
                items.Count().Should().Be(1);
            });

            await OpenMenuAsync();

            var clearItem = FindMenuItem(Icons.Material.Filled.Clear);
            await _target.InvokeAsync(() => clearItem.Instance.OnClick.InvokeAsync());

            var clearedItems = table.Instance.Items.Should().NotBeNull().And.Subject;
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

            await TriggerTimerTickAsync();

            var table = _target.FindComponent<DynamicTable<PeerLog>>();
            table.WaitForAssertion(() =>
            {
                var items = table.Instance.Items.Should().NotBeNull().And.Subject.ToList();
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

            await TriggerTimerTickAsync();

            Mock.Get(_apiClient).Verify(c => c.GetPeerLog(It.IsAny<int?>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GIVEN_DisposeInvoked_WHEN_Disposed_THEN_NoCrash()
        {
            await _target.Instance.DisposeAsync();
        }

        private async Task InvokeSubmitAsync()
        {
            var form = _target.FindComponent<EditForm>();
            await _target.InvokeAsync(() => form.Instance.OnSubmit.InvokeAsync(form.Instance.EditContext));
        }

        private async Task TriggerContextMenuAsync(PeerLog item)
        {
            var table = _target.FindComponent<DynamicTable<PeerLog>>();
            var args = new TableDataContextMenuEventArgs<PeerLog>(new MouseEventArgs(), new MudTd(), item);
            await _target.InvokeAsync(() => table.Instance.OnTableDataContextMenu.InvokeAsync(args));
        }

        private async Task TriggerLongPressAsync(PeerLog item)
        {
            var table = _target.FindComponent<DynamicTable<PeerLog>>();
            var args = new TableDataLongPressEventArgs<PeerLog>(new LongPressEventArgs(), new MudTd(), item);
            await _target.InvokeAsync(() => table.Instance.OnTableDataLongPress.InvokeAsync(args));
        }

        private async Task OpenMenuAsync()
        {
            var menu = _target.FindComponent<MudMenu>();
            await _target.InvokeAsync(() => menu.Instance.OpenMenuAsync(new MouseEventArgs()));
        }

        private async Task TriggerTimerTickAsync()
        {
            var handler = GetTickHandler();
            await _target.InvokeAsync(() => handler(CancellationToken.None));
        }

        private Func<CancellationToken, Task<ManagedTimerTickResult>> GetTickHandler()
        {
            _target.WaitForAssertion(() =>
            {
                Mock.Get(_timer).Verify(
                    timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            _tickHandler.Should().NotBeNull();
            return _tickHandler!;
        }

        private IRenderedComponent<MudMenuItem> FindMenuItem(string icon)
        {
            return _popoverProvider.FindComponents<MudMenuItem>()
                .Single(item => item.Instance.Icon == icon);
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
    }
}
