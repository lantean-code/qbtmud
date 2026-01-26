namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Creates managed timers backed by periodic timers.
    /// </summary>
    public sealed class ManagedTimerFactory : IManagedTimerFactory
    {
        private readonly IPeriodicTimerFactory _timerFactory;
        private readonly IManagedTimerRegistry _registry;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedTimerFactory"/> class.
        /// </summary>
        /// <param name="timerFactory">The periodic timer factory.</param>
        /// <param name="registry">The registry used to track managed timers.</param>
        public ManagedTimerFactory(IPeriodicTimerFactory timerFactory, IManagedTimerRegistry registry)
        {
            _timerFactory = timerFactory;
            _registry = registry;
        }

        /// <summary>
        /// Creates a managed timer with the specified name and interval.
        /// </summary>
        /// <param name="name">The timer name.</param>
        /// <param name="interval">The timer interval.</param>
        /// <returns>A new <see cref="IManagedTimer"/> instance.</returns>
        public IManagedTimer Create(string name, TimeSpan interval)
        {
            var timer = new RegisteredManagedTimer(new ManagedTimer(_timerFactory, name, interval), _registry);
            _registry.Register(timer);
            return timer;
        }
    }
}
