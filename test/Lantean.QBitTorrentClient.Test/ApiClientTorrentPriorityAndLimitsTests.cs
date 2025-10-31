using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using System.Net;

namespace Lantean.QBitTorrentClient.Test
{
    public class ApiClientTorrentPriorityAndLimitsTests
    {
        private readonly ApiClient _target;
        private readonly StubHttpMessageHandler _handler;

        public ApiClientTorrentPriorityAndLimitsTests()
        {
            _handler = new StubHttpMessageHandler();
            var http = new HttpClient(_handler) { BaseAddress = new Uri("http://localhost/") };
            _target = new ApiClient(http);
        }

        [Fact]
        public async Task GIVEN_Hashes_WHEN_IncreaseTorrentPriority_THEN_ShouldPostHashes()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/increasePrio");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("hashes=h1%7Ch2");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.IncreaseTorrentPriority(false, "h1", "h2");
        }

        [Fact]
        public async Task GIVEN_AllTrue_WHEN_DecreaseTorrentPriority_THEN_ShouldPostAll()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/decreasePrio");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("hashes=all");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.DecreaseTorrentPriority(true);
        }

        [Fact]
        public async Task GIVEN_Hashes_WHEN_MaxTorrentPriority_THEN_ShouldPostHashes()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/topPrio");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("hashes=h");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.MaxTorrentPriority(false, "h");
        }

        [Fact]
        public async Task GIVEN_Hashes_WHEN_MinTorrentPriority_THEN_ShouldPostHashes()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/bottomPrio");
                (await req.Content!.ReadAsStringAsync(ct)).Should().Be("hashes=h1%7Ch2%7Ch3");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.MinTorrentPriority(false, "h1", "h2", "h3");
        }

        [Fact]
        public async Task GIVEN_FileIdsAndPriority_WHEN_SetFilePriority_THEN_ShouldPostIdsAndPriorityInt()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/filePrio");
                var body = Uri.UnescapeDataString(await req.Content!.ReadAsStringAsync(ct));
                body.Should().Be("hash=h1&id=1|2|3&priority=7");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetFilePriority("h1", new[] { 1, 2, 3 }, (Priority)7);
        }

        [Fact]
        public async Task GIVEN_Hashes_WHEN_GetTorrentDownloadLimit_THEN_ShouldReturnDictionary()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"h1\":1000,\"h2\":0}")
            });

            var result = await _target.GetTorrentDownloadLimit(false, "h1", "h2");

            result.Should().NotBeNull();
            result.Count.Should().Be(2);
            result["h1"].Should().Be(1000);
            result["h2"].Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_BadJson_WHEN_GetTorrentDownloadLimit_THEN_ShouldReturnEmptyDict()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("oops")
            });

            var result = await _target.GetTorrentDownloadLimit();

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_LimitAndHashes_WHEN_SetTorrentDownloadLimit_THEN_ShouldPostLimitAndHashes()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setDownloadLimit");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=h%7Ci&limit=500");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetTorrentDownloadLimit(500, false, "h", "i");
        }

        [Fact]
        public async Task GIVEN_Ratios_WHEN_SetTorrentShareLimit_THEN_ShouldPostAllValuesAndOptionalAction()
        {
            var ratio = 1.5f.ToString();
            var seed = 2.25f.ToString();
            var inactive = 0.75f.ToString();

            _handler.Responder = async (req, ct) =>
            {
                var form = await req.Content!.ReadAsStringAsync(ct);
                var parts = form.Split('&').ToDictionary(
                    s => s.Split('=')[0],
                    s => Uri.UnescapeDataString(s.Split('=')[1])
                );

                parts["hashes"].Should().Be("h1|h2");
                parts["ratioLimit"].Should().Be(ratio);
                parts["seedingTimeLimit"].Should().Be(seed);
                parts["inactiveSeedingTimeLimit"].Should().Be(inactive);

                // enum is serialized as its NAME, not numeric value
                parts["shareLimitAction"].Should().Be(ShareLimitAction.Remove.ToString());

                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetTorrentShareLimit(
                ratioLimit: 1.5f,
                seedingTimeLimit: 2.25f,
                inactiveSeedingTimeLimit: 0.75f,
                shareLimitAction: ShareLimitAction.Remove, // <-- changed from Delete
                all: false,
                hashes: new[] { "h1", "h2" }
            );
        }

        [Fact]
        public async Task GIVEN_NoAction_WHEN_SetTorrentShareLimit_THEN_ShouldOmitActionField()
        {
            _handler.Responder = async (req, ct) =>
            {
                var form = await req.Content!.ReadAsStringAsync(ct);
                form.Should().Contain("ratioLimit=");
                form.Should().Contain("seedingTimeLimit=");
                form.Should().Contain("inactiveSeedingTimeLimit=");
                form.Should().NotContain("shareLimitAction=");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetTorrentShareLimit(1, 2, 3, shareLimitAction: null, all: true);
        }

        [Fact]
        public async Task GIVEN_Hashes_WHEN_GetTorrentUploadLimit_THEN_ShouldReturnDictionary()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"x\":10}")
            });

            var result = await _target.GetTorrentUploadLimit(false, "x");

            result.Should().NotBeNull();
            result.Count.Should().Be(1);
            result["x"].Should().Be(10);
        }

        [Fact]
        public async Task GIVEN_BadJson_WHEN_GetTorrentUploadLimit_THEN_ShouldReturnEmptyDict()
        {
            _handler.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("bad")
            });

            var result = await _target.GetTorrentUploadLimit();

            result.Should().NotBeNull();
            result.Count.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_LimitAndHashes_WHEN_SetTorrentUploadLimit_THEN_ShouldPostLimitAndHashes()
        {
            _handler.Responder = async (req, ct) =>
            {
                req.RequestUri!.ToString().Should().Be("http://localhost/torrents/setUploadLimit");
                var body = await req.Content!.ReadAsStringAsync(ct);
                body.Should().Be("hashes=h1&limit=42");
                return new HttpResponseMessage(HttpStatusCode.OK);
            };

            await _target.SetTorrentUploadLimit(42, false, "h1");
        }
    }
}