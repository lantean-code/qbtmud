namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Creates instances of <see cref="IManagedTimer"/>.
    /// </summary>
    public interface IManagedTimerFactory
    {
        /// <summary>
        /// Creates a managed timer with the specified name and interval.
        /// </summary>
        /// <param name="name">The timer name.</param>
        /// <param name="interval">The timer interval.</param>
        /// <param name="retryCount">
        /// The number of consecutive unhandled tick exceptions to tolerate before faulting the timer.
        /// </param>
        /// <returns>A new <see cref="IManagedTimer"/> instance.</returns>
        IManagedTimer Create(string name, TimeSpan interval, int retryCount = 0);
    }
}
