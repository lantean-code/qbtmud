using AwesomeAssertions;
using Lantean.QBTMud.Services;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class ConnectivityStateServiceTests
    {
        private readonly ConnectivityStateService _target;

        public ConnectivityStateServiceTests()
        {
            _target = new ConnectivityStateService();
        }

        [Fact]
        public void GIVEN_NewService_WHEN_ReadingState_THEN_ShouldStartConnected()
        {
            _target.IsLostConnection.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_ConnectedState_WHEN_MarkLostConnection_THEN_ShouldUpdateStateAndRaiseEvent()
        {
            bool? changedState = null;
            _target.ConnectivityChanged += value => changedState = value;

            _target.MarkLostConnection();

            _target.IsLostConnection.Should().BeTrue();
            changedState.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_LostConnectionState_WHEN_MarkConnected_THEN_ShouldUpdateStateAndRaiseEvent()
        {
            _target.MarkLostConnection();
            bool? changedState = null;
            _target.ConnectivityChanged += value => changedState = value;

            _target.MarkConnected();

            _target.IsLostConnection.Should().BeFalse();
            changedState.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_UnchangedState_WHEN_MarkedAgain_THEN_ShouldNotRaiseEvent()
        {
            var changeCount = 0;
            _target.ConnectivityChanged += _ => changeCount++;

            _target.MarkConnected();
            _target.MarkLostConnection();
            _target.MarkLostConnection();

            changeCount.Should().Be(1);
        }
    }
}
