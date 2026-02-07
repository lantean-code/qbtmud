using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class LocalStorageServiceTests
    {
        private readonly TestJsRuntime _jsRuntime;
        private readonly LocalStorageService _target;

        public LocalStorageServiceTests()
        {
            _jsRuntime = new TestJsRuntime();
            _target = new LocalStorageService(_jsRuntime);
        }

        [Fact]
        public async Task GIVEN_NoStoredValue_WHEN_GetItemAsync_THEN_ReturnsDefault()
        {
            _jsRuntime.EnqueueResult(null);

            var result = await _target.GetItemAsync<string>("Missing", Xunit.TestContext.Current.CancellationToken);

            result.Should().BeNull();
            _jsRuntime.LastIdentifier.Should().Be("localStorage.getItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "Missing" });
        }

        [Fact]
        public async Task GIVEN_StoredValue_WHEN_GetItemAsync_THEN_DeserializesJson()
        {
            _jsRuntime.EnqueueResult("5");

            var result = await _target.GetItemAsync<int>("Count", Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(5);
            _jsRuntime.LastIdentifier.Should().Be("localStorage.getItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "Count" });
        }

        [Fact]
        public async Task GIVEN_RawString_WHEN_GetItemAsStringAsync_THEN_ReturnsPlainValue()
        {
            _jsRuntime.EnqueueResult("StatusValue");

            var result = await _target.GetItemAsStringAsync("Status", Xunit.TestContext.Current.CancellationToken);

            result.Should().Be("StatusValue");
            _jsRuntime.LastIdentifier.Should().Be("localStorage.getItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "Status" });
        }

        [Fact]
        public async Task GIVEN_Payload_WHEN_SetAndRemove_THEN_InvokesLocalStorage()
        {
            var payload = new SamplePayload("Name", 1);

            await _target.SetItemAsync("Payload", payload, Xunit.TestContext.Current.CancellationToken);

            _jsRuntime.LastIdentifier.Should().Be("localStorage.setItem");
            _jsRuntime.LastArguments.Should().NotBeNull();
            _jsRuntime.LastArguments!.Length.Should().Be(2);
            _jsRuntime.LastArguments![0].Should().Be("Payload");
            var json = _jsRuntime.LastArguments![1] as string;
            json.Should().NotBeNull();
            var jsonValue = json!;
            jsonValue.Should().Contain("\"sortColumn\":\"Name\"");
            jsonValue.Should().Contain("\"sortDirection\":1");

            await _target.RemoveItemAsync("Payload", Xunit.TestContext.Current.CancellationToken);

            _jsRuntime.LastIdentifier.Should().Be("localStorage.removeItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "Payload" });
        }

        [Fact]
        public async Task GIVEN_RawString_WHEN_SetItemAsStringAsync_THEN_WritesPlainValue()
        {
            await _target.SetItemAsStringAsync("Status", "StatusValue", Xunit.TestContext.Current.CancellationToken);

            _jsRuntime.LastIdentifier.Should().Be("localStorage.setItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "Status", "StatusValue" });
        }

        private sealed record SamplePayload(string SortColumn, int SortDirection);
    }
}
