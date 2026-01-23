using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class RegisteredManagedTimerTests
    {
        private readonly IManagedTimer _inner;
        private readonly IManagedTimerRegistry _registry;
        private readonly RegisteredManagedTimer _target;

        public RegisteredManagedTimerTests()
        {
            _inner = Mock.Of<IManagedTimer>();
            _registry = Mock.Of<IManagedTimerRegistry>();
            _target = new RegisteredManagedTimer(_inner, _registry);
        }

        [Fact]
        public void GIVEN_InnerProperties_WHEN_Read_THEN_ReturnsInnerValues()
        {
            var lastTick = DateTimeOffset.UtcNow;
            var nextTick = DateTimeOffset.UtcNow.AddSeconds(5);
            var fault = new InvalidOperationException("Failure");

            Mock.Get(_inner).SetupGet(t => t.Name).Returns("Name");
            Mock.Get(_inner).SetupGet(t => t.Interval).Returns(TimeSpan.FromSeconds(1));
            Mock.Get(_inner).SetupGet(t => t.State).Returns(ManagedTimerState.Running);
            Mock.Get(_inner).SetupGet(t => t.LastTickUtc).Returns(lastTick);
            Mock.Get(_inner).SetupGet(t => t.NextTickUtc).Returns(nextTick);
            Mock.Get(_inner).SetupGet(t => t.LastFault).Returns(fault);

            _target.Name.Should().Be("Name");
            _target.Interval.Should().Be(TimeSpan.FromSeconds(1));
            _target.State.Should().Be(ManagedTimerState.Running);
            _target.LastTickUtc.Should().Be(lastTick);
            _target.NextTickUtc.Should().Be(nextTick);
            _target.LastFault.Should().Be(fault);
        }

        [Fact]
        public async Task GIVEN_InnerOperations_WHEN_Invoked_THEN_Delegates()
        {
            Mock.Get(_inner)
                .Setup(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            Mock.Get(_inner)
                .Setup(t => t.PauseAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            Mock.Get(_inner)
                .Setup(t => t.ResumeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            Mock.Get(_inner)
                .Setup(t => t.StopAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            Mock.Get(_inner)
                .Setup(t => t.UpdateIntervalAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var started = await _target.StartAsync(_ => Task.FromResult(ManagedTimerTickResult.Continue), CancellationToken.None);
            var paused = await _target.PauseAsync(CancellationToken.None);
            var resumed = await _target.ResumeAsync(CancellationToken.None);
            var stopped = await _target.StopAsync(CancellationToken.None);
            var updated = await _target.UpdateIntervalAsync(TimeSpan.FromSeconds(1), CancellationToken.None);

            started.Should().BeTrue();
            paused.Should().BeTrue();
            resumed.Should().BeFalse();
            stopped.Should().BeTrue();
            updated.Should().BeTrue();

            Mock.Get(_inner).Verify(t => t.StartAsync(It.IsAny<Func<CancellationToken, Task<ManagedTimerTickResult>>>(), It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_inner).Verify(t => t.PauseAsync(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_inner).Verify(t => t.ResumeAsync(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_inner).Verify(t => t.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_inner).Verify(t => t.UpdateIntervalAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_DisposeInvoked_WHEN_Disposed_THEN_Unregisters()
        {
            Mock.Get(_inner).Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

            await _target.DisposeAsync();

            Mock.Get(_registry).Verify(r => r.Unregister(_target), Times.Once);
        }
    }
}
