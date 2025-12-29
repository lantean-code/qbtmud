using AwesomeAssertions;
using Lantean.QBTMud.Services;
using System.Net;

namespace Lantean.QBTMud.Test.Services
{
    public class CookieHandlerTests
    {
        private readonly CookieHandler _target;

        public CookieHandlerTests()
        {
            _target = new CookieHandler();
        }

        [Fact]
        public async Task GIVEN_Request_WHEN_SendAsync_THEN_ShouldIncludeBrowserCredentials()
        {
            var inner = new SpyHandler();
            _target.InnerHandler = inner;
            using var invoker = new HttpMessageInvoker(_target, disposeHandler: false);
            var request = new HttpRequestMessage(HttpMethod.Get, "http://example.com");

            var response = await invoker.SendAsync(request, CancellationToken.None);

            inner.SentRequests.Count.Should().Be(1);
            inner.CredentialsIncluded.Should().BeTrue();
            response.Should().BeSameAs(inner.Response);
        }

        private sealed class SpyHandler : HttpMessageHandler
        {
            public List<HttpRequestMessage> SentRequests { get; } = new();

            public HttpResponseMessage Response { get; } = new(HttpStatusCode.OK);

            public bool CredentialsIncluded { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                SentRequests.Add(request);
                foreach (var option in request.Options)
                {
                    if (option.Value is Dictionary<string, object> fetchOptions &&
                        fetchOptions.TryGetValue("credentials", out var value) &&
                        value is string credentials &&
                        credentials == "include")
                    {
                        CredentialsIncluded = true;
                    }
                }

                return Task.FromResult(Response);
            }
        }
    }
}
