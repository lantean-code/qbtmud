namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Represents the outcome of a managed timer tick.
    /// </summary>
    public sealed class ManagedTimerTickResult
    {
        private const string IntervalMessage = "Interval must be greater than zero.";

        /// <summary>
        /// Gets a tick result that continues scheduling.
        /// </summary>
        public static ManagedTimerTickResult Continue { get; } = new ManagedTimerTickResult(ManagedTimerTickAction.Continue);

        /// <summary>
        /// Gets a tick result that pauses scheduling.
        /// </summary>
        public static ManagedTimerTickResult Pause { get; } = new ManagedTimerTickResult(ManagedTimerTickAction.Pause);

        /// <summary>
        /// Gets a tick result that stops scheduling.
        /// </summary>
        public static ManagedTimerTickResult Stop { get; } = new ManagedTimerTickResult(ManagedTimerTickAction.Stop);

        /// <summary>
        /// Gets the action that should be taken after the tick.
        /// </summary>
        public ManagedTimerTickAction Action { get; }

        /// <summary>
        /// Gets the updated interval for the next tick, if provided.
        /// </summary>
        public TimeSpan? UpdatedInterval { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedTimerTickResult"/> class.
        /// </summary>
        /// <param name="action">The action to take after the tick.</param>
        public ManagedTimerTickResult(ManagedTimerTickAction action)
        {
            Action = action;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedTimerTickResult"/> class.
        /// </summary>
        /// <param name="action">The action to take after the tick.</param>
        /// <param name="updatedInterval">The updated interval for the next tick.</param>
        public ManagedTimerTickResult(ManagedTimerTickAction action, TimeSpan updatedInterval)
        {
            if (updatedInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(updatedInterval), updatedInterval, IntervalMessage);
            }

            Action = action;
            UpdatedInterval = updatedInterval;
        }

        /// <summary>
        /// Creates a tick result that updates the interval and continues scheduling.
        /// </summary>
        /// <param name="updatedInterval">The updated interval for the next tick.</param>
        /// <returns>A configured <see cref="ManagedTimerTickResult"/>.</returns>
        public static ManagedTimerTickResult UpdateInterval(TimeSpan updatedInterval)
        {
            return new ManagedTimerTickResult(ManagedTimerTickAction.Continue, updatedInterval);
        }
    }
}
