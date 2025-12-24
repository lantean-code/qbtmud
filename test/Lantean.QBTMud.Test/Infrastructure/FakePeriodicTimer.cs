using Lantean.QBTMud.Services;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lantean.QBTMud.Test.Infrastructure
{
    public sealed class FakePeriodicTimerFactory : IPeriodicTimerFactory
    {
        private readonly FakePeriodicTimer _timer;

        public FakePeriodicTimerFactory(FakePeriodicTimer timer)
        {
            _timer = timer;
        }

        public IPeriodicTimer Create(TimeSpan period)
        {
            return _timer;
        }
    }

    public sealed class FakePeriodicTimer : IPeriodicTimer
    {
        private bool _disposed;
        private TaskCompletionSource<bool>? _pendingTick;
        private readonly Queue<bool> _scheduledResults = new Queue<bool>();

        public Task<bool> WaitForNextTickAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                return Task.FromResult(false);
            }

            if (_scheduledResults.Count > 0)
            {
                return Task.FromResult(_scheduledResults.Dequeue());
            }

            _pendingTick = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => _pendingTick.TrySetResult(false));
            return _pendingTick.Task;
        }

        public Task TriggerTickAsync(bool result = true)
        {
            if (_pendingTick is null)
            {
                _scheduledResults.Enqueue(result);
                return Task.CompletedTask;
            }

            _pendingTick.TrySetResult(result);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            _pendingTick?.TrySetResult(false);
            return ValueTask.CompletedTask;
        }
    }
}
