using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Pages;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using ClientModels = Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Test.Pages
{
    public sealed class TorrentCreatorTests : RazorComponentTestBase<TorrentCreator>
    {
        private readonly IApiClient _apiClient;
        private readonly IDialogService _dialogService;
        private readonly ISnackbar _snackbar;
        private readonly IManagedTimerFactory _timerFactory;
        private readonly IManagedTimer _timer;

        public TorrentCreatorTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _dialogService = Mock.Of<IDialogService>();
            _snackbar = Mock.Of<ISnackbar>();
            _timerFactory = Mock.Of<IManagedTimerFactory>();
            _timer = Mock.Of<IManagedTimer>();

            Mock.Get(_timerFactory)
                .Setup(factory => factory.Create(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .Returns(_timer);
            Mock.Get(_timer)
                .Setup(timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            Mock.Get(_timer)
                .SetupGet(timer => timer.State)
                .Returns(ManagedTimerState.Running);

            TestContext.Services.RemoveAll<IApiClient>();
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.RemoveAll<IDialogService>();
            TestContext.Services.AddSingleton(_dialogService);
            TestContext.Services.RemoveAll<ISnackbar>();
            TestContext.Services.AddSingleton(_snackbar);
            TestContext.Services.RemoveAll<IManagedTimerFactory>();
            TestContext.Services.AddSingleton(_timerFactory);
        }

        [Fact]
        public void GIVEN_NoTasks_WHEN_Rendered_THEN_ShowsEmptyMessage()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(Array.Empty<ClientModels.TorrentCreationTaskStatus>());

            var target = RenderPage();

            var emptyState = FindComponentByTestId<MudText>(target, "TorrentCreatorEmptyState");
            GetChildContentText(emptyState.Instance.ChildContent).Should().Be("No torrent creation tasks.");
        }

        [Fact]
        public void GIVEN_NullTasks_WHEN_Rendered_THEN_ShowsEmptyMessage()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .Returns(Task.FromResult<IReadOnlyList<ClientModels.TorrentCreationTaskStatus>>(null!));

            var target = RenderPage();

            var emptyState = FindComponentByTestId<MudText>(target, "TorrentCreatorEmptyState");
            GetChildContentText(emptyState.Instance.ChildContent).Should().Be("No torrent creation tasks.");
        }

        [Fact]
        public void GIVEN_TasksReturned_WHEN_Rendered_THEN_RendersTable()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[]
                {
                    CreateTask("TaskId", "C:/Source", "Finished", 100)
                });

            var target = RenderPage();

            var table = target.FindComponent<DynamicTable<ClientModels.TorrentCreationTaskStatus>>();
            table.Instance.Items.Should().ContainSingle();
        }

        [Fact]
        public void GIVEN_LostConnection_WHEN_Rendered_THEN_ShowsWarningAndSkipsApiCall()
        {
            var mainData = CreateMainData(lostConnection: true);

            RenderPage(mainData: mainData);

            Mock.Get(_apiClient).Verify(client => client.GetTorrentCreationTasks(), Times.Never);
            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("qBittorrent client is not reachable.", Severity.Warning, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        [Fact]
        public void GIVEN_NonTerminalTask_WHEN_Rendered_THEN_StartsPolling()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[]
                {
                    CreateTask("TaskId", "C:/Source", "Running", 10)
                });

            RenderPage();

            Mock.Get(_timer).Verify(
                timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task GIVEN_TerminalTasksOnly_WHEN_Ticked_THEN_DoesNotRefreshTasks()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[]
                {
                    CreateTask("TaskId", "C:/Source", "Finished", 100)
                });

            var handler = CapturePollHandler();
            RenderPage();
            await handler(CancellationToken.None);

            Mock.Get(_apiClient).Verify(client => client.GetTorrentCreationTasks(), Times.Once);
        }

        [Fact]
        public void GIVEN_ColumnsDefinitions_WHEN_Requested_THEN_ContainsExpectedColumns()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { CreateTask("TaskId", "SourcePath", "Running", 50) });
            var target = RenderPage();
            var table = target.FindComponent<DynamicTable<ClientModels.TorrentCreationTaskStatus>>();
            var columns = table.Instance.ColumnDefinitions;

            columns.Should().ContainSingle(column => column.Header == "Name");
            columns.Should().ContainSingle(column => column.Header == "Torrent File" && !column.Enabled);
            columns.Should().ContainSingle(column => column.Header == "Error Message" && !column.Enabled);
            columns.Should().ContainSingle(column => column.Header == "Actions" && column.Width == 140);
        }

        [Fact]
        public void GIVEN_ColumnSortSelectors_WHEN_Invoked_THEN_ReturnExpectedValues()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { CreateTask("TaskId", "SourcePath", "Running", 50) });
            var target = RenderPage();
            var table = target.FindComponent<DynamicTable<ClientModels.TorrentCreationTaskStatus>>();
            var columns = table.Instance.ColumnDefinitions;
            var taskWithValues = new ClientModels.TorrentCreationTaskStatus(
                "TaskId",
                "SourcePath",
                null,
                null,
                "TimeAdded",
                "Format",
                null,
                null,
                "Status",
                null,
                "TorrentFilePath",
                "Source",
                Array.Empty<string>(),
                Array.Empty<string>(),
                "TimeStarted",
                "TimeFinished",
                "ErrorMessage",
                42.0);
            var taskWithNulls = new ClientModels.TorrentCreationTaskStatus(
                "TaskId",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                null,
                null,
                null);

            foreach (var column in columns)
            {
                var valueWithValues = column.SortSelector(taskWithValues);
                var valueWithNulls = column.SortSelector(taskWithNulls);

                switch (column.Header)
                {
                    case "Status":
                        valueWithValues.Should().Be("Status");
                        valueWithNulls.Should().Be(string.Empty);
                        break;

                    case "Progress":
                        valueWithValues.Should().Be(42.0);
                        valueWithNulls.Should().Be(0.0);
                        break;

                    case "Name":
                        valueWithValues.Should().Be("SourcePath.torrent");
                        valueWithNulls.Should().Be("TaskId.torrent");
                        break;

                    case "Source Path":
                        valueWithValues.Should().Be("SourcePath");
                        valueWithNulls.Should().BeNull();
                        break;

                    case "Torrent File":
                        valueWithValues.Should().Be("TorrentFilePath");
                        valueWithNulls.Should().Be(string.Empty);
                        break;

                    case "Added On":
                        valueWithValues.Should().Be("TimeAdded");
                        valueWithNulls.Should().Be(string.Empty);
                        break;

                    case "Started On":
                        valueWithValues.Should().Be("TimeStarted");
                        valueWithNulls.Should().Be(string.Empty);
                        break;

                    case "Completed On":
                        valueWithValues.Should().Be("TimeFinished");
                        valueWithNulls.Should().Be(string.Empty);
                        break;

                    case "Error Message":
                        valueWithValues.Should().Be("ErrorMessage");
                        valueWithNulls.Should().Be(string.Empty);
                        break;

                    case "Actions":
                        valueWithValues.Should().Be("TaskId");
                        valueWithNulls.Should().Be("TaskId");
                        break;
                }
            }
        }

        [Fact]
        public async Task GIVEN_DownloadRequested_WHEN_TaskFinished_THEN_TriggersFileDownload()
        {
            var task = CreateTask("TaskId", "C:/Folder/File.txt", "Finished", 100);
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { task });
            var downloadInvocation = TestContext.JSInterop.SetupVoid("qbt.triggerFileDownload", _ => true);
            downloadInvocation.SetVoidResult();

            var target = RenderPage();

            var downloadButton = FindIconButton(target, Icons.Material.Filled.Download);
            await target.InvokeAsync(() => downloadButton.Instance.OnClick.InvokeAsync());

            var arguments = downloadInvocation.Invocations
                .Select(invocation => invocation.Arguments.OfType<string>().ToList())
                .Single();
            arguments.Should().Equal("api/v2/torrentcreator/torrentFile?taskID=TaskId", "File.txt.torrent");
        }

        [Fact]
        public async Task GIVEN_DownloadRequested_WHEN_NotFinished_THEN_DoesNotDownload()
        {
            var task = CreateTask("TaskId", "C:/Folder/File.txt", "Running", 50);
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { task });
            TestContext.JSInterop.SetupVoid("qbt.triggerFileDownload", _ => true).SetVoidResult();

            var target = RenderPage();

            var downloadButton = FindIconButton(target, Icons.Material.Filled.Download);
            await target.InvokeAsync(() => downloadButton.Instance.OnClick.InvokeAsync());

            TestContext.JSInterop.Invocations
                .Where(invocation => invocation.Identifier == "qbt.triggerFileDownload")
                .Should()
                .BeEmpty();
        }

        [Fact]
        public async Task GIVEN_DownloadRequestedWithDriveRoot_WHEN_Clicked_THEN_FallsBackToTaskId()
        {
            var task = CreateTask("TaskId", "C:", "Finished", 100);
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { task });
            var downloadInvocation = TestContext.JSInterop.SetupVoid("qbt.triggerFileDownload", _ => true);
            downloadInvocation.SetVoidResult();

            var target = RenderPage();

            var downloadButton = FindIconButton(target, Icons.Material.Filled.Download);
            await target.InvokeAsync(() => downloadButton.Instance.OnClick.InvokeAsync());

            var arguments = downloadInvocation.Invocations
                .Select(invocation => invocation.Arguments.OfType<string>().ToList())
                .Single();
            arguments.Last().Should().Be("TaskId.torrent");
        }

        [Fact]
        public async Task GIVEN_DownloadRequestedWithTorrentFile_WHEN_Clicked_THEN_KeepsTorrentExtension()
        {
            var task = CreateTask("TaskId", "C:/Folder/File.torrent", "Finished", 100);
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { task });
            var downloadInvocation = TestContext.JSInterop.SetupVoid("qbt.triggerFileDownload", _ => true);
            downloadInvocation.SetVoidResult();

            var target = RenderPage();

            var downloadButton = FindIconButton(target, Icons.Material.Filled.Download);
            await target.InvokeAsync(() => downloadButton.Instance.OnClick.InvokeAsync());

            var arguments = downloadInvocation.Invocations
                .Select(invocation => invocation.Arguments.OfType<string>().ToList())
                .Single();
            arguments.Last().Should().Be("File.torrent");
        }

        [Fact]
        public async Task GIVEN_DeleteRequested_WHEN_ApiFails_THEN_ShowsError()
        {
            var task = CreateTask("TaskId", "C:/Source", "Finished", 100);
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { task });
            Mock.Get(_apiClient)
                .Setup(client => client.DeleteTorrentCreationTask("TaskId"))
                .ThrowsAsync(new HttpRequestException("Failure"));

            var target = RenderPage();

            var deleteButton = FindIconButton(target, Icons.Material.Filled.Delete);
            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to delete task: Failure", Severity.Error, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_DeleteRequested_WHEN_ApiSucceeds_THEN_Refreshes()
        {
            var task = CreateTask("TaskId", "C:/Source", "Finished", 100);
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { task })
                .ReturnsAsync(Array.Empty<ClientModels.TorrentCreationTaskStatus>());
            Mock.Get(_apiClient)
                .Setup(client => client.DeleteTorrentCreationTask("TaskId"))
                .Returns(Task.CompletedTask);

            var target = RenderPage();

            var deleteButton = FindIconButton(target, Icons.Material.Filled.Delete);
            await target.InvokeAsync(() => deleteButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.GetTorrentCreationTasks(), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_OpenCreateDialogCanceled_WHEN_Clicked_THEN_NoApiCall()
        {
            var reference = new Mock<IDialogReference>(MockBehavior.Strict);
            reference.SetupGet(r => r.Result).Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));
            Mock.Get(_dialogService)
                .Setup(service => service.ShowAsync<CreateTorrentDialog>(
                    It.IsAny<string>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(reference.Object);

            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(Array.Empty<ClientModels.TorrentCreationTaskStatus>());

            var target = RenderPage();

            var createButton = FindIconButton(target, Icons.Material.Filled.AddBox);
            await target.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.AddTorrentCreationTask(It.IsAny<ClientModels.TorrentCreationTaskRequest>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_OpenCreateDialogWithRequest_WHEN_Clicked_THEN_CreatesTask()
        {
            var request = new ClientModels.TorrentCreationTaskRequest { SourcePath = "C:/Source" };
            var reference = new Mock<IDialogReference>(MockBehavior.Strict);
            reference.SetupGet(r => r.Result).Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(request)));
            Mock.Get(_dialogService)
                .Setup(service => service.ShowAsync<CreateTorrentDialog>(
                    It.IsAny<string>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(reference.Object);

            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(Array.Empty<ClientModels.TorrentCreationTaskStatus>())
                .ReturnsAsync(Array.Empty<ClientModels.TorrentCreationTaskStatus>());
            Mock.Get(_apiClient)
                .Setup(client => client.AddTorrentCreationTask(request))
                .ReturnsAsync("TaskId");

            var target = RenderPage();

            var createButton = FindIconButton(target, Icons.Material.Filled.AddBox);
            await target.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.AddTorrentCreationTask(request), Times.Once);
            Mock.Get(_apiClient).Verify(client => client.GetTorrentCreationTasks(), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_OpenCreateDialogFailure_WHEN_Clicked_THEN_ShowsError()
        {
            var request = new ClientModels.TorrentCreationTaskRequest { SourcePath = "C:/Source" };
            var reference = new Mock<IDialogReference>(MockBehavior.Strict);
            reference.SetupGet(r => r.Result).Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(request)));
            Mock.Get(_dialogService)
                .Setup(service => service.ShowAsync<CreateTorrentDialog>(
                    It.IsAny<string>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(reference.Object);

            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(Array.Empty<ClientModels.TorrentCreationTaskStatus>());
            Mock.Get(_apiClient)
                .Setup(client => client.AddTorrentCreationTask(request))
                .ThrowsAsync(new HttpRequestException("Failure"));

            var target = RenderPage();

            var createButton = FindIconButton(target, Icons.Material.Filled.AddBox);
            await target.InvokeAsync(() => createButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to create torrent. Failure", Severity.Error, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_NavigateBack_WHEN_Clicked_THEN_NavigatesHome()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(Array.Empty<ClientModels.TorrentCreationTaskStatus>());

            var navigation = TestContext.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("http://localhost/other");

            var target = RenderPage();
            var backButton = FindIconButton(target, Icons.Material.Outlined.NavigateBefore);

            await target.InvokeAsync(() => backButton.Instance.OnClick.InvokeAsync());

            navigation.Uri.Should().Be("http://localhost/");
        }

        [Fact]
        public async Task GIVEN_RefreshClicked_WHEN_Invoked_THEN_RefreshesTasks()
        {
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(Array.Empty<ClientModels.TorrentCreationTaskStatus>())
                .ReturnsAsync(Array.Empty<ClientModels.TorrentCreationTaskStatus>());

            var target = RenderPage();
            var refreshButton = FindIconButton(target, Icons.Material.Filled.Refresh);

            await target.InvokeAsync(() => refreshButton.Instance.OnClick.InvokeAsync());

            Mock.Get(_apiClient).Verify(client => client.GetTorrentCreationTasks(), Times.Exactly(2));
        }

        [Fact]
        public void GIVEN_LoadTasksFails_WHEN_Rendered_THEN_ShowsError()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ThrowsAsync(new HttpRequestException("Failure"));

            RenderPage();

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to load torrent creation tasks: Failure", Severity.Error, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        [Fact]
        public void GIVEN_MainDataMissing_WHEN_Rendered_THEN_LoadsTasks()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(Array.Empty<ClientModels.TorrentCreationTaskStatus>());

            RenderPage(includeMainData: false);

            Mock.Get(_apiClient).Verify(client => client.GetTorrentCreationTasks(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RefreshThrows_WHEN_PollingTicked_THEN_ShowsErrorAndStops()
        {
            var handler = CapturePollHandler();
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { CreateTask("TaskId", "C:/Source", "Running", 10) })
                .ThrowsAsync(new InvalidOperationException("Failure"));

            RenderPage();

            var result = await handler.Invoke(CancellationToken.None);
            result.Action.Should().Be(ManagedTimerTickAction.Stop);

            Mock.Get(_snackbar).Verify(
                snackbar => snackbar.Add("Unable to refresh torrent creation tasks: Failure", Severity.Error, It.IsAny<Action<SnackbarOptions>>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_RefreshCancelled_WHEN_PollingTicked_THEN_Stops()
        {
            var handler = CapturePollHandler();
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { CreateTask("TaskId", "C:/Source", "Running", 10) })
                .ThrowsAsync(new OperationCanceledException());

            RenderPage();

            var result = await handler.Invoke(CancellationToken.None);
            result.Action.Should().Be(ManagedTimerTickAction.Stop);
        }

        [Fact]
        public async Task GIVEN_RunningTask_WHEN_PollingTicked_THEN_Continues()
        {
            var handler = CapturePollHandler();
            Mock.Get(_apiClient)
                .SetupSequence(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { CreateTask("TaskId", "C:/Source", "Running", 10) })
                .ReturnsAsync(new[] { CreateTask("TaskId", "C:/Source", "Running", 20) });

            RenderPage();

            var result = await handler.Invoke(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Continue);
            Mock.Get(_apiClient).Verify(client => client.GetTorrentCreationTasks(), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_NoTasks_WHEN_PollingTicked_THEN_ContinuesWithoutRefreshing()
        {
            var handler = CapturePollHandler();
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(Array.Empty<ClientModels.TorrentCreationTaskStatus>());

            RenderPage();

            var result = await handler.Invoke(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Continue);
            Mock.Get(_apiClient).Verify(client => client.GetTorrentCreationTasks(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CancelledToken_WHEN_PollingTicked_THEN_Stops()
        {
            var handler = CapturePollHandler();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[]
                {
                    CreateTask("TaskId", "C:/Source", "Running", 10)
                });

            RenderPage();

            var result = await handler.Invoke(cts.Token);

            result.Action.Should().Be(ManagedTimerTickAction.Stop);
        }

        [Fact]
        public async Task GIVEN_RunningTask_WHEN_Rendered_THEN_ShowsProgressAndStatus()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[]
                {
                    CreateTask("TaskId", "C:/Source", "Running", 42)
                });

            var target = RenderPage();

            var status = FindComponentByTestId<MudChip<string>>(target, "TorrentCreatorStatus-TaskId");
            status.Instance.Text.Should().Be("Running");

            var progress = FindComponentByTestId<MudProgressLinear>(target, "TorrentCreatorProgress-TaskId");
            progress.Instance.GetState(x => x.Value).Should().Be(42);
        }

        [Fact]
        public async Task GIVEN_NoProgress_WHEN_Rendered_THEN_ShowsZeroPercent()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[]
                {
                    CreateTask("TaskId", "C:/Source", "Running", null)
                });

            var target = RenderPage();

            var progress = FindComponentByTestId<MudProgressLinear>(target, "TorrentCreatorProgress-TaskId");
            progress.Instance.GetState(x => x.Value).Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_FinishedTask_WHEN_Rendered_THEN_ShowsProgressComplete()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[]
                {
                    CreateTask("TaskId", "C:/Source", "Finished", null)
                });

            var target = RenderPage();

            var progress = FindComponentByTestId<MudProgressLinear>(target, "TorrentCreatorProgress-TaskId");
            progress.Instance.GetState(x => x.Value).Should().Be(100);
        }

        [Fact]
        public async Task GIVEN_FailedAndUnknownStatuses_WHEN_Rendered_THEN_ShowsStatuses()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[]
                {
                    CreateTask("TaskId1", "C:/Source", "Failed", 100),
                    CreateTask("TaskId2", "C:/Source", "Unknown", 10)
                });

            var target = RenderPage();

            var failedStatus = FindComponentByTestId<MudChip<string>>(target, "TorrentCreatorStatus-TaskId1");
            failedStatus.Instance.Text.Should().Be("Failed");

            var unknownStatus = FindComponentByTestId<MudChip<string>>(target, "TorrentCreatorStatus-TaskId2");
            unknownStatus.Instance.Text.Should().Be("Unknown");
        }

        [Fact]
        public void GIVEN_QueuedStatus_WHEN_Rendered_THEN_ShowsQueued()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[]
                {
                    CreateTask("TaskId", "C:/Source", "Queued", 0)
                });

            var target = RenderPage();

            var queuedStatus = FindComponentByTestId<MudChip<string>>(target, "TorrentCreatorStatus-TaskId");
            queuedStatus.Instance.Text.Should().Be("Queued");
        }

        [Fact]
        public void GIVEN_WhitespaceStatus_WHEN_Rendered_THEN_ShowsEmptyStatusText()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[]
                {
                    CreateTask("TaskId", "C:/Source", " ", 0)
                });

            var target = RenderPage();

            var status = FindComponentByTestId<MudChip<string>>(target, "TorrentCreatorStatus-TaskId");
            status.Instance.Text.Should().Be(string.Empty);
        }

        [Fact]
        public async Task GIVEN_SourcePathWhitespace_WHEN_Downloaded_THEN_FallsBackToTaskId()
        {
            var task = CreateTask("TaskId", "  ", "Finished", 100);
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { task });
            var downloadInvocation = TestContext.JSInterop.SetupVoid("qbt.triggerFileDownload", _ => true);
            downloadInvocation.SetVoidResult();

            var target = RenderPage();

            var downloadButton = FindIconButton(target, Icons.Material.Filled.Download);
            await target.InvokeAsync(() => downloadButton.Instance.OnClick.InvokeAsync());

            var arguments = downloadInvocation.Invocations
                .Select(invocation => invocation.Arguments.OfType<string>().ToList())
                .Single();
            arguments.Last().Should().Be("TaskId.torrent");
        }

        [Fact]
        public async Task GIVEN_SourcePathRootSeparator_WHEN_Downloaded_THEN_FallsBackToTaskId()
        {
            var task = CreateTask("TaskId", "/", "Finished", 100);
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { task });
            var downloadInvocation = TestContext.JSInterop.SetupVoid("qbt.triggerFileDownload", _ => true);
            downloadInvocation.SetVoidResult();

            var target = RenderPage();

            var downloadButton = FindIconButton(target, Icons.Material.Filled.Download);
            await target.InvokeAsync(() => downloadButton.Instance.OnClick.InvokeAsync());

            var arguments = downloadInvocation.Invocations
                .Select(invocation => invocation.Arguments.OfType<string>().ToList())
                .Single();
            arguments.Last().Should().Be("TaskId.torrent");
        }

        [Fact]
        public async Task GIVEN_SourcePathTrailingSpaceSegment_WHEN_Downloaded_THEN_FallsBackToTaskId()
        {
            var task = CreateTask("TaskId", "C:/ ", "Finished", 100);
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[] { task });
            var downloadInvocation = TestContext.JSInterop.SetupVoid("qbt.triggerFileDownload", _ => true);
            downloadInvocation.SetVoidResult();

            var target = RenderPage();

            var downloadButton = FindIconButton(target, Icons.Material.Filled.Download);
            await target.InvokeAsync(() => downloadButton.Instance.OnClick.InvokeAsync());

            var arguments = downloadInvocation.Invocations
                .Select(invocation => invocation.Arguments.OfType<string>().ToList())
                .Single();
            arguments.Last().Should().Be("TaskId.torrent");
        }

        [Fact]
        public async Task GIVEN_TimerRunning_WHEN_Disposed_THEN_DisposesTimer()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetTorrentCreationTasks())
                .ReturnsAsync(new[]
                {
                    CreateTask("TaskId", "C:/Source", "Running", 10)
                });

            Mock.Get(_timer)
                .Setup(timer => timer.DisposeAsync())
                .Returns(ValueTask.CompletedTask);

            var target = RenderPage();

            await target.Instance.DisposeAsync();

            Mock.Get(_timer).Verify(timer => timer.DisposeAsync(), Times.Once);
        }

        private IRenderedComponent<TorrentCreator> RenderPage(MainData? mainData = null, bool includeMainData = true)
        {
            return TestContext.Render<TorrentCreator>(parameters =>
            {
                parameters.AddCascadingValue("DrawerOpen", false);
                if (includeMainData)
                {
                    parameters.AddCascadingValue(mainData ?? CreateMainData());
                }
            });
        }

        private Func<CancellationToken, Task<ManagedTimerTickResult>> CapturePollHandler()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            Mock.Get(_timer)
                .Setup(timer => timer.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            return cancellationToken => handler!.Invoke(cancellationToken);
        }

        private static ClientModels.TorrentCreationTaskStatus CreateTask(string taskId, string sourcePath, string status, double? progress)
        {
            return new ClientModels.TorrentCreationTaskStatus(
                taskId,
                sourcePath,
                null,
                false,
                "TimeAdded",
                "hybrid",
                true,
                null,
                status,
                null,
                null,
                null,
                Array.Empty<string>(),
                Array.Empty<string>(),
                null,
                null,
                null,
                progress);
        }

        private static MainData CreateMainData(bool lostConnection = false)
        {
            return new MainData(
                new Dictionary<string, Torrent>(),
                new List<string>(),
                new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                new Lantean.QBTMud.Models.ServerState { ConnectionStatus = lostConnection ? "Disconnected" : "Connected" },
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>())
            {
                LostConnection = lostConnection
            };
        }
    }
}
