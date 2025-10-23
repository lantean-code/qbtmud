using System.Net;
using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientTransferInfoTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientTransferInfoTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_OKJson_WHEN_GetGlobalTransferInfo_THEN_ShouldDeserialize()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });

            var result = await _target.GetGlobalTransferInfo();

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetGlobalTransferInfo_THEN_ShouldThrowWithStatusAndMessage()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.GetGlobalTransferInfo();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            ex.Which.Message.Should().Be("bad");
        }

        [Fact]
        public async Task GIVEN_ResponseIsOne_WHEN_GetAlternativeSpeedLimitsState_THEN_ShouldBeTrue()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("1")
            });

            var result = await _target.GetAlternativeSpeedLimitsState();

            result.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ResponseIsZero_WHEN_GetAlternativeSpeedLimitsState_THEN_ShouldBeFalse()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("0")
            });

            var result = await _target.GetAlternativeSpeedLimitsState();

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetAlternativeSpeedLimitsState_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("no")
            });

            var act = async () => await _target.GetAlternativeSpeedLimitsState();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            ex.Which.Message.Should().Be("no");
        }

        [Fact]
        public async Task GIVEN_OK_WHEN_ToggleAlternativeSpeedLimits_THEN_ShouldPOSTAndNotThrow()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/transfer/toggleSpeedLimitsMode");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            };

            await _target.ToggleAlternativeSpeedLimits();
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_ToggleAlternativeSpeedLimits_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("err")
            });

            var act = async () => await _target.ToggleAlternativeSpeedLimits();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            ex.Which.Message.Should().Be("err");
        }

        [Fact]
        public async Task GIVEN_Digits_WHEN_GetGlobalDownloadLimit_THEN_ShouldParseLong()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("1234567890")
            });

            var result = await _target.GetGlobalDownloadLimit();

            result.Should().Be(1234567890);
        }

        [Fact]
        public async Task GIVEN_InvalidNumber_WHEN_GetGlobalDownloadLimit_THEN_ShouldThrowFormatException()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("oops")
            });

            var act = async () => await _target.GetGlobalDownloadLimit();

            await act.Should().ThrowAsync<FormatException>();
        }

        [Fact]
        public async Task GIVEN_Limit_WHEN_SetGlobalDownloadLimit_THEN_ShouldPOSTFormWithLimit()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/transfer/setDownloadLimit");
                req.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("limit=5000");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetGlobalDownloadLimit(5000);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_SetGlobalDownloadLimit_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.SetGlobalDownloadLimit(1);

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad");
        }

        [Fact]
        public async Task GIVEN_Digits_WHEN_GetGlobalUploadLimit_THEN_ShouldParseLong()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("4321")
            });

            var result = await _target.GetGlobalUploadLimit();

            result.Should().Be(4321);
        }

        [Fact]
        public async Task GIVEN_InvalidNumber_WHEN_GetGlobalUploadLimit_THEN_ShouldThrowFormatException()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("NaN")
            });

            var act = async () => await _target.GetGlobalUploadLimit();

            await act.Should().ThrowAsync<FormatException>();
        }

        [Fact]
        public async Task GIVEN_Limit_WHEN_SetGlobalUploadLimit_THEN_ShouldPOSTFormWithLimit()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/transfer/setUploadLimit");
                req.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("limit=9001");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetGlobalUploadLimit(9001);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_SetGlobalUploadLimit_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("nope")
            });

            var act = async () => await _target.SetGlobalUploadLimit(1);

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ex.Which.Message.Should().Be("nope");
        }

        [Fact]
        public async Task GIVEN_EmptyPeers_WHEN_BanPeers_THEN_ShouldPOSTFormWithEmptyPeersValue()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/transfer/banPeers");
                req.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("peers=");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.BanPeers(Array.Empty<PeerId>());
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_BanPeers_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("conflict")
            });

            var act = async () => await _target.BanPeers(Array.Empty<PeerId>());

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
            ex.Which.Message.Should().Be("conflict");
        }
    }
}
