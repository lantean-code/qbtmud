using AwesomeAssertions;

namespace Lantean.QBitTorrentClient.Test
{
    public class QueryBuilderTests
    {
        private readonly QueryBuilder _target;

        public QueryBuilderTests()
        {
            _target = new QueryBuilder();
        }

        [Fact]
        public void GIVEN_NoParameters_WHEN_ToQueryString_THEN_ShouldBeEmptyString()
        {
            var result = _target.ToQueryString();

            result.Should().Be(string.Empty);
            _target.GetParameters().Count.Should().Be(0);
        }

        [Fact]
        public void GIVEN_MultipleParameters_WHEN_ToQueryString_THEN_ShouldStartWithQuestionAndUseAmpersandBetweenPairs()
        {
            _target.Add("first", "one");
            _target.Add("second", "two");

            var result = _target.ToQueryString();

            result.Should().Be("?first=one&second=two");

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(2);
            parameters[0].Key.Should().Be("first");
            parameters[0].Value.Should().Be("one");
            parameters[1].Key.Should().Be("second");
            parameters[1].Value.Should().Be("two");
        }

        [Fact]
        public void GIVEN_SpecialChars_WHEN_ToQueryString_THEN_ShouldBeUriEscaped()
        {
            _target.Add("a b", "c+d&");
            _target.Add("こんにちは", "é l'œ");

            var result = _target.ToQueryString();

            result.Should().Be("?a%20b=c%2Bd%26&%E3%81%93%E3%82%93%E3%81%AB%E3%81%A1%E3%81%AF=%C3%A9%20l%27%C5%93");

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(2);
            parameters[0].Key.Should().Be("a b");
            parameters[0].Value.Should().Be("c+d&");
            parameters[1].Key.Should().Be("こんにちは");
            parameters[1].Value.Should().Be("é l'œ");
        }

        [Fact]
        public void GIVEN_NonEmptyString_WHEN_AddIfNotNullOrEmpty_THEN_ShouldAddPair()
        {
            _target.AddIfNotNullOrEmpty("key", "value");

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("key");
            parameters[0].Value.Should().Be("value");

            _target.ToQueryString().Should().Be("?key=value");
        }

        [Fact]
        public void GIVEN_EmptyOrNullString_WHEN_AddIfNotNullOrEmpty_THEN_ShouldNotAddPair()
        {
            _target.AddIfNotNullOrEmpty("k1", "");
            _target.AddIfNotNullOrEmpty("k2", null);

            _target.GetParameters().Count.Should().Be(0);
            _target.ToQueryString().Should().Be(string.Empty);
        }

        [Fact]
        public void GIVEN_NullableValueHasNoValue_WHEN_AddIfNotNullOrEmpty_THEN_ShouldNotAddPair()
        {
            int? value = null;

            _target.AddIfNotNullOrEmpty("count", value);

            _target.GetParameters().Count.Should().Be(0);
            _target.ToQueryString().Should().Be(string.Empty);
        }

        [Fact]
        public void GIVEN_NullableValueHasValue_WHEN_AddIfNotNullOrEmpty_THEN_ShouldAddPairUsingToString()
        {
            int? value = 42;

            _target.AddIfNotNullOrEmpty("count", value);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("count");
            parameters[0].Value.Should().Be("42");

            _target.ToQueryString().Should().Be("?count=42");
        }

        [Fact]
        public void GIVEN_NullableConvertibleValue_WHEN_AddIfNotNullOrEmpty_THEN_ShouldUseEmptyStringWhenConversionReturnsNull()
        {
            NullStringConvertible? value = new NullStringConvertible();

            _target.AddIfNotNullOrEmpty("count", value);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("count");
            parameters[0].Value.Should().Be(string.Empty);
            _target.ToQueryString().Should().Be("?count=");
        }

        [Fact]
        public void GIVEN_FluentAdd_WHEN_Used_THEN_ShouldReturnSameInstanceAndAppendParameter()
        {
            var returned = _target.Add("x", "y");

            ReferenceEquals(_target, returned).Should().BeTrue();

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("x");
            parameters[0].Value.Should().Be("y");
        }

        [Fact]
        public void GIVEN_CustomParameterList_WHEN_ConstructedWithList_THEN_ShouldUseInjectedList()
        {
            var backing = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("a", "1")
            };

            var builder = new QueryBuilder(backing);
            builder.Add("b", "2");

            var observed = builder.GetParameters();
            ReferenceEquals(backing, observed).Should().BeTrue();
            observed.Count.Should().Be(2);
            observed[0].Key.Should().Be("a");
            observed[0].Value.Should().Be("1");
            observed[1].Key.Should().Be("b");
            observed[1].Value.Should().Be("2");

            builder.ToQueryString().Should().Be("?a=1&b=2");
        }

        [Fact]
        public void GIVEN_ToStringCalled_WHEN_ParametersExist_THEN_ShouldReturnSameAsToQueryString()
        {
            _target.Add("p", "q");

            var qs = _target.ToQueryString();
            var result = _target.ToString();

            result.Should().Be(qs);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("p");
            parameters[0].Value.Should().Be("q");
        }

        private readonly struct NullStringConvertible : IConvertible
        {
            public TypeCode GetTypeCode()
            {
                return TypeCode.Object;
            }

            public bool ToBoolean(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public byte ToByte(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public char ToChar(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public DateTime ToDateTime(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public decimal ToDecimal(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public double ToDouble(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public short ToInt16(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public int ToInt32(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public long ToInt64(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public sbyte ToSByte(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public float ToSingle(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public string ToString(IFormatProvider? provider)
            {
                return null!;
            }

            public object ToType(Type conversionType, IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public ushort ToUInt16(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public uint ToUInt32(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }

            public ulong ToUInt64(IFormatProvider? provider)
            {
                throw new NotSupportedException();
            }
        }
    }
}
