using System.Text.Json;
using AwesomeAssertions;
using Lantean.QBTMud.Core.Models;
using Moq;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Application.Test.Services
{
    public sealed class ClientDataPresenceServiceTests
    {
        private readonly IWebApiCapabilityService _webApiCapabilityService;
        private readonly IClientDataStorageAdapter _clientDataStorageAdapter;
        private readonly ClientDataPresenceService _target;

        public ClientDataPresenceServiceTests()
        {
            _webApiCapabilityService = Mock.Of<IWebApiCapabilityService>();
            _clientDataStorageAdapter = Mock.Of<IClientDataStorageAdapter>();
            _target = new ClientDataPresenceService(_webApiCapabilityService, _clientDataStorageAdapter);
        }

        [Fact]
        public async Task GIVEN_ClientDataUnsupported_WHEN_HasStoredClientDataInvoked_THEN_ShouldReturnFalse()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 12, 0), false));

            var result = await _target.HasStoredClientDataAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().BeFalse();
            Mock.Get(_clientDataStorageAdapter)
                .Verify(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ClientDataSupportedWithoutEntries_WHEN_HasStoredClientDataInvoked_THEN_ShouldReturnFalse()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(new Dictionary<string, JsonElement>(StringComparer.Ordinal)));

            var result = await _target.HasStoredClientDataAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ClientDataSupportedWithEntries_WHEN_HasStoredClientDataInvoked_THEN_ShouldReturnTrue()
        {
            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromEntries(
                    new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                    {
                        ["QbtMud.AppSettings.State.v2"] = JsonDocument.Parse("{\"notificationsEnabled\":true}").RootElement.Clone()
                    }));

            var result = await _target.HasStoredClientDataAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ClientDataLoadFails_WHEN_HasStoredClientDataInvoked_THEN_ShouldReturnFalse()
        {
            var failure = CreateFailureResult();

            Mock.Get(_webApiCapabilityService)
                .Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 13, 1), true));
            Mock.Get(_clientDataStorageAdapter)
                .Setup(adapter => adapter.LoadPrefixedEntriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(ClientDataLoadResult.FromFailure(failure));

            var result = await _target.HasStoredClientDataAsync(Xunit.TestContext.Current.CancellationToken);

            result.Should().BeFalse();
        }

        private static ApiResult CreateFailureResult()
        {
            return ApiResult.CreateFailure(new ApiFailure
            {
                Kind = ApiFailureKind.ServerError,
                Operation = "Operation",
                UserMessage = "Failure"
            });
        }
    }
}
