using AwesomeAssertions;
using System.Net;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientSearchTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientSearchTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler) { BaseAddress = new Uri("http://localhost/") };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_PatternAndPlugins_WHEN_StartSearch_THEN_ShouldPOSTFormAndReturnId()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/search/start");
                req.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");

                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("pattern=My+pattern&plugins=a%7Cb%7Cc&category=all");

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":123}")
                };
            };

            var id = await _target.StartSearch("My pattern", new[] { "a", "b", "c" });

            id.Should().Be(123);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_StartSearch_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.StartSearch("p", new[] { "x" });

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad");
        }

        [Fact]
        public async Task GIVEN_Id_WHEN_StopSearch_THEN_ShouldPOSTFormWithId()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/search/stop");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("id=77");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.StopSearch(77);
        }

        [Fact]
        public async Task GIVEN_Id_WHEN_GetSearchStatus_THEN_ShouldGETWithIdAndReturnNullOnEmpty()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/search/status?id=5");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var status = await _target.GetSearchStatus(5);

            status.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_NotFound_WHEN_GetSearchStatus_THEN_ShouldReturnNull()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            var status = await _target.GetSearchStatus(1);

            status.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetSearchStatus_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("nope")
            });

            var act = async () => await _target.GetSearchStatus(2);

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ex.Which.Message.Should().Be("nope");
        }

        [Fact]
        public async Task GIVEN_Request_WHEN_GetSearchesStatus_THEN_ShouldGETAndReturnListOrEmptyOnBadJson()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });

            var list = await _target.GetSearchesStatus();
            list.Should().NotBeNull();
            list.Count.Should().Be(0);

            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("oops")
            });

            var empty = await _target.GetSearchesStatus();
            empty.Should().NotBeNull();
            empty.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_IdOnly_WHEN_GetSearchResults_THEN_ShouldGETWithIdOnly()
        {
            _handler.Responder = (req, _) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/search/results?id=9");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });
            };

            var results = await _target.GetSearchResults(9);

            results.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_LimitAndOffset_WHEN_GetSearchResults_THEN_ShouldGETWithAllParamsInOrder()
        {
            _handler.Responder = (req, _) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/search/results?id=9&limit=50&offset=100");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });
            };

            var results = await _target.GetSearchResults(9, limit: 50, offset: 100);

            results.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetSearchResults_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("err")
            });

            var act = async () => await _target.GetSearchResults(1);

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            ex.Which.Message.Should().Be("err");
        }

        [Fact]
        public async Task GIVEN_Id_WHEN_DeleteSearch_THEN_ShouldPOSTFormWithId()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/search/delete");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("id=3");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.DeleteSearch(3);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_DeleteSearch_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.DeleteSearch(3);

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad");
        }

        [Fact]
        public async Task GIVEN_Request_WHEN_GetSearchPlugins_THEN_ShouldGETAndReturnListOrEmptyOnBadJson()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });

            var list = await _target.GetSearchPlugins();
            list.Should().NotBeNull();
            list.Count.Should().Be(0);

            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("bad")
            });

            var empty = await _target.GetSearchPlugins();
            empty.Should().NotBeNull();
            empty.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_Sources_WHEN_InstallSearchPlugins_THEN_ShouldPOSTPipeSeparatedSources()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/search/installPlugin");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("sources=s1%7Cs2");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.InstallSearchPlugins("s1", "s2");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_InstallSearchPlugins_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("conflict")
            });

            var act = async () => await _target.InstallSearchPlugins("s");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
            ex.Which.Message.Should().Be("conflict");
        }

        [Fact]
        public async Task GIVEN_Names_WHEN_UninstallSearchPlugins_THEN_ShouldPOSTPipeSeparatedNames()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/search/uninstallPlugin");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("names=p1%7Cp2");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.UninstallSearchPlugins("p1", "p2");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_UninstallSearchPlugins_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("nope")
            });

            var act = async () => await _target.UninstallSearchPlugins("p");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ex.Which.Message.Should().Be("nope");
        }

        [Fact]
        public async Task GIVEN_Names_WHEN_EnableSearchPlugins_THEN_ShouldPOSTNamesAndEnableTrue()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/search/enablePlugin");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("names=p1%7Cp2&enable=true");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.EnableSearchPlugins("p1", "p2");
        }

        [Fact]
        public async Task GIVEN_Names_WHEN_DisableSearchPlugins_THEN_ShouldPOSTNamesAndEnableFalse()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/search/enablePlugin");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("names=p1%7Cp2&enable=false");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.DisableSearchPlugins("p1", "p2");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_EnableOrDisableSearchPlugins_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("err")
            });

            var act1 = async () => await _target.EnableSearchPlugins("p");
            var ex1 = await act1.Should().ThrowAsync<HttpRequestException>();
            ex1.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            ex1.Which.Message.Should().Be("err");

            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("err2")
            });

            var act2 = async () => await _target.DisableSearchPlugins("p");
            var ex2 = await act2.Should().ThrowAsync<HttpRequestException>();
            ex2.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            ex2.Which.Message.Should().Be("err2");
        }

        [Fact]
        public async Task GIVEN_PluginAndUrl_WHEN_DownloadSearchResult_THEN_ShouldPOSTForm()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/search/downloadTorrent");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("pluginName=qb&torrentUrl=http%3A%2F%2Fexample.com");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.DownloadSearchResult("qb", "http://example.com");
        }

        [Fact]
        public async Task GIVEN_Request_WHEN_UpdateSearchPlugins_THEN_ShouldPOSTAndNotThrow()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/search/updatePlugins");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            };

            await _target.UpdateSearchPlugins();
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_UpdateSearchPlugins_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.UpdateSearchPlugins();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            ex.Which.Message.Should().Be("bad");
        }
    }
}
