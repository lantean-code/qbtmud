using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
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
        private readonly FakePeriodicTimer _timer;
        private readonly IRenderedComponent<LogHarness> _target;

        public LogTests()
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
                .Setup(c => c.GetLog(It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<LogEntry>());

            _target = TestContext.Render<LogHarness>(parameters =>
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

            await _target.InvokeAsync(() => _target.Instance.InvokeSelectedValuesChanged(values));

            var stored = await TestContext.LocalStorage.GetItemAsync<IEnumerable<string>>(SelectedTypesStorageKey);
            stored.Should().BeEquivalentTo(values);
        }

        [Fact]
        public void GIVEN_MultiSelectionTextFunc_WHEN_CountsProvided_THEN_ReturnsExpected()
        {
            var text = _target.Instance.InvokeGenerateSelectedText(new List<string> { "Normal", "Info", "Warning", "Critical" });
            text.Should().Be("All");

            text = _target.Instance.InvokeGenerateSelectedText(new List<string> { "Normal" });
            text.Should().Be("Normal");

            text = _target.Instance.InvokeGenerateSelectedText(new List<string> { "Normal", "Warning" });
            text.Should().Be("2 selected");
        }

        [Fact]
        public async Task GIVEN_TimerTick_WHEN_ResultsReturned_THEN_TableUpdated()
        {
            var results = new List<LogEntry> { CreateLog(1, "Message", LogType.Warning) };
            Mock.Get(_apiClient)
                .Setup(c => c.GetLog(It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>()))
                .ReturnsAsync(results);

            await _timer.TriggerTickAsync();

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
            _target.Instance.SetContextMenuItem(item);

            await _target.InvokeAsync(() => _target.Instance.InvokeCopyContextMenuItem());

            TestContext.Clipboard.PeekLast().Should().Be("Message");
            Mock.Get(_snackbar).Verify(s => s.Add("Log entry copied to clipboard.", Severity.Info, null, null), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ContextMenuCopy_WHEN_MessageMissing_THEN_DoesNotCopy()
        {
            var item = CreateLog(1, string.Empty, LogType.Normal);
            _target.Instance.SetContextMenuItem(item);

            await _target.InvokeAsync(() => _target.Instance.InvokeCopyContextMenuItem());

            TestContext.Clipboard.PeekLast().Should().BeNull();
            Mock.Get(_snackbar).Verify(s => s.Add(It.IsAny<string>(), It.IsAny<Severity>(), null, null), Times.Never);
        }

        [Fact]
        public void GIVEN_NavigateBack_WHEN_Invoked_THEN_NavigatesHome()
        {
            var navigationManager = TestContext.Services.GetRequiredService<NavigationManager>();

            _target.Instance.InvokeNavigateBack();

            navigationManager.Uri.Should().EndWith("/");
        }

        [Fact]
        public async Task GIVEN_ContextMenuReferenceMissing_WHEN_TableDataContextMenuInvoked_THEN_ContextItemSet()
        {
            var item = CreateLog(1, "Message", LogType.Info);
            _target.Instance.ClearContextMenuReference();

            await _target.InvokeAsync(() => _target.Instance.InvokeTableDataContextMenu(item));

            _target.Instance.CurrentContextMenuItem.Should().Be(item);
        }

        [Fact]
        public async Task GIVEN_ContextMenuReferenceMissing_WHEN_TableDataLongPressInvoked_THEN_ContextItemSet()
        {
            var item = CreateLog(1, "Message", LogType.Info);
            _target.Instance.ClearContextMenuReference();

            await _target.InvokeAsync(() => _target.Instance.InvokeTableDataLongPress(item));

            _target.Instance.CurrentContextMenuItem.Should().Be(item);
        }

        [Fact]
        public async Task GIVEN_ContextMenuReferenceAvailable_WHEN_TableDataContextMenuInvoked_THEN_ContextItemSet()
        {
            var item = CreateLog(1, "Message", LogType.Info);

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
            var results = new List<LogEntry> { CreateLog(1, "Message", LogType.Info) };
            Mock.Get(_apiClient)
                .Setup(c => c.GetLog(It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>()))
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

            await _timer.TriggerTickAsync();

            _target.WaitForAssertion(() =>
            {
                var items = _target.Instance.CurrentResults.Should().NotBeNull().And.Subject.ToList();
                items.Count.Should().Be(500);
                items[0].Id.Should().Be(2);
            });
        }

        [Fact]
        public async Task GIVEN_FormSubmitted_WHEN_SubmitInvoked_THEN_LogsRequested()
        {
            var form = _target.FindComponent<EditForm>();

            await _target.InvokeAsync(() => form.Instance.OnSubmit.InvokeAsync(form.Instance.EditContext));

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
            localContext.Services.RemoveAll(typeof(IPeriodicTimerFactory));
            localContext.Services.AddSingleton<IPeriodicTimerFactory>(new FakePeriodicTimerFactory(new FakePeriodicTimer()));

            var localTarget = localContext.Render<LogHarness>(parameters =>
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

            await _timer.TriggerTickAsync();

            Mock.Get(_apiClient).Verify(c => c.GetLog(It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<int?>()), Times.AtLeastOnce);
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

        private sealed class LogHarness : LogPage
        {
            public IEnumerable<string> CurrentSelectedTypes
            {
                get { return Model.SelectedTypes; }
            }

            public List<LogEntry>? CurrentResults
            {
                get { return Results; }
            }

            public LogEntry? CurrentContextMenuItem
            {
                get { return ContextMenuItem; }
            }

            public bool HasContextMenuReference
            {
                get { return ContextMenu is not null; }
            }

            public void SetContextMenuItem(LogEntry? item)
            {
                ContextMenuItem = item;
            }

            public void ClearContextMenuReference()
            {
                ContextMenu = null;
            }

            public Task InvokeTableDataContextMenu(LogEntry? item)
            {
                var args = new TableDataContextMenuEventArgs<LogEntry>(new MouseEventArgs(), new MudTd(), item);
                return TableDataContextMenu(args);
            }

            public Task InvokeTableDataLongPress(LogEntry? item)
            {
                var args = new TableDataLongPressEventArgs<LogEntry>(new LongPressEventArgs(), new MudTd(), item);
                return TableDataLongPress(args);
            }

            public void InvokeNavigateBack()
            {
                NavigateBack();
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
