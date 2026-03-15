using AwesomeAssertions;
using Lantean.QBTMud.Services;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class ManagedTimerTickResultTests
    {
        [Fact]
        public void GIVEN_ActionOnlyConstructor_WHEN_Created_THEN_SetsActionAndNoInterval()
        {
            var target = new ManagedTimerTickResult(ManagedTimerTickAction.Continue);

            target.Action.Should().Be(ManagedTimerTickAction.Continue);
            target.UpdatedInterval.Should().BeNull();
        }

        [Fact]
        public void GIVEN_ActionAndIntervalConstructor_WHEN_Created_THEN_SetsInterval()
        {
            var target = new ManagedTimerTickResult(ManagedTimerTickAction.Stop, TimeSpan.FromMilliseconds(250));

            target.Action.Should().Be(ManagedTimerTickAction.Stop);
            target.UpdatedInterval.Should().Be(TimeSpan.FromMilliseconds(250));
        }

        [Fact]
        public void GIVEN_InvalidInterval_WHEN_Constructed_THEN_ThrowsArgumentOutOfRangeException()
        {
            Action action = () => new ManagedTimerTickResult(ManagedTimerTickAction.Continue, TimeSpan.Zero);

            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void GIVEN_UpdateIntervalFactory_WHEN_Called_THEN_ReturnsContinueWithInterval()
        {
            var target = ManagedTimerTickResult.UpdateInterval(TimeSpan.FromSeconds(2));

            target.Action.Should().Be(ManagedTimerTickAction.Continue);
            target.UpdatedInterval.Should().Be(TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void GIVEN_StaticResults_WHEN_Accessed_THEN_ActionsMatch()
        {
            ManagedTimerTickResult.Continue.Action.Should().Be(ManagedTimerTickAction.Continue);
            ManagedTimerTickResult.Pause.Action.Should().Be(ManagedTimerTickAction.Pause);
            ManagedTimerTickResult.Stop.Action.Should().Be(ManagedTimerTickAction.Stop);
        }
    }
}
