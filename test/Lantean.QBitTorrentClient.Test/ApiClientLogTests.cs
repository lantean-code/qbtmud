using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using System.Net;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientLogTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientLogTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_NoFilters_WHEN_GetLog_THEN_ShouldGETWithoutQueryAndReturnList()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/log/main");
                req.RequestUri!.Query.Should().BeEmpty();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var result = await _target.GetLog();

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_LogsJson_WHEN_GetLog_THEN_ShouldDeserializeLogEntries()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/log/main");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        [
                            {
                                "id": 7,
                                "message": "Message",
                                "timestamp": 1700000000,
                                "type": 2
                            }
                        ]
                        """)
                });
            };

            var result = await _target.GetLog();

            result.Should().ContainSingle();
            result[0].Id.Should().Be(7);
            result[0].Message.Should().Be("Message");
            result[0].Timestamp.Should().Be(1700000000);
            result[0].Type.Should().Be(LogType.Info);
        }

        [Fact]
        public async Task GIVEN_AllFilters_WHEN_GetLog_THEN_ShouldIncludeAllQueryParams()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/log/main");
                req.RequestUri!.Query.Should().Be("?normal=true&info=false&warning=true&critical=false&last_known_id=123");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var result = await _target.GetLog(normal: true, info: false, warning: true, critical: false, lastKnownId: 123);

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_BadJson_WHEN_GetLog_THEN_ShouldReturnEmptyList()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json")
            });

            var result = await _target.GetLog();

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetLog_THEN_ShouldThrowWithStatusAndMessage()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("boom")
            });

            var act = async () => await _target.GetLog();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            ex.Which.Message.Should().Be("boom");
        }

        [Fact]
        public async Task GIVEN_NoLastKnownId_WHEN_GetPeerLog_THEN_ShouldGETWithoutQueryAndReturnList()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/log/peers");
                req.RequestUri!.Query.Should().BeEmpty();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var result = await _target.GetPeerLog();

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_PeerLogJson_WHEN_GetPeerLog_THEN_ShouldDeserializeEntries()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/log/peers");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        [
                            {
                                "id": 11,
                                "ip": "127.0.0.1",
                                "timestamp": 1700000100,
                                "blocked": true,
                                "reason": "Reason"
                            }
                        ]
                        """)
                });
            };

            var result = await _target.GetPeerLog();

            result.Should().ContainSingle();
            result[0].Id.Should().Be(11);
            result[0].IPAddress.Should().Be("127.0.0.1");
            result[0].Timestamp.Should().Be(1700000100);
            result[0].Blocked.Should().BeTrue();
            result[0].Reason.Should().Be("Reason");
        }

        [Fact]
        public async Task GIVEN_LastKnownId_WHEN_GetPeerLog_THEN_ShouldIncludeQueryParam()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/log/peers");
                req.RequestUri!.Query.Should().Be("?last_known_id=77");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var result = await _target.GetPeerLog(77);

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetPeerLog_THEN_ShouldThrowWithStatusAndMessage()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("fail")
            });

            var act = async () => await _target.GetPeerLog();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            ex.Which.Message.Should().Be("fail");
        }
    }
}
