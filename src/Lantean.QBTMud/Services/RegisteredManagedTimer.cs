namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Wraps a managed timer and unregisters it when disposed.
    /// </summary>
    public sealed class RegisteredManagedTimer : IManagedTimer
    {
        private readonly IManagedTimer _inner;
        private readonly IManagedTimerRegistry _registry;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisteredManagedTimer"/> class.
        /// </summary>
        /// <param name="inner">The inner managed timer.</param>
        /// <param name="registry">The registry used to unregister the timer.</param>
        public RegisteredManagedTimer(IManagedTimer inner, IManagedTimerRegistry registry)
        {
            _inner = inner;
            _registry = registry;
        }

        /// <summary>
        /// Gets the timer name.
        /// </summary>
        public string Name
        {
            get
            {
                return _inner.Name;
            }
        }

        /// <summary>
        /// Gets the current timer interval.
        /// </summary>
        public TimeSpan Interval
        {
            get
            {
                return _inner.Interval;
            }
        }

        /// <summary>
        /// Gets the current timer state.
        /// </summary>
        public ManagedTimerState State
        {
            get
            {
                return _inner.State;
            }
        }

        /// <summary>
        /// Gets the UTC timestamp of the last tick, if any.
        /// </summary>
        public DateTimeOffset? LastTickUtc
        {
            get
            {
                return _inner.LastTickUtc;
            }
        }

        /// <summary>
        /// Gets the UTC timestamp for the next tick, if scheduled.
        /// </summary>
        public DateTimeOffset? NextTickUtc
        {
            get
            {
                return _inner.NextTickUtc;
            }
        }

        /// <summary>
        /// Gets the last fault that stopped the timer, if any.
        /// </summary>
        public Exception? LastFault
        {
            get
            {
                return _inner.LastFault;
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
            return _inner.StartAsync(tickHandler, cancellationToken);
        }

        /// <summary>
        /// Pauses the timer.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when the timer pauses; otherwise <see langword="false"/>.</returns>
        public Task<bool> PauseAsync(CancellationToken cancellationToken)
        {
            return _inner.PauseAsync(cancellationToken);
        }

        /// <summary>
        /// Resumes the timer.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when the timer resumes; otherwise <see langword="false"/>.</returns>
        public Task<bool> ResumeAsync(CancellationToken cancellationToken)
        {
            return _inner.ResumeAsync(cancellationToken);
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when the timer stops; otherwise <see langword="false"/>.</returns>
        public Task<bool> StopAsync(CancellationToken cancellationToken)
        {
            return _inner.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Updates the timer interval.
        /// </summary>
        /// <param name="interval">The new interval.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when the interval is updated; otherwise <see langword="false"/>.</returns>
        public Task<bool> UpdateIntervalAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            return _inner.UpdateIntervalAsync(interval, cancellationToken);
        }

        /// <summary>
        /// Disposes the timer and unregisters it.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            try
            {
                await _inner.DisposeAsync();
            }
            finally
            {
                _registry.Unregister(this);
            }
        }
    }
}
