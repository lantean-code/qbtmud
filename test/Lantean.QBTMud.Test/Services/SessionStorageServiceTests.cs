using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class SessionStorageServiceTests
    {
        private readonly TestJsRuntime _jsRuntime;
        private readonly SessionStorageService _target;

        public SessionStorageServiceTests()
        {
            _jsRuntime = new TestJsRuntime();
            _target = new SessionStorageService(_jsRuntime);
        }

        [Fact]
        public async Task GIVEN_SessionValue_WHEN_GetItemAsync_THEN_DeserializesJson()
        {
            _jsRuntime.EnqueueResult("\"Session\"");

            var result = await _target.GetItemAsync<string>("Key");

            result.Should().Be("Session");
            _jsRuntime.LastIdentifier.Should().Be("sessionStorage.getItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "Key" });
        }

        [Fact]
        public async Task GIVEN_SessionPayload_WHEN_SetAndRemove_THEN_InvokesSessionStorage()
        {
            var payload = new SamplePayload("Column", 2);

            await _target.SetItemAsync("Payload", payload);

            _jsRuntime.LastIdentifier.Should().Be("sessionStorage.setItem");
            _jsRuntime.LastArguments.Should().NotBeNull();
            _jsRuntime.LastArguments!.Length.Should().Be(2);
            _jsRuntime.LastArguments![0].Should().Be("Payload");
            var json = _jsRuntime.LastArguments![1] as string;
            json.Should().NotBeNull();
            var jsonValue = json!;
            jsonValue.Should().Contain("\"sortColumn\":\"Column\"");
            jsonValue.Should().Contain("\"sortDirection\":2");

            await _target.RemoveItemAsync("Payload");

            _jsRuntime.LastIdentifier.Should().Be("sessionStorage.removeItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "Payload" });
        }

        private sealed record SamplePayload(string SortColumn, int SortDirection);
    }
}
