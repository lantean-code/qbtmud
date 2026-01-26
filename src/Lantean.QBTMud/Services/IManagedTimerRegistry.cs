namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Tracks managed timers for UI inspection and control.
    /// </summary>
    public interface IManagedTimerRegistry
    {
        /// <summary>
        /// Gets the currently registered timers.
        /// </summary>
        /// <returns>The registered timers.</returns>
        IReadOnlyList<IManagedTimer> GetTimers();

        /// <summary>
        /// Registers a managed timer.
        /// </summary>
        /// <param name="timer">The timer to register.</param>
        void Register(IManagedTimer timer);

        /// <summary>
        /// Unregisters a managed timer.
        /// </summary>
        /// <param name="timer">The timer to unregister.</param>
        void Unregister(IManagedTimer timer);
    }
}
