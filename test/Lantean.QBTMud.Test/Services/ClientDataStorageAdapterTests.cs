using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Moq;
using QBittorrent.ApiClient;
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
            Mock.Get(_apiClient).Verify(client => client.LoadClientDataAsync(It.IsAny<IEnumerable<string>?>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_MixedKeysAndResults_WHEN_LoadPrefixedEntriesAsyncWithKeys_THEN_ShouldNormalizeRequestAndFilterResponse()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoadClientDataAsync(It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["QbtMud.KeyA"] = JsonDocument.Parse("{\"enabled\":true}").RootElement.Clone(),
                    ["QbtMud.KeyB"] = JsonDocument.Parse("\"value\"").RootElement.Clone(),
                    ["NotPrefixed"] = JsonDocument.Parse("1").RootElement.Clone()
                });

            var result = await _target.LoadPrefixedEntriesAsync(
                [" QbtMud.KeyA ", "QbtMud.KeyB", "QbtMud.KeyA", "NotPrefixed", ""],
                TestContext.Current.CancellationToken);

            result.Keys.Should().Equal("QbtMud.KeyA", "QbtMud.KeyB");

            Mock.Get(_apiClient)
                .Verify(client => client.LoadClientDataAsync(It.Is<IEnumerable<string>?>(keys =>
                    keys != null
                    && keys.Count() == 2
                    && keys.Contains("QbtMud.KeyA")
                    && keys.Contains("QbtMud.KeyB")
                ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_MixedResults_WHEN_LoadPrefixedEntriesAsyncWithoutKeys_THEN_ShouldFilterResponseToPrefixedEntries()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoadClientDataAsync(It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
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
        public async Task GIVEN_LoadWithKeysFails_WHEN_LoadPrefixedEntriesAsyncWithKeys_THEN_ShouldReturnEmpty()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoadClientDataAsync(It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient, IReadOnlyDictionary<string, JsonElement>>(ApiFailureKind.ServerError, "Failure");

            var result = await _target.LoadPrefixedEntriesAsync(["QbtMud.Key"], TestContext.Current.CancellationToken);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_LoadWithoutKeysFails_WHEN_LoadPrefixedEntriesAsyncWithoutKeys_THEN_ShouldReturnEmpty()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoadClientDataAsync(It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient, IReadOnlyDictionary<string, JsonElement>>(ApiFailureKind.ServerError, "Failure");

            var result = await _target.LoadPrefixedEntriesAsync(TestContext.Current.CancellationToken);

            result.Should().BeEmpty();
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

            Mock.Get(_apiClient).Verify(client => client.StoreClientDataAsync(It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_MixedValues_WHEN_StorePrefixedEntriesAsync_THEN_ShouldNormalizeAndStoreOnlyPrefixedValues()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.StoreClientDataAsync(It.IsAny<IReadOnlyDictionary<string, object?>>()))
                .Returns(Task.CompletedTask);

            await _target.StorePrefixedEntriesAsync(
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    [" QbtMud.K1 "] = "V1",
                    ["NotPrefixed"] = "V2",
                    ["QbtMud.K2"] = 2
                },
                TestContext.Current.CancellationToken);

            Mock.Get(_apiClient)
                .Verify(client => client.StoreClientDataAsync(It.Is<IReadOnlyDictionary<string, object?>>(
                    p => p.Count == 2
                        && p.ContainsKey("QbtMud.K1")
                        && p.ContainsKey("QbtMud.K2")
                        && p["QbtMud.K1"]!.Equals("V1")
                        && p["QbtMud.K2"]!.Equals(2)
                    ), It.IsAny<CancellationToken>()), Times.Once);
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

            Mock.Get(_apiClient).Verify(client => client.StoreClientDataAsync(It.IsAny<IReadOnlyDictionary<string, object?>>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_MixedKeys_WHEN_RemovePrefixedEntriesAsync_THEN_ShouldStoreDistinctNullValuesForPrefixedKeys()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.StoreClientDataAsync(It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await _target.RemovePrefixedEntriesAsync(["QbtMud.Key", " QbtMud.Key ", "Other"], TestContext.Current.CancellationToken);

            Mock.Get(_apiClient)
                .Verify(client => client.StoreClientDataAsync(It.Is<IReadOnlyDictionary<string, object?>>(
                    p => p.Count == 1
                        && p.ContainsKey("QbtMud.Key")
                        && p["QbtMud.Key"] == null
                    ), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
