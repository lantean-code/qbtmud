using System.Text.Json;
using AwesomeAssertions;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Application.Test.Services
{
    public sealed class Version53DataManagerTests
    {
        [Fact]
        public void GIVEN_Version53ServerData_WHEN_CreatedAndMerged_THEN_PreservesOmittedValuesAndAppliesFalseAndZero()
        {
            var target = new TorrentDataManager();
            var data = JsonSerializer.Deserialize<MainData>("""{"server_state":{"session_state":true,"queued_tracker_announces":4,"request_latency":20},"torrents":{"Hash":{"name":"Name","share_limits_mode":1}}}""")!;
            var result = target.CreateMainData(data);
            result.ServerState.SessionState.Should().BeTrue();
            result.ServerState.QueuedTrackerAnnounces.Should().Be(4);
            result.ServerState.RequestLatency.Should().Be(20);
            result.Torrents["Hash"].ShareLimitsMode.Should().Be(ShareLimitsMode.MatchAll);
            var unchanged = JsonSerializer.Deserialize<MainData>("""{"server_state":{"session_state":true,"queued_tracker_announces":4,"request_latency":20},"torrents":{"Hash":{"share_limits_mode":1}}}""")!;
            target.MergeMainData(unchanged, result, out _).Should().BeFalse();
            var omitted = JsonSerializer.Deserialize<MainData>("""{"server_state":{},"torrents":{"Hash":{}}}""")!;
            target.MergeMainData(omitted, result, out _).Should().BeFalse();
            result.ServerState.SessionState.Should().BeTrue();
            result.Torrents["Hash"].ShareLimitsMode.Should().Be(ShareLimitsMode.MatchAll);
            var changed = JsonSerializer.Deserialize<MainData>("""{"server_state":{"session_state":false,"queued_tracker_announces":0,"request_latency":0},"torrents":{"Hash":{"share_limits_mode":0}}}""")!;
            target.MergeMainData(changed, result, out _).Should().BeTrue();
            result.ServerState.SessionState.Should().BeFalse();
            result.ServerState.QueuedTrackerAnnounces.Should().Be(0);
            result.ServerState.RequestLatency.Should().Be(0);
            result.Torrents["Hash"].ShareLimitsMode.Should().Be(ShareLimitsMode.MatchAny);
        }

        [Fact]
        public void GIVEN_PeerContribution_WHEN_CreatedAndMerged_THEN_PreservesMissingValuesAndAppliesZero()
        {
            var target = new PeerDataManager();
            var data = JsonSerializer.Deserialize<TorrentPeers>("""{"peers":{"Key":{"contribution":0.5}}}""")!;
            var result = target.CreatePeerList(data);
            result.Peers["Key"].Contribution.Should().Be(0.5);
            target.MergeTorrentPeers(JsonSerializer.Deserialize<TorrentPeers>("""{"peers":{"Key":{}}}""")!, result);
            result.Peers["Key"].Contribution.Should().Be(0.5);
            target.MergeTorrentPeers(JsonSerializer.Deserialize<TorrentPeers>("""{"peers":{"Key":{"contribution":0}}}""")!, result);
            result.Peers["Key"].Contribution.Should().Be(0);
        }
    }
}
