using System.Net;
using AwesomeAssertions;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientTorrentAutoAndRenameTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientTorrentAutoAndRenameTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler) { BaseAddress = new Uri("http://localhost/") };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_EnableTrueAndHashes_WHEN_SetAutomaticTorrentManagement_THEN_ShouldPostEnableAndHashes()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setAutoManagement");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=h1%7Ch2&enable=true");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetAutomaticTorrentManagement(true, false, "h1", "h2");
        }

        [Fact]
        public async Task GIVEN_AllTrue_WHEN_ToggleSequentialDownload_THEN_ShouldOnlyPostHashesAll()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/toggleSequentialDownload");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("hashes=all");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.ToggleSequentialDownload(true);
        }

        [Fact]
        public async Task GIVEN_Hashes_WHEN_SetFirstLastPiecePriority_THEN_ShouldOnlyPostHashes()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/toggleFirstLastPiecePrio");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("hashes=h");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetFirstLastPiecePriority(false, "h");
        }

        [Fact]
        public async Task GIVEN_ValueTrue_WHEN_SetForceStart_THEN_ShouldPostValueAndHashes()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setForceStart");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=h1%7Ch2&value=true");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetForceStart(true, false, "h1", "h2");
        }

        [Fact]
        public async Task GIVEN_ValueFalse_WHEN_SetSuperSeeding_THEN_ShouldPostValueAndAll()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setSuperSeeding");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=all&value=false");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetSuperSeeding(false, true);
        }

        [Fact]
        public async Task GIVEN_RenameFile_WHEN_RenameFile_THEN_ShouldPostHashAndPaths()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/renameFile");
                var decoded = Uri.UnescapeDataString(await req.Content!.ReadAsStringAsync(ct));
                decoded.Should().Be("hash=h&oldPath=old/name&newPath=new/name");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.RenameFile("h", "old/name", "new/name");
        }

        [Fact]
        public async Task GIVEN_RenameFolder_WHEN_RenameFolder_THEN_ShouldPostHashAndPaths()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/renameFolder");
                var decoded = Uri.UnescapeDataString(await req.Content!.ReadAsStringAsync(ct));
                decoded.Should().Be("hash=h&oldPath=old/folder&newPath=new/folder");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.RenameFolder("h", "old/folder", "new/folder");
        }
    }
}
