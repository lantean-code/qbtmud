namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Represents the runtime state of a managed timer.
    /// </summary>
    public enum ManagedTimerState
    {
        /// <summary>
        /// The timer is not running.
        /// </summary>
        Stopped = 0,

        /// <summary>
        /// The timer is running and waiting for ticks.
        /// </summary>
        Running = 1,

        /// <summary>
        /// The timer is paused and will not tick until resumed.
        /// </summary>
        Paused = 2,

        /// <summary>
        /// The timer stopped because an unhandled error occurred.
        /// </summary>
        Faulted = 3,
    }
}
