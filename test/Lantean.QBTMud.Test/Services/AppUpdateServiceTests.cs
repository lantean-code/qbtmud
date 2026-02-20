using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class AppUpdateServiceTests : IDisposable
    {
        private readonly IAppBuildInfoService _appBuildInfoService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AppUpdateService> _logger;
        private readonly List<IDisposable> _createdDisposables;

        public AppUpdateServiceTests()
        {
            _appBuildInfoService = Mock.Of<IAppBuildInfoService>();
            _httpClientFactory = Mock.Of<IHttpClientFactory>();
            _logger = Mock.Of<ILogger<AppUpdateService>>();
            _createdDisposables = [];
        }

        [Fact]
        public async Task GIVEN_NewerStableRelease_WHEN_GetUpdateStatusInvoked_THEN_ReturnsUpdateAvailable()
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));

            HttpRequestMessage? capturedRequest = null;
            var handler = new DelegateMessageHandler((request, _) =>
            {
                capturedRequest = request;
                return Task.FromResult(CreateJsonResponse(
                    HttpStatusCode.OK,
                    "{\"tag_name\":\"v1.1.0\",\"name\":\"v1.1.0\",\"html_url\":\"https://github.com/lantean-code/qbtmud/releases/tag/v1.1.0\",\"published_at\":\"2025-01-01T00:00:00Z\"}"));
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);

            var result = await target.GetUpdateStatusAsync(forceRefresh: true, cancellationToken: TestContext.Current.CancellationToken);

            result.IsUpdateAvailable.Should().BeTrue();
            result.CanCompareVersions.Should().BeTrue();
            result.LatestRelease.Should().NotBeNull();
            result.LatestRelease!.TagName.Should().Be("v1.1.0");
            capturedRequest.Should().NotBeNull();
            capturedRequest!.RequestUri!.AbsolutePath.Should().Be("/repos/lantean-code/qbtmud/releases/latest");
        }

        [Fact]
        public async Task GIVEN_HttpFailure_WHEN_GetUpdateStatusInvoked_THEN_ReturnsStatusWithoutLatestRelease()
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));

            var handler = new DelegateMessageHandler((_, _) =>
            {
                return Task.FromResult(CreateResponse(HttpStatusCode.InternalServerError));
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);

            var result = await target.GetUpdateStatusAsync(forceRefresh: true, cancellationToken: TestContext.Current.CancellationToken);

            result.LatestRelease.Should().BeNull();
            result.IsUpdateAvailable.Should().BeFalse();
            result.CanCompareVersions.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_UnparseableReleaseTag_WHEN_GetUpdateStatusInvoked_THEN_ReturnsNonComparableStatus()
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));

            var handler = new DelegateMessageHandler((_, _) =>
            {
                return Task.FromResult(CreateJsonResponse(
                    HttpStatusCode.OK,
                    "{\"tag_name\":\"latest\",\"name\":\"latest\",\"html_url\":\"https://github.com/lantean-code/qbtmud/releases/tag/latest\",\"published_at\":\"2025-01-01T00:00:00Z\"}"));
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);

            var result = await target.GetUpdateStatusAsync(forceRefresh: true, cancellationToken: TestContext.Current.CancellationToken);

            result.LatestRelease.Should().NotBeNull();
            result.IsUpdateAvailable.Should().BeFalse();
            result.CanCompareVersions.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_RequestThrows_WHEN_GetUpdateStatusInvoked_THEN_ReturnsStatusWithoutLatestRelease()
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));

            var handler = new DelegateMessageHandler((_, _) =>
            {
                throw new HttpRequestException("Request failed");
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);

            var result = await target.GetUpdateStatusAsync(forceRefresh: true, cancellationToken: TestContext.Current.CancellationToken);

            result.LatestRelease.Should().BeNull();
            result.IsUpdateAvailable.Should().BeFalse();
            result.CanCompareVersions.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ReleaseWithBlankName_WHEN_GetUpdateStatusInvoked_THEN_UsesTagAsReleaseName()
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));

            var handler = new DelegateMessageHandler((_, _) =>
            {
                return Task.FromResult(CreateJsonResponse(
                    HttpStatusCode.OK,
                    "{\"tag_name\":\" v1.0.0 \",\"name\":\"   \",\"html_url\":\" https://github.com/lantean-code/qbtmud/releases/tag/v1.0.0 \"}"));
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);

            var result = await target.GetUpdateStatusAsync(forceRefresh: true, cancellationToken: TestContext.Current.CancellationToken);

            result.LatestRelease.Should().NotBeNull();
            result.LatestRelease!.TagName.Should().Be("v1.0.0");
            result.LatestRelease.Name.Should().Be("v1.0.0");
            result.LatestRelease.HtmlUrl.Should().Be("https://github.com/lantean-code/qbtmud/releases/tag/v1.0.0");
        }

        [Fact]
        public async Task GIVEN_ReleasePayloadMissingTagOrUrl_WHEN_GetUpdateStatusInvoked_THEN_ReturnsNoLatestRelease()
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));

            var handler = new DelegateMessageHandler((_, _) =>
            {
                return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{\"tag_name\":\"\",\"html_url\":\"\"}"));
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);

            var result = await target.GetUpdateStatusAsync(forceRefresh: true, cancellationToken: TestContext.Current.CancellationToken);

            result.LatestRelease.Should().BeNull();
            result.CanCompareVersions.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_InvalidJsonPayload_WHEN_GetUpdateStatusInvoked_THEN_ReturnsNoLatestRelease()
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));

            var handler = new DelegateMessageHandler((_, _) =>
            {
                return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{\"tag_name\":"));
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);

            var result = await target.GetUpdateStatusAsync(forceRefresh: true, cancellationToken: TestContext.Current.CancellationToken);

            result.LatestRelease.Should().BeNull();
            result.CanCompareVersions.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_CachedStatusAndNoForceRefresh_WHEN_GetUpdateStatusInvokedTwice_THEN_UsesCache()
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));

            var requestCount = 0;
            var handler = new DelegateMessageHandler((_, _) =>
            {
                requestCount++;
                return Task.FromResult(CreateJsonResponse(
                    HttpStatusCode.OK,
                    "{\"tag_name\":\"v1.0.0\",\"name\":\"v1.0.0\",\"html_url\":\"https://github.com/lantean-code/qbtmud/releases/tag/v1.0.0\"}"));
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);

            _ = await target.GetUpdateStatusAsync(forceRefresh: false, cancellationToken: TestContext.Current.CancellationToken);
            _ = await target.GetUpdateStatusAsync(forceRefresh: false, cancellationToken: TestContext.Current.CancellationToken);

            requestCount.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_ForceRefresh_WHEN_GetUpdateStatusInvokedTwice_THEN_BypassesCache()
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));

            var requestCount = 0;
            var handler = new DelegateMessageHandler((_, _) =>
            {
                requestCount++;
                return Task.FromResult(CreateJsonResponse(
                    HttpStatusCode.OK,
                    "{\"tag_name\":\"v1.0.1\",\"name\":\"v1.0.1\",\"html_url\":\"https://github.com/lantean-code/qbtmud/releases/tag/v1.0.1\"}"));
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);

            _ = await target.GetUpdateStatusAsync(forceRefresh: true, cancellationToken: TestContext.Current.CancellationToken);
            _ = await target.GetUpdateStatusAsync(forceRefresh: true, cancellationToken: TestContext.Current.CancellationToken);

            requestCount.Should().Be(2);
        }

        [Theory]
        [InlineData("v1.0.0+build.5", "1.0.0+build.6", false)]
        [InlineData("1.1", "1.1.0.1", true)]
        [InlineData("1.0.0.1", "1.0.0.2", true)]
        [InlineData("2.0.0", "1.9.9", false)]
        [InlineData("1.2.3-rc1", "1.2.3", true)]
        [InlineData("1.2.3", "1.2.3-rc1", false)]
        public async Task GIVEN_ComparableVersions_WHEN_GetUpdateStatusInvoked_THEN_ComputesExpectedAvailability(
            string currentVersion,
            string latestTag,
            bool expectedUpdateAvailable)
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo(currentVersion, "AssemblyMetadata"));

            var handler = new DelegateMessageHandler((_, _) =>
            {
                var payload = $"{{\"tag_name\":\"{latestTag}\",\"name\":\"{latestTag}\",\"html_url\":\"https://github.com/lantean-code/qbtmud/releases/tag/{latestTag}\"}}";
                return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, payload));
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);

            var result = await target.GetUpdateStatusAsync(forceRefresh: true, cancellationToken: TestContext.Current.CancellationToken);

            result.CanCompareVersions.Should().BeTrue();
            result.IsUpdateAvailable.Should().Be(expectedUpdateAvailable);
        }

        [Theory]
        [InlineData("   ", "1.0.0")]
        [InlineData("1.a.0", "1.0.0")]
        public async Task GIVEN_InvalidCurrentBuildVersion_WHEN_GetUpdateStatusInvoked_THEN_ReturnsNonComparableStatus(string currentVersion, string latestTag)
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo(currentVersion, "AssemblyMetadata"));

            var handler = new DelegateMessageHandler((_, _) =>
            {
                var payload = $"{{\"tag_name\":\"{latestTag}\",\"name\":\"{latestTag}\",\"html_url\":\"https://github.com/lantean-code/qbtmud/releases/tag/{latestTag}\"}}";
                return Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, payload));
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);

            var result = await target.GetUpdateStatusAsync(forceRefresh: true, cancellationToken: TestContext.Current.CancellationToken);

            result.CanCompareVersions.Should().BeFalse();
            result.IsUpdateAvailable.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_CallerCancellationDuringReleaseFetch_WHEN_GetUpdateStatusInvoked_THEN_ThrowsAndSubsequentCallDoesNotUseCancelledFallback()
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));

            var requestCount = 0;
            var firstRequestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var handler = new DelegateMessageHandler(async (_, cancellationToken) =>
            {
                var currentRequestNumber = Interlocked.Increment(ref requestCount);
                if (currentRequestNumber == 1)
                {
                    firstRequestStarted.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }

                return CreateJsonResponse(
                    HttpStatusCode.OK,
                    "{\"tag_name\":\"v1.0.1\",\"name\":\"v1.0.1\",\"html_url\":\"https://github.com/lantean-code/qbtmud/releases/tag/v1.0.1\"}");
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancelledCall = target.GetUpdateStatusAsync(forceRefresh: false, cancellationTokenSource.Token);
            await firstRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            await cancellationTokenSource.CancelAsync();

            Func<Task> cancelledCallAction = async () => await cancelledCall;
            await cancelledCallAction.Should().ThrowAsync<TaskCanceledException>();

            var successfulCall = await target.GetUpdateStatusAsync(forceRefresh: false, cancellationToken: TestContext.Current.CancellationToken);

            requestCount.Should().Be(2);
            successfulCall.CanCompareVersions.Should().BeTrue();
            successfulCall.IsUpdateAvailable.Should().BeTrue();
            successfulCall.LatestRelease.Should().NotBeNull();
            successfulCall.LatestRelease!.TagName.Should().Be("v1.0.1");
        }

        [Fact]
        public async Task GIVEN_ConcurrentRefreshRequests_WHEN_FirstRequestPopulatesCache_THEN_SecondRequestUsesSemaphoreCache()
        {
            Mock.Get(_appBuildInfoService)
                .Setup(service => service.GetCurrentBuildInfo())
                .Returns(new AppBuildInfo("1.0.0", "AssemblyMetadata"));

            var requestCount = 0;
            var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var releaseRequest = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

            var handler = new DelegateMessageHandler(async (_, _) =>
            {
                requestCount++;
                requestStarted.TrySetResult();
                return await releaseRequest.Task;
            });
            ConfigureHttpClient(handler);

            var target = new AppUpdateService(_httpClientFactory, _appBuildInfoService, _logger);
            var first = target.GetUpdateStatusAsync(forceRefresh: false, cancellationToken: TestContext.Current.CancellationToken);
            await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            var second = target.GetUpdateStatusAsync(forceRefresh: false, cancellationToken: TestContext.Current.CancellationToken);

            releaseRequest.TrySetResult(CreateJsonResponse(
                HttpStatusCode.OK,
                "{\"tag_name\":\"v1.0.1\",\"name\":\"v1.0.1\",\"html_url\":\"https://github.com/lantean-code/qbtmud/releases/tag/v1.0.1\"}"));

            _ = await first;
            _ = await second;

            requestCount.Should().Be(1);
        }

        private void ConfigureHttpClient(HttpMessageHandler handler)
        {
            var httpClient = TestHttpClientFactory.CreateClient(handler);
            httpClient.BaseAddress = new Uri("https://api.github.com/");
            _createdDisposables.Add(httpClient);
            _createdDisposables.Add(handler);

            Mock.Get(_httpClientFactory)
                .Setup(factory => factory.CreateClient("GitHubReleases"))
                .Returns(httpClient);
        }

        private HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string json)
        {
            var response = CreateResponse(statusCode);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        private HttpResponseMessage CreateResponse(HttpStatusCode statusCode)
        {
            var response = new HttpResponseMessage(statusCode);
            _createdDisposables.Add(response);
            return response;
        }

        public void Dispose()
        {
            foreach (var disposable in _createdDisposables)
            {
                disposable.Dispose();
            }
        }

        private sealed class DelegateMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

            public DelegateMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _handler(request, cancellationToken);
            }
        }
    }
}
