using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using System.Net;
using System.Text.Json;

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
        public async Task GIVEN_RichBuildInfoJson_WHEN_GetBuildInfo_THEN_ShouldMapAllFields()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                        "qt": "QtVersion",
                        "libtorrent": "LibTorrentVersion",
                        "boost": "BoostVersion",
                        "openssl": "OpenSslVersion",
                        "zlib": "ZLibVersion",
                        "bitness": 64
                    }
                    """)
            });

            var result = await _target.GetBuildInfo();

            result.QTVersion.Should().Be("QtVersion");
            result.LibTorrentVersion.Should().Be("LibTorrentVersion");
            result.BoostVersion.Should().Be("BoostVersion");
            result.OpenSSLVersion.Should().Be("OpenSslVersion");
            result.ZLibVersion.Should().Be("ZLibVersion");
            result.Bitness.Should().Be(64);
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
            var serialized = JsonSerializer.Serialize(result);
            serialized.Should().Contain("\"up_limit\":0");
        }

        [Fact]
        public async Task GIVEN_PreferencesWithScanDirs_WHEN_GetApplicationPreferences_THEN_ShouldMapSaveLocations()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                        "up_limit": 10240,
                        "scan_dirs":
                        {
                            "Watch": 0,
                            "Default": 1,
                            "Custom": "/downloads/custom"
                        }
                    }
                    """)
            });

            var result = await _target.GetApplicationPreferences();

            result.UpLimit.Should().Be(10240);
            result.ScanDirs.Should().HaveCount(3);
            result.ScanDirs["Watch"].IsWatchedFolder.Should().BeTrue();
            result.ScanDirs["Watch"].IsDefaultFolder.Should().BeFalse();
            result.ScanDirs["Default"].IsWatchedFolder.Should().BeFalse();
            result.ScanDirs["Default"].IsDefaultFolder.Should().BeTrue();
            result.ScanDirs["Custom"].SavePath.Should().Be("/downloads/custom");
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
        public async Task GIVEN_CookiesJson_WHEN_GetApplicationCookies_THEN_ShouldDeserializeCookies()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    [
                        {
                            "name": "Name",
                            "domain": "Domain",
                            "path": "/Path",
                            "value": "Value",
                            "expirationDate": 1700000000
                        }
                    ]
                    """)
            });

            var result = await _target.GetApplicationCookies();

            result.Should().ContainSingle();
            result[0].Name.Should().Be("Name");
            result[0].Domain.Should().Be("Domain");
            result[0].Path.Should().Be("/Path");
            result[0].Value.Should().Be("Value");
            result[0].ExpirationDate.Should().Be(1700000000);
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
        public async Task GIVEN_Success_WHEN_SendTestEmail_THEN_ShouldPOSTToEndpoint()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Post);
                req.RequestUri!.ToString().Should().Be("http://localhost/app/sendTestEmail");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            };

            await _target.SendTestEmail();
        }

        [Fact]
        public async Task GIVEN_Parameters_WHEN_GetDirectoryContent_THEN_ShouldQueryAndReturnList()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/app/getDirectoryContent?dirPath=%2Fdata&mode=dirs");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[\"/data/folder\"]")
                });
            };

            var result = await _target.GetDirectoryContent("/data", DirectoryContentMode.Directories);

            result.Should().ContainSingle().Which.Should().Be("/data/folder");
        }

        [Fact]
        public async Task GIVEN_FilesMode_WHEN_GetDirectoryContent_THEN_ShouldUseFilesModeQuery()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/app/getDirectoryContent?dirPath=%2Fdata&mode=files");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[\"/data/file1\"]")
                });
            };

            var result = await _target.GetDirectoryContent("/data", DirectoryContentMode.Files);

            result.Should().ContainSingle().Which.Should().Be("/data/file1");
        }

        [Fact]
        public async Task GIVEN_DefaultMode_WHEN_GetDirectoryContent_THEN_ShouldUseAllModeQuery()
        {
            _handler.Responder = (req, _) =>
            {
                req.Method.Should().Be(HttpMethod.Get);
                req.RequestUri!.ToString().Should().Be("http://localhost/app/getDirectoryContent?dirPath=%2Fdata&mode=all");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("[]")
                });
            };

            var result = await _target.GetDirectoryContent("/data");

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_WhitespacePath_WHEN_GetDirectoryContent_THEN_ShouldThrowArgumentException()
        {
            var action = async () => await _target.GetDirectoryContent(" ");

            var exception = await action.Should().ThrowAsync<ArgumentException>();
            exception.Which.ParamName.Should().Be("directoryPath");
        }

        [Fact]
        public async Task GIVEN_NonSuccess_WHEN_GetDirectoryContent_THEN_ShouldThrow()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad directory")
            });

            var action = async () => await _target.GetDirectoryContent("/data", DirectoryContentMode.Directories);

            var exception = await action.Should().ThrowAsync<HttpRequestException>();
            exception.Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            exception.Which.Message.Should().Be("bad directory");
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
        public async Task GIVEN_NetworkInterfacesJson_WHEN_GetNetworkInterfaces_THEN_ShouldDeserializeInterfaces()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    [
                        {
                            "name": "eth0",
                            "value": "Ethernet 0"
                        }
                    ]
                    """)
            });

            var result = await _target.GetNetworkInterfaces();

            result.Should().ContainSingle();
            result[0].Name.Should().Be("eth0");
            result[0].Value.Should().Be("Ethernet 0");
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
