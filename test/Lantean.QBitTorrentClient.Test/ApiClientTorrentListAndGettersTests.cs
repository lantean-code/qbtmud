using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using System.Net;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientTorrentListAndGettersTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientTorrentListAndGettersTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_NoFilters_WHEN_GetTorrentList_THEN_ShouldGETWithoutQueryAndReturnList()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/torrents/info");
                req.RequestUri!.Query.Should().BeEmpty();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var result = await _target.GetTorrentList();

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_AllFilters_WHEN_GetTorrentList_THEN_ShouldIncludeAllParamsInOrder()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/torrents/info");
                req.RequestUri!.Query.Should().Be("?filter=active&category=Movies&tag=HD&sort=name&reverse=true&limit=50&offset=5&hashes=a%7Cb%7Cc&private=true&includeFiles=false&includeTrackers=true");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var result = await _target.GetTorrentList(
                filter: "active",
                category: "Movies",
                tag: "HD",
                sort: "name",
                reverse: true,
                limit: 50,
                offset: 5,
                isPrivate: true,
                includeFiles: false,
                includeTrackers: true,
                "a", "b", "c"
            );

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_BooleanFlagsFalseAndTrueMix_WHEN_GetTorrentList_THEN_ShouldSerializeExpectedBooleanValues()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/torrents/info");
                req.RequestUri!.Query.Should().Be("?private=false&includeFiles=true&includeTrackers=false");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var result = await _target.GetTorrentList(
                isPrivate: false,
                includeFiles: true,
                includeTrackers: false);

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_Response_WHEN_GetTorrentCount_THEN_ShouldParseInteger()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("42")
            });

            var result = await _target.GetTorrentCount();

            result.Should().Be(42);
        }

        [Fact]
        public async Task GIVEN_InvalidResponse_WHEN_GetTorrentCount_THEN_ShouldReturnZero()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("NaN")
            });

            var result = await _target.GetTorrentCount();

            result.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_BadJson_WHEN_GetTorrentList_THEN_ShouldReturnEmptyList()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json")
            });

            var result = await _target.GetTorrentList();

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetTorrentList_THEN_ShouldThrowWithStatusAndMessage()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("no")
            });

            var act = async () => await _target.GetTorrentList();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ex.Which.Message.Should().Be("no");
        }

        [Fact]
        public async Task GIVEN_Hash_WHEN_GetTorrentProperties_THEN_ShouldGETWithHashAndDeserialize()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/properties?hash=abc");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });
            };

            var result = await _target.GetTorrentProperties("abc");

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetTorrentProperties_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing")
            });

            var act = async () => await _target.GetTorrentProperties("abc");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
            ex.Which.Message.Should().Be("missing");
        }

        [Fact]
        public async Task GIVEN_Hash_WHEN_GetTorrentTrackers_THEN_ShouldGETAndReturnList()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/trackers?hash=xyz");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var result = await _target.GetTorrentTrackers("xyz");

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_HashAndTrackerPayload_WHEN_GetTorrentTrackers_THEN_ShouldDeserializeTrackerAndEndpoints()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/trackers?hash=xyz");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                        [
                            {
                                "url": "Url",
                                "status": 2,
                                "tier": 1,
                                "num_peers": 3,
                                "num_seeds": 4,
                                "num_leeches": 5,
                                "num_downloaded": 6,
                                "msg": "Message",
                                "next_announce": 7,
                                "min_announce": 8,
                                "endpoints": [
                                    {
                                        "name": "Name",
                                        "updating": true,
                                        "status": 3,
                                        "msg": "Message",
                                        "bt_version": 1,
                                        "num_peers": 2,
                                        "num_seeds": 3,
                                        "num_leeches": 4,
                                        "num_downloaded": 5,
                                        "next_announce": 6,
                                        "min_announce": 7
                                    }
                                ]
                            }
                        ]
                        """)
                });
            };

            var result = await _target.GetTorrentTrackers("xyz");

            result.Should().ContainSingle();
            result[0].Url.Should().Be("Url");
            result[0].Status.Should().Be(TrackerStatus.Working);
            result[0].Tier.Should().Be(1);
            result[0].Peers.Should().Be(3);
            result[0].Seeds.Should().Be(4);
            result[0].Leeches.Should().Be(5);
            result[0].Downloads.Should().Be(6);
            result[0].Message.Should().Be("Message");
            result[0].NextAnnounce.Should().Be(7);
            result[0].MinAnnounce.Should().Be(8);
            result[0].Endpoints.Should().ContainSingle();
            result[0].Endpoints[0].Name.Should().Be("Name");
            result[0].Endpoints[0].Updating.Should().BeTrue();
            result[0].Endpoints[0].Status.Should().Be(TrackerStatus.Updating);
            result[0].Endpoints[0].BitTorrentVersion.Should().Be(1);
            result[0].Endpoints[0].Peers.Should().Be(2);
            result[0].Endpoints[0].Seeds.Should().Be(3);
            result[0].Endpoints[0].Leeches.Should().Be(4);
            result[0].Endpoints[0].Downloads.Should().Be(5);
            result[0].Endpoints[0].NextAnnounce.Should().Be(6);
            result[0].Endpoints[0].MinAnnounce.Should().Be(7);
        }

        [Fact]
        public async Task GIVEN_Hash_WHEN_GetTorrentWebSeeds_THEN_ShouldGETAndReturnList()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/webseeds?hash=h1");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var result = await _target.GetTorrentWebSeeds("h1");

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_HashOnly_WHEN_GetTorrentContents_THEN_ShouldGETWithHashOnly()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/torrents/files");
                req.RequestUri!.Query.Should().Be("?hash=abc");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var result = await _target.GetTorrentContents("abc");

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_Indexes_WHEN_GetTorrentContents_THEN_ShouldGETWithIndexesPipeSeparated()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/torrents/files");
                req.RequestUri!.Query.Should().Be("?hash=abc&indexes=1%7C2%7C3");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var result = await _target.GetTorrentContents("abc", 1, 2, 3);

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_BadJson_WHEN_GetTorrentContents_THEN_ShouldReturnEmptyList()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("oops")
            });

            var result = await _target.GetTorrentContents("abc");

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetTorrentContents_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.GetTorrentContents("abc");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad");
        }

        [Fact]
        public async Task GIVEN_Hash_WHEN_GetTorrentPieceStates_THEN_ShouldGETAndReturnListOrEmptyOnBadJson()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/pieceStates?hash=abc");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("not json")
                });
            };

            var result = await _target.GetTorrentPieceStates("abc");

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetTorrentPieceStates_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing")
            });

            var act = async () => await _target.GetTorrentPieceStates("abc");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
            ex.Which.Message.Should().Be("missing");
        }

        [Fact]
        public async Task GIVEN_Hash_WHEN_GetTorrentPieceHashes_THEN_ShouldGETAndReturnStrings()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/pieceHashes?hash=abc");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[\"h1\",\"h2\"]")
                });
            };

            var result = await _target.GetTorrentPieceHashes("abc");

            result.Should().NotBeNull();
            result.Count.Should().Be(2);
            result[0].Should().Be("h1");
            result[1].Should().Be("h2");
        }

        [Fact]
        public async Task GIVEN_BadJson_WHEN_GetTorrentPieceHashes_THEN_ShouldReturnEmptyList()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("bad")
            });

            var result = await _target.GetTorrentPieceHashes("abc");

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetTorrentPieceHashes_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("err")
            });

            var act = async () => await _target.GetTorrentPieceHashes("abc");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            ex.Which.Message.Should().Be("err");
        }
    }
}
