using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBTMud.Infrastructure.Services;
using Moq;
using QBittorrent.ApiClient;

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

            result.Succeeded.Should().BeTrue();
            result.Entries.Should().BeEmpty();
            Mock.Get(_apiClient).Verify(client => client.LoadClientDataAsync(It.IsAny<IEnumerable<string>?>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_MixedKeysAndResults_WHEN_LoadPrefixedEntriesAsyncWithKeys_THEN_ShouldNormalizeRequestAndFilterResponse()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoadClientDataAsync(It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsSuccessAsync(new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["QbtMud.KeyA"] = JsonDocument.Parse("{\"enabled\":true}").RootElement.Clone(),
                    ["QbtMud.KeyB"] = JsonDocument.Parse("\"value\"").RootElement.Clone(),
                    ["NotPrefixed"] = JsonDocument.Parse("1").RootElement.Clone()
                });

            var result = await _target.LoadPrefixedEntriesAsync(
                [" QbtMud.KeyA ", "QbtMud.KeyB", "QbtMud.KeyA", "NotPrefixed", ""],
                TestContext.Current.CancellationToken);

            result.Succeeded.Should().BeTrue();
            result.Entries.Should().NotBeNull();
            result.Entries!.Keys.Should().Equal("QbtMud.KeyA", "QbtMud.KeyB");

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
                .ReturnsSuccessAsync(new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["QbtMud.One"] = JsonDocument.Parse("1").RootElement.Clone(),
                    ["Two"] = JsonDocument.Parse("2").RootElement.Clone()
                });

            var result = await _target.LoadPrefixedEntriesAsync(TestContext.Current.CancellationToken);

            result.Succeeded.Should().BeTrue();
            result.Entries.Should().NotBeNull();
            result.Entries!.Should().ContainKey("QbtMud.One");
            result.Entries.Should().NotContainKey("Two");
        }

        [Fact]
        public async Task GIVEN_LoadWithKeysFails_WHEN_LoadPrefixedEntriesAsyncWithKeys_THEN_ShouldReturnFailure()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoadClientDataAsync(It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient, IReadOnlyDictionary<string, JsonElement>>(ApiFailureKind.ServerError, "Failure");

            var result = await _target.LoadPrefixedEntriesAsync(["QbtMud.Key"], TestContext.Current.CancellationToken);

            result.Succeeded.Should().BeFalse();
            result.Entries.Should().BeNull();
            result.FailureResult.Should().NotBeNull();
            result.FailureResult!.IsFailure.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_LoadWithoutKeysFails_WHEN_LoadPrefixedEntriesAsyncWithoutKeys_THEN_ShouldReturnFailure()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.LoadClientDataAsync(It.IsAny<IEnumerable<string>?>(), It.IsAny<CancellationToken>()))
                .ReturnsFailure<IApiClient, IReadOnlyDictionary<string, JsonElement>>(ApiFailureKind.ServerError, "Failure");

            var result = await _target.LoadPrefixedEntriesAsync(TestContext.Current.CancellationToken);

            result.Succeeded.Should().BeFalse();
            result.Entries.Should().BeNull();
            result.FailureResult.Should().NotBeNull();
            result.FailureResult!.IsFailure.Should().BeTrue();
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
            var result = await _target.StorePrefixedEntriesAsync(
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    [""] = "A",
                    ["NotPrefixed"] = "B"
                },
                TestContext.Current.CancellationToken);

            result.Succeeded.Should().BeTrue();
            Mock.Get(_apiClient).Verify(client => client.StoreClientDataAsync(It.IsAny<IReadOnlyDictionary<string, JsonElement?>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_MixedValues_WHEN_StorePrefixedEntriesAsync_THEN_ShouldNormalizeAndStoreOnlyPrefixedValues()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.StoreClientDataAsync(It.IsAny<IReadOnlyDictionary<string, JsonElement?>>(), It.IsAny<CancellationToken>()))
                .ReturnsSuccess(Task.CompletedTask);

            var result = await _target.StorePrefixedEntriesAsync(
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    [" QbtMud.K1 "] = "V1",
                    ["NotPrefixed"] = "V2",
                    ["QbtMud.K2"] = 2
                },
                TestContext.Current.CancellationToken);

            result.Succeeded.Should().BeTrue();
            Mock.Get(_apiClient)
                .Verify(client => client.StoreClientDataAsync(
                    It.Is<IReadOnlyDictionary<string, JsonElement?>>(payload => MatchesStoredPayload(payload)),
                    It.IsAny<CancellationToken>()), Times.Once);
            result.FailureResult.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_StoreFails_WHEN_StorePrefixedEntriesAsync_THEN_ShouldReturnFailure()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.StoreClientDataAsync(It.IsAny<IReadOnlyDictionary<string, JsonElement?>>(), It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure");

            var result = await _target.StorePrefixedEntriesAsync(
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["QbtMud.K1"] = "V1"
                },
                TestContext.Current.CancellationToken);

            result.Succeeded.Should().BeFalse();
            result.FailureResult.Should().NotBeNull();
            result.FailureResult!.IsFailure.Should().BeTrue();
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
            var result = await _target.RemovePrefixedEntriesAsync(["", "NotPrefixed"], TestContext.Current.CancellationToken);

            result.Succeeded.Should().BeTrue();
            Mock.Get(_apiClient).Verify(client => client.DeleteClientDataAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_MixedKeys_WHEN_RemovePrefixedEntriesAsync_THEN_ShouldStoreDistinctNullValuesForPrefixedKeys()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.DeleteClientDataAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsSuccess(Task.CompletedTask);

            var result = await _target.RemovePrefixedEntriesAsync(["QbtMud.Key", " QbtMud.Key ", "Other"], TestContext.Current.CancellationToken);

            result.Succeeded.Should().BeTrue();
            Mock.Get(_apiClient)
                .Verify(client => client.DeleteClientDataAsync(It.Is<IEnumerable<string>>(
                    p => p.Count() == 1
                        && p.Contains("QbtMud.Key")
                    ), It.IsAny<CancellationToken>()), Times.Once);
            result.FailureResult.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_RemoveFails_WHEN_RemovePrefixedEntriesAsync_THEN_ShouldReturnFailure()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.DeleteClientDataAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsFailure(ApiFailureKind.ServerError, "Failure");

            var result = await _target.RemovePrefixedEntriesAsync(["QbtMud.Key"], TestContext.Current.CancellationToken);

            result.Succeeded.Should().BeFalse();
            result.FailureResult.Should().NotBeNull();
            result.FailureResult!.IsFailure.Should().BeTrue();
        }

        private static bool MatchesStoredPayload(IReadOnlyDictionary<string, JsonElement?> payload)
        {
            if (payload.Count != 2
                || !payload.ContainsKey("QbtMud.K1")
                || !payload.ContainsKey("QbtMud.K2"))
            {
                return false;
            }

            var stringValue = payload["QbtMud.K1"];
            var numberValue = payload["QbtMud.K2"];

            return stringValue.HasValue
                && stringValue.Value.ValueKind == JsonValueKind.String
                && stringValue.Value.GetString() == "V1"
                && numberValue.HasValue
                && numberValue.Value.ValueKind == JsonValueKind.Number
                && numberValue.Value.GetInt32() == 2;
        }
    }
}
