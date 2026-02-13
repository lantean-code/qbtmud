using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
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
        public async Task GIVEN_Hashes_WHEN_SetTorrentSavePath_THEN_ShouldPOSTIdsAndPath()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setSavePath");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("id=a%7Cb&path=%2Fmnt%2Fsaves");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetTorrentSavePath(new[] { "a", "b" }, "/mnt/saves");
        }

        [Fact]
        public async Task GIVEN_EmptyPath_WHEN_SetTorrentSavePath_THEN_ShouldThrowArgumentException()
        {
            var action = async () => await _target.SetTorrentSavePath(new[] { "a" }, string.Empty);

            var exception = await action.Should().ThrowAsync<ArgumentException>();
            exception.Which.ParamName.Should().Be("path");
        }

        [Fact]
        public async Task GIVEN_EmptyHashes_WHEN_SetTorrentSavePath_THEN_ShouldThrowArgumentException()
        {
            var action = async () => await _target.SetTorrentSavePath(Array.Empty<string>(), "/path");

            var exception = await action.Should().ThrowAsync<ArgumentException>();
            exception.Which.ParamName.Should().Be("hashes");
        }

        [Fact]
        public async Task GIVEN_Hashes_WHEN_SetTorrentDownloadPath_THEN_ShouldPOSTIdsAndPath()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setDownloadPath");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("id=a%7Cb&path=temp");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetTorrentDownloadPath(new[] { "a", "b" }, "temp");
        }

        [Fact]
        public async Task GIVEN_NullPath_WHEN_SetTorrentDownloadPath_THEN_ShouldPostEmptyPath()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setDownloadPath");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("id=a&path=");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetTorrentDownloadPath(new[] { "a" }, null);
        }

        [Fact]
        public async Task GIVEN_EmptyHashes_WHEN_SetTorrentDownloadPath_THEN_ShouldThrowArgumentException()
        {
            var action = async () => await _target.SetTorrentDownloadPath(Array.Empty<string>(), "temp");

            var exception = await action.Should().ThrowAsync<ArgumentException>();
            exception.Which.ParamName.Should().Be("hashes");
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

        [Fact]
        public async Task GIVEN_Hash_WHEN_GetTorrentSslParameters_THEN_ShouldDeserialize()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ssl_certificate\":\"cert\",\"ssl_private_key\":\"key\",\"ssl_dh_params\":\"dh\"}")
            });

            var result = await _target.GetTorrentSslParameters("abc");

            result.Certificate.Should().Be("cert");
            result.PrivateKey.Should().Be("key");
            result.DhParams.Should().Be("dh");
        }

        [Fact]
        public async Task GIVEN_Parameters_WHEN_SetTorrentSslParameters_THEN_ShouldPOSTAllFields()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setSSLParameters");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hash=abc&ssl_certificate=cert&ssl_private_key=key&ssl_dh_params=dh");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            var parameters = new SslParameters("cert", "key", "dh");

            await _target.SetTorrentSslParameters("abc", parameters);
        }

        [Fact]
        public async Task GIVEN_NullParameters_WHEN_SetTorrentSslParameters_THEN_ShouldThrowArgumentNullException()
        {
            var action = async () => await _target.SetTorrentSslParameters("abc", null!);

            var exception = await action.Should().ThrowAsync<ArgumentNullException>();
            exception.Which.ParamName.Should().Be("parameters");
        }

        [Fact]
        public async Task GIVEN_MissingRequiredSslFields_WHEN_SetTorrentSslParameters_THEN_ShouldThrowArgumentException()
        {
            var action = async () => await _target.SetTorrentSslParameters("abc", new SslParameters("", "key", "dh"));

            var exception = await action.Should().ThrowAsync<ArgumentException>();
            exception.Which.ParamName.Should().Be("parameters");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_SetTorrentSslParameters_THEN_ShouldThrowHttpRequestException()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("ssl failed")
            });

            var action = async () => await _target.SetTorrentSslParameters("abc", new SslParameters("cert", "key", "dh"));

            var exception = await action.Should().ThrowAsync<HttpRequestException>();
            exception.Which.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            exception.Which.Message.Should().Be("ssl failed");
        }
    }
}
