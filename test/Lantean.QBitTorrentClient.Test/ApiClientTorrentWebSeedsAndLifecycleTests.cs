using AwesomeAssertions;
using System.Net;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientTorrentWebSeedsAndLifecycleTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientTorrentWebSeedsAndLifecycleTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_Urls_WHEN_AddTorrentWebSeeds_THEN_ShouldPOSTFormWithPipeSeparatedUrls()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/addWebSeeds");
                req.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");

                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hash=h123&urls=a%7Cb%7Cc");

                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.AddTorrentWebSeeds("h123", new[] { "a", "b", "c" });
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_AddTorrentWebSeeds_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.AddTorrentWebSeeds("h", new[] { "u" });

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad");
        }

        [Fact]
        public async Task GIVEN_Urls_WHEN_RemoveTorrentWebSeeds_THEN_ShouldPOSTFormWithPipeSeparatedUrls()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/removeWebSeeds");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hash=h1&urls=http%3A%2F%2Fe1%7Chttp%3A%2F%2Fe2");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.RemoveTorrentWebSeeds("h1", new[] { "http://e1", "http://e2" });
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_RemoveTorrentWebSeeds_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("conflict")
            });

            var act = async () => await _target.RemoveTorrentWebSeeds("h", new[] { "u" });

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
            ex.Which.Message.Should().Be("conflict");
        }

        [Fact]
        public async Task GIVEN_EditParams_WHEN_EditTorrentWebSeed_THEN_ShouldPOSTFormWithAllFields()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/editWebSeed");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hash=hx&origUrl=old%2Furl&newUrl=new%2Furl");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.EditTorrentWebSeed("hx", "old/url", "new/url");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_EditTorrentWebSeed_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing")
            });

            var act = async () => await _target.EditTorrentWebSeed("h", "o", "n");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
            ex.Which.Message.Should().Be("missing");
        }

        [Fact]
        public async Task GIVEN_NoArgs_WHEN_StopTorrents_THEN_ShouldPOSTWithEmptyHashesValue()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/stop");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.StopTorrents();
        }

        [Fact]
        public async Task GIVEN_AllTrue_WHEN_StopTorrents_THEN_ShouldSendAllLiteral()
        {
            _handler.Responder = async (req, ct) =>
            {
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=all");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.StopTorrents(all: true);
        }

        [Fact]
        public async Task GIVEN_Hashes_WHEN_StartTorrents_THEN_ShouldPipeSeparate()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/start");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=a%7Cb%7Cc");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.StartTorrents(false, "a", "b", "c");
        }

        [Fact]
        public async Task GIVEN_DeleteFilesTrue_WHEN_DeleteTorrents_THEN_ShouldIncludeDeleteFlag()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/delete");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=a%7Cb&deleteFiles=true");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.DeleteTorrents(false, true, "a", "b");
        }

        [Fact]
        public async Task GIVEN_AllTrueAndDefaultDeleteFlag_WHEN_DeleteTorrents_THEN_ShouldSendFalseAndAll()
        {
            _handler.Responder = async (req, ct) =>
            {
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=all&deleteFiles=false");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.DeleteTorrents(true);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_DeleteTorrents_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.DeleteTorrents();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad");
        }

        [Fact]
        public async Task GIVEN_Hashes_WHEN_RecheckTorrents_THEN_ShouldPOST()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/recheck");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=h1%7Ch2");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.RecheckTorrents(false, "h1", "h2");
        }

        [Fact]
        public async Task GIVEN_AllTrueAndNoTrackers_WHEN_ReannounceTorrents_THEN_ShouldOnlySendHashes()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/reannounce");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=all");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.ReannounceTorrents(true, trackers: null);
        }

        [Fact]
        public async Task GIVEN_Trackers_WHEN_ReannounceTorrents_THEN_ShouldIncludeUrlsJoinedByPipe()
        {
            _handler.Responder = async (req, ct) =>
            {
                var body = await req.Content!.ReadAsStringAsync(ct);
                // Decode to assert actual values
                var parts = body.Split('&').ToDictionary(
                    s => s.Split('=')[0],
                    s => Uri.UnescapeDataString(s.Split('=')[1])
                );

                parts["hashes"].Should().Be("h1|h2");
                parts["urls"].Should().Be("http://t1|http://t2");

                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.ReannounceTorrents(false, new[] { "http://t1", "http://t2" }, "h1", "h2");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_ReannounceTorrents_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("nope")
            });

            var act = async () => await _target.ReannounceTorrents();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ex.Which.Message.Should().Be("nope");
        }
    }
}