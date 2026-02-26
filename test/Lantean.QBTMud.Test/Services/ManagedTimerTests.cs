using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Moq;
using System.Diagnostics;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class ManagedTimerTests
    {
        private readonly IPeriodicTimer _timer;
        private readonly IPeriodicTimerFactory _timerFactory;
        private readonly ManagedTimer _target;
        private TaskCompletionSource<bool>? _pendingTick;
        private TaskCompletionSource<bool>? _waitEntered;
        private readonly Queue<bool> _scheduledResults = new Queue<bool>();
        private bool _disposed;

        public ManagedTimerTests()
        {
            _timer = Mock.Of<IPeriodicTimer>();
            _timerFactory = Mock.Of<IPeriodicTimerFactory>();
            ConfigureTimerMocks();
            _target = new ManagedTimer(_timerFactory, "Name", TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public void GIVEN_InvalidInterval_WHEN_Constructing_THEN_ThrowsArgumentOutOfRangeException()
        {
            Action action = () => new ManagedTimer(_timerFactory, "Name", TimeSpan.Zero);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task GIVEN_StoppedTimer_WHEN_PauseResumeStop_THEN_ReturnsFalse()
        {
            var paused = await _target.PauseAsync(CancellationToken.None);
            var resumed = await _target.ResumeAsync(CancellationToken.None);
            var stopped = await _target.StopAsync(CancellationToken.None);

            paused.Should().BeFalse();
            resumed.Should().BeFalse();
            stopped.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_StoppedTimerWithoutHandler_WHEN_Restarted_THEN_ReturnsFalse()
        {
            var restarted = await _target.RestartAsync(CancellationToken.None);

            restarted.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_UpdateIntervalSame_WHEN_Updating_THEN_ReturnsFalse()
        {
            var updated = await _target.UpdateIntervalAsync(TimeSpan.FromMilliseconds(100), CancellationToken.None);

            updated.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_UpdateIntervalDifferent_WHEN_Updating_THEN_ReturnsTrueAndUpdatesInterval()
        {
            var updated = await _target.UpdateIntervalAsync(TimeSpan.FromMilliseconds(250), CancellationToken.None);

            updated.Should().BeTrue();
            _target.Interval.Should().Be(TimeSpan.FromMilliseconds(250));
        }

        [Fact]
        public async Task GIVEN_InvalidInterval_WHEN_Updating_THEN_ThrowsArgumentOutOfRangeException()
        {
            Func<Task> action = async () => await _target.UpdateIntervalAsync(TimeSpan.Zero, CancellationToken.None);

            await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task GIVEN_RunningTimer_WHEN_StartAgain_THEN_ReturnsFalse()
        {
            var started = await _target.StartAsync(_ => Task.FromResult(ManagedTimerTickResult.Continue), CancellationToken.None);
            var startedAgain = await _target.StartAsync(_ => Task.FromResult(ManagedTimerTickResult.Continue), CancellationToken.None);

            started.Should().BeTrue();
            startedAgain.Should().BeFalse();

            await _target.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task GIVEN_RunningTimer_WHEN_Restarted_THEN_ReturnsFalse()
        {
            await _target.StartAsync(_ => Task.FromResult(ManagedTimerTickResult.Continue), CancellationToken.None);

            var restarted = await _target.RestartAsync(CancellationToken.None);

            restarted.Should().BeFalse();
            await _target.StopAsync(CancellationToken.None);
        }

        [Fact]
        public async Task GIVEN_StoppedTimerWithHandler_WHEN_Restarted_THEN_TicksAgain()
        {
            var tickCount = 0;

            await _target.StartAsync(
                _ =>
                {
                    tickCount++;
                    return Task.FromResult(ManagedTimerTickResult.Stop);
                },
                CancellationToken.None);

            await TriggerTickAsync();
            (await WaitUntilAsync(() => tickCount == 1)).Should().BeTrue();
            await _target.StopAsync(CancellationToken.None);
            tickCount.Should().Be(1);

            _disposed = false;

            var restarted = await _target.RestartAsync(CancellationToken.None);
            restarted.Should().BeTrue();

            await TriggerTickAsync();
            (await WaitUntilAsync(() => tickCount == 2)).Should().BeTrue();
            await _target.StopAsync(CancellationToken.None);

            tickCount.Should().Be(2);
            _target.State.Should().Be(ManagedTimerState.Stopped);
        }

        [Fact]
        public async Task GIVEN_RunningTimer_WHEN_PauseAndResume_THEN_StateTransitions()
        {
            await _target.StartAsync(_ => Task.FromResult(ManagedTimerTickResult.Continue), CancellationToken.None);

            (await WaitForWaitAsync()).Should().BeTrue();

            var paused = await _target.PauseAsync(CancellationToken.None);
            paused.Should().BeTrue();
            _target.State.Should().Be(ManagedTimerState.Paused);

            var resumed = await _target.ResumeAsync(CancellationToken.None);
            resumed.Should().BeTrue();
            _target.State.Should().Be(ManagedTimerState.Running);
            await _target.StopAsync(CancellationToken.None);

            _target.State.Should().Be(ManagedTimerState.Stopped);
        }

        [Fact]
        public async Task GIVEN_TickResultPause_WHEN_Resumed_THEN_StateReturnsToRunning()
        {
            var tickCount = 0;
            var firstTick = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            await _target.StartAsync(
                _ =>
                {
                    tickCount++;
                    if (tickCount == 1)
                    {
                        firstTick.TrySetResult(true);
                        return Task.FromResult(ManagedTimerTickResult.Pause);
                    }

                    return Task.FromResult(ManagedTimerTickResult.Stop);
                },
                CancellationToken.None);

            (await WaitForWaitAsync()).Should().BeTrue();
            await TriggerTickAsync();
            (await WaitForTaskAsync(firstTick.Task)).Should().BeTrue();
            (await WaitUntilAsync(() => _target.State == ManagedTimerState.Paused)).Should().BeTrue();

            var resumed = await _target.ResumeAsync(CancellationToken.None);
            resumed.Should().BeTrue();
            (await WaitUntilAsync(() => _target.State == ManagedTimerState.Running)).Should().BeTrue();
            (await WaitForWaitAsync()).Should().BeTrue();

            await _target.StopAsync(CancellationToken.None);

            tickCount.Should().Be(1);
            _target.State.Should().Be(ManagedTimerState.Stopped);
        }

        [Fact]
        public async Task GIVEN_TickResultStop_WHEN_Ticked_THEN_StopsAndUpdatesTickTimes()
        {
            var ticked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            await _target.StartAsync(
                _ =>
                {
                    ticked.TrySetResult(true);
                    return Task.FromResult(ManagedTimerTickResult.Stop);
                },
                CancellationToken.None);

            await TriggerTickAsync();
            await ticked.Task;
            await _target.StopAsync(CancellationToken.None);

            _target.LastTickUtc.Should().NotBeNull();
            _target.NextTickUtc.Should().BeNull();
            _target.State.Should().Be(ManagedTimerState.Stopped);
        }

        [Fact]
        public async Task GIVEN_TickResultUpdatesInterval_WHEN_Ticked_THEN_IntervalUpdated()
        {
            var ticked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            await _target.StartAsync(
                _ =>
                {
                    ticked.TrySetResult(true);
                    return Task.FromResult(new ManagedTimerTickResult(ManagedTimerTickAction.Stop, TimeSpan.FromMilliseconds(400)));
                },
                CancellationToken.None);

            await TriggerTickAsync();
            await ticked.Task;
            await _target.StopAsync(CancellationToken.None);

            _target.Interval.Should().Be(TimeSpan.FromMilliseconds(400));
        }

        [Fact]
        public async Task GIVEN_FirstTickContinues_WHEN_SecondTickStops_THEN_ProcessesContinueBranch()
        {
            var tickCount = 0;

            await _target.StartAsync(
                _ =>
                {
                    tickCount++;
                    return Task.FromResult(tickCount == 1 ? ManagedTimerTickResult.Continue : ManagedTimerTickResult.Stop);
                },
                CancellationToken.None);

            await TriggerTickAsync();
            await TriggerTickAsync();
            (await WaitUntilAsync(() => _target.State == ManagedTimerState.Stopped)).Should().BeTrue();
            await _target.StopAsync(CancellationToken.None);

            tickCount.Should().Be(2);
        }

        [Fact]
        public async Task GIVEN_WaitReturnsFalse_WHEN_NoTick_THEN_HandlerNotInvoked()
        {
            var tickCount = 0;

            await _target.StartAsync(
                _ =>
                {
                    tickCount++;
                    return Task.FromResult(ManagedTimerTickResult.Stop);
                },
                CancellationToken.None);

            await TriggerTickAsync(false);
            await _target.StopAsync(CancellationToken.None);

            tickCount.Should().Be(0);
            _target.State.Should().Be(ManagedTimerState.Stopped);
        }

        [Fact]
        public async Task GIVEN_TickThrows_WHEN_Ticked_THEN_StateFaultedAndFaultCaptured()
        {
            await _target.StartAsync(_ => throw new InvalidOperationException("Failure"), CancellationToken.None);

            await TriggerTickAsync();
            (await WaitUntilAsync(() => _target.State == ManagedTimerState.Faulted)).Should().BeTrue();
            await _target.StopAsync(CancellationToken.None);

            _target.LastFault.Should().NotBeNull();
            _target.State.Should().Be(ManagedTimerState.Faulted);
        }

        [Fact]
        public async Task GIVEN_TickReturnsNull_WHEN_Ticked_THEN_StateFaulted()
        {
            await _target.StartAsync(_ => Task.FromResult<ManagedTimerTickResult>(null!), CancellationToken.None);

            await TriggerTickAsync();
            (await WaitUntilAsync(() => _target.State == ManagedTimerState.Faulted)).Should().BeTrue();
            await _target.StopAsync(CancellationToken.None);

            _target.LastFault.Should().NotBeNull();
            _target.State.Should().Be(ManagedTimerState.Faulted);
        }

        [Fact]
        public async Task GIVEN_FaultedTimer_WHEN_Start_THEN_AllowsRestart()
        {
            await _target.StartAsync(_ => throw new InvalidOperationException("Failure"), CancellationToken.None);
            await TriggerTickAsync();
            (await WaitUntilAsync(() => _target.State == ManagedTimerState.Faulted)).Should().BeTrue();

            var restarted = await _target.StartAsync(_ => Task.FromResult(ManagedTimerTickResult.Stop), CancellationToken.None);

            restarted.Should().BeTrue();
            await TriggerTickAsync();
            await _target.StopAsync(CancellationToken.None);
            _target.State.Should().Be(ManagedTimerState.Stopped);
        }

        [Fact]
        public async Task GIVEN_DisposedTimer_WHEN_StartOrUpdateInterval_THEN_ReturnsFalse()
        {
            await _target.DisposeAsync();

            var started = await _target.StartAsync(_ => Task.FromResult(ManagedTimerTickResult.Stop), CancellationToken.None);
            var restarted = await _target.RestartAsync(CancellationToken.None);
            var updated = await _target.UpdateIntervalAsync(TimeSpan.FromMilliseconds(200), CancellationToken.None);

            started.Should().BeFalse();
            restarted.Should().BeFalse();
            updated.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_DisposedTimer_WHEN_PausedOrResumed_THEN_ReturnsFalse()
        {
            await _target.DisposeAsync();

            var paused = await _target.PauseAsync(CancellationToken.None);
            var resumed = await _target.ResumeAsync(CancellationToken.None);

            paused.Should().BeFalse();
            resumed.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_RunningTimer_WHEN_IntervalUpdated_THEN_ReconfiguresTimer()
        {
            await _target.StartAsync(_ => Task.FromResult(ManagedTimerTickResult.Continue), CancellationToken.None);
            (await WaitForWaitAsync()).Should().BeTrue();

            var updated = await _target.UpdateIntervalAsync(TimeSpan.FromMilliseconds(250), CancellationToken.None);
            updated.Should().BeTrue();

            (await WaitUntilAsync(() =>
            {
                try
                {
                    Mock.Get(_timerFactory).Verify(factory => factory.Create(TimeSpan.FromMilliseconds(250)), Times.AtLeastOnce);
                    return true;
                }
                catch (MockException)
                {
                    return false;
                }
            })).Should().BeTrue();

            await _target.StopAsync(CancellationToken.None);

            Mock.Get(_timer).Verify(timer => timer.DisposeAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task GIVEN_RunningTimer_WHEN_StoppedWhilePaused_THEN_DoesNotFault()
        {
            await _target.StartAsync(_ => Task.FromResult(ManagedTimerTickResult.Continue), CancellationToken.None);
            (await WaitForWaitAsync()).Should().BeTrue();

            var paused = await _target.PauseAsync(CancellationToken.None);
            paused.Should().BeTrue();

            var stopped = await _target.StopAsync(CancellationToken.None);
            stopped.Should().BeTrue();
            _target.LastFault.Should().BeNull();
            _target.State.Should().Be(ManagedTimerState.Stopped);
        }

        [Fact]
        public async Task GIVEN_WaitThrowsUnexpectedException_WHEN_Running_THEN_FaultIsCaptured()
        {
            Mock.Get(_timer)
                .Setup(timer => timer.WaitForNextTickAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Failure"));

            await _target.StartAsync(_ => Task.FromResult(ManagedTimerTickResult.Continue), CancellationToken.None);

            (await WaitUntilAsync(() => _target.State == ManagedTimerState.Faulted)).Should().BeTrue();
            _target.LastFault.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public async Task GIVEN_TickThrowsOperationCanceledException_WHEN_Ticked_THEN_StopsWithoutFault()
        {
            await _target.StartAsync(_ => throw new OperationCanceledException(), CancellationToken.None);

            await TriggerTickAsync();
            (await WaitUntilAsync(() => _target.State == ManagedTimerState.Stopped)).Should().BeTrue();
            _target.LastFault.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_DisposeCalledTwice_WHEN_Disposing_THEN_NoError()
        {
            await _target.DisposeAsync();
            await _target.DisposeAsync();

            _target.State.Should().Be(ManagedTimerState.Stopped);
        }

        private static async Task<bool> WaitUntilAsync(Func<bool> condition)
        {
            var timeout = TimeSpan.FromSeconds(10);
            var stopwatch = Stopwatch.StartNew();
            while (!condition())
            {
                if (stopwatch.Elapsed >= timeout)
                {
                    return false;
                }

                await Task.Yield();
            }

            return true;
        }

        private static async Task<bool> WaitForTaskAsync(Task task)
        {
            var timeout = TimeSpan.FromSeconds(10);
            try
            {
                await task.WaitAsync(timeout);
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
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
                pendingTick.TrySetResult(false);
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
    }
}
