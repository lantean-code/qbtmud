using AwesomeAssertions;
using Lantean.QBitTorrentClient.Models;
using System.Text.Json;

namespace Lantean.QBitTorrentClient.Test.Models
{
    public sealed class TrackerEndpointTests
    {
        private readonly TrackerEndpoint _target;

        public TrackerEndpointTests()
        {
            _target = new TrackerEndpoint(
                Name: "Name",
                Updating: true,
                Status: TrackerStatus.Error,
                Message: "Message",
                BitTorrentVersion: 1,
                Peers: 2,
                Seeds: 3,
                Leeches: 4,
                Downloads: 5,
                NextAnnounce: 6,
                MinAnnounce: 7);
        }

        [Fact]
        public void GIVEN_ConstructorValues_WHEN_ReadingProperties_THEN_ShouldMatchInput()
        {
            _target.Name.Should().Be("Name");
            _target.Updating.Should().BeTrue();
            _target.Status.Should().Be(TrackerStatus.Error);
            _target.Message.Should().Be("Message");
            _target.BitTorrentVersion.Should().Be(1);
            _target.Peers.Should().Be(2);
            _target.Seeds.Should().Be(3);
            _target.Leeches.Should().Be(4);
            _target.Downloads.Should().Be(5);
            _target.NextAnnounce.Should().Be(6);
            _target.MinAnnounce.Should().Be(7);
        }

        [Fact]
        public void GIVEN_JsonPayload_WHEN_Deserialized_THEN_ShouldMapJsonPropertyNames()
        {
            const string Json = """
                {
                    "name": "Name",
                    "updating": false,
                    "status": 2,
                    "msg": "Message",
                    "bt_version": 9,
                    "num_peers": 10,
                    "num_seeds": 11,
                    "num_leeches": 12,
                    "num_downloaded": 13,
                    "next_announce": 14,
                    "min_announce": 15
                }
                """;

            var result = JsonSerializer.Deserialize<TrackerEndpoint>(Json);

            result.Should().NotBeNull();
            result!.Name.Should().Be("Name");
            result.Updating.Should().BeFalse();
            result.Status.Should().Be(TrackerStatus.Working);
            result.Message.Should().Be("Message");
            result.BitTorrentVersion.Should().Be(9);
            result.Peers.Should().Be(10);
            result.Seeds.Should().Be(11);
            result.Leeches.Should().Be(12);
            result.Downloads.Should().Be(13);
            result.NextAnnounce.Should().Be(14);
            result.MinAnnounce.Should().Be(15);
        }
    }
}
