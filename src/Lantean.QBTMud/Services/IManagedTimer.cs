namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Represents a controllable timer with observable state.
    /// </summary>
    public interface IManagedTimer : IAsyncDisposable
    {
        /// <summary>
        /// Gets the timer name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the current timer interval.
        /// </summary>
        TimeSpan Interval { get; }

        /// <summary>
        /// Gets the current timer state.
        /// </summary>
        ManagedTimerState State { get; }

        /// <summary>
        /// Gets the UTC timestamp of the last tick, if any.
        /// </summary>
        DateTimeOffset? LastTickUtc { get; }

        /// <summary>
        /// Gets the UTC timestamp for the next tick, if scheduled.
        /// </summary>
        DateTimeOffset? NextTickUtc { get; }

        /// <summary>
        /// Gets the last fault that stopped the timer, if any.
        /// </summary>
        Exception? LastFault { get; }

        /// <summary>
        /// Starts the timer loop.
        /// </summary>
        /// <param name="tickHandler">The handler to execute on each tick.</param>
        /// <param name="cancellationToken">A token that cancels the timer loop.</param>
        /// <returns><see langword="true"/> when the timer starts; otherwise <see langword="false"/>.</returns>
        Task<bool> StartAsync(Func<CancellationToken, Task<ManagedTimerTickResult>> tickHandler, CancellationToken cancellationToken);

        /// <summary>
        /// Pauses the timer.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when the timer pauses; otherwise <see langword="false"/>.</returns>
        Task<bool> PauseAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Resumes the timer.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when the timer resumes; otherwise <see langword="false"/>.</returns>
        Task<bool> ResumeAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops the timer.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when the timer stops; otherwise <see langword="false"/>.</returns>
        Task<bool> StopAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Updates the timer interval.
        /// </summary>
        /// <param name="interval">The new interval.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns><see langword="true"/> when the interval is updated; otherwise <see langword="false"/>.</returns>
        Task<bool> UpdateIntervalAsync(TimeSpan interval, CancellationToken cancellationToken);
    }
}
