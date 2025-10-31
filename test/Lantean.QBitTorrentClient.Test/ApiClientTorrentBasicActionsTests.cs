using AwesomeAssertions;
using System.Net;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientTorrentBasicActionsTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientTorrentBasicActionsTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_LocationAndHashes_WHEN_SetTorrentLocation_THEN_ShouldPOSTForm()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setLocation");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=h1%7Ch2&location=%2Fdata%2Fdl");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetTorrentLocation("/data/dl", false, "h1", "h2");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_SetTorrentLocation_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.SetTorrentLocation("/x");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad");
        }

        [Fact]
        public async Task GIVEN_NameAndHash_WHEN_SetTorrentName_THEN_ShouldPOSTForm()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/rename");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hash=hx&name=My+Torrent"); // spaces => '+'
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetTorrentName("My Torrent", "hx");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_SetTorrentName_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("exists")
            });

            var act = async () => await _target.SetTorrentName("n", "h");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
            ex.Which.Message.Should().Be("exists");
        }

        [Fact]
        public async Task GIVEN_HashesAndComment_WHEN_SetTorrentComment_THEN_ShouldPOSTForm()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setComment");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=h1%7Ch2&comment=hello+world");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetTorrentComment(new[] { "h1", "h2" }, "hello world");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_SetTorrentComment_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("forbidden")
            });

            var act = async () => await _target.SetTorrentComment(Array.Empty<string>(), "x");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ex.Which.Message.Should().Be("forbidden");
        }

        [Fact]
        public async Task GIVEN_CategoryAndHashes_WHEN_SetTorrentCategory_THEN_ShouldPOSTForm()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setCategory");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=h1%7Ch2&category=Movies");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetTorrentCategory("Movies", false, "h1", "h2");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_SetTorrentCategory_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.SetTorrentCategory("c");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            ex.Which.Message.Should().Be("bad");
        }
    }
}