using AwesomeAssertions;
using Lantean.QBTMud.Models;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Models
{
    public sealed class KeyboardEventTests
    {
        private readonly KeyboardEvent _target;

        public KeyboardEventTests()
        {
            _target = new KeyboardEvent("Key");
        }

        [Fact]
        public void GIVEN_KeyOnlyConstructor_WHEN_Constructed_THEN_ShouldSetKeyAndCode()
        {
            _target.Key.Should().Be("Key");
            _target.Code.Should().Be("Key");
            _target.Repeat.Should().BeFalse();
            _target.CtrlKey.Should().BeFalse();
            _target.ShiftKey.Should().BeFalse();
            _target.AltKey.Should().BeFalse();
            _target.MetaKey.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_JsonConstructorParameters_WHEN_Constructed_THEN_ShouldSetAllProperties()
        {
            var target = new KeyboardEvent(
                key: "Key",
                repeat: true,
                ctrlKey: true,
                shiftKey: true,
                altKey: true,
                metaKey: true,
                code: "Code");

            target.Key.Should().Be("Key");
            target.Repeat.Should().BeTrue();
            target.CtrlKey.Should().BeTrue();
            target.ShiftKey.Should().BeTrue();
            target.AltKey.Should().BeTrue();
            target.MetaKey.Should().BeTrue();
            target.Code.Should().Be("Code");
        }

        [Fact]
        public void GIVEN_AllModifiersDisabledAndNullKey_WHEN_GetCanonicalKey_THEN_ShouldUseEmptyKeyPrefix()
        {
            var target = new KeyboardEvent(null!);

            var result = target.GetCanonicalKey();

            result.Should().Be("00000");
        }

        [Fact]
        public void GIVEN_AllModifiersEnabled_WHEN_GetCanonicalKey_THEN_ShouldIncludeAllFlags()
        {
            var target = new KeyboardEvent("Key")
            {
                CtrlKey = true,
                ShiftKey = true,
                AltKey = true,
                MetaKey = true,
                Repeat = true,
            };

            var result = target.GetCanonicalKey();

            result.Should().Be("Key11111");
        }

        [Fact]
        public void GIVEN_NonKeyboardEventObject_WHEN_EqualsInvoked_THEN_ShouldReturnFalse()
        {
            var result = _target.Equals("Key");

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_EquivalentKeyboardEvents_WHEN_EqualsInvoked_THEN_ShouldReturnTrue()
        {
            var compare = CreateEvent();

            var result = _target.Equals(compare);

            result.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_DifferentKey_WHEN_EqualsInvoked_THEN_ShouldReturnFalse()
        {
            var compare = CreateEvent(key: "Other");

            var result = _target.Equals(compare);

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_DifferentRepeat_WHEN_EqualsInvoked_THEN_ShouldReturnFalse()
        {
            var compare = CreateEvent(repeat: true);

            var result = _target.Equals(compare);

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_DifferentCtrlKey_WHEN_EqualsInvoked_THEN_ShouldReturnFalse()
        {
            var compare = CreateEvent(ctrlKey: true);

            var result = _target.Equals(compare);

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_DifferentShiftKey_WHEN_EqualsInvoked_THEN_ShouldReturnFalse()
        {
            var compare = CreateEvent(shiftKey: true);

            var result = _target.Equals(compare);

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_DifferentAltKey_WHEN_EqualsInvoked_THEN_ShouldReturnFalse()
        {
            var compare = CreateEvent(altKey: true);

            var result = _target.Equals(compare);

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_DifferentMetaKey_WHEN_EqualsInvoked_THEN_ShouldReturnFalse()
        {
            var compare = CreateEvent(metaKey: true);

            var result = _target.Equals(compare);

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_DifferentCode_WHEN_EqualsInvoked_THEN_ShouldReturnFalse()
        {
            var compare = CreateEvent(code: "Code");

            var result = _target.Equals(compare);

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_EqualEvents_WHEN_GetHashCodeInvoked_THEN_ShouldReturnSameValue()
        {
            var compare = CreateEvent();

            _target.GetHashCode().Should().Be(compare.GetHashCode());
        }

        [Fact]
        public void GIVEN_Key_WHEN_ToStringInvoked_THEN_ShouldReturnKey()
        {
            _target.ToString().Should().Be("Key");
        }

        [Fact]
        public void GIVEN_NullKey_WHEN_ToStringInvoked_THEN_ShouldReturnUnidentified()
        {
            var target = new KeyboardEvent(null!);

            target.ToString().Should().Be("Unidentified");
        }

        [Fact]
        public void GIVEN_EmptyKey_WHEN_ToStringInvoked_THEN_ShouldReturnUnidentified()
        {
            var target = new KeyboardEvent(string.Empty);

            target.ToString().Should().Be("Unidentified");
        }

        [Fact]
        public void GIVEN_PlusKey_WHEN_ToStringInvoked_THEN_ShouldEscapePlus()
        {
            var target = new KeyboardEvent("+");

            target.ToString().Should().Be("'+'");
        }

        [Fact]
        public void GIVEN_SpaceKey_WHEN_ToStringInvoked_THEN_ShouldReturnSpaceToken()
        {
            var target = new KeyboardEvent(" ");

            target.ToString().Should().Be("Space");
        }

        [Fact]
        public void GIVEN_ModifiersAndRepeat_WHEN_ToStringInvoked_THEN_ShouldIncludeAllSegments()
        {
            var target = new KeyboardEvent("Key")
            {
                CtrlKey = true,
                ShiftKey = true,
                AltKey = true,
                MetaKey = true,
                Repeat = true,
            };

            target.ToString().Should().Be("Ctrl+Shift+Alt+Meta+Key (repeat)");
        }

        [Fact]
        public void GIVEN_JsonPayload_WHEN_Deserialized_THEN_ShouldMapJsonPropertyNames()
        {
            const string Json = """
                {
                    "key": "Key",
                    "repeat": true,
                    "ctrlKey": true,
                    "shiftKey": true,
                    "altKey": true,
                    "metaKey": true,
                    "code": "Code"
                }
                """;

            var result = JsonSerializer.Deserialize<KeyboardEvent>(Json);

            result.Should().NotBeNull();
            result!.Key.Should().Be("Key");
            result.Repeat.Should().BeTrue();
            result.CtrlKey.Should().BeTrue();
            result.ShiftKey.Should().BeTrue();
            result.AltKey.Should().BeTrue();
            result.MetaKey.Should().BeTrue();
            result.Code.Should().Be("Code");
        }

        [Fact]
        public void GIVEN_String_WHEN_ImplicitlyConvertedToKeyboardEvent_THEN_ShouldCreateMatchingEvent()
        {
            KeyboardEvent result = "Key";

            result.Key.Should().Be("Key");
            result.Code.Should().Be("Key");
        }

        [Fact]
        public void GIVEN_KeyboardEvent_WHEN_ImplicitlyConvertedToString_THEN_ShouldUseToStringValue()
        {
            var input = new KeyboardEvent("+");

            string result = input;

            result.Should().Be("'+'");
        }

        private static KeyboardEvent CreateEvent(
            string key = "Key",
            bool repeat = false,
            bool ctrlKey = false,
            bool shiftKey = false,
            bool altKey = false,
            bool metaKey = false,
            string code = "Key")
        {
            return new KeyboardEvent(
                key: key,
                repeat: repeat,
                ctrlKey: ctrlKey,
                shiftKey: shiftKey,
                altKey: altKey,
                metaKey: metaKey,
                code: code);
        }
    }
}
