using AwesomeAssertions;
using System.Net;
using System.Text;

namespace Lantean.QBitTorrentClient.Test
{
    public sealed class HttpClientExtensionsTests : IDisposable
    {
        private readonly CapturingHandler _handler;
        private readonly HttpClient _target;

        public HttpClientExtensionsTests()
        {
            _handler = new CapturingHandler();
            _target = new HttpClient(_handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };
        }

        [Fact]
        public async Task GIVEN_FormUrlEncodedBuilder_WHEN_PostAsyncCalled_THEN_ShouldPostEncodedPayload()
        {
            HttpRequestMessage? capturedRequest = null;
            _handler.SetResponder(async request =>
            {
                capturedRequest = request;
                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });

            var builder = new FormUrlEncodedBuilder()
                .Add("key", "value");

            var response = await _target.PostAsync("/api/v2/test", builder);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            capturedRequest.Should().NotBeNull();
            capturedRequest!.Method.Should().Be(HttpMethod.Post);
            capturedRequest.RequestUri!.ToString().Should().Be("http://localhost/api/v2/test");

            var payload = await capturedRequest.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
            payload.Should().Be("key=value");
        }

        [Fact]
        public async Task GIVEN_QueryBuilder_WHEN_GetAsyncCalled_THEN_ShouldAppendQueryStringToUrl()
        {
            HttpRequestMessage? capturedRequest = null;
            _handler.SetResponder(async request =>
            {
                capturedRequest = request;
                return await Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("ok", Encoding.UTF8, "text/plain")
                });
            });

            var builder = new QueryBuilder()
                .Add("first", "one")
                .Add("second", "two");

            var response = await _target.GetAsync("/api/v2/test", builder);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            capturedRequest.Should().NotBeNull();
            capturedRequest!.Method.Should().Be(HttpMethod.Get);
            capturedRequest.RequestUri!.ToString().Should().Be("http://localhost/api/v2/test?first=one&second=two");
        }

        public void Dispose()
        {
            _target.Dispose();
        }

        private sealed class CapturingHandler : HttpMessageHandler
        {
            private Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

            public CapturingHandler()
            {
                _responder = _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            public void SetResponder(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _responder(request);
            }
        }
    }
}
