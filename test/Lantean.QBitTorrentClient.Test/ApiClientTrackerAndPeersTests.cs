using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using System.Net;

namespace Lantean.QBitTorrentClient.Test
{
    public sealed class ApiClientTrackerAndPeersTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientTrackerAndPeersTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler) { BaseAddress = new Uri("http://localhost/") };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_NoHashesAndAllFalse_WHEN_AddTrackersToTorrent_THEN_ShouldThrowArgumentException()
        {
            var action = async () => await _target.AddTrackersToTorrent(new[] { "udp://tracker.example.com:80/announce" }, false);

            var exception = await action.Should().ThrowAsync<ArgumentException>();
            exception.Which.ParamName.Should().Be("hashes");
        }

        [Fact]
        public async Task GIVEN_AllTrue_WHEN_AddTrackersToTorrent_THEN_ShouldPostAllAndUrlList()
        {
            _handler.Responder = async (request, cancellationToken) =>
            {
                request.RequestUri!.ToString().Should().Be("http://localhost/torrents/addTrackers");
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                body.Should().Be("hash=all&urls=udp%3A%2F%2Fa%0Audp%3A%2F%2Fb");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.AddTrackersToTorrent(new[] { "udp://a", "udp://b" }, true);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_AddTrackersToTorrent_THEN_ShouldThrowHttpRequestException()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("failed")
            });

            var action = async () => await _target.AddTrackersToTorrent(new[] { "udp://a" }, false, "hash1");

            var exception = await action.Should().ThrowAsync<HttpRequestException>();
            exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            exception.Which.Message.Should().Be("failed");
        }

        [Fact]
        public async Task GIVEN_NoUpdateValues_WHEN_EditTracker_THEN_ShouldThrowArgumentException()
        {
            var action = async () => await _target.EditTracker("hash", "udp://old", null, null);

            await action.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GIVEN_NewUrlAndTier_WHEN_EditTracker_THEN_ShouldPostAllFields()
        {
            _handler.Responder = async (request, cancellationToken) =>
            {
                request.RequestUri!.ToString().Should().Be("http://localhost/torrents/editTracker");
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                body.Should().Be("hash=hash&url=udp%3A%2F%2Fold&newUrl=udp%3A%2F%2Fnew&tier=2");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.EditTracker("hash", "udp://old", "udp://new", 2);
        }

        [Fact]
        public async Task GIVEN_EmptyNewUrlWithoutTier_WHEN_EditTracker_THEN_ShouldPostOnlyRequiredFields()
        {
            _handler.Responder = async (request, cancellationToken) =>
            {
                request.RequestUri!.ToString().Should().Be("http://localhost/torrents/editTracker");
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                body.Should().Be("hash=hash&url=udp%3A%2F%2Fold");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.EditTracker("hash", "udp://old", string.Empty, null);
        }

        [Fact]
        public async Task GIVEN_NoHashesAndAllFalse_WHEN_RemoveTrackers_THEN_ShouldThrowArgumentException()
        {
            var action = async () => await _target.RemoveTrackers(new[] { "udp://tracker.example.com" }, false);

            var exception = await action.Should().ThrowAsync<ArgumentException>();
            exception.Which.ParamName.Should().Be("hashes");
        }

        [Fact]
        public async Task GIVEN_TrackersAndAllTrue_WHEN_RemoveTrackers_THEN_ShouldPostPipeSeparatedUrls()
        {
            _handler.Responder = async (request, cancellationToken) =>
            {
                request.RequestUri!.ToString().Should().Be("http://localhost/torrents/removeTrackers");
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                body.Should().Be("hash=all&urls=udp%3A%2F%2Fa%7Cudp%3A%2F%2Fb");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.RemoveTrackers(new[] { "udp://a", "udp://b" }, true);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_RemoveTrackers_THEN_ShouldThrowHttpRequestException()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("remove failed")
            });

            var action = async () => await _target.RemoveTrackers(new[] { "udp://a" }, false, "hash1");

            var exception = await action.Should().ThrowAsync<HttpRequestException>();
            exception.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
            exception.Which.Message.Should().Be("remove failed");
        }

        [Fact]
        public async Task GIVEN_HashesAndPeers_WHEN_AddPeers_THEN_ShouldPostPipeSeparatedValues()
        {
            _handler.Responder = async (request, cancellationToken) =>
            {
                request.RequestUri!.ToString().Should().Be("http://localhost/torrents/addPeers");
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                body.Should().Be("hashes=h1%7Ch2&urls=127.0.0.1%3A6881%7C127.0.0.2%3A6882");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.AddPeers(new[] { "h1", "h2" }, new[] { new PeerId("127.0.0.1", 6881), new PeerId("127.0.0.2", 6882) });
        }
    }
}
