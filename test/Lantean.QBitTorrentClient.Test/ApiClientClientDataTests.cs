using System.Net;
using System.Text.Json;
using AwesomeAssertions;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientClientDataTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientClientDataTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_NoKeys_WHEN_LoadClientData_THEN_ShouldGETAndReturnDictionary()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/clientdata/load");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"k1\":1,\"k2\":2}")
                };
            };

            var result = await _target.LoadClientData();

            result.Should().NotBeNull();
            result.Count.Should().Be(2);
            result["k1"].GetInt32().Should().Be(1);
            result["k2"].GetInt32().Should().Be(2);
        }

        [Fact]
        public async Task GIVEN_Keys_WHEN_LoadClientData_THEN_ShouldEncodeKeysAsJsonQuery()
        {
            var keys = new[] { "alpha", "beta gamma" };
            var expectedJson = JsonSerializer.Serialize(keys);

            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.AbsolutePath.Should().Be("/clientdata/load");
                var query = req.RequestUri!.Query.TrimStart('?');
                query.Should().StartWith("keys=");

                var encoded = query.Substring("keys=".Length);
                var decoded = Uri.UnescapeDataString(encoded);
                decoded.Should().Be(expectedJson);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                };
            };

            var result = await _target.LoadClientData(keys);

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_LoadClientData_THEN_ShouldThrowWithStatusAndMessage()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("err")
            });

            var act = async () => await _target.LoadClientData();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            ex.Which.Message.Should().Be("err");
        }

        [Fact]
        public async Task GIVEN_InvalidJson_WHEN_LoadClientData_THEN_ShouldReturnEmptyDictionary()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json")
            });

            var result = await _target.LoadClientData();

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_Data_WHEN_StoreClientData_THEN_ShouldPostFormWithJson()
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("{\"a\":1,\"b\":\"x\"}")!;
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/clientdata/store");
                req.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");

                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().StartWith("data=");

                var encoded = body.Substring("data=".Length);
                var json = Uri.UnescapeDataString(encoded);
                var roundTrip = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;

                roundTrip["a"].GetInt32().Should().Be(1);
                roundTrip["b"].GetString().Should().Be("x");

                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.StoreClientData(data);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_StoreClientData_THEN_ShouldThrow()
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("{\"a\":1}")!;
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.StoreClientData(data);

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad");
        }
    }
}
