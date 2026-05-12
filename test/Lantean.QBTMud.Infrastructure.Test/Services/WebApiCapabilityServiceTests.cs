using System.Net;
using AwesomeAssertions;
using Moq;
using QBittorrent.ApiClient;

namespace Lantean.QBTMud.Infrastructure.Test.Services
{
    public sealed class WebApiCapabilityServiceTests
    {
        private readonly IApiClient _apiClient;
        private readonly WebApiCapabilityService _target;

        public WebApiCapabilityServiceTests()
        {
            _apiClient = Mock.Of<IApiClient>();
            _target = new WebApiCapabilityService(_apiClient);
        }

        [Fact]
        public async Task GIVEN_WebApiVersionAtClientDataThreshold_WHEN_GetCapabilityStateAsync_THEN_ShouldReportClientDataSupported()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetAPIVersionAsync())
                .ReturnsSuccessAsync("2.13.1");

            var result = await _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);

            result.SupportsClientData.Should().BeTrue();
            result.SupportsTrackerErrorFilters.Should().BeFalse();
            result.ParsedWebApiVersion.Should().Be(new Version(2, 13, 1));
            result.RawWebApiVersion.Should().Be("2.13.1");
        }

        [Fact]
        public async Task GIVEN_WebApiVersionAtTrackerFilterThreshold_WHEN_GetCapabilityStateAsync_THEN_ShouldReportTrackerFiltersSupported()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetAPIVersionAsync())
                .ReturnsSuccessAsync("2.15.1");

            var result = await _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);

            result.SupportsClientData.Should().BeTrue();
            result.SupportsTrackerErrorFilters.Should().BeTrue();
            result.ParsedWebApiVersion.Should().Be(new Version(2, 15, 1));
            result.RawWebApiVersion.Should().Be("2.15.1");
        }

        [Fact]
        public async Task GIVEN_WebApiVersionBeforeThreshold_WHEN_GetCapabilityStateAsync_THEN_ShouldReportClientDataUnsupported()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetAPIVersionAsync())
                .ReturnsSuccessAsync("2.13.0");

            var result = await _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);

            result.SupportsClientData.Should().BeFalse();
            result.SupportsTrackerErrorFilters.Should().BeFalse();
            result.ParsedWebApiVersion.Should().Be(new Version(2, 13, 0));
            result.RawWebApiVersion.Should().Be("2.13.0");
        }

        [Fact]
        public async Task GIVEN_MalformedWebApiVersion_WHEN_GetCapabilityStateAsync_THEN_ShouldReportUnsupported()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetAPIVersionAsync())
                .ReturnsSuccessAsync("not-a-version");

            var result = await _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);

            result.SupportsClientData.Should().BeFalse();
            result.SupportsTrackerErrorFilters.Should().BeFalse();
            result.ParsedWebApiVersion.Should().BeNull();
            result.RawWebApiVersion.Should().Be("not-a-version");
        }

        [Fact]
        public async Task GIVEN_ApiVersionRequestThrows_WHEN_GetCapabilityStateAsync_THEN_ShouldReportUnsupported()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetAPIVersionAsync())
                .ReturnsFailure(ApiFailureKind.ServerError, "failure", HttpStatusCode.InternalServerError);

            var result = await _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);

            result.SupportsClientData.Should().BeFalse();
            result.SupportsTrackerErrorFilters.Should().BeFalse();
            result.ParsedWebApiVersion.Should().BeNull();
            result.RawWebApiVersion.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_FirstApiVersionRequestThrowsThenSucceeds_WHEN_GetCapabilityStateAsyncCalledTwice_THEN_ShouldRetryAndCacheSuccessfulResult()
        {
            var apiClientMock = Mock.Get(_apiClient);
            apiClientMock
                .SetupSequence(client => client.GetAPIVersionAsync())
                .ReturnsFailure(ApiFailureKind.ServerError, "failure", HttpStatusCode.InternalServerError)
                .ReturnsAsync("2.13.1");

            var failed = await _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);
            var succeeded = await _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);
            var cached = await _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);

            failed.SupportsClientData.Should().BeFalse();
            succeeded.SupportsClientData.Should().BeTrue();
            cached.SupportsClientData.Should().BeTrue();
            apiClientMock.Verify(client => client.GetAPIVersionAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_FirstResultCached_WHEN_GetCapabilityStateAsyncCalledTwice_THEN_ShouldCallApiOnce()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetAPIVersionAsync())
                .ReturnsSuccessAsync("2.13.1");

            var first = await _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);
            var second = await _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);

            first.SupportsClientData.Should().BeTrue();
            second.SupportsClientData.Should().BeTrue();
            Mock.Get(_apiClient)
                .Verify(client => client.GetAPIVersionAsync(), Times.Once);
        }

        [Fact]
        public async Task GIVEN_VersionWithWhitespace_WHEN_GetCapabilityStateAsync_THEN_ShouldTrimAndParseVersion()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetAPIVersionAsync())
                .ReturnsSuccessAsync(" 2.13.1 ");

            var result = await _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);

            result.RawWebApiVersion.Should().Be("2.13.1");
            result.ParsedWebApiVersion.Should().Be(new Version(2, 13, 1));
            result.SupportsClientData.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_BlankVersion_WHEN_GetCapabilityStateAsync_THEN_ShouldReportUnsupportedWithNullRawValue()
        {
            Mock.Get(_apiClient)
                .Setup(client => client.GetAPIVersionAsync())
                .ReturnsSuccessAsync("   ");

            var result = await _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);

            result.RawWebApiVersion.Should().BeNull();
            result.ParsedWebApiVersion.Should().BeNull();
            result.SupportsClientData.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ConcurrentCalls_WHEN_FirstCallInitializesCache_THEN_SecondCallUsesCachedValueInsideSemaphore()
        {
            var versionCompletion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            Mock.Get(_apiClient)
                .Setup(client => client.GetAPIVersionAsync())
                .ReturnsSuccess(versionCompletion.Task);

            var firstTask = _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);
            var secondTask = _target.GetCapabilityStateAsync(TestContext.Current.CancellationToken);

            versionCompletion.SetResult("2.13.1");

            var first = await firstTask;
            var second = await secondTask;

            first.SupportsClientData.Should().BeTrue();
            second.SupportsClientData.Should().BeTrue();
            Mock.Get(_apiClient)
                .Verify(client => client.GetAPIVersionAsync(), Times.Once);
        }
    }
}
