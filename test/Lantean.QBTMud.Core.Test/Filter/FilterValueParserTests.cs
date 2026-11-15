using AwesomeAssertions;
using Lantean.QBTMud.Core.Filter;

namespace Lantean.QBTMud.Core.Test.Filter
{
    public sealed class FilterValueParserTests
    {
        [Theory]
        [InlineData(typeof(byte))]
        [InlineData(typeof(sbyte))]
        [InlineData(typeof(short))]
        [InlineData(typeof(ushort))]
        [InlineData(typeof(int))]
        [InlineData(typeof(uint))]
        [InlineData(typeof(long))]
        [InlineData(typeof(ulong))]
        [InlineData(typeof(float))]
        [InlineData(typeof(double))]
        [InlineData(typeof(decimal))]
        public void GIVEN_NumericType_WHEN_ParsingText_THEN_ShouldReturnTypedNumber(Type type)
        {
            var succeeded = FilterValueParser.TryParse(type, "10", out var value);

            succeeded.Should().BeTrue();
            value.Should().BeOfType(type);
            Convert.ToDecimal(value).Should().Be(10);
            FilterValueParser.TryParse(type, "Invalid", out _).Should().BeFalse();
        }

        [Theory]
        [InlineData(typeof(int), null, false)]
        [InlineData(typeof(int?), null, true)]
        [InlineData(typeof(int?), " ", true)]
        [InlineData(typeof(string), null, true)]
        [InlineData(typeof(string), "Value", true)]
        [InlineData(typeof(string), 10, true)]
        [InlineData(typeof(int), 10, true)]
        [InlineData(typeof(bool), "true", true)]
        [InlineData(typeof(bool), "Invalid", false)]
        [InlineData(typeof(Guid), "00000000-0000-0000-0000-000000000001", true)]
        [InlineData(typeof(Guid), "Invalid", false)]
        [InlineData(typeof(DateTime), "2000-01-01", true)]
        [InlineData(typeof(DateTime), "Invalid", false)]
        [InlineData(typeof(DayOfWeek), "Monday", true)]
        [InlineData(typeof(DayOfWeek), "Invalid", false)]
        [InlineData(typeof(object), "Value", true)]
        [InlineData(typeof(Version), "Invalid", false)]
        public void GIVEN_PropertyTypeAndValue_WHEN_Parsing_THEN_ShouldValidateConversion(Type type, object? input, bool expected)
        {
            FilterValueParser.TryParse(type, input, out _).Should().Be(expected);
        }
    }
}
