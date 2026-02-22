using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using System.Text.Json;

namespace Lantean.QBitTorrentClient.Test.Models
{
    public sealed class TorrentTrackerTests
    {
        private readonly TorrentTracker _target;

        public TorrentTrackerTests()
        {
            _target = new TorrentTracker(
                url: "Url",
                status: TrackerStatus.Working,
                tier: 2,
                peers: 3,
                seeds: 4,
                leeches: 5,
                downloads: 6,
                message: "Message",
                nextAnnounce: 7,
                minAnnounce: 8,
                endpoints: null);
        }

        [Fact]
        public void GIVEN_NullEndpoints_WHEN_Constructed_THEN_ShouldUseEmptyEndpointList()
        {
            _target.Url.Should().Be("Url");
            _target.Status.Should().Be(TrackerStatus.Working);
            _target.Tier.Should().Be(2);
            _target.Peers.Should().Be(3);
            _target.Seeds.Should().Be(4);
            _target.Leeches.Should().Be(5);
            _target.Downloads.Should().Be(6);
            _target.Message.Should().Be("Message");
            _target.NextAnnounce.Should().Be(7);
            _target.MinAnnounce.Should().Be(8);
            _target.Endpoints.Should().NotBeNull();
            _target.Endpoints.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_Endpoints_WHEN_Constructed_THEN_ShouldKeepProvidedEndpointList()
        {
            var endpoints = new[]
            {
                new TrackerEndpoint(
                    Name: "Name",
                    Updating: true,
                    Status: TrackerStatus.Updating,
                    Message: "Message",
                    BitTorrentVersion: 1,
                    Peers: 2,
                    Seeds: 3,
                    Leeches: 4,
                    Downloads: 5,
                    NextAnnounce: 6,
                    MinAnnounce: 7),
            };

            var target = new TorrentTracker(
                url: "Url",
                status: TrackerStatus.Updating,
                tier: 1,
                peers: 2,
                seeds: 3,
                leeches: 4,
                downloads: 5,
                message: "Message",
                nextAnnounce: 6,
                minAnnounce: 7,
                endpoints: endpoints);

            target.Endpoints.Should().ContainSingle();
            target.Endpoints[0].Name.Should().Be("Name");
            target.Endpoints[0].Status.Should().Be(TrackerStatus.Updating);
        }

        [Fact]
        public void GIVEN_JsonPayload_WHEN_Deserialized_THEN_ShouldMapTrackerAndEndpoints()
        {
            const string Json = """
                {
                    "url": "Url",
                    "status": 2,
                    "tier": 1,
                    "num_peers": 3,
                    "num_seeds": 4,
                    "num_leeches": 5,
                    "num_downloaded": 6,
                    "msg": "Message",
                    "next_announce": 7,
                    "min_announce": 8,
                    "endpoints": [
                        {
                            "name": "Name",
                            "updating": true,
                            "status": 3,
                            "msg": "Message",
                            "bt_version": 1,
                            "num_peers": 2,
                            "num_seeds": 3,
                            "num_leeches": 4,
                            "num_downloaded": 5,
                            "next_announce": 6,
                            "min_announce": 7
                        }
                    ]
                }
                """;

            var result = JsonSerializer.Deserialize<TorrentTracker>(Json);

            result.Should().NotBeNull();
            result!.Url.Should().Be("Url");
            result.Status.Should().Be(TrackerStatus.Working);
            result.Tier.Should().Be(1);
            result.Peers.Should().Be(3);
            result.Seeds.Should().Be(4);
            result.Leeches.Should().Be(5);
            result.Downloads.Should().Be(6);
            result.Message.Should().Be("Message");
            result.NextAnnounce.Should().Be(7);
            result.MinAnnounce.Should().Be(8);
            result.Endpoints.Should().ContainSingle();
            result.Endpoints[0].Name.Should().Be("Name");
            result.Endpoints[0].Updating.Should().BeTrue();
            result.Endpoints[0].Status.Should().Be(TrackerStatus.Updating);
            result.Endpoints[0].Message.Should().Be("Message");
            result.Endpoints[0].BitTorrentVersion.Should().Be(1);
            result.Endpoints[0].Peers.Should().Be(2);
            result.Endpoints[0].Seeds.Should().Be(3);
            result.Endpoints[0].Leeches.Should().Be(4);
            result.Endpoints[0].Downloads.Should().Be(5);
            result.Endpoints[0].NextAnnounce.Should().Be(6);
            result.Endpoints[0].MinAnnounce.Should().Be(7);
        }
    }
}
