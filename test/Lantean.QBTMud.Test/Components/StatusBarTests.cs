using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using System.Text.Json;
using ClientModels = Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class StatusBarTests : RazorComponentTestBase<StatusBar>
    {
        private readonly IManagedTimerRegistry _timerRegistry;
        private readonly Mock<IManagedTimerRegistry> _timerRegistryMock;
        private readonly IRenderedComponent<StatusBar> _target;

        public StatusBarTests()
        {
            _timerRegistry = Mock.Of<IManagedTimerRegistry>();
            _timerRegistryMock = Mock.Get(_timerRegistry);
            _timerRegistryMock.Setup(registry => registry.GetTimers()).Returns(new List<IManagedTimer>());

            TestContext.Services.RemoveAll(typeof(IManagedTimerRegistry));
            TestContext.Services.AddSingleton(_timerRegistry);

            _target = RenderStatusBar();
        }

        [Fact]
        public void GIVEN_LargePortraitBreakpoint_WHEN_Rendered_THEN_ShowsStatusLabels()
        {
            var target = RenderStatusBar(
                breakpoint: Breakpoint.Lg,
                orientation: Orientation.Portrait);

            var freeSpace = FindComponentByTestId<MudText>(target, "Status-FreeSpace");
            var dhtNodes = FindComponentByTestId<MudText>(target, "Status-DhtNodes");

            GetChildContentText(freeSpace.Instance.ChildContent).Should().StartWith("Free space:");
            GetChildContentText(dhtNodes.Instance.ChildContent).Should().StartWith("DHT:");
        }

        [Fact]
        public void GIVEN_MediumLandscapeBreakpoint_WHEN_Rendered_THEN_ShowsStatusLabels()
        {
            var target = RenderStatusBar(
                breakpoint: Breakpoint.Md,
                orientation: Orientation.Landscape);

            var freeSpace = FindComponentByTestId<MudText>(target, "Status-FreeSpace");
            var dhtNodes = FindComponentByTestId<MudText>(target, "Status-DhtNodes");

            GetChildContentText(freeSpace.Instance.ChildContent).Should().StartWith("Free space:");
            GetChildContentText(dhtNodes.Instance.ChildContent).Should().StartWith("DHT:");
        }

        [Fact]
        public void GIVEN_SmallLandscapeBreakpoint_WHEN_Rendered_THEN_HidesStatusLabels()
        {
            var target = RenderStatusBar(
                breakpoint: Breakpoint.Sm,
                orientation: Orientation.Landscape);

            var freeSpace = FindComponentByTestId<MudText>(target, "Status-FreeSpace");
            var dhtNodes = FindComponentByTestId<MudText>(target, "Status-DhtNodes");

            GetChildContentText(freeSpace.Instance.ChildContent).Should().NotContain("Free space:");
            GetChildContentText(dhtNodes.Instance.ChildContent).Should().Be("10");
        }

        [Fact]
        public void GIVEN_SmallLandscapeBreakpoint_WHEN_Rendered_THEN_AutoScrollsStatusBarToEnd()
        {
            var scrollInvocation = TestContext.JSInterop.SetupVoid(
                "qbt.scrollElementToEnd",
                invocation => invocation.Arguments.Count == 1 && invocation.Arguments[0] is ".app-shell__status-bar");
            scrollInvocation.SetVoidResult();

            RenderStatusBar(
                breakpoint: Breakpoint.Sm,
                orientation: Orientation.Landscape);

            scrollInvocation.Invocations.Should().ContainSingle();
        }

        [Fact]
        public void GIVEN_LargePortraitBreakpoint_WHEN_Rendered_THEN_DoesNotAutoScrollStatusBarToEnd()
        {
            var scrollInvocation = TestContext.JSInterop.SetupVoid("qbt.scrollElementToEnd", _ => true);
            scrollInvocation.SetVoidResult();

            RenderStatusBar(
                breakpoint: Breakpoint.Lg,
                orientation: Orientation.Portrait);

            scrollInvocation.Invocations.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_SmallLandscapeBreakpoint_WHEN_ReRendered_THEN_AutoScrollsStatusBarToEndAgain()
        {
            var scrollInvocation = TestContext.JSInterop.SetupVoid(
                "qbt.scrollElementToEnd",
                invocation => invocation.Arguments.Count == 1 && invocation.Arguments[0] is ".app-shell__status-bar");
            scrollInvocation.SetVoidResult();

            var target = RenderStatusBar(
                breakpoint: Breakpoint.Sm,
                orientation: Orientation.Landscape);

            target.Render();

            target.WaitForAssertion(() => scrollInvocation.Invocations.Should().HaveCount(2));
        }

        [Fact]
        public void GIVEN_SmallLandscapeBreakpoint_WHEN_Rendered_THEN_ShowsReducedModeStatusTooltips()
        {
            var target = RenderStatusBar(
                preferences: CreatePreferences(true),
                breakpoint: Breakpoint.Sm,
                orientation: Orientation.Landscape);

            var freeSpace = FindComponentByTestId<MudText>(target, "Status-FreeSpace");
            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");
            var dhtNodes = FindComponentByTestId<MudText>(target, "Status-DhtNodes");
            var freeSpaceTooltip = FindComponentByTestId<MudTooltip>(target, "Status-FreeSpaceTooltip");
            var externalIpTooltip = FindComponentByTestId<MudTooltip>(target, "Status-ExternalIpTooltip");
            var dhtNodesTooltip = FindComponentByTestId<MudTooltip>(target, "Status-DhtNodesTooltip");

            GetChildContentText(freeSpace.Instance.ChildContent).Should().Be(DisplayHelpers.Size(1024));
            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("1.1.1.1");
            GetChildContentText(dhtNodes.Instance.ChildContent).Should().Be("10");
            freeSpaceTooltip.Instance.Text.Should().Be($"Free space: {DisplayHelpers.Size(1024)}");
            externalIpTooltip.Instance.Text.Should().Be("External IP: 1.1.1.1");
            dhtNodesTooltip.Instance.Text.Should().Be("DHT: 10 nodes");
        }

        [Fact]
        public void GIVEN_LargePortraitBreakpoint_WHEN_Rendered_THEN_DoesNotShowReducedModeStatusTooltips()
        {
            var target = RenderStatusBar(
                preferences: CreatePreferences(true),
                breakpoint: Breakpoint.Lg,
                orientation: Orientation.Portrait);

            ContainsComponentWithTestId<MudTooltip>(target, "Status-FreeSpaceTooltip").Should().BeFalse();
            ContainsComponentWithTestId<MudTooltip>(target, "Status-ExternalIpTooltip").Should().BeFalse();
            ContainsComponentWithTestId<MudTooltip>(target, "Status-DhtNodesTooltip").Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ExternalIpDisabled_WHEN_Rendered_THEN_DoesNotRenderExternalIp()
        {
            var target = RenderStatusBar(preferences: CreatePreferences(false));

            ContainsComponentWithTestId<MudText>(target, "Status-ExternalIp").Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndNoAddresses_WHEN_LabelsShown_THEN_ShowsNotAvailableText()
        {
            var serverState = CreateServerState(lastExternalAddressV4: string.Empty, lastExternalAddressV6: string.Empty);
            var mainData = CreateMainData(serverState);

            var target = RenderStatusBar(
                mainData: mainData,
                preferences: CreatePreferences(true),
                breakpoint: Breakpoint.Lg,
                orientation: Orientation.Portrait);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");

            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("External IP: N/A");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndTwoAddresses_WHEN_LabelsShown_THEN_ShowsDualAddressText()
        {
            var serverState = CreateServerState(lastExternalAddressV4: "1.1.1.1", lastExternalAddressV6: "2001:db8::1");
            var mainData = CreateMainData(serverState);

            var target = RenderStatusBar(
                mainData: mainData,
                preferences: CreatePreferences(true),
                breakpoint: Breakpoint.Lg,
                orientation: Orientation.Portrait);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");

            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("External IPs: 1.1.1.1, 2001:db8::1");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndSingleAddress_WHEN_LabelsShown_THEN_ShowsSingleAddressText()
        {
            var serverState = CreateServerState(lastExternalAddressV4: "1.1.1.1", lastExternalAddressV6: null!);
            var mainData = CreateMainData(serverState);

            var target = RenderStatusBar(
                mainData: mainData,
                preferences: CreatePreferences(true),
                breakpoint: Breakpoint.Lg,
                orientation: Orientation.Portrait);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");

            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("External IP: 1.1.1.1");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndSingleV6Address_WHEN_LabelsShown_THEN_ShowsSingleAddressText()
        {
            var serverState = CreateServerState(lastExternalAddressV4: null!, lastExternalAddressV6: "2001:db8::1");
            var mainData = CreateMainData(serverState);

            var target = RenderStatusBar(
                mainData: mainData,
                preferences: CreatePreferences(true),
                breakpoint: Breakpoint.Lg,
                orientation: Orientation.Portrait);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");

            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("External IP: 2001:db8::1");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndTwoAddresses_WHEN_LabelsHidden_THEN_ShowsAddressValuesOnly()
        {
            var serverState = CreateServerState(lastExternalAddressV4: "1.1.1.1", lastExternalAddressV6: "2001:db8::1");
            var mainData = CreateMainData(serverState);

            var target = RenderStatusBar(
                mainData: mainData,
                preferences: CreatePreferences(true),
                breakpoint: Breakpoint.Sm,
                orientation: Orientation.Landscape);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");

            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("1.1.1.1, 2001:db8::1");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndSingleV6Address_WHEN_LabelsHidden_THEN_ShowsSingleAddressValue()
        {
            var serverState = CreateServerState(lastExternalAddressV4: string.Empty, lastExternalAddressV6: "2001:db8::1");
            var mainData = CreateMainData(serverState);

            var target = RenderStatusBar(
                mainData: mainData,
                preferences: CreatePreferences(true),
                breakpoint: Breakpoint.Sm,
                orientation: Orientation.Landscape);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");

            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("2001:db8::1");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndSingleV4Address_WHEN_LabelsHidden_THEN_ShowsSingleAddressValue()
        {
            var serverState = CreateServerState(lastExternalAddressV4: "1.1.1.1", lastExternalAddressV6: string.Empty);
            var mainData = CreateMainData(serverState);

            var target = RenderStatusBar(
                mainData: mainData,
                preferences: CreatePreferences(true),
                breakpoint: Breakpoint.Sm,
                orientation: Orientation.Landscape);

            var externalIp = FindComponentByTestId<MudText>(target, "Status-ExternalIp");

            GetChildContentText(externalIp.Instance.ChildContent).Should().Be("1.1.1.1");
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndNoAddresses_WHEN_LabelsHidden_THEN_DoesNotRenderExternalIp()
        {
            var serverState = CreateServerState(lastExternalAddressV4: string.Empty, lastExternalAddressV6: string.Empty);
            var mainData = CreateMainData(serverState);

            var target = RenderStatusBar(
                mainData: mainData,
                preferences: CreatePreferences(true),
                breakpoint: Breakpoint.Sm,
                orientation: Orientation.Landscape);

            ContainsComponentWithTestId<MudText>(target, "Status-ExternalIp").Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndMissingServerState_WHEN_LabelsShown_THEN_DoesNotRenderExternalIp()
        {
            var target = RenderStatusBar(
                includeMainData: false,
                preferences: CreatePreferences(true),
                breakpoint: Breakpoint.Lg,
                orientation: Orientation.Portrait);

            ContainsComponentWithTestId<MudText>(target, "Status-ExternalIp").Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ExternalIpEnabledAndMissingServerState_WHEN_LabelsHidden_THEN_DoesNotRenderExternalIp()
        {
            var target = RenderStatusBar(
                includeMainData: false,
                preferences: CreatePreferences(true),
                breakpoint: Breakpoint.Sm,
                orientation: Orientation.Landscape);

            ContainsComponentWithTestId<MudText>(target, "Status-ExternalIp").Should().BeFalse();
        }

        [Theory]
        [InlineData("connected", "Connection status: Connected", Icons.Material.Outlined.SignalWifi4Bar, Color.Success)]
        [InlineData("firewalled", "Connection status: Firewalled", Icons.Material.Outlined.SignalWifiStatusbarConnectedNoInternet4, Color.Warning)]
        [InlineData("disconnected", "Connection status: Disconnected", Icons.Material.Outlined.SignalWifiOff, Color.Error)]
        [InlineData("ConnectionStatus", "ConnectionStatus", Icons.Material.Outlined.SignalWifiOff, Color.Error)]
        [InlineData(" ", null, Icons.Material.Outlined.SignalWifiOff, Color.Error)]
        public void GIVEN_ConnectionStatus_WHEN_Rendered_THEN_ShowsExpectedTooltipAndIcon(string connectionStatus, string? expectedTooltip, string expectedIcon, Color expectedColor)
        {
            var serverState = CreateServerState(connectionStatus: connectionStatus);
            var mainData = CreateMainData(serverState);

            var target = RenderStatusBar(mainData: mainData);

            var tooltip = FindComponentByTestId<MudTooltip>(target, "Status-ConnectionTooltip");
            var icon = FindComponentByTestId<MudIcon>(target, "Status-ConnectionIcon");

            tooltip.Instance.Text.Should().Be(expectedTooltip);
            icon.Instance.Icon.Should().Be(expectedIcon);
            icon.Instance.Color.Should().Be(expectedColor);
        }

        [Fact]
        public void GIVEN_NoMainData_WHEN_Rendered_THEN_ConnectionTooltipIsNullAndIconShowsDisconnected()
        {
            var target = RenderStatusBar(includeMainData: false);

            var tooltip = FindComponentByTestId<MudTooltip>(target, "Status-ConnectionTooltip");
            var icon = FindComponentByTestId<MudIcon>(target, "Status-ConnectionIcon");

            tooltip.Instance.Text.Should().BeNull();
            icon.Instance.Icon.Should().Be(Icons.Material.Outlined.SignalWifiOff);
            icon.Instance.Color.Should().Be(Color.Error);
        }

        [Fact]
        public void GIVEN_NoTimers_WHEN_Rendered_THEN_ShowsDefaultTimerStatus()
        {
            var timerButton = FindComponentByTestId<MudIconButton>(_target, "Status-TimerButton");
            var timerTooltip = FindComponentByTestId<MudTooltip>(_target, "Status-TimerTooltip");

            timerButton.Instance.Icon.Should().Be(Icons.Material.Filled.TimerOff);
            timerButton.Instance.Color.Should().Be(Color.Default);
            timerTooltip.Instance.Text.Should().Be("No timers registered.");
        }

        [Fact]
        public void GIVEN_AllTimersRunning_WHEN_Rendered_THEN_ShowsRunningTimerStatus()
        {
            var target = RenderStatusBar(timers: new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Running),
            });

            var timerButton = FindComponentByTestId<MudIconButton>(target, "Status-TimerButton");

            timerButton.Instance.Icon.Should().Be(Icons.Material.Filled.Timer);
            timerButton.Instance.Color.Should().Be(Color.Success);
        }

        [Fact]
        public void GIVEN_PausedTimerPresent_WHEN_Rendered_THEN_ShowsPausedTimerStatus()
        {
            var target = RenderStatusBar(timers: new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Paused),
            });

            var timerButton = FindComponentByTestId<MudIconButton>(target, "Status-TimerButton");

            timerButton.Instance.Icon.Should().Be(Icons.Material.Filled.PauseCircle);
            timerButton.Instance.Color.Should().Be(Color.Warning);
        }

        [Fact]
        public void GIVEN_FaultedTimerPresent_WHEN_Rendered_THEN_ShowsFaultedTimerStatus()
        {
            var target = RenderStatusBar(timers: new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Paused),
                CreateTimer(ManagedTimerState.Faulted),
            });

            var timerButton = FindComponentByTestId<MudIconButton>(target, "Status-TimerButton");

            timerButton.Instance.Icon.Should().Be(Icons.Material.Filled.Error);
            timerButton.Instance.Color.Should().Be(Color.Error);
        }

        [Fact]
        public void GIVEN_StoppedTimerPresent_WHEN_Rendered_THEN_ShowsStoppedTimerStatus()
        {
            var target = RenderStatusBar(timers: new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Stopped),
            });

            var timerButton = FindComponentByTestId<MudIconButton>(target, "Status-TimerButton");

            timerButton.Instance.Icon.Should().Be(Icons.Material.Filled.TimerOff);
            timerButton.Instance.Color.Should().Be(Color.Default);
        }

        [Fact]
        public void GIVEN_UnknownTimerState_WHEN_Rendered_THEN_ShowsDefaultTimerStatus()
        {
            var target = RenderStatusBar(timers: new List<IManagedTimer>
            {
                CreateTimer((ManagedTimerState)42),
            });

            var timerButton = FindComponentByTestId<MudIconButton>(target, "Status-TimerButton");
            var timerTooltip = FindComponentByTestId<MudTooltip>(target, "Status-TimerTooltip");

            timerButton.Instance.Icon.Should().Be(Icons.Material.Filled.TimerOff);
            timerButton.Instance.Color.Should().Be(Color.Default);
            timerTooltip.Instance.Text.Should().Be("Timers: 0 running, 0 paused, 0 stopped, 0 faulted");
        }

        [Fact]
        public void GIVEN_TimersInAllStates_WHEN_Rendered_THEN_ShowsTimerCountsInTooltip()
        {
            var target = RenderStatusBar(timers: new List<IManagedTimer>
            {
                CreateTimer(ManagedTimerState.Running),
                CreateTimer(ManagedTimerState.Paused),
                CreateTimer(ManagedTimerState.Stopped),
                CreateTimer(ManagedTimerState.Faulted),
            });

            var timerTooltip = FindComponentByTestId<MudTooltip>(target, "Status-TimerTooltip");

            timerTooltip.Instance.Text.Should().Be("Timers: 1 running, 1 paused, 1 stopped, 1 faulted");
        }

        [Fact]
        public void GIVEN_AlternativeSpeedLimitsDisabled_WHEN_Rendered_THEN_ShowsOffTooltipAndSuccessButton()
        {
            var serverState = CreateServerState(useAltSpeedLimits: false);
            var mainData = CreateMainData(serverState);

            var target = RenderStatusBar(mainData: mainData);

            var tooltip = FindComponentByTestId<MudTooltip>(target, "Status-AltSpeedTooltip");
            var button = FindComponentByTestId<MudIconButton>(target, "Status-AltSpeedButton");

            tooltip.Instance.Text.Should().Be("Alternative speed limits: Off");
            button.Instance.Color.Should().Be(Color.Success);
            button.Instance.Class.Should().NotContain("app-shell__alt-speed-enabled");
        }

        [Fact]
        public void GIVEN_AlternativeSpeedLimitsEnabled_WHEN_Rendered_THEN_ShowsOnTooltipAndErrorButton()
        {
            var serverState = CreateServerState(useAltSpeedLimits: true);
            var mainData = CreateMainData(serverState);

            var target = RenderStatusBar(mainData: mainData);

            var tooltip = FindComponentByTestId<MudTooltip>(target, "Status-AltSpeedTooltip");
            var button = FindComponentByTestId<MudIconButton>(target, "Status-AltSpeedButton");

            tooltip.Instance.Text.Should().Be("Alternative speed limits: On");
            button.Instance.Color.Should().Be(Color.Error);
            button.Instance.Class.Should().Contain("app-shell__alt-speed-enabled");
        }

        [Fact]
        public void GIVEN_AlternativeSpeedToggleInProgress_WHEN_Rendered_THEN_ButtonIsDisabled()
        {
            var target = RenderStatusBar(toggleAlternativeSpeedLimitsInProgress: true);

            var button = FindComponentByTestId<MudIconButton>(target, "Status-AltSpeedButton");

            button.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_TimerButtonClicked_WHEN_CallbackProvided_THEN_InvokesToggleTimerDrawer()
        {
            var clicked = false;
            var target = RenderStatusBar(onToggleTimerDrawer: () =>
            {
                clicked = true;
                return Task.CompletedTask;
            });

            var button = FindComponentByTestId<MudIconButton>(target, "Status-TimerButton");

            await target.InvokeAsync(() => button.Find("button").Click());

            clicked.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_AlternativeSpeedButtonClicked_WHEN_CallbackProvided_THEN_InvokesToggleAlternativeSpeedLimits()
        {
            var clicked = false;
            var target = RenderStatusBar(onToggleAlternativeSpeedLimits: () =>
            {
                clicked = true;
                return Task.CompletedTask;
            });

            var button = FindComponentByTestId<MudIconButton>(target, "Status-AltSpeedButton");

            await target.InvokeAsync(() => button.Find("button").Click());

            clicked.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_DarkModeEnabled_WHEN_Rendered_THEN_DividersUseLightVariant()
        {
            var target = RenderStatusBar(isDarkMode: true);
            var dividers = target.FindComponents<MudDivider>();

            dividers.Should().NotBeEmpty();
            dividers.Should().OnlyContain(divider => divider.Instance.Light);
        }

        [Fact]
        public void GIVEN_DarkModeDisabled_WHEN_Rendered_THEN_DividersDoNotUseLightVariant()
        {
            var dividers = _target.FindComponents<MudDivider>();

            dividers.Should().NotBeEmpty();
            dividers.Should().OnlyContain(divider => !divider.Instance.Light);
        }

        [Fact]
        public void GIVEN_TransferRateAndLimitProvided_WHEN_Rendered_THEN_ShowsLimitAndTransferredData()
        {
            var serverState = CreateServerState(
                downloadInfoSpeed: 1024,
                downloadRateLimit: 4096,
                downloadInfoData: 2048);
            var mainData = CreateMainData(serverState);

            var target = RenderStatusBar(mainData: mainData);
            var download = FindComponentByTestId<MudText>(target, "Status-Download");
            var expected = $"{DisplayHelpers.Speed(1024)} [{DisplayHelpers.Speed(4096)}] ({DisplayHelpers.Size(2048)})";

            GetChildContentText(download.Instance.ChildContent).Should().Be(expected);
        }

        [Fact]
        public void GIVEN_NoMainData_WHEN_Rendered_THEN_TransferValuesAreEmpty()
        {
            var target = RenderStatusBar(includeMainData: false);

            var download = FindComponentByTestId<MudText>(target, "Status-Download");
            var upload = FindComponentByTestId<MudText>(target, "Status-Upload");

            GetChildContentText(download.Instance.ChildContent).Should().BeEmpty();
            GetChildContentText(upload.Instance.ChildContent).Should().BeEmpty();
        }

        private IRenderedComponent<StatusBar> RenderStatusBar(
            IReadOnlyList<IManagedTimer>? timers = null,
            MainData? mainData = null,
            bool includeMainData = true,
            ClientModels.Preferences? preferences = null,
            bool isDarkMode = false,
            Breakpoint breakpoint = Breakpoint.Lg,
            Orientation orientation = Orientation.Portrait,
            bool toggleAlternativeSpeedLimitsInProgress = false,
            Func<Task>? onToggleTimerDrawer = null,
            Func<Task>? onToggleAlternativeSpeedLimits = null)
        {
            _timerRegistryMock.Setup(registry => registry.GetTimers()).Returns(timers ?? new List<IManagedTimer>());

            return TestContext.Render<StatusBar>(parameters =>
            {
                parameters.Add(parameter => parameter.MainData, includeMainData ? mainData ?? CreateMainData(CreateServerState()) : null);
                parameters.Add(parameter => parameter.Preferences, preferences);
                parameters.Add(parameter => parameter.IsDarkMode, isDarkMode);
                parameters.Add(parameter => parameter.CurrentBreakpoint, breakpoint);
                parameters.Add(parameter => parameter.CurrentOrientation, orientation);
                parameters.Add(parameter => parameter.ToggleAlternativeSpeedLimitsInProgress, toggleAlternativeSpeedLimitsInProgress);
                parameters.Add(parameter => parameter.OnToggleTimerDrawer, EventCallback.Factory.Create(this, onToggleTimerDrawer ?? (() => Task.CompletedTask)));
                parameters.Add(parameter => parameter.OnToggleAlternativeSpeedLimits, EventCallback.Factory.Create(this, onToggleAlternativeSpeedLimits ?? (() => Task.CompletedTask)));
            });
        }

        private static bool ContainsComponentWithTestId<TComponent>(IRenderedComponent<StatusBar> target, string testId)
            where TComponent : IComponent
        {
            return target.FindComponents<TComponent>().Any(component => HasTestId(component, testId));
        }

        private static MainData CreateMainData(ServerState serverState, bool lostConnection = false)
        {
            return new MainData(
                torrents: new Dictionary<string, Torrent>(),
                tags: new List<string>(),
                categories: new Dictionary<string, Category>(),
                trackers: new Dictionary<string, IReadOnlyList<string>>(),
                serverState: serverState,
                tagState: new Dictionary<string, HashSet<string>>(),
                categoriesState: new Dictionary<string, HashSet<string>>(),
                statusState: new Dictionary<string, HashSet<string>>(),
                trackersState: new Dictionary<string, HashSet<string>>())
            {
                LostConnection = lostConnection
            };
        }

        private static ServerState CreateServerState(
            string connectionStatus = "connected",
            int dhtNodes = 10,
            long freeSpaceOnDisk = 1024,
            long downloadInfoSpeed = 1024,
            long downloadRateLimit = 0,
            long downloadInfoData = 2048,
            long uploadInfoSpeed = 512,
            long uploadRateLimit = 0,
            long uploadInfoData = 1024,
            bool useAltSpeedLimits = false,
            string lastExternalAddressV4 = "1.1.1.1",
            string lastExternalAddressV6 = "")
        {
            return new ServerState
            {
                ConnectionStatus = connectionStatus,
                DHTNodes = dhtNodes,
                FreeSpaceOnDisk = freeSpaceOnDisk,
                DownloadInfoSpeed = downloadInfoSpeed,
                DownloadRateLimit = downloadRateLimit,
                DownloadInfoData = downloadInfoData,
                UploadInfoSpeed = uploadInfoSpeed,
                UploadRateLimit = uploadRateLimit,
                UploadInfoData = uploadInfoData,
                UseAltSpeedLimits = useAltSpeedLimits,
                LastExternalAddressV4 = lastExternalAddressV4,
                LastExternalAddressV6 = lastExternalAddressV6
            };
        }

        private static ClientModels.Preferences CreatePreferences(bool statusBarExternalIp)
        {
            var json = $"{{\"rss_processing_enabled\":false,\"status_bar_external_ip\":{statusBarExternalIp.ToString().ToLowerInvariant()}}}";
            return JsonSerializer.Deserialize<ClientModels.Preferences>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        private static IManagedTimer CreateTimer(ManagedTimerState state)
        {
            var timer = Mock.Of<IManagedTimer>();
            var timerMock = Mock.Get(timer);
            timerMock.SetupGet(managedTimer => managedTimer.State).Returns(state);
            return timer;
        }
    }
}
