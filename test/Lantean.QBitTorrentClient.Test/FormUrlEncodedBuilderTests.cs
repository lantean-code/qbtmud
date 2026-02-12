using AwesomeAssertions;

namespace Lantean.QBitTorrentClient.Test
{
    public class FormUrlEncodedBuilderTests
    {
        private readonly FormUrlEncodedBuilder _target;

        public FormUrlEncodedBuilderTests()
        {
            _target = new FormUrlEncodedBuilder();
        }

        [Fact]
        public async Task GIVEN_NoParameters_WHEN_ToFormUrlEncodedContent_THEN_ShouldBeEmptyString()
        {
            using var content = _target.ToFormUrlEncodedContent();

            var payload = await content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            payload.Should().Be(string.Empty);
            _target.GetParameters().Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_SingleParameter_WHEN_ToFormUrlEncodedContent_THEN_ShouldEncodeAndContainPair()
        {
            _target.Add("first", "one");

            using var content = _target.ToFormUrlEncodedContent();
            var payload = await content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            payload.Should().Be("first=one");

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("first");
            parameters[0].Value.Should().Be("one");
        }

        [Fact]
        public async Task GIVEN_MultipleParameters_WHEN_ToFormUrlEncodedContent_THEN_ShouldPreserveOrderWithAmpersand()
        {
            _target.Add("a", "1").Add("b", "2").Add("c", "3");

            using var content = _target.ToFormUrlEncodedContent();
            var payload = await content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            payload.Should().Be("a=1&b=2&c=3");

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(3);
            parameters[0].Key.Should().Be("a");
            parameters[0].Value.Should().Be("1");
            parameters[1].Key.Should().Be("b");
            parameters[1].Value.Should().Be("2");
            parameters[2].Key.Should().Be("c");
            parameters[2].Value.Should().Be("3");
        }

        [Fact]
        public async Task GIVEN_SpecialCharacters_WHEN_ToFormUrlEncodedContent_THEN_ShouldBeProperlyEncoded()
        {
            _target.Add("a b", "c+d&=");

            using var content = _target.ToFormUrlEncodedContent();
            var payload = await content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            (payload.StartsWith("a%20b=") || payload.StartsWith("a+b=")).Should().BeTrue();
            payload.EndsWith("c%2Bd%26%3D").Should().BeTrue();

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("a b");
            parameters[0].Value.Should().Be("c+d&=");
        }

        [Fact]
        public async Task GIVEN_NonEmptyString_WHEN_AddIfNotNullOrEmpty_THEN_ShouldAddPair()
        {
            _target.AddIfNotNullOrEmpty("key", "value");

            _target.GetParameters().Count.Should().Be(1);

            using var content = _target.ToFormUrlEncodedContent();
            var payload = await content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            payload.Should().Be("key=value");
        }

        [Fact]
        public async Task GIVEN_EmptyOrNullString_WHEN_AddIfNotNullOrEmpty_THEN_ShouldNotAddPair()
        {
            _target.AddIfNotNullOrEmpty("k1", "");
            _target.AddIfNotNullOrEmpty("k2", null);

            _target.GetParameters().Count.Should().Be(0);

            using var content = _target.ToFormUrlEncodedContent();
            var payload = await content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            payload.Should().Be(string.Empty);
        }

        [Fact]
        public async Task GIVEN_NullableValueHasNoValue_WHEN_AddIfNotNullOrEmpty_Generic_THEN_ShouldNotAddPair()
        {
            int? value = null;

            _target.AddIfNotNullOrEmpty("count", value);

            _target.GetParameters().Count.Should().Be(0);

            using var content = _target.ToFormUrlEncodedContent();
            var payload = await content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            payload.Should().Be(string.Empty);
        }

        [Fact]
        public async Task GIVEN_NullableValueHasValue_WHEN_AddIfNotNullOrEmpty_Generic_THEN_ShouldAddPairUsingToString()
        {
            int? value = 42;

            _target.AddIfNotNullOrEmpty("count", value);

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("count");
            parameters[0].Value.Should().Be("42");

            using var content = _target.ToFormUrlEncodedContent();
            var payload = await content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            payload.Should().Be("count=42");
        }

        [Fact]
        public void GIVEN_FluentAdd_WHEN_Used_THEN_ShouldReturnSameInstance()
        {
            var returned = _target.Add("x", "y");

            ReferenceEquals(_target, returned).Should().BeTrue();

            var parameters = _target.GetParameters();
            parameters.Count.Should().Be(1);
            parameters[0].Key.Should().Be("x");
            parameters[0].Value.Should().Be("y");
        }

        [Fact]
        public async Task GIVEN_CustomParameterList_WHEN_ConstructedWithList_THEN_ShouldUseInjectedList()
        {
            var backing = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("a", "1")
            };

            var builder = new FormUrlEncodedBuilder(backing);
            builder.Add("b", "2");

            var observed = builder.GetParameters();
            ReferenceEquals(backing, observed).Should().BeTrue();
            observed.Count.Should().Be(2);
            observed[0].Key.Should().Be("a");
            observed[0].Value.Should().Be("1");
            observed[1].Key.Should().Be("b");
            observed[1].Value.Should().Be("2");

            using var content = builder.ToFormUrlEncodedContent();
            var payload = await content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            payload.Should().Be("a=1&b=2");
        }
    }
}
