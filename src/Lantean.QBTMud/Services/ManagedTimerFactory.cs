namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Creates managed timers backed by periodic timers.
    /// </summary>
    public sealed class ManagedTimerFactory : IManagedTimerFactory
    {
        private readonly IPeriodicTimerFactory _timerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedTimerFactory"/> class.
        /// </summary>
        /// <param name="timerFactory">The periodic timer factory.</param>
        public ManagedTimerFactory(IPeriodicTimerFactory timerFactory)
        {
            _timerFactory = timerFactory;
        }

        /// <summary>
        /// Creates a managed timer with the specified name and interval.
        /// </summary>
        /// <param name="name">The timer name.</param>
        /// <param name="interval">The timer interval.</param>
        /// <returns>A new <see cref="IManagedTimer"/> instance.</returns>
        public IManagedTimer Create(string name, TimeSpan interval)
        {
            return new ManagedTimer(_timerFactory, name, interval);
        }
    }
}
