namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Creates instances of <see cref="IPeriodicTimer"/>.
    /// </summary>
    public interface IPeriodicTimerFactory
    {
        /// <summary>
        /// Creates a timer that ticks at the specified interval.
        /// </summary>
        /// <param name="period">The interval between ticks.</param>
        /// <returns>A new <see cref="IPeriodicTimer"/> instance.</returns>
        IPeriodicTimer Create(TimeSpan period);
    }
}
