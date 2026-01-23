namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Describes the action a managed timer should take after a tick.
    /// </summary>
    public enum ManagedTimerTickAction
    {
        /// <summary>
        /// Continue scheduling ticks.
        /// </summary>
        Continue = 0,

        /// <summary>
        /// Pause the timer.
        /// </summary>
        Pause = 1,

        /// <summary>
        /// Stop the timer.
        /// </summary>
        Stop = 2,
    }
}
