using AwesomeAssertions;

namespace Lantean.QBitTorrentClient.Test
{
    public class FormUrlEncodedBuilderExtensionsTests
    {
        private readonly FormUrlEncodedBuilder _target;

        public FormUrlEncodedBuilderExtensionsTests()
        {
            _target = new FormUrlEncodedBuilder();
        }

        [Fact]
        public async Task GIVEN_BoolTrue_WHEN_Add_THEN_ShouldSerializeAsTrue()
        {
            var returned = _target.Add("flag", true);

            ReferenceEquals(_target, returned).Should().BeTrue();

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("flag");
            parameters[0].Value.Should().Be("true");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("flag=true");
        }

        [Fact]
        public async Task GIVEN_BoolFalse_WHEN_Add_THEN_ShouldSerializeAsFalse()
        {
            _target.Add("flag", false);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("flag");
            parameters[0].Value.Should().Be("false");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("flag=false");
        }

        [Fact]
        public async Task GIVEN_Int_WHEN_Add_THEN_ShouldSerializeAsDigits()
        {
            _target.Add("count", 123);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("count");
            parameters[0].Value.Should().Be("123");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("count=123");
        }

        [Fact]
        public async Task GIVEN_Long_WHEN_Add_THEN_ShouldSerializeAsDigits()
        {
            _target.Add("size", 9223372036854775807);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("size");
            parameters[0].Value.Should().Be("9223372036854775807");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("size=9223372036854775807");
        }

        [Fact]
        public async Task GIVEN_DateTimeOffsetSeconds_WHEN_Add_THEN_ShouldUseUnixSeconds()
        {
            var when = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

            _target.Add("epoch", when);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("epoch");
            parameters[0].Value.Should().Be("946684800");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("epoch=946684800");
        }

        [Fact]
        public async Task GIVEN_DateTimeOffsetMilliseconds_WHEN_Add_THEN_ShouldUseUnixMilliseconds()
        {
            var when = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

            _target.Add("epochMs", when, useSeconds: false);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("epochMs");
            parameters[0].Value.Should().Be("946684800000");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("epochMs=946684800000");
        }

        [Fact]
        public async Task GIVEN_Float_WHEN_Add_THEN_ShouldUseCurrentCultureToString()
        {
            _target.Add("ratio", 42f);

            var expected = 42f.ToString();

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("ratio");
            parameters[0].Value.Should().Be(expected);

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be($"ratio={expected}");
        }

        [Fact]
        public async Task GIVEN_GenericByte_WHEN_Add_Generic_THEN_ShouldSerializeAsInt32String()
        {
            _target.Add<byte>("b", 7);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("b");
            parameters[0].Value.Should().Be("7");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("b=7");
        }

        [Fact]
        public async Task GIVEN_GenericEnum_WHEN_Add_Generic_THEN_ShouldSerializeUnderlyingInt32String()
        {
            _target.Add("day", DayOfWeek.Friday);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("day");
            parameters[0].Value.Should().Be("5");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("day=5");
        }

        [Fact]
        public async Task GIVEN_AllTrue_WHEN_AddAllOrPipeSeparated_THEN_ShouldUseAllLiteral()
        {
            var returned = _target.AddAllOrPipeSeparated("list", all: true, "a", "b");

            ReferenceEquals(_target, returned).Should().BeTrue();

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("list");
            parameters[0].Value.Should().Be("all");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("list=all");
        }

        [Fact]
        public async Task GIVEN_AllNullOrFalse_WHEN_AddAllOrPipeSeparated_THEN_ShouldJoinWithPipes()
        {
            _target.AddAllOrPipeSeparated("list", null, "a", "b c", "d|e");

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("list");
            parameters[0].Value.Should().Be("a|b c|d|e");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("list=a%7Cb+c%7Cd%7Ce");
        }

        [Fact]
        public async Task GIVEN_NoValues_WHEN_AddAllOrPipeSeparatedWithFalse_THEN_ShouldYieldEmptyValue()
        {
            _target.AddAllOrPipeSeparated("list", false);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("list");
            parameters[0].Value.Should().Be(string.Empty);

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("list=");
        }

        [Fact]
        public async Task GIVEN_PipeSeparatedValues_WHEN_AddPipeSeparated_THEN_ShouldJoinAndEncodeProperly()
        {
            _target.AddPipeSeparated("ids", new[] { "a", "b c", "d|e" });

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("ids");
            parameters[0].Value.Should().Be("a|b c|d|e");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("ids=a%7Cb+c%7Cd%7Ce");
        }

        [Fact]
        public async Task GIVEN_CommaSeparatedValues_WHEN_AddCommaSeparated_THEN_ShouldJoinAndEncodeProperly()
        {
            _target.AddCommaSeparated("items", new[] { 1, 2, 3 });

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("items");
            parameters[0].Value.Should().Be("1,2,3");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("items=1%2C2%2C3");
        }

        [Fact]
        public async Task GIVEN_MultipleAdds_WHEN_Chained_THEN_ShouldPreserveOrder()
        {
            var returned = _target
                .Add("flag", true)
                .Add("count", 2)
                .Add("epoch", new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero))
                .Add<byte>("b", 7);

            ReferenceEquals(_target, returned).Should().BeTrue();

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(4);
            parameters[0].Key.Should().Be("flag");
            parameters[0].Value.Should().Be("true");
            parameters[1].Key.Should().Be("count");
            parameters[1].Value.Should().Be("2");
            parameters[2].Key.Should().Be("epoch");
            parameters[2].Value.Should().Be("946684800");
            parameters[3].Key.Should().Be("b");
            parameters[3].Value.Should().Be("7");

            using var content = _target.ToFormUrlEncodedContent();
            (await content.ReadAsStringAsync()).Should().Be("flag=true&count=2&epoch=946684800&b=7");
        }
    }
}