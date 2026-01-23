namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides a controllable timer loop with observable state.
    /// </summary>
    public sealed class ManagedTimer : IManagedTimer
    {
        private const string IntervalMessage = "Interval must be greater than zero.";

        private readonly IPeriodicTimerFactory _timerFactory;
        private readonly object _syncLock = new();

        private bool _disposed;
        private ManagedTimerState _state;
        private TimeSpan _interval;
        private DateTimeOffset? _lastTickUtc;
        private DateTimeOffset? _nextTickUtc;
        private Exception? _lastFault;
        private Func<CancellationToken, Task<ManagedTimerTickResult>>? _tickHandler;
        private CancellationTokenSource? _runCancellationTokenSource;
        private CancellationTokenSource? _waitCancellation;
        private Task? _runTask;
        private TaskCompletionSource<bool>? _resumeSignal;
        private bool _intervalChangeRequested;
        private bool _waitCanceled;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedTimer"/> class.
        /// </summary>
        /// <param name="timerFactory">The timer factory to create periodic timers.</param>
        /// <param name="name">The timer name.</param>
        /// <param name="interval">The initial interval.</param>
        public ManagedTimer(IPeriodicTimerFactory timerFactory, string name, TimeSpan interval)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, IntervalMessage);
            }

            _timerFactory = timerFactory;
            Name = name;
            _interval = interval;
            _state = ManagedTimerState.Stopped;
        }

        /// <summary>
        /// Gets the timer name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the current timer interval.
        /// </summary>
        public TimeSpan Interval
        {
            get
            {
                lock (_syncLock)
                {
                    return _interval;
                }
            }
        }

        /// <summary>
        /// Gets the current timer state.
        /// </summary>
        public ManagedTimerState State
        {
            get
            {
                lock (_syncLock)
                {
                    return _state;
                }
            }
        }

        /// <summary>
        /// Gets the UTC timestamp of the last tick, if any.
        /// </summary>
        public DateTimeOffset? LastTickUtc
        {
            get
            {
                lock (_syncLock)
                {
                    return _lastTickUtc;
                }
            }
        }

        /// <summary>
        /// Gets the UTC timestamp for the next tick, if scheduled.
        /// </summary>
        public DateTimeOffset? NextTickUtc
        {
            get
            {
                lock (_syncLock)
                {
                    return _nextTickUtc;
                }
            }
        }

        /// <summary>
        /// Gets the last fault that stopped the timer, if any.
        /// </summary>
        public Exception? LastFault
        {
            get
            {
                lock (_syncLock)
                {
                    return _lastFault;
                }
            }
        }

        /// <summary>
        /// Starts the timer loop.
        /// </summary>
        /// <param name="tickHandler">The handler to execute on each tick.</param>
        /// <param name="cancellationToken">A token that cancels the timer loop.</param>
        /// <returns><see langword="true"/> when the timer starts; otherwise <see langword="false"/>.</returns>
        public Task<bool> StartAsync(Func<CancellationToken, Task<ManagedTimerTickResult>> tickHandler, CancellationToken cancellationToken)
        {
            lock (_syncLock)
            {
                if (_disposed)
                {
                    return Task.FromResult(false);
                }

                if (_state != ManagedTimerState.Stopped && _state != ManagedTimerState.Faulted)
                {
                    return Task.FromResult(false);
                }

                _tickHandler = tickHandler;
                _lastFault = null;
                _lastTickUtc = null;
                _nextTickUtc = null;
                _intervalChangeRequested = false;
                _waitCanceled = false;
                _resumeSignal = null;
                _runCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _state = ManagedTimerState.Running;
                _runTask = RunAsync(_runCancellationTokenSource.Token);
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Pauses the timer.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when the timer pauses; otherwise <see langword="false"/>.</returns>
        public Task<bool> PauseAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var paused = false;

            lock (_syncLock)
            {
                if (_disposed)
                {
                    return Task.FromResult(false);
                }

                if (_state != ManagedTimerState.Running)
                {
                    return Task.FromResult(false);
                }

                _state = ManagedTimerState.Paused;
                _nextTickUtc = null;
                _resumeSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                paused = true;
            }

            CancelWait();
            return Task.FromResult(paused);
        }

        /// <summary>
        /// Resumes the timer.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when the timer resumes; otherwise <see langword="false"/>.</returns>
        public Task<bool> ResumeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TaskCompletionSource<bool>? resumeSignal = null;

            lock (_syncLock)
            {
                if (_disposed)
                {
                    return Task.FromResult(false);
                }

                if (_state != ManagedTimerState.Paused)
                {
                    return Task.FromResult(false);
                }

                _state = ManagedTimerState.Running;
                resumeSignal = _resumeSignal;
                _resumeSignal = null;
            }

            resumeSignal?.TrySetResult(true);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when the timer stops; otherwise <see langword="false"/>.</returns>
        public async Task<bool> StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Task? runTask;
            var shouldStop = false;

            lock (_syncLock)
            {
                if (_state == ManagedTimerState.Stopped || _state == ManagedTimerState.Faulted)
                {
                    runTask = _runTask;
                }
                else
                {
                    _state = ManagedTimerState.Stopped;
                    _nextTickUtc = null;
                    _runCancellationTokenSource?.CancelIfNotDisposed();
                    _resumeSignal?.TrySetResult(false);
                    runTask = _runTask;
                    shouldStop = true;
                }
            }

            CancelWait();

            if (runTask is not null && !runTask.IsCompleted && Task.CurrentId != runTask.Id)
            {
                await runTask.WaitAsync(cancellationToken);
            }

            return shouldStop;
        }

        /// <summary>
        /// Updates the timer interval.
        /// </summary>
        /// <param name="interval">The new interval.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when the interval is updated; otherwise <see langword="false"/>.</returns>
        public Task<bool> UpdateIntervalAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            if (interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), interval, IntervalMessage);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var updated = false;

            lock (_syncLock)
            {
                if (_disposed)
                {
                    return Task.FromResult(false);
                }

                if (interval == _interval)
                {
                    return Task.FromResult(false);
                }

                _interval = interval;
                _intervalChangeRequested = true;
                updated = true;
            }

            CancelWait();
            return Task.FromResult(updated);
        }

        /// <summary>
        /// Disposes the timer and stops scheduling ticks.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            var shouldDispose = false;

            lock (_syncLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                shouldDispose = true;
            }

            if (!shouldDispose)
            {
                return;
            }

            await StopAsync(CancellationToken.None);

            CancellationTokenSource? waitCancellation;
            CancellationTokenSource? runCancellation;

            lock (_syncLock)
            {
                waitCancellation = _waitCancellation;
                runCancellation = _runCancellationTokenSource;
                _waitCancellation = null;
                _runCancellationTokenSource = null;
                _tickHandler = null;
                _resumeSignal = null;
                _runTask = null;
                _intervalChangeRequested = false;
                _waitCanceled = false;
            }

            waitCancellation?.Dispose();
            runCancellation?.Dispose();
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            IPeriodicTimer? timer = null;

            try
            {
                timer = _timerFactory.Create(Interval);

                while (!cancellationToken.IsCancellationRequested)
                {
                    await WaitWhilePausedAsync(cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    TimeSpan? newInterval = null;
                    lock (_syncLock)
                    {
                        if (_intervalChangeRequested)
                        {
                            _intervalChangeRequested = false;
                            newInterval = _interval;
                        }
                    }

                    if (newInterval.HasValue)
                    {
                        await timer.DisposeAsync();
                        timer = _timerFactory.Create(newInterval.Value);
                    }

                    var waitResult = await WaitForNextTickAsync(timer, cancellationToken);
                    if (!waitResult.HasValue)
                    {
                        continue;
                    }

                    if (!waitResult.Value)
                    {
                        break;
                    }

                    UpdateTickTimes();

                    ManagedTimerTickResult? tickResult;
                    try
                    {
                        tickResult = await _tickHandler!.Invoke(cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        SetFault(exception);
                        break;
                    }

                    if (!ApplyTickResult(tickResult))
                    {
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                SetFault(exception);
            }
            finally
            {
                if (timer is not null)
                {
                    await timer.DisposeAsync();
                }

                CompleteRun();
            }
        }

        private async Task WaitWhilePausedAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                Task resumeTask;

                lock (_syncLock)
                {
                    if (_state != ManagedTimerState.Paused)
                    {
                        return;
                    }

                    resumeTask = _resumeSignal!.Task;
                }

                await Task.WhenAny(resumeTask, Task.Delay(Timeout.Infinite, cancellationToken));
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private async Task<bool?> WaitForNextTickAsync(IPeriodicTimer timer, CancellationToken cancellationToken)
        {
            var waitCancellation = new CancellationTokenSource();
            var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, waitCancellation.Token);

            lock (_syncLock)
            {
                _waitCanceled = false;
                _waitCancellation = waitCancellation;
            }

            try
            {
                var ticked = await timer.WaitForNextTickAsync(linkedCancellation.Token);
                if (!ticked)
                {
                    if (!cancellationToken.IsCancellationRequested && WasWaitCanceled())
                    {
                        return null;
                    }

                    return false;
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                if (!cancellationToken.IsCancellationRequested && WasWaitCanceled())
                {
                    return null;
                }

                return false;
            }
            finally
            {
                lock (_syncLock)
                {
                    if (ReferenceEquals(_waitCancellation, waitCancellation))
                    {
                        _waitCancellation = null;
                    }
                }

                linkedCancellation.Dispose();
                waitCancellation.Dispose();
            }
        }

        private bool ApplyTickResult(ManagedTimerTickResult? tickResult)
        {
            if (tickResult is null)
            {
                SetFault(new InvalidOperationException("Managed timer tick returned a null result."));
                return false;
            }

            if (tickResult.UpdatedInterval.HasValue)
            {
                UpdateIntervalInternal(tickResult.UpdatedInterval.Value);
            }

            if (tickResult.Action == ManagedTimerTickAction.Pause)
            {
                lock (_syncLock)
                {
                    _state = ManagedTimerState.Paused;
                    _nextTickUtc = null;
                    _resumeSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                }

                CancelWait();
                return true;
            }

            if (tickResult.Action == ManagedTimerTickAction.Stop)
            {
                return false;
            }

            return true;
        }

        private void UpdateIntervalInternal(TimeSpan interval)
        {
            lock (_syncLock)
            {
                _interval = interval;
                _intervalChangeRequested = true;
            }

            CancelWait();
        }

        private void UpdateTickTimes()
        {
            var now = DateTimeOffset.UtcNow;

            lock (_syncLock)
            {
                _lastTickUtc = now;
                _nextTickUtc = now.Add(_interval);
            }
        }

        private void SetFault(Exception exception)
        {
            lock (_syncLock)
            {
                _lastFault = exception;
                _state = ManagedTimerState.Faulted;
                _nextTickUtc = null;
            }
        }

        private void CancelWait()
        {
            CancellationTokenSource? waitCancellation;

            lock (_syncLock)
            {
                _waitCanceled = true;
                waitCancellation = _waitCancellation;
            }

            waitCancellation?.CancelIfNotDisposed();
        }

        private bool WasWaitCanceled()
        {
            lock (_syncLock)
            {
                return _waitCanceled;
            }
        }

        private void CompleteRun()
        {
            CancellationTokenSource? runCancellation;

            lock (_syncLock)
            {
                if (_state != ManagedTimerState.Faulted)
                {
                    _state = ManagedTimerState.Stopped;
                }

                _nextTickUtc = null;
                _runTask = null;
                runCancellation = _runCancellationTokenSource;
                _runCancellationTokenSource = null;
                _waitCancellation = null;
                _resumeSignal = null;
                _intervalChangeRequested = false;
                _waitCanceled = false;
            }

            runCancellation?.Dispose();
        }
    }
}
