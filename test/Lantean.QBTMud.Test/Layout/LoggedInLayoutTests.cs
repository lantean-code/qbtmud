using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Layout;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Text.Json;
using ClientModels = Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Test.Layout
{
    public sealed class LoggedInLayoutTests : RazorComponentTestBase<LoggedInLayout>
    {
        private const string PendingDownloadStorageKey = "LoggedInLayout.PendingDownload";
        private const string LastProcessedDownloadStorageKey = "LoggedInLayout.LastProcessedDownload";
        private const string WelcomeWizardCompletedStorageKey = "WelcomeWizard.Completed.v1";

        private readonly IApiClient _apiClient;
        private readonly Mock<IApiClient> _apiClientMock;
        private readonly ITorrentDataManager _dataManager;
        private readonly Mock<ITorrentDataManager> _dataManagerMock;
        private readonly ISpeedHistoryService _speedHistoryService;
        private readonly Mock<ISpeedHistoryService> _speedHistoryServiceMock;
        private readonly IManagedTimerFactory _managedTimerFactory;
        private readonly Mock<IManagedTimerFactory> _managedTimerFactoryMock;
        private readonly IManagedTimerRegistry _timerRegistry;
        private readonly Mock<IManagedTimerRegistry> _timerRegistryMock;
        private readonly IManagedTimer _refreshTimer;
        private readonly Mock<IManagedTimer> _refreshTimerMock;
        private readonly IDialogWorkflow _dialogWorkflow;
        private readonly Mock<IDialogWorkflow> _dialogWorkflowMock;
        private readonly IDialogService _dialogService;
        private readonly Mock<IDialogService> _dialogServiceMock;
        private readonly ISnackbar _snackbar;
        private readonly Mock<ISnackbar> _snackbarMock;
        private readonly TestNavigationManager _navigationManager;
        private readonly IRenderedComponent<LoggedInLayout> _target;

        public LoggedInLayoutTests()
        {
            _navigationManager = new TestNavigationManager();
            TestContext.Services.RemoveAll(typeof(NavigationManager));
            TestContext.Services.AddSingleton<NavigationManager>(_navigationManager);

            _apiClient = Mock.Of<IApiClient>();
            _apiClientMock = Mock.Get(_apiClient);
            _apiClientMock.Setup(c => c.CheckAuthState()).ReturnsAsync(true);
            _apiClientMock.Setup(c => c.GetApplicationPreferences()).ReturnsAsync(CreatePreferences());
            _apiClientMock.Setup(c => c.GetApplicationVersion()).ReturnsAsync("Version");
            _apiClientMock.Setup(c => c.GetMainData(It.IsAny<int>())).ReturnsAsync(CreateClientMainData());

            _dataManager = Mock.Of<ITorrentDataManager>();
            _dataManagerMock = Mock.Get(_dataManager);
            _dataManagerMock.Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(CreateMainData());

            _speedHistoryService = Mock.Of<ISpeedHistoryService>();
            _speedHistoryServiceMock = Mock.Get(_speedHistoryService);
            _speedHistoryServiceMock.Setup(s => s.InitializeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _speedHistoryServiceMock.Setup(s => s.PushSampleAsync(It.IsAny<DateTime>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _refreshTimer = Mock.Of<IManagedTimer>();
            _refreshTimerMock = Mock.Get(_refreshTimer);
            _refreshTimerMock.Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _refreshTimerMock.Setup(t => t.UpdateIntervalAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            _managedTimerFactory = Mock.Of<IManagedTimerFactory>();
            _managedTimerFactoryMock = Mock.Get(_managedTimerFactory);
            _managedTimerFactoryMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(_refreshTimer);

            _timerRegistry = Mock.Of<IManagedTimerRegistry>();
            _timerRegistryMock = Mock.Get(_timerRegistry);
            _timerRegistryMock.Setup(r => r.GetTimers()).Returns(new List<IManagedTimer>());

            _dialogWorkflow = Mock.Of<IDialogWorkflow>();
            _dialogWorkflowMock = Mock.Get(_dialogWorkflow);

            _dialogService = Mock.Of<IDialogService>();
            _dialogServiceMock = Mock.Get(_dialogService);
            _dialogServiceMock
                .Setup(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()))
                .ReturnsAsync(Mock.Of<IDialogReference>(MockBehavior.Loose));
            _dialogServiceMock
                .Setup(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()))
                .ReturnsAsync(Mock.Of<IDialogReference>(MockBehavior.Loose));

            _snackbar = Mock.Of<ISnackbar>();
            _snackbarMock = Mock.Get(_snackbar);

            TestContext.Services.RemoveAll(typeof(IApiClient));
            TestContext.Services.RemoveAll(typeof(ITorrentDataManager));
            TestContext.Services.RemoveAll(typeof(ISpeedHistoryService));
            TestContext.Services.RemoveAll(typeof(IManagedTimerFactory));
            TestContext.Services.RemoveAll(typeof(IManagedTimerRegistry));
            TestContext.Services.RemoveAll(typeof(IDialogWorkflow));
            TestContext.Services.RemoveAll(typeof(IDialogService));
            TestContext.Services.RemoveAll(typeof(ISnackbar));
            TestContext.Services.AddSingleton(_apiClient);
            TestContext.Services.AddSingleton(_dataManager);
            TestContext.Services.AddSingleton(_speedHistoryService);
            TestContext.Services.AddSingleton(_managedTimerFactory);
            TestContext.Services.AddSingleton(_timerRegistry);
            TestContext.Services.AddSingleton(_dialogWorkflow);
            TestContext.Services.AddSingleton(_dialogService);
            TestContext.Services.AddSingleton(_snackbar);

            _target = RenderLayout(new List<IManagedTimer>());
        }

        [Fact]
        public void GIVEN_WelcomeWizardIncomplete_WHEN_Rendered_THEN_ShowsWizardDialog()
        {
            DisposeDefaultTarget();
            _dialogServiceMock.Invocations.Clear();

            TestContext.LocalStorage.RemoveItemAsync(WelcomeWizardCompletedStorageKey, Xunit.TestContext.Current.CancellationToken);

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() =>
            {
                _dialogServiceMock.Verify(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()), Times.Once);
            });
        }

        [Fact]
        public void GIVEN_WelcomeWizardCompleted_WHEN_Rendered_THEN_DoesNotShowWizardDialog()
        {
            DisposeDefaultTarget();
            _dialogServiceMock.Invocations.Clear();

            TestContext.LocalStorage.SetItemAsync(WelcomeWizardCompletedStorageKey, true, Xunit.TestContext.Current.CancellationToken);

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() =>
            {
                _dialogServiceMock.Verify(service => service.ShowAsync<WelcomeWizardDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()), Times.Never);
            });
        }

        [Fact]
        public void GIVEN_NoTimers_WHEN_LoggedInLayoutRendered_THEN_TimerIconShowsDefault()
        {
            var button = FindTimerButton(_target, Icons.Material.Filled.TimerOff);

            button.Instance.Color.Should().Be(Color.Default);
        }

        [Fact]
        public void GIVEN_AllTimersRunning_WHEN_LoggedInLayoutRendered_THEN_TimerIconShowsSuccess()
        {
            var timers = new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Running)
            };

            var target = RenderLayout(timers);

            var button = FindTimerButton(target, Icons.Material.Filled.Timer);

            button.Instance.Color.Should().Be(Color.Success);
        }

        [Fact]
        public void GIVEN_PausedTimerPresent_WHEN_LoggedInLayoutRendered_THEN_TimerIconShowsWarning()
        {
            var timers = new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Paused)
            };

            var target = RenderLayout(timers);

            var button = FindTimerButton(target, Icons.Material.Filled.PauseCircle);

            button.Instance.Color.Should().Be(Color.Warning);
        }

        [Fact]
        public void GIVEN_FaultedTimerPresent_WHEN_LoggedInLayoutRendered_THEN_TimerIconShowsError()
        {
            var timers = new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Faulted)
            };

            var target = RenderLayout(timers);

            var button = FindTimerButton(target, Icons.Material.Filled.Error);

            button.Instance.Color.Should().Be(Color.Error);
        }

        [Fact]
        public void GIVEN_NotAllTimersRunning_WHEN_LoggedInLayoutRendered_THEN_TimerIconShowsDefault()
        {
            var timers = new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Stopped)
            };

            var target = RenderLayout(timers);

            var button = FindTimerButton(target, Icons.Material.Filled.TimerOff);

            button.Instance.Color.Should().Be(Color.Default);
        }

        [Fact]
        public void GIVEN_UnknownTimerState_WHEN_LoggedInLayoutRendered_THEN_TimerIconShowsDefault()
        {
            var timers = new List<IManagedTimer>
            {
                CreateTimer((ManagedTimerState)42)
            };

            var target = RenderLayout(timers);

            var button = FindTimerButton(target, Icons.Material.Filled.TimerOff);

            button.Instance.Color.Should().Be(Color.Default);
        }

        [Fact]
        public void GIVEN_NoTimers_WHEN_Rendered_THEN_TimerTooltipShowsEmptyMessage()
        {
            var tooltip = FindTimerTooltip(_target);

            tooltip.Instance.Text.Should().Be("No timers registered.");
        }

        [Fact]
        public void GIVEN_TimersPresent_WHEN_Rendered_THEN_TimerTooltipShowsCounts()
        {
            var timers = new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Paused),
                CreateTimer(ManagedTimerState.Stopped),
                CreateTimer(ManagedTimerState.Faulted)
            };

            var target = RenderLayout(timers);

            var tooltip = FindTimerTooltip(target);

            tooltip.Instance.Text.Should().Be("Timers: 1 running, 1 paused, 1 stopped, 1 faulted");
        }

        [Fact]
        public async Task GIVEN_TimerDrawerClosed_WHEN_TimerIconClicked_THEN_CallbackInvoked()
        {
            var opened = false;
            var callback = EventCallback.Factory.Create<bool>(this, value => opened = value);

            var target = RenderLayout(new List<IManagedTimer>(), timerDrawerOpen: false, timerDrawerOpenChanged: callback);
            var button = FindTimerButton(target, Icons.Material.Filled.TimerOff);

            await target.InvokeAsync(() => button.Find("button").Click());

            opened.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NoTimerDrawerCallback_WHEN_TimerIconClicked_THEN_TimerDrawerStateToggles()
        {
            var target = RenderLayout(new List<IManagedTimer>(), timerDrawerOpen: false);
            var button = FindTimerButton(target, Icons.Material.Filled.TimerOff);

            await target.InvokeAsync(() => button.Find("button").Click());

            target.Instance.TimerDrawerOpen.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_Unauthenticated_WHEN_Initialized_THEN_NavigatesToLoginAndShowsProgress()
        {
            DisposeDefaultTarget();
            await TestContext.SessionStorage.SetItemAsync(PendingDownloadStorageKey, "magnet:?xt=urn:btih:ABC", Xunit.TestContext.Current.CancellationToken);

            _apiClientMock.Setup(c => c.CheckAuthState()).ReturnsAsync(false);

            var target = RenderLayout(new List<IManagedTimer>());

            _navigationManager.LastNavigationUri.Should().Be("login");
            target.FindComponent<MudProgressLinear>().Should().NotBeNull();
            var pending = await TestContext.SessionStorage.GetItemAsync<string>(PendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            pending.Should().BeNull();
        }

        [Fact]
        public void GIVEN_LostConnection_WHEN_Rendered_THEN_ShowsLostConnectionDialog()
        {
            _dialogServiceMock.Invocations.Clear();
            var mainData = CreateMainData(lostConnection: true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);

            target.WaitForAssertion(() =>
            {
                _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                    It.IsAny<string?>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions?>()), Times.Once);
            });
        }

        [Fact]
        public void GIVEN_StatusLabelsEnabled_WHEN_Rendered_THEN_ShowsLabelText()
        {
            var mainData = CreateMainData(serverState: CreateServerState());

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, breakpoint: Breakpoint.Lg, orientation: Orientation.Portrait);

            var freeSpace = FindComponentByTestId<MudText>(target, "Status-FreeSpace");
            GetChildContentText(freeSpace.Instance.ChildContent).Should().StartWith("Free space: ");

            var dhtNodes = FindComponentByTestId<MudText>(target, "Status-DhtNodes");
            GetChildContentText(dhtNodes.Instance.ChildContent).Should().Contain("nodes");
        }

        [Fact]
        public void GIVEN_StatusLabelsDisabled_WHEN_Rendered_THEN_OmitsLabelText()
        {
            var mainData = CreateMainData(serverState: CreateServerState());

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, breakpoint: Breakpoint.Sm, orientation: Orientation.Landscape);

            var freeSpace = FindComponentByTestId<MudText>(target, "Status-FreeSpace");
            GetChildContentText(freeSpace.Instance.ChildContent).Should().NotStartWith("Free space: ");

            var dhtNodes = FindComponentByTestId<MudText>(target, "Status-DhtNodes");
            GetChildContentText(dhtNodes.Instance.ChildContent).Should().NotContain("nodes");
        }

        [Fact]
        public void GIVEN_StatusLabelsWithLandscapeMd_WHEN_Rendered_THEN_ShowsLabelText()
        {
            var mainData = CreateMainData(serverState: CreateServerState());

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, breakpoint: Breakpoint.Md, orientation: Orientation.Landscape);

            var freeSpace = FindComponentByTestId<MudText>(target, "Status-FreeSpace");
            GetChildContentText(freeSpace.Instance.ChildContent).Should().StartWith("Free space: ");

            var dhtNodes = FindComponentByTestId<MudText>(target, "Status-DhtNodes");
            GetChildContentText(dhtNodes.Instance.ChildContent).Should().Contain("nodes");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndNoAddresses_WHEN_Rendered_THEN_ShowsNotAvailableLabel()
        {
            var mainData = CreateMainData(serverState: CreateServerState(v4: string.Empty, v6: string.Empty));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(true), breakpoint: Breakpoint.Lg, orientation: Orientation.Portrait);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");
            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("External IP: N/A");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledWithDualAddresses_WHEN_Rendered_THEN_ShowsBothLabels()
        {
            var mainData = CreateMainData(serverState: CreateServerState(v4: "1.1.1.1", v6: "2.2.2.2"));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(true), breakpoint: Breakpoint.Lg, orientation: Orientation.Portrait);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");
            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("External IPs: 1.1.1.1, 2.2.2.2");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledWithSingleAddress_WHEN_Rendered_THEN_ShowsSingleLabel()
        {
            var mainData = CreateMainData(serverState: CreateServerState(v4: "1.1.1.1", v6: string.Empty));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(true), breakpoint: Breakpoint.Lg, orientation: Orientation.Portrait);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");
            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("External IP: 1.1.1.1");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndNoAddressesWithLabelsHidden_WHEN_Rendered_THEN_HidesExternalIp()
        {
            var mainData = CreateMainData(serverState: CreateServerState(v4: string.Empty, v6: string.Empty));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(true), breakpoint: Breakpoint.Sm, orientation: Orientation.Landscape);

            HasComponentWithTestId<MudText>(target, "Status-ExternalIp").Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndDualAddressesWithLabelsHidden_WHEN_Rendered_THEN_ShowsCombinedValue()
        {
            var mainData = CreateMainData(serverState: CreateServerState(v4: "1.1.1.1", v6: "2.2.2.2"));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(true), breakpoint: Breakpoint.Sm, orientation: Orientation.Landscape);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");
            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("1.1.1.1, 2.2.2.2");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndSingleAddressWithLabelsHidden_WHEN_Rendered_THEN_ShowsSingleValue()
        {
            var mainData = CreateMainData(serverState: CreateServerState(v4: string.Empty, v6: "2.2.2.2"));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, preferences: CreatePreferences(true), breakpoint: Breakpoint.Sm, orientation: Orientation.Landscape);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");
            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("2.2.2.2");
        }

        [Fact]
        public void GIVEN_ConnectionStatusFirewalled_WHEN_Rendered_THEN_ShowsWarningIcon()
        {
            var mainData = CreateMainData(serverState: CreateServerState(connectionStatus: "firewalled"));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);

            var tooltip = FindComponentByTestId<MudTooltip>(target, "Status-ConnectionTooltip");
            tooltip.Instance.Text.Should().Be("Connection status: Firewalled");

            var icon = FindComponentByTestId<MudIcon>(target, "Status-ConnectionIcon");
            icon.Instance.Icon.Should().Be(Icons.Material.Outlined.SignalWifiStatusbarConnectedNoInternet4);
            icon.Instance.Color.Should().Be(Color.Warning);
        }

        [Fact]
        public void GIVEN_ConnectionStatusConnected_WHEN_Rendered_THEN_ShowsSuccessIcon()
        {
            var mainData = CreateMainData(serverState: CreateServerState(connectionStatus: "connected"));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);

            var tooltip = FindComponentByTestId<MudTooltip>(target, "Status-ConnectionTooltip");
            tooltip.Instance.Text.Should().Be("Connection status: Connected");

            var icon = FindComponentByTestId<MudIcon>(target, "Status-ConnectionIcon");
            icon.Instance.Icon.Should().Be(Icons.Material.Outlined.SignalWifi4Bar);
            icon.Instance.Color.Should().Be(Color.Success);
        }

        [Fact]
        public void GIVEN_ConnectionStatusUnknown_WHEN_Rendered_THEN_ShowsErrorIcon()
        {
            var mainData = CreateMainData(serverState: CreateServerState(connectionStatus: "offline"));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);

            var tooltip = FindComponentByTestId<MudTooltip>(target, "Status-ConnectionTooltip");
            tooltip.Instance.Text.Should().Be("offline");

            var icon = FindComponentByTestId<MudIcon>(target, "Status-ConnectionIcon");
            icon.Instance.Icon.Should().Be(Icons.Material.Outlined.SignalWifiOff);
            icon.Instance.Color.Should().Be(Color.Error);
        }

        [Fact]
        public async Task GIVEN_FilterCallbacks_WHEN_Invoked_THEN_StateUpdatesAndVersionChanges()
        {
            var torrents = new List<Torrent>
            {
                CreateTorrent("Hash1", "Alpha", "Cat1", new[] { "Tag1" }, "Tracker1", "downloading"),
                CreateTorrent("Hash2", "Beta", "Cat2", Array.Empty<string>(), "Tracker2", "pausedUP")
            };
            var mainData = CreateMainData(torrents: torrents, serverState: CreateServerState());

            var target = RenderLayoutWithProbe(mainData);
            var probe = target.FindComponent<LayoutProbe>();
            var initialVersion = probe.Instance.TorrentsVersion;

            await target.InvokeAsync(() => probe.Instance.CategoryChanged.InvokeAsync(FilterHelper.CATEGORY_ALL));
            await target.InvokeAsync(() => probe.Instance.StatusChanged.InvokeAsync(Status.All));
            await target.InvokeAsync(() => probe.Instance.TagChanged.InvokeAsync(FilterHelper.TAG_ALL));
            await target.InvokeAsync(() => probe.Instance.TrackerChanged.InvokeAsync(FilterHelper.TRACKER_ALL));
            await target.InvokeAsync(() => probe.Instance.SearchTermChanged.InvokeAsync(new FilterSearchState(null, TorrentFilterField.Name, false, true)));

            await target.InvokeAsync(() => probe.Instance.CategoryChanged.InvokeAsync("Cat1"));
            await target.InvokeAsync(() => probe.Instance.StatusChanged.InvokeAsync(Status.Downloading));
            await target.InvokeAsync(() => probe.Instance.TagChanged.InvokeAsync("Tag1"));
            await target.InvokeAsync(() => probe.Instance.TrackerChanged.InvokeAsync("Tracker1"));
            await target.InvokeAsync(() => probe.Instance.SearchTermChanged.InvokeAsync(new FilterSearchState("Alpha", TorrentFilterField.Name, false, true)));
            await target.InvokeAsync(() => probe.Instance.SortColumnChanged.InvokeAsync("Name"));
            await target.InvokeAsync(() => probe.Instance.SortDirectionChanged.InvokeAsync(SortDirection.Descending));

            target.WaitForAssertion(() =>
            {
                probe.Instance.TorrentsVersion.Should().BeGreaterThan(initialVersion);
            });

            probe.Instance.SortColumn.Should().Be("Name");
            probe.Instance.SortDirection.Should().Be(SortDirection.Descending);
        }

        [Fact]
        public async Task GIVEN_AlternativeSpeedLimitToggleSucceeds_WHEN_Enabled_THEN_UpdatesStateAndShowsSnackbar()
        {
            var mainData = CreateMainData(serverState: CreateServerState(useAltSpeedLimits: false));
            _snackbarMock.Invocations.Clear();

            _apiClientMock.Setup(c => c.ToggleAlternativeSpeedLimits()).Returns(Task.CompletedTask);
            _apiClientMock.Setup(c => c.GetAlternativeSpeedLimitsState()).ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var button = target.FindComponents<MudIconButton>().Single(i => i.Instance.Icon == Icons.Material.Outlined.Speed);

            await target.InvokeAsync(() => button.Find("button").Click());

            mainData.ServerState.UseAltSpeedLimits.Should().BeTrue();
            _snackbarMock.Invocations.Single(i => i.Method.Name == nameof(ISnackbar.Add)).Arguments[0].Should().Be("Alternative speed limits: On");
        }

        [Fact]
        public async Task GIVEN_AlternativeSpeedLimitToggleSucceeds_WHEN_Disabled_THEN_UpdatesStateAndShowsSnackbar()
        {
            var mainData = CreateMainData(serverState: CreateServerState(useAltSpeedLimits: true));
            _snackbarMock.Invocations.Clear();

            _apiClientMock.Setup(c => c.ToggleAlternativeSpeedLimits()).Returns(Task.CompletedTask);
            _apiClientMock.Setup(c => c.GetAlternativeSpeedLimitsState()).ReturnsAsync(false);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var button = target.FindComponents<MudIconButton>().Single(i => i.Instance.Icon == Icons.Material.Outlined.Speed);

            await target.InvokeAsync(() => button.Find("button").Click());

            mainData.ServerState.UseAltSpeedLimits.Should().BeFalse();
            _snackbarMock.Invocations.Single(i => i.Method.Name == nameof(ISnackbar.Add)).Arguments[0].Should().Be("Alternative speed limits: Off");
        }

        [Fact]
        public async Task GIVEN_AlternativeSpeedLimitToggleFails_WHEN_Clicked_THEN_ShowsErrorSnackbar()
        {
            var mainData = CreateMainData(serverState: CreateServerState(useAltSpeedLimits: false));
            _snackbarMock.Invocations.Clear();

            _apiClientMock.Setup(c => c.ToggleAlternativeSpeedLimits()).ThrowsAsync(new HttpRequestException("Fail"));

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var button = target.FindComponents<MudIconButton>().Single(i => i.Instance.Icon == Icons.Material.Outlined.Speed);

            await target.InvokeAsync(() => button.Find("button").Click());

            _snackbarMock.Invocations.Single(i => i.Method.Name == nameof(ISnackbar.Add)).Arguments[0].Should().Be("Unable to toggle alternative speed limits: Fail");
        }

        [Fact]
        public async Task GIVEN_AlternativeSpeedLimitToggleInProgress_WHEN_ClickedAgain_THEN_IgnoresSecondRequest()
        {
            var mainData = CreateMainData(serverState: CreateServerState(useAltSpeedLimits: false));
            var toggleTaskSource = new TaskCompletionSource<bool>();
            _snackbarMock.Invocations.Clear();

            _apiClientMock.Setup(c => c.ToggleAlternativeSpeedLimits()).Returns(toggleTaskSource.Task);
            _apiClientMock.Setup(c => c.GetAlternativeSpeedLimitsState()).ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData);
            var button = target.FindComponents<MudIconButton>().Single(i => i.Instance.Icon == Icons.Material.Outlined.Speed);

            var firstClick = target.InvokeAsync(() => button.Find("button").Click());
            await target.InvokeAsync(() => button.Find("button").Click());

            _apiClientMock.Verify(c => c.ToggleAlternativeSpeedLimits(), Times.Once);

            toggleTaskSource.SetResult(true);
            await firstClick;
        }

        [Fact]
        public void GIVEN_RefreshIntervalZero_WHEN_Initialized_THEN_DoesNotUpdateInterval()
        {
            var mainData = CreateMainData(serverState: CreateServerState());
            mainData.ServerState.RefreshInterval = 0;
            _refreshTimerMock.Invocations.Clear();

            RenderLayout(new List<IManagedTimer>(), mainData: mainData);

            _refreshTimerMock.Verify(t => t.UpdateIntervalAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_RefreshTickBeforeAuth_WHEN_Ticked_THEN_ReturnsStop()
        {
            var tickSource = new TaskCompletionSource<bool>();
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;

            _apiClientMock.Setup(c => c.CheckAuthState()).Returns(tickSource.Task);

            _refreshTimerMock
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() => handler.Should().NotBeNull());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Stop);

            tickSource.SetResult(false);
        }

        [Fact]
        public async Task GIVEN_RefreshTickThrows_WHEN_Ticked_THEN_LostConnectionSetAndStops()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var probeBody = CreateProbeBody();
            var mainData = CreateMainData(serverState: CreateServerState());
            _dialogServiceMock.Invocations.Clear();

            _apiClientMock.SetupSequence(c => c.GetMainData(It.IsAny<int>()))
                .ReturnsAsync(CreateClientMainData())
                .ThrowsAsync(new HttpRequestException());

            _dataManagerMock.Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);

            _refreshTimerMock
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, body: probeBody);
            var probe = target.FindComponent<LayoutProbe>();

            target.WaitForAssertion(() => handler.Should().NotBeNull());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Stop);
            target.WaitForAssertion(() => probe.Instance.MainData!.LostConnection.Should().BeTrue());
            _dialogServiceMock.Verify(service => service.ShowAsync<LostConnectionDialog>(
                It.IsAny<string?>(),
                It.IsAny<DialogParameters>(),
                It.IsAny<DialogOptions?>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_RefreshTickFullUpdate_WHEN_Ticked_THEN_RecreatesMainData()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var probeBody = CreateProbeBody();
            var initialData = CreateMainData(serverState: CreateServerState());
            var updatedData = CreateMainData(serverState: CreateServerState(connectionStatus: "connected"));

            _apiClientMock.SetupSequence(c => c.GetMainData(It.IsAny<int>()))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false))
                .ReturnsAsync(CreateClientMainData(fullUpdate: true));

            _dataManagerMock.SetupSequence(m => m.CreateMainData(It.IsAny<ClientModels.MainData>()))
                .Returns(initialData)
                .Returns(updatedData);

            _refreshTimerMock
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: initialData, body: probeBody, configureMainData: false);
            var probe = target.FindComponent<LayoutProbe>();
            var initialVersion = probe.Instance.TorrentsVersion;

            target.WaitForAssertion(() => handler.Should().NotBeNull());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Continue);

            target.Render();
            probe.Instance.TorrentsVersion.Should().BeGreaterThan(initialVersion);
        }

        [Fact]
        public async Task GIVEN_RefreshTickMergeFilterChanged_WHEN_Ticked_THEN_MarksDirty()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var probeBody = CreateProbeBody();
            var mainData = CreateMainData(serverState: CreateServerState());
            var filterChanged = true;

            _apiClientMock.SetupSequence(c => c.GetMainData(It.IsAny<int>()))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false));

            _dataManagerMock.Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);
            _dataManagerMock.Setup(m => m.MergeMainData(It.IsAny<ClientModels.MainData>(), mainData, out filterChanged)).Returns(false);

            _refreshTimerMock
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, body: probeBody);
            var probe = target.FindComponent<LayoutProbe>();
            var initialVersion = probe.Instance.TorrentsVersion;

            target.WaitForAssertion(() => handler.Should().NotBeNull());
            target.WaitForAssertion(() => target.FindComponents<MudProgressLinear>().Should().BeEmpty());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Continue);
            target.Render();
            probe.Instance.TorrentsVersion.Should().BeGreaterThan(initialVersion);
        }

        [Fact]
        public async Task GIVEN_RefreshTickMergeDataChanged_WHEN_Ticked_THEN_IncrementsVersion()
        {
            Func<CancellationToken, Task<ManagedTimerTickResult>>? handler = null;
            var probeBody = CreateProbeBody();
            var mainData = CreateMainData(serverState: CreateServerState());
            var filterChanged = false;

            _apiClientMock.SetupSequence(c => c.GetMainData(It.IsAny<int>()))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false))
                .ReturnsAsync(CreateClientMainData(fullUpdate: false));

            _dataManagerMock.Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData);
            _dataManagerMock.Setup(m => m.MergeMainData(It.IsAny<ClientModels.MainData>(), mainData, out filterChanged)).Returns(true);

            _refreshTimerMock
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<CancellationToken, Task<ManagedTimerTickResult>>, CancellationToken>((callback, _) => handler = callback)
                .ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>(), mainData: mainData, body: probeBody);
            var probe = target.FindComponent<LayoutProbe>();
            var initialVersion = probe.Instance.TorrentsVersion;

            target.WaitForAssertion(() => handler.Should().NotBeNull());
            target.WaitForAssertion(() => target.FindComponents<MudProgressLinear>().Should().BeEmpty());

            var result = await handler!(CancellationToken.None);

            result.Action.Should().Be(ManagedTimerTickAction.Continue);
            target.Render();
            probe.Instance.TorrentsVersion.Should().BeGreaterThan(initialVersion);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("magnet:?dn=missing")]
        [InlineData("http://example.com/file.torrent\n")]
        [InlineData("not a uri")]
        [InlineData("ftp://example.com/file.torrent")]
        [InlineData("http://example.com/file.txt")]
        public async Task GIVEN_InvalidPendingDownload_WHEN_Initialized_THEN_Removed(string pending)
        {
            await TestContext.SessionStorage.SetItemAsync(PendingDownloadStorageKey, pending, Xunit.TestContext.Current.CancellationToken);

            RenderLayout(new List<IManagedTimer>());

            var stored = await TestContext.SessionStorage.GetItemAsync<string>(PendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);

            stored.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_PendingDownloadTooLong_WHEN_Initialized_THEN_Removed()
        {
            var pending = new string('a', 8200);
            await TestContext.SessionStorage.SetItemAsync(PendingDownloadStorageKey, pending, Xunit.TestContext.Current.CancellationToken);

            RenderLayout(new List<IManagedTimer>());

            var stored = await TestContext.SessionStorage.GetItemAsync<string>(PendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);

            stored.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ValidPendingMagnet_WHEN_Initialized_THEN_InvokesDialogWorkflow()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            var magnet = "magnet:?xt=urn:btih:ABC";
            await TestContext.SessionStorage.SetItemAsync(PendingDownloadStorageKey, magnet, Xunit.TestContext.Current.CancellationToken);

            _dialogWorkflowMock.Setup(d => d.InvokeAddTorrentLinkDialog(magnet)).Returns(Task.CompletedTask);

            RenderLayout(new List<IManagedTimer>());

            _dialogWorkflowMock.Verify(d => d.InvokeAddTorrentLinkDialog(magnet), Times.Once);
            var pending = await TestContext.SessionStorage.GetItemAsync<string>(PendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            var lastProcessed = await TestContext.SessionStorage.GetItemAsync<string>(LastProcessedDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            pending.Should().BeNull();
            lastProcessed.Should().Be(magnet);
        }

        [Fact]
        public async Task GIVEN_ValidPendingTorrentLink_WHEN_Initialized_THEN_InvokesDialogWorkflow()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            var link = "http://example.com/file.torrent";
            await TestContext.SessionStorage.SetItemAsync(PendingDownloadStorageKey, link, Xunit.TestContext.Current.CancellationToken);

            _dialogWorkflowMock.Setup(d => d.InvokeAddTorrentLinkDialog(link)).Returns(Task.CompletedTask);

            RenderLayout(new List<IManagedTimer>());

            _dialogWorkflowMock.Verify(d => d.InvokeAddTorrentLinkDialog(link), Times.Once);
            var pending = await TestContext.SessionStorage.GetItemAsync<string>(PendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            var lastProcessed = await TestContext.SessionStorage.GetItemAsync<string>(LastProcessedDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            pending.Should().BeNull();
            lastProcessed.Should().Be(link);
        }

        [Fact]
        public async Task GIVEN_PendingMatchesProcessed_WHEN_Initialized_THEN_ClearsPendingAndNavigates()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            var magnet = "magnet:?xt=urn:btih:ABC";
            await TestContext.SessionStorage.SetItemAsync(PendingDownloadStorageKey, magnet, Xunit.TestContext.Current.CancellationToken);
            await TestContext.SessionStorage.SetItemAsync(LastProcessedDownloadStorageKey, magnet, Xunit.TestContext.Current.CancellationToken);

            RenderLayout(new List<IManagedTimer>());

            _navigationManager.LastNavigationUri.Should().Be("./");
            _navigationManager.ForceLoad.Should().BeTrue();
            var pending = await TestContext.SessionStorage.GetItemAsync<string>(PendingDownloadStorageKey, Xunit.TestContext.Current.CancellationToken);
            pending.Should().BeNull();
        }

        [Fact]
        public void GIVEN_DownloadInFragment_WHEN_Initialized_THEN_InvokesDialogWorkflow()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/#download=magnet:?xt=urn:btih:ABC");
            _dialogWorkflowMock.Setup(d => d.InvokeAddTorrentLinkDialog("magnet:?xt=urn:btih:ABC")).Returns(Task.CompletedTask);

            RenderLayout(new List<IManagedTimer>());

            _dialogWorkflowMock.Verify(d => d.InvokeAddTorrentLinkDialog("magnet:?xt=urn:btih:ABC"), Times.Once);
        }

        [Fact]
        public void GIVEN_DownloadInQuery_WHEN_Initialized_THEN_InvokesDialogWorkflow()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/?download=http://example.com/file.torrent");
            _dialogWorkflowMock.Setup(d => d.InvokeAddTorrentLinkDialog("http://example.com/file.torrent")).Returns(Task.CompletedTask);

            RenderLayout(new List<IManagedTimer>());

            _dialogWorkflowMock.Verify(d => d.InvokeAddTorrentLinkDialog("http://example.com/file.torrent"), Times.Once);
        }

        [Fact]
        public void GIVEN_DownloadKeyWithoutValue_WHEN_Initialized_THEN_IgnoresDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/#download");

            RenderLayout(new List<IManagedTimer>());

            _dialogWorkflowMock.Verify(d => d.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_DownloadValueDecodedWhitespace_WHEN_Initialized_THEN_IgnoresDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetRawUri("http://localhost/?download=%20");

            RenderLayout(new List<IManagedTimer>());

            _dialogWorkflowMock.Verify(d => d.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_DownloadAlreadyProcessed_WHEN_Initialized_THEN_IgnoresDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            var magnet = "magnet:?xt=urn:btih:ABC";
            await TestContext.SessionStorage.SetItemAsync(LastProcessedDownloadStorageKey, magnet, Xunit.TestContext.Current.CancellationToken);
            _navigationManager.SetUri("http://localhost/#download=magnet:?xt=urn:btih:ABC");

            RenderLayout(new List<IManagedTimer>());

            _dialogWorkflowMock.Verify(d => d.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_InvalidDownloadValue_WHEN_Initialized_THEN_IgnoresDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/?download=http://example.com/file.txt");

            RenderLayout(new List<IManagedTimer>());

            _dialogWorkflowMock.Verify(d => d.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_FragmentDelimiterOnly_WHEN_Initialized_THEN_IgnoresDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/#");

            RenderLayout(new List<IManagedTimer>());

            _dialogWorkflowMock.Verify(d => d.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_QueryWithoutDownload_WHEN_Initialized_THEN_IgnoresDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _navigationManager.SetUri("http://localhost/?view=all");

            RenderLayout(new List<IManagedTimer>());

            _dialogWorkflowMock.Verify(d => d.InvokeAddTorrentLinkDialog(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void GIVEN_DialogWorkflowThrows_WHEN_Initialized_THEN_Throws()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            var magnet = "magnet:?xt=urn:btih:ABC";
            _navigationManager.SetUri($"http://localhost/#download={magnet}");
            _dialogWorkflowMock.Setup(d => d.InvokeAddTorrentLinkDialog(magnet))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            Action action = () => RenderLayout(new List<IManagedTimer>());

            action.Should().Throw<Exception>();
        }

        [Fact]
        public void GIVEN_NavigationAfterAuth_WHEN_LocationChanges_THEN_ProcessesDownload()
        {
            DisposeDefaultTarget();
            ResetDialogInvocations();
            _dialogWorkflowMock.Setup(d => d.InvokeAddTorrentLinkDialog("http://example.com/file.torrent")).Returns(Task.CompletedTask);

            var target = RenderLayout(new List<IManagedTimer>());

            target.WaitForAssertion(() => target.FindComponent<MudAppBar>().Should().NotBeNull());

            target.InvokeAsync(() => _navigationManager.NavigateTo("http://localhost/?download=http://example.com/file.torrent"));

            _dialogWorkflowMock.Verify(d => d.InvokeAddTorrentLinkDialog("http://example.com/file.torrent"), Times.AtLeastOnce);
        }

        [Fact]
        public void GIVEN_MenuProvided_WHEN_Initialized_THEN_MenuShown()
        {
            var menu = TestContext.Render<Menu>();
            var target = RenderLayout(new List<IManagedTimer>(), menu: menu.Instance);

            target.WaitForAssertion(() => menu.FindComponents<MudMenu>().Should().NotBeEmpty());
        }

        [Fact]
        public async Task GIVEN_DefaultRender_WHEN_Disposed_THEN_Disposes()
        {
            await _target.Instance.DisposeAsync();

            await _target.Instance.DisposeAsync();
        }

        [Fact]
        public async Task GIVEN_DisposedLayout_WHEN_Disposed_THEN_DisposesRefreshTimer()
        {
            DisposeDefaultTarget();
            var timer = new Mock<IManagedTimer>();
            timer.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
            _managedTimerFactoryMock.Setup(f => f.Create(It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns(timer.Object);

            var target = RenderLayout(new List<IManagedTimer>());

            await target.Instance.DisposeAsync();

            timer.Verify(t => t.DisposeAsync(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_ToggleInProgress_WHEN_ClickedAgain_THEN_DoesNotCallApiTwice()
        {
            DisposeDefaultTarget();
            var completionSource = new TaskCompletionSource<bool>();
            _apiClientMock.Setup(c => c.ToggleAlternativeSpeedLimits()).Returns(completionSource.Task);
            _apiClientMock.Setup(c => c.GetAlternativeSpeedLimitsState()).ReturnsAsync(true);

            var target = RenderLayout(new List<IManagedTimer>());
            var button = target.FindComponents<MudIconButton>()
                .Single(component => component.Instance.Icon == Icons.Material.Outlined.Speed);

            var firstClick = button.Find("button").TriggerEventAsync("onclick", new MouseEventArgs());

            target.WaitForAssertion(() => _apiClientMock.Verify(c => c.ToggleAlternativeSpeedLimits(), Times.Once));

            await target.InvokeAsync(() => button.Instance.OnClick.InvokeAsync(null));

            _apiClientMock.Verify(c => c.ToggleAlternativeSpeedLimits(), Times.Once);

            completionSource.SetResult(true);
            await firstClick;
        }

        private IRenderedComponent<LoggedInLayout> RenderLayout(
            IReadOnlyList<IManagedTimer> timers,
            MainData? mainData = null,
            ClientModels.Preferences? preferences = null,
            Breakpoint breakpoint = Breakpoint.Lg,
            Orientation orientation = Orientation.Landscape,
            bool isDarkMode = false,
            bool timerDrawerOpen = false,
            EventCallback<bool>? timerDrawerOpenChanged = null,
            RenderFragment? body = null,
            Menu? menu = null,
            bool configureMainData = true)
        {
            _timerRegistryMock.Setup(r => r.GetTimers()).Returns(timers);
            if (configureMainData)
            {
                _dataManagerMock.Setup(m => m.CreateMainData(It.IsAny<ClientModels.MainData>())).Returns(mainData ?? CreateMainData());
            }
            _apiClientMock.Setup(c => c.GetApplicationPreferences()).ReturnsAsync(preferences ?? CreatePreferences());

            return TestContext.Render<LoggedInLayout>(parameters =>
            {
                parameters.Add(p => p.Body, body ?? (builder => { }));
                parameters.AddCascadingValue(breakpoint);
                parameters.AddCascadingValue(orientation);
                parameters.AddCascadingValue("DrawerOpen", false);
                parameters.AddCascadingValue("TimerDrawerOpen", timerDrawerOpen);
                if (timerDrawerOpenChanged.HasValue)
                {
                    parameters.AddCascadingValue("TimerDrawerOpenChanged", timerDrawerOpenChanged.Value);
                }
                parameters.AddCascadingValue("IsDarkMode", isDarkMode);
                if (menu is not null)
                {
                    parameters.AddCascadingValue(menu);
                }
            });
        }

        private IRenderedComponent<LoggedInLayout> RenderLayoutWithProbe(MainData mainData)
        {
            var body = CreateProbeBody();
            return RenderLayout(new List<IManagedTimer>(), mainData: mainData, body: body);
        }

        private static RenderFragment CreateProbeBody()
        {
            return builder =>
            {
                builder.OpenComponent<LayoutProbe>(0);
                builder.CloseComponent();
            };
        }

        private void DisposeDefaultTarget()
        {
            _target.Dispose();
        }

        private void ResetDialogInvocations()
        {
            _dialogWorkflowMock.Invocations.Clear();
        }

        private static IRenderedComponent<MudIconButton> FindTimerButton(IRenderedComponent<LoggedInLayout> target, string icon)
        {
            target.WaitForState(() => target.FindComponents<MudIconButton>().Any());

            return target.FindComponents<MudIconButton>().Single(button => button.Instance.Icon == icon);
        }

        private static IRenderedComponent<MudTooltip> FindTimerTooltip(IRenderedComponent<LoggedInLayout> target)
        {
            return target.FindComponents<MudTooltip>()
                .Single(t =>
                {
                    var text = t.Instance.Text ?? string.Empty;
                    return text == "No timers registered."
                        || text.StartsWith("Timers:", StringComparison.Ordinal);
                });
        }

        private static bool HasComponentWithTestId<TComponent>(IRenderedComponent<LoggedInLayout> target, string testId)
            where TComponent : MudComponentBase
        {
            var expected = TestIdHelper.For(testId);
            return target.FindComponents<TComponent>().Any(component =>
            {
                if (component.Instance.UserAttributes is null)
                {
                    return false;
                }

                return component.Instance.UserAttributes.TryGetValue("data-test-id", out var value)
                    && string.Equals(value?.ToString(), expected, StringComparison.Ordinal);
            });
        }

        private static IManagedTimer CreateTimer(ManagedTimerState state)
        {
            var timer = new Mock<IManagedTimer>();
            timer.SetupGet(t => t.State).Returns(state);
            timer.SetupGet(t => t.Name).Returns("Name");
            timer.SetupGet(t => t.Interval).Returns(TimeSpan.FromSeconds(1));
            return timer.Object;
        }

        private static MainData CreateMainData(
            IEnumerable<Torrent>? torrents = null,
            ServerState? serverState = null,
            bool lostConnection = false)
        {
            var torrentList = torrents?.ToDictionary(t => t.Hash, t => t) ?? new Dictionary<string, Torrent>();
            var data = new MainData(
                torrentList,
                Array.Empty<string>(),
                new Dictionary<string, Category>(),
                new Dictionary<string, IReadOnlyList<string>>(),
                serverState ?? CreateServerState(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>(),
                new Dictionary<string, HashSet<string>>());
            data.LostConnection = lostConnection;
            return data;
        }

        private static ServerState CreateServerState(
            string connectionStatus = "connected",
            string v4 = "1.1.1.1",
            string v6 = "2.2.2.2",
            bool useAltSpeedLimits = false)
        {
            return new ServerState
            {
                ConnectionStatus = connectionStatus,
                DHTNodes = 1,
                DownloadInfoData = 100,
                DownloadInfoSpeed = 10,
                DownloadRateLimit = 0,
                UploadInfoData = 200,
                UploadInfoSpeed = 20,
                UploadRateLimit = 100,
                FreeSpaceOnDisk = 1024,
                RefreshInterval = 1500,
                UseAltSpeedLimits = useAltSpeedLimits,
                LastExternalAddressV4 = v4,
                LastExternalAddressV6 = v6
            };
        }

        private static Torrent CreateTorrent(string hash, string name, string category, IReadOnlyCollection<string> tags, string tracker, string state)
        {
            return new Torrent(
                hash,
                addedOn: 0,
                amountLeft: 0,
                automaticTorrentManagement: false,
                aavailability: 0,
                category: category,
                completed: 0,
                completionOn: 0,
                contentPath: "ContentPath",
                downloadLimit: 0,
                downloadSpeed: 0,
                downloaded: 0,
                downloadedSession: 0,
                estimatedTimeOfArrival: 0,
                firstLastPiecePriority: false,
                forceStart: false,
                infoHashV1: "InfoHashV1",
                infoHashV2: "InfoHashV2",
                lastActivity: 0,
                magnetUri: "MagnetUri",
                maxRatio: 0,
                maxSeedingTime: 0,
                name: name,
                numberComplete: 0,
                numberIncomplete: 0,
                numberLeeches: 0,
                numberSeeds: 0,
                priority: 0,
                progress: 0,
                ratio: 0,
                ratioLimit: 0,
                savePath: "SavePath",
                seedingTime: 0,
                seedingTimeLimit: 0,
                seenComplete: 0,
                sequentialDownload: false,
                size: 0,
                state: state,
                superSeeding: false,
                tags: tags.ToArray(),
                timeActive: 0,
                totalSize: 0,
                tracker: tracker,
                trackersCount: 0,
                hasTrackerError: false,
                hasTrackerWarning: false,
                hasOtherAnnounceError: false,
                uploadLimit: 0,
                uploaded: 0,
                uploadedSession: 0,
                uploadSpeed: 0,
                reannounce: 0,
                inactiveSeedingTimeLimit: 0,
                maxInactiveSeedingTime: 0,
                popularity: 0,
                downloadPath: "DownloadPath",
                rootPath: "RootPath",
                isPrivate: false,
                shareLimitAction: Lantean.QBitTorrentClient.Models.ShareLimitAction.Default,
                comment: "Comment");
        }

        private static ClientModels.MainData CreateClientMainData(bool fullUpdate = true)
        {
            return new ClientModels.MainData(1, fullUpdate, null, null, null, null, null, null, null, null, null);
        }

        private static ClientModels.Preferences CreatePreferences(bool statusBarExternalIp = false)
        {
            var json = $"{{\"rss_processing_enabled\":false,\"status_bar_external_ip\":{statusBarExternalIp.ToString().ToLowerInvariant()}}}";
            return JsonSerializer.Deserialize<ClientModels.Preferences>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        private sealed class LayoutProbe : ComponentBase
        {
            [CascadingParameter]
            public IReadOnlyList<Torrent>? Torrents { get; set; }

            [CascadingParameter(Name = "TorrentsVersion")]
            public int TorrentsVersion { get; set; }

            [CascadingParameter]
            public MainData? MainData { get; set; }

            [CascadingParameter(Name = "SortColumn")]
            public string? SortColumn { get; set; }

            [CascadingParameter(Name = "SortDirection")]
            public SortDirection SortDirection { get; set; }

            [CascadingParameter(Name = "CategoryChanged")]
            public EventCallback<string> CategoryChanged { get; set; }

            [CascadingParameter(Name = "StatusChanged")]
            public EventCallback<Status> StatusChanged { get; set; }

            [CascadingParameter(Name = "TagChanged")]
            public EventCallback<string> TagChanged { get; set; }

            [CascadingParameter(Name = "TrackerChanged")]
            public EventCallback<string> TrackerChanged { get; set; }

            [CascadingParameter(Name = "SearchTermChanged")]
            public EventCallback<FilterSearchState> SearchTermChanged { get; set; }

            [CascadingParameter(Name = "SortColumnChanged")]
            public EventCallback<string> SortColumnChanged { get; set; }

            [CascadingParameter(Name = "SortDirectionChanged")]
            public EventCallback<SortDirection> SortDirectionChanged { get; set; }
        }

        private sealed class TestNavigationManager : NavigationManager
        {
            public TestNavigationManager()
            {
                Initialize("http://localhost/", "http://localhost/");
            }

            public string? LastNavigationUri { get; private set; }

            public bool ForceLoad { get; private set; }

            public void SetUri(string uri)
            {
                Uri = ToAbsoluteUri(uri).ToString();
            }

            public void SetRawUri(string uri)
            {
                Uri = uri;
            }

            protected override void NavigateToCore(string uri, bool forceLoad)
            {
                LastNavigationUri = uri;
                ForceLoad = forceLoad;
                Uri = ToAbsoluteUri(uri).ToString();
                NotifyLocationChanged(false);
            }
        }
    }
}
