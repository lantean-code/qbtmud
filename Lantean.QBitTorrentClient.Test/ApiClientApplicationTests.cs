using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using System.Net;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientApplicationTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientApplicationTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler)
            {
                BaseAddress = new Uri("http://localhost/")
            };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_OK_WHEN_GetApplicationVersion_THEN_ShouldReturnRawBody()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/app/version");
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("4.6.0")
                };
            };

            var result = await _target.GetApplicationVersion();

            result.Should().Be("4.6.0");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetApplicationVersion_THEN_ShouldThrowWithStatusAndMessage()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("bad")
            });

            var act = async () => await _target.GetApplicationVersion();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            ex.Which.Message.Should().Be("bad");
        }

        [Fact]
        public async Task GIVEN_OK_WHEN_GetAPIVersion_THEN_ShouldReturnRawBody()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("2.10")
            });

            var result = await _target.GetAPIVersion();

            result.Should().Be("2.10");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetAPIVersion_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("no")
            });

            var act = async () => await _target.GetAPIVersion();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            ex.Which.Message.Should().Be("no");
        }

        [Fact]
        public async Task GIVEN_OKAndJson_WHEN_GetBuildInfo_THEN_ShouldDeserialize()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });

            var result = await _target.GetBuildInfo();

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetBuildInfo_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("missing")
            });

            var act = async () => await _target.GetBuildInfo();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
            ex.Which.Message.Should().Be("missing");
        }

        [Fact]
        public async Task GIVEN_OK_WHEN_Shutdown_THEN_ShouldPostAndNotThrow()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/app/shutdown");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            };

            await _target.Shutdown();
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_Shutdown_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("busy")
            });

            var act = async () => await _target.Shutdown();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            ex.Which.Message.Should().Be("busy");
        }

        [Fact]
        public async Task GIVEN_OKAndJson_WHEN_GetApplicationPreferences_THEN_ShouldDeserialize()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });

            var result = await _target.GetApplicationPreferences();

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetApplicationPreferences_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad prefs")
            });

            var act = async () => await _target.GetApplicationPreferences();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ex.Which.Message.Should().Be("bad prefs");
        }

        [Fact]
        public async Task GIVEN_Preferences_WHEN_SetApplicationPreferences_THEN_ShouldPostJsonFormAndSucceed()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/app/setPreferences");
                req.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().StartWith("json=");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            var prefs = new UpdatePreferences();
            await _target.SetApplicationPreferences(prefs);
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_SetApplicationPreferences_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("conflict")
            });

            var prefs = new UpdatePreferences();
            var act = async () => await _target.SetApplicationPreferences(prefs);

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
            ex.Which.Message.Should().Be("conflict");
        }

        [Fact]
        public async Task GIVEN_OKAndJsonList_WHEN_GetApplicationCookies_THEN_ShouldReturnListOrEmptyOnBadJson()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json")
            });

            var result = await _target.GetApplicationCookies();

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_ListOfCookies_WHEN_SetApplicationCookies_THEN_ShouldPostJsonArrayInForm()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/app/setCookies");
                req.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().StartWith("cookies=");
                body.Should().Contain("%5B"); // '[' encoded
                body.Should().Contain("%5D"); // ']' encoded
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            var cookies = new List<ApplicationCookie>();
            await _target.SetApplicationCookies(cookies);
        }

        [Fact]
        public async Task GIVEN_OKEmptyBody_WHEN_RotateApiKey_THEN_ShouldReturnEmptyString()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(string.Empty)
            });

            var result = await _target.RotateApiKey();

            result.Should().Be(string.Empty);
        }

        [Fact]
        public async Task GIVEN_ObjectWithApiKey_WHEN_RotateApiKey_THEN_ShouldReturnApiKeyValue()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"apiKey\":\"abcd1234\"}")
            });

            var result = await _target.RotateApiKey();

            result.Should().Be("abcd1234");
        }

        [Fact]
        public async Task GIVEN_ObjectWithoutApiKey_WHEN_RotateApiKey_THEN_ShouldReturnEmptyString()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });

            var result = await _target.RotateApiKey();

            result.Should().Be(string.Empty);
        }

        [Fact]
        public async Task GIVEN_NonObjectJson_WHEN_RotateApiKey_THEN_ShouldReturnEmptyString()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });

            var result = await _target.RotateApiKey();

            result.Should().Be(string.Empty);
        }

        [Fact]
        public async Task GIVEN_OK_WHEN_GetDefaultSavePath_THEN_ShouldReturnRawBody()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("/data/downloads")
            });

            var result = await _target.GetDefaultSavePath();

            result.Should().Be("/data/downloads");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetDefaultSavePath_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("nope")
            });

            var act = async () => await _target.GetDefaultSavePath();

            var ex = await act.Should().ThrowAsync<HttpRequestException>();
            ex.Which.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ex.Which.Message.Should().Be("nope");
        }

        [Fact]
        public async Task GIVEN_BadJson_WHEN_GetNetworkInterfaces_THEN_ShouldReturnEmptyList()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json")
            });

            var result = await _target.GetNetworkInterfaces();

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_OKJsonArrayOfStrings_WHEN_GetNetworkInterfaceAddressList_THEN_ShouldDeserializeStrings()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/app/networkInterfaceAddressList?iface=eth0");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[\"192.168.1.10\",\"fe80::1\"]")
                });
            };

            var result = await _target.GetNetworkInterfaceAddressList("eth0");

            result.Should().NotBeNull();
            result.Count.Should().Be(2);
            result[0].Should().Be("192.168.1.10");
            result[1].Should().Be("fe80::1");
        }
    }
}
