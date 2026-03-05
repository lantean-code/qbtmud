using AwesomeAssertions;
using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Services;
using Moq;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class ClientDataStorageAdapterTests
    {
        private readonly IApiClient _apiClient;
        private readonly ClientDataStorageAdapter _target;

        public ClientDataStorageAdapterTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _target = new ClientDataStorageAdapter(_apiClient);
        }

        [Fact]
        public async Task GIVEN_NullKeys_WHEN_LoadPrefixedEntriesAsyncWithKeys_THEN_ShouldThrowArgumentNullException()
        {
            var act = async () => await _target.LoadPrefixedEntriesAsync((IEnumerable<string>)null!, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GIVEN_CancellationRequested_WHEN_LoadPrefixedEntriesAsyncWithKeys_THEN_ShouldThrowOperationCanceledException()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var act = async () => await _target.LoadPrefixedEntriesAsync(["QbtMud.Key"], cancellationTokenSource.Token);

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GIVEN_NoValidKeys_WHEN_LoadPrefixedEntriesAsyncWithKeys_THEN_ShouldReturnEmptyWithoutCallingApi()
        {
            var result = await _target.LoadPrefixedEntriesAsync(["", " ", "NotPrefixed", "Other"], TestContext.Current.CancellationToken);

            result.Should().BeEmpty();
            Mock.Get(_apiClient).Verify(client => client.LoadClientData(It.IsAny<IEnumerable<string>?>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_MixedKeysAndResults_WHEN_LoadPrefixedEntriesAsyncWithKeys_THEN_ShouldNormalizeRequestAndFilterResponse()
        {
            IEnumerable<string>? requestedKeys = null;
            Mock.Get(_apiClient)
                .Setup(client => client.LoadClientData(It.IsAny<IEnumerable<string>?>()))
                .Callback<IEnumerable<string>?>(keys => requestedKeys = keys)
                .ReturnsAsync(new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["QbtMud.KeyA"] = JsonDocument.Parse("{\"enabled\":true}").RootElement.Clone(),
                    ["QbtMud.KeyB"] = JsonDocument.Parse("\"value\"").RootElement.Clone(),
                    ["NotPrefixed"] = JsonDocument.Parse("1").RootElement.Clone()
                });

            var result = await _target.LoadPrefixedEntriesAsync(
                [" QbtMud.KeyA ", "QbtMud.KeyB", "QbtMud.KeyA", "NotPrefixed", ""],
                TestContext.Current.CancellationToken);

            requestedKeys.Should().NotBeNull();
            requestedKeys!.Should().Equal("QbtMud.KeyA", "QbtMud.KeyB");
            result.Keys.Should().Equal("QbtMud.KeyA", "QbtMud.KeyB");
        }

        [Fact]
        public async Task GIVEN_MixedResults_WHEN_LoadPrefixedEntriesAsyncWithoutKeys_THEN_ShouldFilterResponseToPrefixedEntries()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoadClientData(It.IsAny<IEnumerable<string>?>()))
                .ReturnsAsync(new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["QbtMud.One"] = JsonDocument.Parse("1").RootElement.Clone(),
                    ["Two"] = JsonDocument.Parse("2").RootElement.Clone()
                });

            var result = await _target.LoadPrefixedEntriesAsync(TestContext.Current.CancellationToken);

            result.Should().ContainKey("QbtMud.One");
            result.Should().NotContainKey("Two");
        }

        [Fact]
        public async Task GIVEN_NullValues_WHEN_StorePrefixedEntriesAsync_THEN_ShouldThrowArgumentNullException()
        {
            var act = async () => await _target.StorePrefixedEntriesAsync((IReadOnlyDictionary<string, object?>)null!, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GIVEN_NoValidValues_WHEN_StorePrefixedEntriesAsync_THEN_ShouldReturnWithoutCallingApi()
        {
            await _target.StorePrefixedEntriesAsync(
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    [""] = "A",
                    ["NotPrefixed"] = "B"
                },
                TestContext.Current.CancellationToken);

            Mock.Get(_apiClient).Verify(client => client.StoreClientData(It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_MixedValues_WHEN_StorePrefixedEntriesAsync_THEN_ShouldNormalizeAndStoreOnlyPrefixedValues()
        {
            IReadOnlyDictionary<string, object?>? payload = null;
            Mock.Get(_apiClient)
                .Setup(client => client.StoreClientData(It.IsAny<IReadOnlyDictionary<string, object?>>()))
                .Callback<IReadOnlyDictionary<string, object?>>(entries => payload = entries)
                .Returns(Task.CompletedTask);

            await _target.StorePrefixedEntriesAsync(
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    [" QbtMud.K1 "] = "V1",
                    ["NotPrefixed"] = "V2",
                    ["QbtMud.K2"] = 2
                },
                TestContext.Current.CancellationToken);

            payload.Should().NotBeNull();
            payload!.Keys.Should().Equal("QbtMud.K1", "QbtMud.K2");
            payload["QbtMud.K1"].Should().Be("V1");
            payload["QbtMud.K2"].Should().Be(2);
        }

        [Fact]
        public async Task GIVEN_NullKeys_WHEN_RemovePrefixedEntriesAsync_THEN_ShouldThrowArgumentNullException()
        {
            var act = async () => await _target.RemovePrefixedEntriesAsync((IEnumerable<string>)null!, TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task GIVEN_NoValidKeys_WHEN_RemovePrefixedEntriesAsync_THEN_ShouldReturnWithoutCallingApi()
        {
            await _target.RemovePrefixedEntriesAsync(["", "NotPrefixed"], TestContext.Current.CancellationToken);

            Mock.Get(_apiClient).Verify(client => client.StoreClientData(It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_MixedKeys_WHEN_RemovePrefixedEntriesAsync_THEN_ShouldStoreDistinctNullValuesForPrefixedKeys()
        {
            IReadOnlyDictionary<string, object?>? payload = null;
            Mock.Get(_apiClient)
                .Setup(client => client.StoreClientData(It.IsAny<IReadOnlyDictionary<string, object?>>()))
                .Callback<IReadOnlyDictionary<string, object?>>(entries => payload = entries)
                .Returns(Task.CompletedTask);

            await _target.RemovePrefixedEntriesAsync(["QbtMud.Key", " QbtMud.Key ", "Other"], TestContext.Current.CancellationToken);

            payload.Should().NotBeNull();
            payload!.Should().ContainSingle();
            payload.Should().ContainKey("QbtMud.Key");
            payload["QbtMud.Key"].Should().BeNull();
        }
    }
}
