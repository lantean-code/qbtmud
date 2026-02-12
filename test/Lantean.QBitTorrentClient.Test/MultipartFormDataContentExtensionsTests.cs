using AwesomeAssertions;

namespace Lantean.QBitTorrentClient.Test
{
    public class MultipartFormDataContentExtensionsTests : IDisposable
    {
        private readonly MultipartFormDataContent _target;

        public MultipartFormDataContentExtensionsTests()
        {
            _target = new MultipartFormDataContent();
        }

        [Fact]
        public async Task GIVEN_String_WHEN_AddString_THEN_ShouldAddWithNameAndValue()
        {
            _target.AddString("name", "value");

            var part = _target.ToList().Single();
            part.Headers.ContentDisposition!.Name.Should().Be("name");
            (await part.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("value");
        }

        [Fact]
        public async Task GIVEN_BoolTrue_WHEN_AddString_THEN_ShouldStoreTrue()
        {
            _target.AddString("flag", true);

            var part = _target.ToList().Single();
            part.Headers.ContentDisposition!.Name.Should().Be("flag");
            (await part.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("true");
        }

        [Fact]
        public async Task GIVEN_BoolFalse_WHEN_AddString_THEN_ShouldStoreFalse()
        {
            _target.AddString("flag", false);

            var part = _target.ToList().Single();
            part.Headers.ContentDisposition!.Name.Should().Be("flag");
            (await part.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("false");
        }

        [Fact]
        public async Task GIVEN_Int_WHEN_AddString_THEN_ShouldStoreNumericString()
        {
            _target.AddString("count", 123);

            var part = _target.ToList().Single();
            part.Headers.ContentDisposition!.Name.Should().Be("count");
            (await part.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("123");
        }

        [Fact]
        public async Task GIVEN_Long_WHEN_AddString_THEN_ShouldStoreNumericString()
        {
            _target.AddString("size", 9223372036854775807);

            var part = _target.ToList().Single();
            part.Headers.ContentDisposition!.Name.Should().Be("size");
            (await part.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("9223372036854775807");
        }

        [Fact]
        public async Task GIVEN_Float_WHEN_AddString_THEN_ShouldUseCurrentCultureToString()
        {
            _target.AddString("ratio", 42f);

            var part = _target.ToList().Single();
            part.Headers.ContentDisposition!.Name.Should().Be("ratio");
            (await part.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be(42f.ToString());
        }

        [Fact]
        public async Task GIVEN_Enum_WHEN_AddString_THEN_ShouldStoreEnumName()
        {
            _target.AddString("day", DayOfWeek.Monday);

            var part = _target.ToList().Single();
            part.Headers.ContentDisposition!.Name.Should().Be("day");
            (await part.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("Monday");
        }

        [Fact]
        public async Task GIVEN_DateTimeOffsetSeconds_WHEN_AddString_THEN_ShouldUseUnixSeconds()
        {
            var when = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

            _target.AddString("epoch", when);

            var part = _target.ToList().Single();
            part.Headers.ContentDisposition!.Name.Should().Be("epoch");
            (await part.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("946684800");
        }

        [Fact]
        public async Task GIVEN_DateTimeOffsetMilliseconds_WHEN_AddString_THEN_ShouldUseUnixMilliseconds()
        {
            var when = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

            _target.AddString("epochMs", when, useSeconds: false);

            var part = _target.ToList().Single();
            part.Headers.ContentDisposition!.Name.Should().Be("epochMs");
            (await part.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("946684800000");
        }

        public void Dispose()
        {
            _target.Dispose();
        }
    }
}
