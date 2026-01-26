using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class ManagedTimerFactoryTests
    {
        private readonly IPeriodicTimerFactory _timerFactory;
        private readonly IManagedTimerRegistry _registry;
        private readonly ManagedTimerFactory _target;

        public ManagedTimerFactoryTests()
        {
            _timerFactory = Mock.Of<IPeriodicTimerFactory>();
            _registry = Mock.Of<IManagedTimerRegistry>();
            _target = new ManagedTimerFactory(_timerFactory, _registry);
        }

        [Fact]
        public void GIVEN_ValidArguments_WHEN_Create_THEN_ReturnsManagedTimer()
        {
            var result = _target.Create("Name", TimeSpan.FromSeconds(1));

            result.Name.Should().Be("Name");
            result.Interval.Should().Be(TimeSpan.FromSeconds(1));
            result.State.Should().Be(ManagedTimerState.Stopped);
            Mock.Get(_registry).Verify(r => r.Register(result), Times.Once);
        }

        [Fact]
        public void GIVEN_MultipleCalls_WHEN_Create_THEN_ReturnsDistinctInstances()
        {
            var first = _target.Create("Name", TimeSpan.FromSeconds(1));
            var second = _target.Create("Name", TimeSpan.FromSeconds(1));

            ReferenceEquals(first, second).Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_TimerCreated_WHEN_Disposed_THEN_Unregisters()
        {
            var timer = _target.Create("Name", TimeSpan.FromSeconds(1));

            await timer.DisposeAsync();

            Mock.Get(_registry).Verify(r => r.Unregister(timer), Times.Once);
        }
    }
}
