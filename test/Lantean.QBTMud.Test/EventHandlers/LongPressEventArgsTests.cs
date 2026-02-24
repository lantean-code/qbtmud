using AwesomeAssertions;

namespace Lantean.QBTMud.Test
{
    public sealed class LongPressEventArgsTests
    {
        private readonly LongPressEventArgs _target;

        public LongPressEventArgsTests()
        {
            _target = new LongPressEventArgs();
        }

        [Fact]
        public void GIVEN_NewInstance_WHEN_Constructed_THEN_ShouldUseExpectedDefaults()
        {
            _target.Should().BeAssignableTo<EventArgs>();
            _target.Bubbles.Should().BeFalse();
            _target.Cancelable.Should().BeFalse();
            _target.ScreenX.Should().Be(0);
            _target.ScreenY.Should().Be(0);
            _target.ClientX.Should().Be(0);
            _target.ClientY.Should().Be(0);
            _target.OffsetX.Should().Be(0);
            _target.OffsetY.Should().Be(0);
            _target.PageX.Should().Be(0);
            _target.PageY.Should().Be(0);
            _target.SourceElement.Should().BeNull();
            _target.TargetElement.Should().BeNull();
            _target.TimeStamp.Should().Be(0);
            _target.Type.Should().BeNull();
        }

        [Fact]
        public void GIVEN_AssignedProperties_WHEN_ReadBack_THEN_ShouldReturnAssignedValues()
        {
            _target.Bubbles = true;
            _target.Cancelable = true;
            _target.ScreenX = 1;
            _target.ScreenY = 2;
            _target.ClientX = 3;
            _target.ClientY = 4;
            _target.OffsetX = 5;
            _target.OffsetY = 6;
            _target.PageX = 7;
            _target.PageY = 8;
            _target.SourceElement = "SourceElement";
            _target.TargetElement = "TargetElement";
            _target.TimeStamp = 9;
            _target.Type = "Type";

            _target.Bubbles.Should().BeTrue();
            _target.Cancelable.Should().BeTrue();
            _target.ScreenX.Should().Be(1);
            _target.ScreenY.Should().Be(2);
            _target.ClientX.Should().Be(3);
            _target.ClientY.Should().Be(4);
            _target.OffsetX.Should().Be(5);
            _target.OffsetY.Should().Be(6);
            _target.PageX.Should().Be(7);
            _target.PageY.Should().Be(8);
            _target.SourceElement.Should().Be("SourceElement");
            _target.TargetElement.Should().Be("TargetElement");
            _target.TimeStamp.Should().Be(9);
            _target.Type.Should().Be("Type");
        }
    }
}
