using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class ManagedTimerOperationCanceledTests
    {
        private readonly IPeriodicTimer _timer;
        private readonly IPeriodicTimerFactory _timerFactory;
        private readonly ManagedTimer _target;
        private TaskCompletionSource<bool>? _pendingTick;
        private TaskCompletionSource<bool>? _waitEntered;
        private readonly Queue<bool> _scheduledResults = new Queue<bool>();
        private bool _disposed;
        private bool _throwOnCancellation;

        public ManagedTimerOperationCanceledTests()
        {
            _timer = Mock.Of<IPeriodicTimer>();
            _timerFactory = Mock.Of<IPeriodicTimerFactory>();
            _throwOnCancellation = true;
            ConfigureTimerMocks();
            _target = new ManagedTimer(_timerFactory, "Name", TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public async Task GIVEN_PausingWhileWaiting_WHEN_CancellationOccurs_THEN_DoesNotFault()
        {
            await _target.StartAsync(_ => Task.FromResult(ManagedTimerTickResult.Continue), CancellationToken.None);

            (await WaitForWaitAsync()).Should().BeTrue();

            var paused = await _target.PauseAsync(CancellationToken.None);
            paused.Should().BeTrue();
            _target.State.Should().Be(ManagedTimerState.Paused);

            _target.LastFault.Should().BeNull();

            var resumed = await _target.ResumeAsync(CancellationToken.None);
            resumed.Should().BeTrue();

            await TriggerTickAsync(false);
            await _target.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task GIVEN_StopWhileWaiting_WHEN_CancellationOccurs_THEN_StopsWithoutFault()
        {
            await _target.StartAsync(_ => Task.FromResult(ManagedTimerTickResult.Continue), CancellationToken.None);

            await Task.Yield();

            var stopped = await _target.StopAsync(CancellationToken.None);

            stopped.Should().BeTrue();
            _target.State.Should().Be(ManagedTimerState.Stopped);
            _target.LastFault.Should().BeNull();
        }

        private void ConfigureTimerMocks()
        {
            Mock.Get(_timerFactory)
                .Setup(factory => factory.Create(It.IsAny<TimeSpan>()))
                .Returns(_timer);

            Mock.Get(_timer)
                .Setup(timer => timer.WaitForNextTickAsync(It.IsAny<CancellationToken>()))
                .Returns((CancellationToken cancellationToken) => WaitForNextTickAsync(cancellationToken));

            Mock.Get(_timer)
                .Setup(timer => timer.DisposeAsync())
                .Returns(() =>
                {
                    _disposed = true;
                    _pendingTick?.TrySetResult(false);
                    return ValueTask.CompletedTask;
                });
        }

        private Task<bool> WaitForNextTickAsync(CancellationToken cancellationToken)
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
            _waitEntered?.TrySetResult(true);
            _waitEntered = null;
            var pendingTick = _pendingTick;
            cancellationToken.Register(() =>
            {
                if (_throwOnCancellation)
                {
                    pendingTick.TrySetException(new OperationCanceledException(cancellationToken));
                }
                else
                {
                    pendingTick.TrySetResult(false);
                }

                if (ReferenceEquals(_pendingTick, pendingTick))
                {
                    _pendingTick = null;
                }
            });
            return _pendingTick.Task;
        }

        private Task TriggerTickAsync(bool result = true)
        {
            if (_pendingTick is null)
            {
                _scheduledResults.Enqueue(result);
                return Task.CompletedTask;
            }

            var pendingTick = _pendingTick;
            _pendingTick = null;
            pendingTick.TrySetResult(result);
            return Task.CompletedTask;
        }

        private Task<bool> WaitForWaitAsync()
        {
            if (_pendingTick is not null)
            {
                return Task.FromResult(true);
            }

            _waitEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return WaitForTaskAsync(_waitEntered.Task);
        }

        private static async Task<bool> WaitForTaskAsync(Task task)
        {
            var timeout = TimeSpan.FromSeconds(10);
            var completed = await Task.WhenAny(task, Task.Delay(timeout));
            return ReferenceEquals(completed, task);
        }
    }
}
