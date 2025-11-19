using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using System.Net;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientRssTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientRssTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler) { BaseAddress = new Uri("http://localhost/") };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_Path_WHEN_AddRssFolder_THEN_ShouldPOSTForm()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/rss/addFolder");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("path=%2Ffeeds%2Ftv");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.AddRssFolder("/feeds/tv");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_AddRssFolder_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("exists")
            });

            var act = async () => await _target.AddRssFolder("/x");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
            ex.Which.Message.Should().Be("exists");
        }

        [Fact]
        public async Task GIVEN_UrlOnly_WHEN_AddRssFeed_THEN_ShouldPOSTUrlWithEmptyPath()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/rss/addFeed");
                var decoded = Uri.UnescapeDataString(await req.Content!.ReadAsStringAsync(ct));
                decoded.Should().Be("url=http://feed&path=");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.AddRssFeed("http://feed");
        }

        [Fact]
        public async Task GIVEN_UrlAndPath_WHEN_AddRssFeed_THEN_ShouldIncludeBoth()
        {
            _handler.Responder = async (req, ct) =>
            {
                var decoded = Uri.UnescapeDataString(await req.Content!.ReadAsStringAsync(ct));
                decoded.Should().Be("url=http://feed&path=/podcasts");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.AddRssFeed("http://feed", "/podcasts");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_AddRssFeed_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.AddRssFeed("u");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad");
        }

        [Fact]
        public async Task GIVEN_Path_WHEN_RemoveRssItem_THEN_ShouldPOSTForm()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/rss/removeItem");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("path=%2Ffeeds%2Ftv");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.RemoveRssItem("/feeds/tv");
        }

        [Fact]
        public async Task GIVEN_ItemAndDest_WHEN_MoveRssItem_THEN_ShouldPOSTBoth()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/rss/moveItem");
                var decoded = Uri.UnescapeDataString(await req.Content!.ReadAsStringAsync(ct));
                decoded.Should().Be("itemPath=/feeds/tv&destPath=/feeds/news");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.MoveRssItem("/feeds/tv", "/feeds/news");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_MoveRssItem_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("nope")
            });

            var act = async () => await _target.MoveRssItem("/a", "/b");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ex.Which.Message.Should().Be("nope");
        }

        [Fact]
        public async Task GIVEN_PathAndUrl_WHEN_SetRssFeedUrl_THEN_ShouldPOSTBoth()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/rss/setFeedURL");
                var decoded = Uri.UnescapeDataString(await req.Content!.ReadAsStringAsync(ct));
                decoded.Should().Be("path=/feeds/tv&url=http://example.com");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetRssFeedUrl("/feeds/tv", "http://example.com");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_SetRssFeedUrl_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.SetRssFeedUrl("/feeds/tv", "http://example.com");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad");
        }

        [Fact]
        public async Task GIVEN_NoFlag_WHEN_GetAllRssItems_THEN_ShouldGETWithoutQueryAndReturnDict()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/rss/items");
                req.RequestUri!.Query.Should().BeEmpty();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });
            };

            var dict = await _target.GetAllRssItems();

            dict.Should().NotBeNull();
            dict.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_WithDataTrue_WHEN_GetAllRssItems_THEN_ShouldQueryWithTrueCapitalized()
        {
            _handler.Responder = (req, _) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/rss/items?withData=True");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });
            };

            var dict = await _target.GetAllRssItems(true);

            dict.Should().NotBeNull();
            dict.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetAllRssItems_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("err")
            });

            var act = async () => await _target.GetAllRssItems();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            ex.Which.Message.Should().Be("err");
        }

        [Fact]
        public async Task GIVEN_ItemPathOnly_WHEN_MarkRssItemAsRead_THEN_ShouldPOSTOnlyItemPath()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/rss/markAsRead");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("itemPath=%2Ffeeds%2Ftv");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.MarkRssItemAsRead("/feeds/tv");
        }

        [Fact]
        public async Task GIVEN_ArticleId_WHEN_MarkRssItemAsRead_THEN_ShouldIncludeArticleId()
        {
            _handler.Responder = async (req, ct) =>
            {
                var decoded = Uri.UnescapeDataString(await req.Content!.ReadAsStringAsync(ct));
                decoded.Should().Be("itemPath=/feeds/tv&articleId=a1");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.MarkRssItemAsRead("/feeds/tv", "a1");
        }

        [Fact]
        public async Task GIVEN_ItemPath_WHEN_RefreshRssItem_THEN_ShouldPOSTForm()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/rss/refreshItem");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("itemPath=%2Ffeeds%2Ftv");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.RefreshRssItem("/feeds/tv");
        }

        [Fact]
        public async Task GIVEN_Rule_WHEN_SetRssAutoDownloadingRule_THEN_ShouldPOSTRuleNameAndRuleDefJson()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/rss/setRule");

                var decoded = Uri.UnescapeDataString(await req.Content!.ReadAsStringAsync(ct));
                decoded.Should().StartWith("ruleName=r1&ruleDef=");

                var json = decoded.Substring("ruleName=r1&ruleDef=".Length);
                var expectedJson = System.Text.Json.JsonSerializer.Serialize(new AutoDownloadingRule());

                json.Should().Be(expectedJson);

                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            };

            await _target.SetRssAutoDownloadingRule("r1", new AutoDownloadingRule());
        }

        [Fact]
        public async Task GIVEN_RuleNames_WHEN_RenameRssAutoDownloadingRule_THEN_ShouldPOSTBothNames()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/rss/renameRule");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("ruleName=old&newRuleName=new");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.RenameRssAutoDownloadingRule("old", "new");
        }

        [Fact]
        public async Task GIVEN_RuleName_WHEN_RemoveRssAutoDownloadingRule_THEN_ShouldPOSTName()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/rss/removeRule");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("ruleName=dead");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.RemoveRssAutoDownloadingRule("dead");
        }

        [Fact]
        public async Task GIVEN_OKOrBadJson_WHEN_GetAllRssAutoDownloadingRules_THEN_ShouldDeserializeOrReturnEmpty()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });

            var dict = await _target.GetAllRssAutoDownloadingRules();
            dict.Should().NotBeNull();
            dict.Count.Should().Be(0);

            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("bad")
            });

            var empty = await _target.GetAllRssAutoDownloadingRules();
            empty.Should().NotBeNull();
            empty.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_RuleName_WHEN_GetRssMatchingArticles_THEN_ShouldGETAndReturnDictionaryOfLists()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/rss/matchingArticles?ruleName=myrule");

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"feed1\":[\"a\",\"b\"]}")
                });
            };

            var dict = await _target.GetRssMatchingArticles("myrule");

            dict.Should().NotBeNull();
            dict.Count.Should().Be(1);
            dict["feed1"].Count.Should().Be(2);
            dict["feed1"][0].Should().Be("a");
            dict["feed1"][1].Should().Be("b");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetRssMatchingArticles_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("fail")
            });

            var act = async () => await _target.GetRssMatchingArticles("x");

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            ex.Which.Message.Should().Be("fail");
        }
    }
}
