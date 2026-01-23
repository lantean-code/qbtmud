using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class ManagedTimerRegistryTests
    {
        private readonly ManagedTimerRegistry _target;

        public ManagedTimerRegistryTests()
        {
            _target = new ManagedTimerRegistry();
        }

        [Fact]
        public void GIVEN_NoTimers_WHEN_GetTimers_THEN_ReturnsEmpty()
        {
            var timers = _target.GetTimers();

            timers.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_TimerRegistered_WHEN_GetTimers_THEN_ReturnsTimer()
        {
            var timer = new Mock<IManagedTimer>();

            _target.Register(timer.Object);

            var timers = _target.GetTimers();
            timers.Should().ContainSingle().Which.Should().Be(timer.Object);
        }

        [Fact]
        public void GIVEN_TimerRegisteredTwice_WHEN_Register_THEN_Deduplicates()
        {
            var timer = new Mock<IManagedTimer>();

            _target.Register(timer.Object);
            _target.Register(timer.Object);

            _target.GetTimers().Should().ContainSingle();
        }

        [Fact]
        public void GIVEN_TimerUnregistered_WHEN_GetTimers_THEN_Removed()
        {
            var timer = new Mock<IManagedTimer>();

            _target.Register(timer.Object);
            _target.Unregister(timer.Object);

            _target.GetTimers().Should().BeEmpty();
        }
    }
}
