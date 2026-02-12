using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using System.Net;
using System.Text;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientAddTorrentAndMetadataTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientAddTorrentAndMetadataTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler) { BaseAddress = new Uri("http://localhost/") };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_OnlyUrls_WHEN_AddTorrent_THEN_ShouldPostMultipartWithUrlsNewlineSeparated()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/add");
                req.Content.Should().BeOfType<MultipartFormDataContent>();

                var parts = (req.Content as MultipartFormDataContent)!.ToList();
                parts.Count.Should().Be(1);

                var urlsPart = parts.Single();
                urlsPart.Headers.ContentDisposition!.Name.Should().Be("urls");
                (await urlsPart.ReadAsStringAsync()).Should().Be("u1\nu2");

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                };
            };

            var p = new AddTorrentParams
            {
                Urls = new[] { "u1", "u2" }
            };

            var result = await _target.AddTorrent(p);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_FilesAndOptions_WHEN_AddTorrent_THEN_ShouldIncludeAllExpectedParts()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/add");
                req.Content.Should().BeOfType<MultipartFormDataContent>();

                var parts = (req.Content as MultipartFormDataContent)!.ToList();

                async Task<string> ReadAsync(string name)
                {
                    var part = parts.Single(p => p.Headers.ContentDisposition!.Name == name);
                    return await part.ReadAsStringAsync(ct);
                }

                parts.Any(p => p.Headers.ContentDisposition!.Name == "torrents" &&
                               p.Headers.ContentDisposition!.FileName == "a.torrent").Should().BeTrue();
                parts.Any(p => p.Headers.ContentDisposition!.Name == "torrents" &&
                               p.Headers.ContentDisposition!.FileName == "b.torrent").Should().BeTrue();

                (await ReadAsync("skip_checking")).Should().Be("true");
                (await ReadAsync("sequentialDownload")).Should().Be("false");
                (await ReadAsync("firstLastPiecePrio")).Should().Be("true");
                (await ReadAsync("addToTopOfQueue")).Should().Be("true");
                (await ReadAsync("forced")).Should().Be("false");
                (await ReadAsync("stopped")).Should().Be("true");
                (await ReadAsync("savepath")).Should().Be("/save");
                (await ReadAsync("downloadPath")).Should().Be("/dl");
                (await ReadAsync("useDownloadPath")).Should().Be("true");
                (await ReadAsync("category")).Should().Be("Movies");
                (await ReadAsync("tags")).Should().Be("one,two");
                (await ReadAsync("rename")).Should().Be("renamed");
                (await ReadAsync("upLimit")).Should().Be("123");
                (await ReadAsync("dlLimit")).Should().Be("456");
                (await ReadAsync("downloader")).Should().Be("curl");
                (await ReadAsync("filePriorities")).Should().Be("0,1");
                (await ReadAsync("ssl_certificate")).Should().Be("cert");
                (await ReadAsync("ssl_private_key")).Should().Be("key");
                (await ReadAsync("ssl_dh_params")).Should().Be("dh");

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                };
            };

            using var s1 = new MemoryStream(Encoding.UTF8.GetBytes("a"));
            using var s2 = new MemoryStream(Encoding.UTF8.GetBytes("b"));

            var p = new AddTorrentParams
            {
                Urls = null,
                Torrents = new Dictionary<string, Stream> { { "a.torrent", (Stream)s1 }, { "b.torrent", (Stream)s2 } },
                SkipChecking = true,
                SequentialDownload = false,
                FirstLastPiecePriority = true,
                AddToTopOfQueue = true,
                Forced = false,
                Stopped = true,
                SavePath = "/save",
                DownloadPath = "/dl",
                UseDownloadPath = true,
                Category = "Movies",
                Tags = new[] { "one", "two" },
                RenameTorrent = "renamed",
                UploadLimit = 123,
                DownloadLimit = 456,
                Downloader = "curl",
                FilePriorities = new[] { (Priority)0, (Priority)1 },
                SslCertificate = "cert",
                SslPrivateKey = "key",
                SslDhParams = "dh"
            };

            var result = await _target.AddTorrent(p);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_ConflictAndEmptyMessage_WHEN_AddTorrent_THEN_ShouldThrowWithDefaultConflictMessage()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(string.Empty)
            });

            var p = new AddTorrentParams { Urls = new[] { "u" } };

            var act = async () => await _target.AddTorrent(p);

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
            ex.Which.Message.Should().Be("All torrents failed to add.");
        }

        [Fact]
        public async Task GIVEN_ConflictWithMessage_WHEN_AddTorrent_THEN_ShouldThrowWithServerMessage()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("some failed")
            });

            var p = new AddTorrentParams { Urls = new[] { "u" } };

            var act = async () => await _target.AddTorrent(p);

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
            ex.Which.Message.Should().Be("some failed");
        }

        [Fact]
        public async Task GIVEN_SuccessAndEmptyBody_WHEN_AddTorrent_THEN_ShouldReturnDefaultResultObject()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty)
            });

            var p = new AddTorrentParams { Urls = new[] { "u" } };

            var result = await _target.AddTorrent(p);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_BaseAddressAndHash_WHEN_GetExportUrl_THEN_ShouldReturnFormattedUrl()
        {
            var result = await _target.GetExportUrl("abc123");

            result.Should().Be("http://localhost/torrents/export?hash=abc123");
        }
    }
}
