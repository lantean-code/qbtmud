using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Components
{
    public partial class StatusBar
    {
        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Inject]
        protected IManagedTimerRegistry TimerRegistry { get; set; } = default!;

        [Parameter]
        public MainData? MainData { get; set; }

        [Parameter]
        public QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        [Parameter]
        public bool IsDarkMode { get; set; }

        [Parameter]
        public Breakpoint CurrentBreakpoint { get; set; }

        [Parameter]
        public Orientation CurrentOrientation { get; set; }

        [Parameter]
        public bool ToggleAlternativeSpeedLimitsInProgress { get; set; }

        [Parameter]
        public EventCallback OnToggleTimerDrawer { get; set; }

        [Parameter]
        public EventCallback OnToggleAlternativeSpeedLimits { get; set; }

        protected bool ShowStatusLabels =>
            (CurrentBreakpoint >= Breakpoint.Lg && CurrentOrientation == Orientation.Portrait) ||
            (CurrentBreakpoint >= Breakpoint.Md && CurrentOrientation == Orientation.Landscape);

        protected bool UseLightStatusBarDividers => IsDarkMode;

        protected static (string, Color) GetConnectionIcon(string? status)
        {
            return status switch
            {
                "firewalled" => (Icons.Material.Outlined.SignalWifiStatusbarConnectedNoInternet4, Color.Warning),
                "connected" => (Icons.Material.Outlined.SignalWifi4Bar, Color.Success),
                _ => (Icons.Material.Outlined.SignalWifiOff, Color.Error),
            };
        }

        protected string GetTimerStatusIcon()
        {
            var status = GetTimerStatus();
            if (!status.HasValue)
            {
                return Icons.Material.Filled.TimerOff;
            }

            if (status.Value == ManagedTimerState.Running)
            {
                return Icons.Material.Filled.Timer;
            }

            if (status.Value == ManagedTimerState.Paused)
            {
                return Icons.Material.Filled.PauseCircle;
            }

            if (status.Value == ManagedTimerState.Faulted)
            {
                return Icons.Material.Filled.Error;
            }

            return Icons.Material.Filled.TimerOff;
        }

        protected Color GetTimerStatusColor()
        {
            var status = GetTimerStatus();
            if (!status.HasValue)
            {
                return Color.Default;
            }

            if (status.Value == ManagedTimerState.Running)
            {
                return Color.Success;
            }

            if (status.Value == ManagedTimerState.Paused)
            {
                return Color.Warning;
            }

            if (status.Value == ManagedTimerState.Faulted)
            {
                return Color.Error;
            }

            return Color.Default;
        }

        protected string BuildTimerTooltip()
        {
            var timers = TimerRegistry.GetTimers();
            if (timers.Count == 0)
            {
                return LanguageLocalizer.Translate("AppTimerStatusPanel", "No timers registered.");
            }

            var running = timers.Count(timer => timer.State == ManagedTimerState.Running);
            var paused = timers.Count(timer => timer.State == ManagedTimerState.Paused);
            var stopped = timers.Count(timer => timer.State == ManagedTimerState.Stopped);
            var faulted = timers.Count(timer => timer.State == ManagedTimerState.Faulted);

            return LanguageLocalizer.Translate(
                "AppTimerStatusPanel",
                "Timers: %1 running, %2 paused, %3 stopped, %4 faulted",
                running,
                paused,
                stopped,
                faulted);
        }

        protected string? BuildExternalIpLabel(ServerState? serverState)
        {
            if (serverState is null)
            {
                return null;
            }

            var v4 = serverState.LastExternalAddressV4;
            var v6 = serverState.LastExternalAddressV6;
            var hasV4 = !string.IsNullOrWhiteSpace(v4);
            var hasV6 = !string.IsNullOrWhiteSpace(v6);

            if (!hasV4 && !hasV6)
            {
                return LanguageLocalizer.Translate("HttpServer", "External IP: N/A");
            }

            if (hasV4 && hasV6)
            {
                return LanguageLocalizer.Translate("HttpServer", "External IPs: %1, %2", v4, v6);
            }

            return LanguageLocalizer.Translate("HttpServer", "External IP: %1%2", v4 ?? string.Empty, v6 ?? string.Empty);
        }

        protected static string? BuildExternalIpValue(ServerState? serverState)
        {
            if (serverState is null)
            {
                return null;
            }

            var v4 = serverState.LastExternalAddressV4;
            var v6 = serverState.LastExternalAddressV6;
            var hasV4 = !string.IsNullOrWhiteSpace(v4);
            var hasV6 = !string.IsNullOrWhiteSpace(v6);

            if (!hasV4 && !hasV6)
            {
                return null;
            }

            if (hasV4 && hasV6)
            {
                return $"{v4}, {v6}";
            }

            return hasV4 ? v4 : v6;
        }

        protected static string BuildTransferInfo(long? speed, long? rateLimit, long? data)
        {
            var speedText = DisplayHelpers.Speed(speed);
            var limitText = rateLimit is > 0 ? $" [{DisplayHelpers.Speed(rateLimit)}]" : string.Empty;
            var dataText = DisplayHelpers.Size(data);
            var dataSuffix = string.IsNullOrEmpty(dataText) ? string.Empty : $" ({dataText})";

            return $"{speedText}{limitText}{dataSuffix}";
        }

        protected string BuildAlternativeSpeedLimitsTooltip()
        {
            return BuildAlternativeSpeedLimitsStatusMessage(MainData?.ServerState.UseAltSpeedLimits ?? false);
        }

        protected string BuildAlternativeSpeedLimitsStatusMessage(bool isEnabled)
        {
            return LanguageLocalizer.Translate(
                "MainWindow",
                isEnabled ? "Alternative speed limits: On" : "Alternative speed limits: Off");
        }

        protected string? BuildConnectionStatusTitle(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            return status switch
            {
                "connected" => LanguageLocalizer.Translate("MainWindow", "Connection status: Connected"),
                "firewalled" => LanguageLocalizer.Translate("MainWindow", "Connection status: Firewalled"),
                "disconnected" => LanguageLocalizer.Translate("MainWindow", "Connection status: Disconnected"),
                _ => status
            };
        }

        protected Task ToggleTimerDrawerClicked(MouseEventArgs args)
        {
            return OnToggleTimerDrawer.InvokeAsync();
        }

        protected Task ToggleAlternativeSpeedLimitsClicked(MouseEventArgs args)
        {
            return OnToggleAlternativeSpeedLimits.InvokeAsync();
        }

        private ManagedTimerState? GetTimerStatus()
        {
            var timers = TimerRegistry.GetTimers();
            if (timers.Count == 0)
            {
                return null;
            }

            var running = timers.Count(timer => timer.State == ManagedTimerState.Running);
            var paused = timers.Count(timer => timer.State == ManagedTimerState.Paused);
            var stopped = timers.Count(timer => timer.State == ManagedTimerState.Stopped);
            var faulted = timers.Count(timer => timer.State == ManagedTimerState.Faulted);

            if (faulted > 0)
            {
                return ManagedTimerState.Faulted;
            }

            if (paused > 0)
            {
                return ManagedTimerState.Paused;
            }

            if (running == timers.Count)
            {
                return ManagedTimerState.Running;
            }

            if (stopped > 0)
            {
                return ManagedTimerState.Stopped;
            }

            return null;
        }
    }
}
