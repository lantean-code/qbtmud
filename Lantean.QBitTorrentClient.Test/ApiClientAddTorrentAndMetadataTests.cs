using System.Net;
using System.Text;
using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;

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

                string Read(string name) =>
                    parts.Single(p => p.Headers.ContentDisposition!.Name == name)
                         .ReadAsStringAsync().GetAwaiter().GetResult();

                parts.Any(p => p.Headers.ContentDisposition!.Name == "torrents" &&
                               p.Headers.ContentDisposition!.FileName == "a.torrent").Should().BeTrue();
                parts.Any(p => p.Headers.ContentDisposition!.Name == "torrents" &&
                               p.Headers.ContentDisposition!.FileName == "b.torrent").Should().BeTrue();

                Read("skip_checking").Should().Be("true");
                Read("sequentialDownload").Should().Be("false");
                Read("firstLastPiecePrio").Should().Be("true");
                Read("addToTopOfQueue").Should().Be("true");
                Read("forced").Should().Be("false");
                Read("stopped").Should().Be("true");
                Read("savepath").Should().Be("/save");
                Read("downloadPath").Should().Be("/dl");
                Read("useDownloadPath").Should().Be("true");
                Read("category").Should().Be("Movies");
                Read("tags").Should().Be("one,two");
                Read("rename").Should().Be("renamed");
                Read("upLimit").Should().Be("123");
                Read("dlLimit").Should().Be("456");
                Read("downloader").Should().Be("curl");
                Read("filePriorities").Should().Be("0,1");
                Read("ssl_certificate").Should().Be("cert");
                Read("ssl_private_key").Should().Be("key");
                Read("ssl_dh_params").Should().Be("dh");

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

        [Fact]
        public async Task GIVEN_SourceOnly_WHEN_FetchMetadata_THEN_ShouldPostFormAndReturnNullOnEmpty()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/fetchMetadata");
                var form = await req.Content!.ReadAsStringAsync(ct);
                Uri.UnescapeDataString(form).Should().Be("source=magnet:?xt=urn:btih:abc");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(string.Empty)
                };
            };

            var result = await _target.FetchMetadata("magnet:?xt=urn:btih:abc");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_SourceAndDownloader_WHEN_FetchMetadata_THEN_ShouldIncludeDownloaderAndDeserialize()
        {
            _handler.Responder = async (req, ct) =>
            {
                var decoded = Uri.UnescapeDataString(await req.Content!.ReadAsStringAsync(ct));
                decoded.Should().Be("source=src&downloader=aria2");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                };
            };

            var result = await _target.FetchMetadata("src", "aria2");

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_TorrentStreams_WHEN_ParseMetadata_THEN_ShouldPostMultipartAndReturnList()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/parseMetadata");

                var parts = (req.Content as MultipartFormDataContent)!.ToList();
                parts.Count.Should().Be(2);
                parts.All(p => p.Headers.ContentDisposition!.Name == "torrents").Should().BeTrue();
                parts.Any(p => p.Headers.ContentDisposition!.FileName == "a.torrent").Should().BeTrue();
                parts.Any(p => p.Headers.ContentDisposition!.FileName == "b.torrent").Should().BeTrue();

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            using var s1 = new MemoryStream(Encoding.UTF8.GetBytes("a"));
            using var s2 = new MemoryStream(Encoding.UTF8.GetBytes("b"));

            var list = await _target.ParseMetadata(new[] { ("a.torrent", (Stream)s1), ("b.torrent", (Stream)s2) });

            list.Should().NotBeNull();
            list.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_BadJson_WHEN_ParseMetadata_THEN_ShouldReturnEmptyList()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("oops")
            });

            var list = await _target.ParseMetadata(Array.Empty<(string, Stream)>());

            list.Should().NotBeNull();
            list.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_ParseMetadata_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.ParseMetadata(Array.Empty<(string, Stream)>());

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad");
        }

        [Fact]
        public async Task GIVEN_Source_WHEN_SaveMetadata_THEN_ShouldPostFormAndReturnBytes()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/saveMetadata");
                var body = await req.Content!.ReadAsStringAsync(ct);
                Uri.UnescapeDataString(body).Should().Be("source=magnet");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
                };
            };

            var bytes = await _target.SaveMetadata("magnet");

            bytes.Should().Equal(new byte[] { 1, 2, 3 });
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_SaveMetadata_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("no")
            });

            var act = async () => await _target.SaveMetadata("x");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ex.Which.Message.Should().Be("no");
        }
    }
}
