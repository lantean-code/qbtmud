using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class ManagedTimerFactoryTests
    {
        private readonly IPeriodicTimerFactory _timerFactory;
        private readonly ManagedTimerFactory _target;

        public ManagedTimerFactoryTests()
        {
            _timerFactory = Mock.Of<IPeriodicTimerFactory>();
            _target = new ManagedTimerFactory(_timerFactory);
        }

        [Fact]
        public void GIVEN_ValidArguments_WHEN_Create_THEN_ReturnsManagedTimer()
        {
            var result = _target.Create("Name", TimeSpan.FromSeconds(1));

            result.Name.Should().Be("Name");
            result.Interval.Should().Be(TimeSpan.FromSeconds(1));
            result.State.Should().Be(ManagedTimerState.Stopped);
        }

        [Fact]
        public void GIVEN_MultipleCalls_WHEN_Create_THEN_ReturnsDistinctInstances()
        {
            var first = _target.Create("Name", TimeSpan.FromSeconds(1));
            var second = _target.Create("Name", TimeSpan.FromSeconds(1));

            ReferenceEquals(first, second).Should().BeFalse();
        }
    }
}
