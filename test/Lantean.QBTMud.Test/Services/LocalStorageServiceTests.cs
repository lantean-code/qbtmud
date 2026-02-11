using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Moq;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class LocalStorageServiceTests
    {
        private readonly TestJsRuntime _jsRuntime;
        private readonly LocalStorageService _target;

        public LocalStorageServiceTests()
        {
            _jsRuntime = new TestJsRuntime();
            _target = new LocalStorageService(new BrowserStorageServiceFactory(_jsRuntime));
        }

        [Fact]
        public async Task GIVEN_NoStoredValue_WHEN_GetItemAsync_THEN_ReturnsDefault()
        {
            _jsRuntime.EnqueueResult(null);

            var result = await _target.GetItemAsync<string>("Missing", Xunit.TestContext.Current.CancellationToken);

            result.Should().BeNull();
            _jsRuntime.LastIdentifier.Should().Be("localStorage.getItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "Missing" });
            _jsRuntime.CallCount.Should().Be(2);
        }

        [Fact]
        public async Task GIVEN_StoredValue_WHEN_GetItemAsync_THEN_DeserializesJson()
        {
            _jsRuntime.EnqueueResult("5");

            var result = await _target.GetItemAsync<int>("Count", Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(5);
            _jsRuntime.LastIdentifier.Should().Be("localStorage.getItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "QbtMud.Count" });
            _jsRuntime.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_LegacyValue_WHEN_GetItemAsync_THEN_UpgradesKeyAndReturnsValue()
        {
            _jsRuntime.EnqueueResult(null);
            _jsRuntime.EnqueueResult("5");

            var result = await _target.GetItemAsync<int>("Count", Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(5);
            _jsRuntime.LastIdentifier.Should().Be("localStorage.removeItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "Count" });
            _jsRuntime.CallCount.Should().Be(4);
        }

        [Fact]
        public async Task GIVEN_RawString_WHEN_GetItemAsStringAsync_THEN_ReturnsPlainValue()
        {
            _jsRuntime.EnqueueResult("StatusValue");

            var result = await _target.GetItemAsStringAsync("Status", Xunit.TestContext.Current.CancellationToken);

            result.Should().Be("StatusValue");
            _jsRuntime.LastIdentifier.Should().Be("localStorage.getItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "QbtMud.Status" });
        }

        [Fact]
        public async Task GIVEN_Payload_WHEN_SetAndRemove_THEN_InvokesLocalStorage()
        {
            var payload = new SamplePayload("Name", 1);

            await _target.SetItemAsync("Payload", payload, Xunit.TestContext.Current.CancellationToken);

            _jsRuntime.LastIdentifier.Should().Be("localStorage.setItem");
            _jsRuntime.LastArguments.Should().NotBeNull();
            _jsRuntime.LastArguments!.Length.Should().Be(2);
            _jsRuntime.LastArguments![0].Should().Be("QbtMud.Payload");
            var json = _jsRuntime.LastArguments![1] as string;
            json.Should().NotBeNull();
            var jsonValue = json!;
            jsonValue.Should().Contain("\"sortColumn\":\"Name\"");
            jsonValue.Should().Contain("\"sortDirection\":1");

            await _target.RemoveItemAsync("Payload", Xunit.TestContext.Current.CancellationToken);

            _jsRuntime.LastIdentifier.Should().Be("localStorage.removeItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "Payload" });
            _jsRuntime.CallCount.Should().Be(3);
        }

        [Fact]
        public async Task GIVEN_RawString_WHEN_SetItemAsStringAsync_THEN_WritesPlainValue()
        {
            await _target.SetItemAsStringAsync("Status", "StatusValue", Xunit.TestContext.Current.CancellationToken);

            _jsRuntime.LastIdentifier.Should().Be("localStorage.setItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "QbtMud.Status", "StatusValue" });
        }

        [Fact]
        public async Task GIVEN_InjectedFactory_WHEN_GetItemAsync_THEN_DelegatesToCreatedStorageService()
        {
            var cancellationToken = Xunit.TestContext.Current.CancellationToken;
            var storage = new Mock<IBrowserStorageService>(MockBehavior.Strict);
            storage.Setup(mock => mock.GetItemAsync<string>("Key", cancellationToken))
                .Returns(new ValueTask<string?>("Value"));
            var factory = new Mock<IBrowserStorageServiceFactory>(MockBehavior.Strict);
            factory.Setup(mock => mock.CreateLocalStorageService()).Returns(storage.Object);
            var target = new LocalStorageService(factory.Object);

            var result = await target.GetItemAsync<string>("Key", cancellationToken);

            result.Should().Be("Value");
            factory.Verify(mock => mock.CreateLocalStorageService(), Times.Once);
            storage.Verify(mock => mock.GetItemAsync<string>("Key", cancellationToken), Times.Once);
        }

        [Fact]
        public void GIVEN_NullFactory_WHEN_Constructing_THEN_ThrowsArgumentNullException()
        {
            var action = () => new LocalStorageService((IBrowserStorageServiceFactory)null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("storageServiceFactory");
        }

        [Fact]
        public async Task GIVEN_PrefixedKey_WHEN_GetItemAsync_THEN_DoesNotQueryLegacyKey()
        {
            _jsRuntime.EnqueueResult("5");

            var result = await _target.GetItemAsync<int>("QbtMud.Count", Xunit.TestContext.Current.CancellationToken);

            result.Should().Be(5);
            _jsRuntime.LastIdentifier.Should().Be("localStorage.getItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "QbtMud.Count" });
            _jsRuntime.CallCount.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_PrefixedKey_WHEN_RemoveItemAsync_THEN_RemovesOnlyPrefixedKey()
        {
            await _target.RemoveItemAsync("QbtMud.Payload", Xunit.TestContext.Current.CancellationToken);

            _jsRuntime.LastIdentifier.Should().Be("localStorage.removeItem");
            _jsRuntime.LastArguments.Should().BeEquivalentTo(new object?[] { "QbtMud.Payload" });
            _jsRuntime.CallCount.Should().Be(1);
        }

        private sealed record SamplePayload(string SortColumn, int SortDirection);
    }
}
