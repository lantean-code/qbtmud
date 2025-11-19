using AwesomeAssertions;

namespace Lantean.QBitTorrentClient.Test
{
    public class QueryBuilderExtensionsTests
    {
        private readonly QueryBuilder _target;

        public QueryBuilderExtensionsTests()
        {
            _target = new QueryBuilder();
        }

        [Fact]
        public void GIVEN_BoolTrue_WHEN_Add_THEN_ShouldStoreTrueString()
        {
            var returned = _target.Add("flag", true);

            ReferenceEquals(_target, returned).Should().BeTrue();

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("flag");
            parameters[0].Value.Should().Be("true");

            _target.ToQueryString().Should().Be("?flag=true");
        }

        [Fact]
        public void GIVEN_BoolFalse_WHEN_Add_THEN_ShouldStoreFalseString()
        {
            _target.Add("flag", false);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("flag");
            parameters[0].Value.Should().Be("false");

            _target.ToQueryString().Should().Be("?flag=false");
        }

        [Fact]
        public void GIVEN_Int_WHEN_Add_THEN_ShouldStoreNumericString()
        {
            _target.Add("count", 123);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("count");
            parameters[0].Value.Should().Be("123");

            _target.ToQueryString().Should().Be("?count=123");
        }

        [Fact]
        public void GIVEN_Long_WHEN_Add_THEN_ShouldStoreNumericString()
        {
            _target.Add("size", 9223372036854775807);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("size");
            parameters[0].Value.Should().Be("9223372036854775807");

            _target.ToQueryString().Should().Be("?size=9223372036854775807");
        }

        [Fact]
        public void GIVEN_DateTimeOffsetSeconds_WHEN_Add_THEN_ShouldUseUnixSeconds()
        {
            var when = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

            _target.Add("epoch", when);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("epoch");
            parameters[0].Value.Should().Be("946684800");

            _target.ToQueryString().Should().Be("?epoch=946684800");
        }

        [Fact]
        public void GIVEN_DateTimeOffsetMilliseconds_WHEN_Add_THEN_ShouldUseUnixMilliseconds()
        {
            var when = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

            _target.Add("epochMs", when, useSeconds: false);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("epochMs");
            parameters[0].Value.Should().Be("946684800000");

            _target.ToQueryString().Should().Be("?epochMs=946684800000");
        }

        [Fact]
        public void GIVEN_Enum_WHEN_Add_THEN_ShouldUseEnumNameString()
        {
            _target.Add("day", DayOfWeek.Monday);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("day");
            parameters[0].Value.Should().Be("Monday");

            _target.ToQueryString().Should().Be("?day=Monday");
        }

        [Fact]
        public void GIVEN_PipeSeparatedValues_WHEN_AddPipeSeparated_THEN_ShouldJoinWithPipeAndEscapeInQuery()
        {
            _target.AddPipeSeparated("list", new[] { "a", "b c", "d|e" });

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("list");
            parameters[0].Value.Should().Be("a|b c|d|e");

            _target.ToQueryString().Should().Be("?list=a%7Cb%20c%7Cd%7Ce");
        }

        [Fact]
        public void GIVEN_CommaSeparatedValues_WHEN_AddCommaSeparated_THEN_ShouldJoinWithCommaAndEscapeInQuery()
        {
            _target.AddCommaSeparated("items", new[] { 1, 2, 3 });

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("items");
            parameters[0].Value.Should().Be("1,2,3");

            _target.ToQueryString().Should().Be("?items=1%2C2%2C3");
        }

        [Fact]
        public void GIVEN_MultipleExtensionAdds_WHEN_Chained_THEN_ShouldPreserveOrderInQuery()
        {
            _target
                .Add("flag", true)
                .Add("count", 2)
                .Add("day", DayOfWeek.Friday);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(3);
            parameters[0].Key.Should().Be("flag");
            parameters[0].Value.Should().Be("true");
            parameters[1].Key.Should().Be("count");
            parameters[1].Value.Should().Be("2");
            parameters[2].Key.Should().Be("day");
            parameters[2].Value.Should().Be("Friday");

            _target.ToQueryString().Should().Be("?flag=true&count=2&day=Friday");
        }
    }
}