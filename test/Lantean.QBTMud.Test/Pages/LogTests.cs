using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using LogEntry = Lantean.QBitTorrentClient.Models.Log;
using LogPage = Lantean.QBTMud.Pages.Log;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class LogTests : RazorComponentTestBase
    {
        private const string SelectedTypesStorageKey = "Log.SelectedTypes";
        private readonly IApiClient _apiClient;
        private readonly ISnackbar _snackbar;
        private readonly IManagedTimer _timer;
        private readonly IManagedTimerFactory _timerFactory;
        private readonly IRenderedComponent<MudPopoverProvider> _popoverProvider;
        private readonly IRenderedComponent<LogPage> _target;

        public LogTests()
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
                .ReturnsAsync(true);

            TestContext.Services.RemoveAll(typeof(IApiClient));
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.RemoveAll(typeof(ISnackbar));
            TestContext.Services.AddSingleton(_snackbar);
            TestContext.Services.RemoveAll(typeof(IManagedTimerFactory));
            TestContext.Services.AddSingleton(_timerFactory);

            _popoverProvider = TestContext.Render<MudPopoverProvider>();

            Mock.Get(_apiClient)
                .Setup(c => c.GetLog(It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<LogEntry>());

            _target = TestContext.Render<LogPage>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", false);
            });
        }

        [Fact]
        public void GIVEN_DefaultLoad_WHEN_Rendered_THEN_LogRequestedWithNormal()
        {
            Mock.Get(_apiClient).Verify(c => c.GetLog(true, false, false, false, It.Is<int?>(id => id == null)), Times.Once);
        }

        [Fact]
        public async Task GIVEN_SelectedValuesChanged_WHEN_Invoked_THEN_Persisted()
        {
            var values = new[] { "Info", "Warning" };
            var select = FindCategorySelect();

            await _target.InvokeAsync(() => select.Instance.SelectedValuesChanged.InvokeAsync(values));

            var stored = await TestContext.LocalStorage.GetItemAsync<IEnumerable<string>>(SelectedTypesStorageKey);
            stored.Should().BeEquivalentTo(values);
        }

        [Fact]
        public void GIVEN_MultiSelectionTextFunc_WHEN_CountsProvided_THEN_ReturnsExpected()
        {
            var select = FindCategorySelect();
            select.Instance.MultiSelectionTextFunc.Should().NotBeNull();

            var func = select.Instance.MultiSelectionTextFunc!;
            func.Invoke(new List<string?> { "Normal", "Info", "Warning", "Critical" }).Should().Be("All");
            func.Invoke(new List<string?> { "Normal" }).Should().Be("Normal Messages");
            func.Invoke(new List<string?> { "Normal", "Warning" }).Should().Be("2 items");
        }

        [Fact]
        public async Task GIVEN_TimerTick_WHEN_ResultsReturned_THEN_TableUpdated()
        {
            var results = new List<LogEntry> { CreateLog(1, "Message", LogType.Warning) };
            Mock.Get(_apiClient)
                .Setup(c => c.GetLog(It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>()))
                .ReturnsAsync(results);

            await TriggerTimerTickAsync();

            var table = _target.FindComponent<DynamicTable<LogEntry>>();
            table.WaitForAssertion(() =>
            {
                var items = table.Instance.Items.Should().NotBeNull().And.Subject;
                items.Count().Should().Be(1);
            });
        }

        [Fact]
        public async Task GIVEN_ContextMenuCopy_WHEN_MessagePresent_THEN_CopiesAndNotifies()
        {
            var item = CreateLog(1, "Message", LogType.Normal);

            await TriggerContextMenuAsync(item);
            await OpenMenuAsync();

            var copyItem = FindMenuItem(Icons.Material.Filled.ContentCopy);
            await _target.InvokeAsync(() => copyItem.Instance.OnClick.InvokeAsync());

            TestContext.Clipboard.PeekLast().Should().Be("Message");
            Mock.Get(_snackbar).Verify(s => s.Add("Log entry copied to clipboard.", Severity.Info, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ContextMenuCopy_WHEN_MessageMissing_THEN_DoesNotCopy()
        {
            var item = CreateLog(1, string.Empty, LogType.Normal);

            await TriggerLongPressAsync(item);
            await OpenMenuAsync();

            var copyItem = FindMenuItem(Icons.Material.Filled.ContentCopy);
            await _target.InvokeAsync(() => copyItem.Instance.OnClick.InvokeAsync());

            TestContext.Clipboard.PeekLast().Should().BeNull();
            Mock.Get(_snackbar).Verify(s => s.Add(It.IsAny<string>(), It.IsAny<Severity>(), null, null), Times.Never);
        }

        [Fact]
        public async Task GIVEN_NavigateBack_WHEN_Clicked_THEN_NavigatesHome()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();
            var backButton = _target.FindComponents<MudIconButton>()
                .Single(button => button.Instance.Icon == Icons.Material.Outlined.NavigateBefore);

            await _target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            navigationManager.Uri.Should().EndWith("/");
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
            var results = new List<LogEntry> { CreateLog(1, "Message", LogType.Info) };
            Mock.Get(_apiClient)
                .Setup(c => c.GetLog(It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>()))
                .ReturnsAsync(results);

            await InvokeSubmitAsync();

            var table = _target.FindComponent<DynamicTable<LogEntry>>();
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
            Mock.Get(_snackbar).Verify(s => s.Add("Log view cleared.", Severity.Info, null, null), Times.Once);
        }

        [Fact]
        public void GIVEN_RowClassFunc_WHEN_LogTypesProvided_THEN_ReturnsExpected()
        {
            var table = _target.FindComponent<DynamicTable<LogEntry>>();
            var func = table.Instance.RowClassFunc;
            func.Should().NotBeNull();

            func!.Invoke(new LogEntry(1, "Message", 1, LogType.Critical), 0).Should().Be("log-critical");
            func!.Invoke(new LogEntry(2, "Message", 1, LogType.Info), 0).Should().Be("log-info");
        }

        [Fact]
        public async Task GIVEN_MoreThanMaxResults_WHEN_Fetched_THEN_TrimsOldest()
        {
            var results = CreateLogs(501, LogType.Warning);
            Mock.Get(_apiClient)
                .Setup(c => c.GetLog(It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>()))
                .ReturnsAsync(results);

            await TriggerTimerTickAsync();

            var table = _target.FindComponent<DynamicTable<LogEntry>>();
            table.WaitForAssertion(() =>
            {
                var items = table.Instance.Items.Should().NotBeNull().And.Subject.ToList();
                items.Count.Should().Be(500);
                items[0].Id.Should().Be(2);
            });
        }

        [Fact]
        public async Task GIVEN_FormSubmitted_WHEN_SubmitInvoked_THEN_LogsRequested()
        {
            await InvokeSubmitAsync();

            Mock.Get(_apiClient).Verify(c => c.GetLog(It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>()), Times.AtLeast(2));
        }

        [Fact]
        public async Task GIVEN_StoredTypes_WHEN_Rendered_THEN_LogRequestedWithStoredSelection()
        {
            var values = new[] { "Info", "Critical" };

            await using var localContext = new ComponentTestContext();
            await localContext.LocalStorage.SetItemAsync(SelectedTypesStorageKey, values);

            var apiClientMock = new Mock<IApiClient>();
            apiClientMock
                .Setup(c => c.GetLog(false, true, false, true, It.IsAny<int?>()))
                .ReturnsAsync(new List<LogEntry>());
            localContext.Services.RemoveAll(typeof(IApiClient));
            localContext.Services.AddSingleton(apiClientMock.Object);
            var managedTimer = new Mock<IManagedTimer>();
            var managedTimerFactory = new Mock<IManagedTimerFactory>();
            managedTimerFactory
                .Setup(factory => factory.Create(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(managedTimer.Object);
            managedTimer
                .Setup(timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            localContext.Services.RemoveAll(typeof(IManagedTimerFactory));
            localContext.Services.AddSingleton(managedTimerFactory.Object);

            var localTarget = localContext.Render<LogPage>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", false);
            });

            localTarget.WaitForAssertion(() =>
            {
                apiClientMock.Verify(c => c.GetLog(false, true, false, true, It.IsAny<int?>()), Times.Once);
            });
        }

        [Fact]
        public async Task GIVEN_TimerTick_WHEN_Forbidden_THEN_NoCrash()
        {
            var exception = new HttpRequestException("Message", null, System.Net.HttpStatusCode.Forbidden);
            Mock.Get(_apiClient)
                .Setup(c => c.GetLog(It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>()))
                .ThrowsAsync(exception);

            await TriggerTimerTickAsync();

            Mock.Get(_apiClient).Verify(c => c.GetLog(It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GIVEN_DisposeInvoked_WHEN_Disposed_THEN_NoCrash()
        {
            await _target.Instance.DisposeAsync();
        }

        private IRenderedComponent<MudSelect<string>> FindCategorySelect()
        {
            return FindComponentByTestId<MudSelect<string>>(_target, "Categories");
        }

        private async Task InvokeSubmitAsync()
        {
            var form = _target.FindComponent<EditForm>();
            await _target.InvokeAsync(() => form.Instance.OnSubmit.InvokeAsync(form.Instance.EditContext));
        }

        private async Task TriggerContextMenuAsync(LogEntry item)
        {
            var table = _target.FindComponent<DynamicTable<LogEntry>>();
            var args = new TableDataContextMenuEventArgs<LogEntry>(new MouseEventArgs(), new MudTd(), item);
            await _target.InvokeAsync(() => table.Instance.OnTableDataContextMenu.InvokeAsync(args));
        }

        private async Task TriggerLongPressAsync(LogEntry item)
        {
            var table = _target.FindComponent<DynamicTable<LogEntry>>();
            var args = new TableDataLongPressEventArgs<LogEntry>(new LongPressEventArgs(), new MudTd(), item);
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

        private static IRenderedComponent<TComponent> FindComponentByTestId<TComponent>(IRenderedComponent<LogPage> target, string testId)
            where TComponent : MudComponentBase
        {
            var expected = TestIdHelper.For(testId);
            return target.FindComponents<TComponent>()
                .First(component => component.Instance.UserAttributes is not null
                    && component.Instance.UserAttributes.TryGetValue("data-test-id", out var value)
                    && string.Equals(value?.ToString(), expected, StringComparison.Ordinal));
        }

        private Func<CancellationToken, Task<ManagedTimerTickResult>> GetTickHandler()
        {
            _target.WaitForAssertion(() =>
            {
                Mock.Get(_timer).Verify(
                    timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            });

            var invocation = Mock.Get(_timer).Invocations.Single(invocation => invocation.Method.Name == nameof(IManagedTimer.StartAsync));
            return (Func<CancellationToken, Task<ManagedTimerTickResult>>)invocation.Arguments[0];
        }

        private IRenderedComponent<MudMenuItem> FindMenuItem(string icon)
        {
            return _popoverProvider.FindComponents<MudMenuItem>()
                .Single(item => item.Instance.Icon == icon);
        }

        private static LogEntry CreateLog(int id, string message, LogType type)
        {
            return new LogEntry(id, message, id, type);
        }

        private static List<LogEntry> CreateLogs(int count, LogType type)
        {
            var results = new List<LogEntry>(count);
            for (var i = 1; i <= count; i++)
            {
                results.Add(CreateLog(i, $"Message{i}", type));
            }

            return results;
        }
    }
}
