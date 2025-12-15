using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using System.Net;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientTorrentCategoriesAndTagsTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientTorrentCategoriesAndTagsTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler) { BaseAddress = new Uri("http://localhost/") };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_OKJson_WHEN_GetAllCategories_THEN_ShouldDeserializeOrEmptyOnBadJson()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });

            var result = await _target.GetAllCategories();
            result.Should().NotBeNull();
            result.Count.Should().Be(0);

            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("bad")
            });

            var empty = await _target.GetAllCategories();
            empty.Should().NotBeNull();
            empty.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_CategoryAndPath_WHEN_AddCategory_THEN_ShouldPOSTForm()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/createCategory");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("category=Movies&savePath=%2Fdata");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.AddCategory("Movies", "/data");
        }

        [Fact]
        public async Task GIVEN_DownloadPathOption_WHEN_AddCategory_THEN_ShouldIncludeDownloadPathFields()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/createCategory");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("category=Shows&savePath=%2Ftv&downloadPathEnabled=true&downloadPath=%2Ftemp");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.AddCategory("Shows", "/tv", new DownloadPathOption(true, "/temp"));
        }

        [Fact]
        public async Task GIVEN_CategoryAndPath_WHEN_EditCategory_THEN_ShouldPOSTForm()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/editCategory");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("category=Shows&savePath=%2Ftv");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.EditCategory("Shows", "/tv");
        }

        [Fact]
        public async Task GIVEN_DownloadPathOption_WHEN_EditCategory_THEN_ShouldIncludeDownloadPathFields()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/editCategory");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("category=Music&savePath=%2Fmusic&downloadPathEnabled=false");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.EditCategory("Music", "/music", new DownloadPathOption(false, null));
        }

        [Fact]
        public async Task GIVEN_Categories_WHEN_RemoveCategories_THEN_ShouldPOSTNewlineSeparated()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/removeCategories");
                var decoded = Uri.UnescapeDataString(await req.Content!.ReadAsStringAsync(ct));
                decoded.Should().Be("categories=a\nb\nc");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.RemoveCategories("a", "b", "c");
        }

        [Fact]
        public async Task GIVEN_TagsAndHashes_WHEN_AddTorrentTags_THEN_ShouldCSVAndEncode()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/addTags");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=h1%7Ch2&tags=one%2Ctwo%2Cthree");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.AddTorrentTags(new[] { "one", "two", "three" }, false, "h1", "h2");
        }

        [Fact]
        public async Task GIVEN_Tags_WHEN_SetTorrentTags_THEN_ShouldPOSTToSetTags()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setTags");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=all&tags=a%2Cb");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetTorrentTags(new[] { "a", "b" }, true);
        }

        [Fact]
        public async Task GIVEN_TagsAndAllTrue_WHEN_RemoveTorrentTags_THEN_ShouldCSVAndAll()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/removeTags");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=all&tags=a%2Cb");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.RemoveTorrentTags(new[] { "a", "b" }, true);
        }

        [Fact]
        public async Task GIVEN_OKJson_WHEN_GetAllTags_THEN_ShouldDeserializeList()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[\"x\",\"y\"]")
            });

            var result = await _target.GetAllTags();

            result.Should().NotBeNull();
            result.Count.Should().Be(2);
            result[0].Should().Be("x");
            result[1].Should().Be("y");
        }

        [Fact]
        public async Task GIVEN_Tags_WHEN_CreateTags_THEN_ShouldCSV()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/createTags");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("tags=a%2Cb%2Cc");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.CreateTags(new[] { "a", "b", "c" });
        }

        [Fact]
        public async Task GIVEN_Tags_WHEN_DeleteTags_THEN_ShouldCSV()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/deleteTags");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("tags=a%2Cb");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.DeleteTags("a", "b");
        }
    }
}
