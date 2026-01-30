using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components
{
    public partial class TimerStatusPanel : IAsyncDisposable
    {
        private const int PollIntervalMilliseconds = 1000;

        private readonly object _syncLock = new();
        private CancellationTokenSource? _pollingCancellationTokenSource;
        private IPeriodicTimer? _pollingTimer;
        private Task? _pollingTask;
        private bool _wasOpen;

        private IReadOnlyList<IManagedTimer> Timers => TimerRegistry.GetTimers().ToList();

        [Inject]
        protected IManagedTimerRegistry TimerRegistry { get; set; } = default!;

        [Inject]
        protected IPeriodicTimerFactory PeriodicTimerFactory { get; set; } = default!;

        [CascadingParameter(Name = "TimerDrawerOpen")]
        public bool TimerDrawerOpen { get; set; }

        [CascadingParameter(Name = "TimerDrawerOpenChanged")]
        public EventCallback<bool> TimerDrawerOpenChanged { get; set; }

        protected override async Task OnParametersSetAsync()
        {
            if (TimerDrawerOpen == _wasOpen)
            {
                return;
            }

            _wasOpen = TimerDrawerOpen;

            if (TimerDrawerOpen)
            {
                StartPolling();
                return;
            }

            await StopPollingAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await StopPollingAsync();
        }

        private async Task PauseAsync(IManagedTimer timer)
        {
            await timer.PauseAsync(CancellationToken.None);
            await InvokeAsync(StateHasChanged);
        }

        private async Task ResumeAsync(IManagedTimer timer)
        {
            await timer.ResumeAsync(CancellationToken.None);
            await InvokeAsync(StateHasChanged);
        }

        private async Task RestartAsync(IManagedTimer timer)
        {
            await timer.RestartAsync(CancellationToken.None);
            await InvokeAsync(StateHasChanged);
        }

        private async Task CloseDrawerAsync()
        {
            if (!TimerDrawerOpenChanged.HasDelegate)
            {
                return;
            }

            await TimerDrawerOpenChanged.InvokeAsync(false);
        }

        private static string FormatInterval(TimeSpan interval)
        {
            if (interval.TotalSeconds >= 1)
            {
                return $"{interval.TotalSeconds:0.##}s";
            }

            return $"{interval.TotalMilliseconds:0}ms";
        }

        private static string FormatTimestamp(DateTimeOffset? timestamp)
        {
            if (!timestamp.HasValue)
            {
                return "-";
            }

            var localTime = timestamp.Value.ToLocalTime();
            var format = localTime.Date == DateTimeOffset.Now.Date
                ? "HH:mm:ss.fff"
                : "yyyy-MM-dd HH:mm:ss.fff";
            return localTime.ToString(format);
        }

        private static string GetStateTooltip(IManagedTimer timer)
        {
            if (timer.State == ManagedTimerState.Faulted && timer.LastFault is not null)
            {
                return $"Faulted: {timer.LastFault.Message}";
            }

            return timer.State.ToString();
        }

        private static string GetStateIcon(ManagedTimerState state)
        {
            return state switch
            {
                ManagedTimerState.Running => Icons.Material.Filled.Timer,
                ManagedTimerState.Paused => Icons.Material.Filled.PauseCircle,
                ManagedTimerState.Faulted => Icons.Material.Filled.Error,
                ManagedTimerState.Stopped => Icons.Material.Filled.TimerOff,
                _ => Icons.Material.Filled.Timer,
            };
        }

        private static Color GetStateColor(ManagedTimerState state)
        {
            return state switch
            {
                ManagedTimerState.Running => Color.Success,
                ManagedTimerState.Paused => Color.Warning,
                ManagedTimerState.Faulted => Color.Error,
                ManagedTimerState.Stopped => Color.Default,
                _ => Color.Default,
            };
        }

        private static string GetTickLabel(IManagedTimer timer)
        {
            return timer.State == ManagedTimerState.Running ? "Next tick" : "Last tick";
        }

        private static DateTimeOffset? GetTickTimestamp(IManagedTimer timer)
        {
            return timer.State == ManagedTimerState.Running ? timer.NextTickUtc : timer.LastTickUtc;
        }

        private void StartPolling()
        {
            lock (_syncLock)
            {
                if (_pollingTask is not null && !_pollingTask.IsCompleted)
                {
                    return;
                }

                _pollingCancellationTokenSource = new CancellationTokenSource();
                _pollingTimer = PeriodicTimerFactory.Create(TimeSpan.FromMilliseconds(PollIntervalMilliseconds));
                _pollingTask = PollAsync(_pollingTimer, _pollingCancellationTokenSource.Token);
            }

            _ = InvokeAsync(StateHasChanged);
        }

        private async Task StopPollingAsync()
        {
            CancellationTokenSource? cancellationTokenSource;
            IPeriodicTimer? timer;

            lock (_syncLock)
            {
                cancellationTokenSource = _pollingCancellationTokenSource;
                timer = _pollingTimer;
                _pollingCancellationTokenSource = null;
                _pollingTimer = null;
                _pollingTask = null;
            }

            cancellationTokenSource?.CancelIfNotDisposed();
            cancellationTokenSource?.Dispose();

            if (timer is not null)
            {
                await timer.DisposeAsync();
            }
        }

        private async Task PollAsync(IPeriodicTimer timer, CancellationToken cancellationToken)
        {
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    await InvokeAsync(StateHasChanged);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected when polling stops.
            }
        }
    }
}
