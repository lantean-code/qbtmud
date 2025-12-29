namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Creates timers that wrap <see cref="PeriodicTimer"/>.
    /// </summary>
    public sealed class PeriodicTimerFactory : IPeriodicTimerFactory
    {
        public IPeriodicTimer Create(TimeSpan period)
        {
            return new PeriodicTimerAdapter(period);
        }

        private sealed class PeriodicTimerAdapter : IPeriodicTimer
        {
            private readonly PeriodicTimer _timer;
            private bool _disposedValue;

            public PeriodicTimerAdapter(TimeSpan period)
            {
                _timer = new PeriodicTimer(period);
            }

            public async Task<bool> WaitForNextTickAsync(CancellationToken cancellationToken)
            {
                if (_disposedValue)
                {
                    return false;
                }

                return await _timer.WaitForNextTickAsync(cancellationToken);
            }

            public async ValueTask DisposeAsync()
            {
                if (_disposedValue)
                {
                    return;
                }

                _disposedValue = true;
                _timer.Dispose();
                await Task.CompletedTask;
            }
        }
    }
}
