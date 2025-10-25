using System.Net;
using AwesomeAssertions;

namespace Lantean.QBitTorrentClient.Test
{
    public partial class ApiClientAuthenticationTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientAuthenticationTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_ServerReturnsOK_WHEN_CheckAuthState_THEN_ShouldBeTrue()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/app/version");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            var result = await _target.CheckAuthState();

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ServerReturnsNonOK_WHEN_CheckAuthState_THEN_ShouldBeFalse()
        {
            _handler.Responder = async (req, ct) =>
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            };

            var result = await _target.CheckAuthState();

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_HandlerThrows_WHEN_CheckAuthState_THEN_ShouldBeFalse()
        {
            _handler.Responder = (_, _) => throw new HttpRequestException("boom", null, HttpStatusCode.BadGateway);

            var result = await _target.CheckAuthState();

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_ValidCredentialsAndSuccessStatus_WHEN_Login_THEN_ShouldPostFormAndNotThrow()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/auth/login");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("username=user&password=pass");
                req.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Ok")
                };
            };

            await _target.Login("user", "pass");
        }

        [Fact]
        public async Task GIVEN_SuccessStatusButFailsBody_WHEN_Login_THEN_ShouldThrowBadRequest()
        {
            _handler.Responder = async (req, ct) =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Fails.")
                };
            };

            var act = async () => await _target.Login("user", "pass");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GIVEN_NonSuccessStatus_WHEN_Login_THEN_ShouldThrowWithStatusAndMessage()
        {
            _handler.Responder = async (req, ct) =>
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("Nope")
                };
            };

            var act = async () => await _target.Login("user", "pass");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            ex.Which.Message.Should().Be("Nope");
        }

        [Fact]
        public async Task GIVEN_Success_WHEN_Logout_THEN_ShouldPostAndNotThrow()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/auth/logout");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.Logout();
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_Logout_THEN_ShouldThrowWithStatusAndMessage()
        {
            _handler.Responder = async (req, ct) =>
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("fail")
                };
            };

            var act = async () => await _target.Logout();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            ex.Which.Message.Should().Be("fail");
        }
    }
}
