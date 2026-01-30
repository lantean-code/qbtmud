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
            timer.SetupGet(t => t.Name).Returns("Name");

            _target.Register(timer.Object);

            var timers = _target.GetTimers();
            timers.Should().ContainSingle().Which.Should().Be(timer.Object);
        }

        [Fact]
        public void GIVEN_TimerRegisteredTwice_WHEN_Register_THEN_Deduplicates()
        {
            var timer = new Mock<IManagedTimer>();
            timer.SetupGet(t => t.Name).Returns("Name");

            _target.Register(timer.Object);
            _target.Register(timer.Object);

            _target.GetTimers().Should().ContainSingle();
        }

        [Fact]
        public void GIVEN_TimerWithSameNameRegistered_WHEN_Register_THEN_Replaces()
        {
            var first = new Mock<IManagedTimer>();
            first.SetupGet(t => t.Name).Returns("Name");
            var second = new Mock<IManagedTimer>();
            second.SetupGet(t => t.Name).Returns("Name");

            _target.Register(first.Object);
            _target.Register(second.Object);

            _target.GetTimers().Should().ContainSingle().Which.Should().Be(second.Object);
        }

        [Fact]
        public void GIVEN_TimersWithDifferentNamesRegistered_WHEN_GetTimers_THEN_ReturnsAll()
        {
            var first = new Mock<IManagedTimer>();
            first.SetupGet(t => t.Name).Returns("First");
            var second = new Mock<IManagedTimer>();
            second.SetupGet(t => t.Name).Returns("Second");

            _target.Register(first.Object);
            _target.Register(second.Object);

            _target.GetTimers().Should().HaveCount(2).And.Contain(new[] { first.Object, second.Object });
        }

        [Fact]
        public void GIVEN_TimerUnregistered_WHEN_GetTimers_THEN_Removed()
        {
            var timer = new Mock<IManagedTimer>();
            timer.SetupGet(t => t.Name).Returns("Name");

            _target.Register(timer.Object);
            _target.Unregister(timer.Object);

            _target.GetTimers().Should().BeEmpty();
        }
    }
}
