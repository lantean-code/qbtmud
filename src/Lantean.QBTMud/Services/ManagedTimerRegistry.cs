namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Maintains a registry of managed timers.
    /// </summary>
    public sealed class ManagedTimerRegistry : IManagedTimerRegistry
    {
        private readonly object _syncLock = new();
        private readonly List<IManagedTimer> _timers = [];

        /// <summary>
        /// Gets the currently registered timers.
        /// </summary>
        /// <returns>The registered timers.</returns>
        public IReadOnlyList<IManagedTimer> GetTimers()
        {
            lock (_syncLock)
            {
                return _timers.AsReadOnly();
            }
        }

        /// <summary>
        /// Registers a managed timer.
        /// </summary>
        /// <param name="timer">The timer to register.</param>
        public void Register(IManagedTimer timer)
        {
            lock (_syncLock)
            {
                if (_timers.Contains(timer))
                {
                    return;
                }

                var existingIndex = _timers.FindIndex(existing => string.Equals(existing.Name, timer.Name, StringComparison.Ordinal));
                if (existingIndex >= 0)
                {
                    _timers[existingIndex] = timer;
                    return;
                }

                _timers.Add(timer);
            }
        }

        /// <summary>
        /// Unregisters a managed timer.
        /// </summary>
        /// <param name="timer">The timer to unregister.</param>
        public void Unregister(IManagedTimer timer)
        {
            lock (_syncLock)
            {
                _timers.Remove(timer);
            }
        }
    }
}
