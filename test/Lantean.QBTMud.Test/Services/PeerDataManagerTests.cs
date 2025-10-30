using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using QbtPeer = Lantean.QBitTorrentClient.Models.Peer;
using QbtTorrentPeers = Lantean.QBitTorrentClient.Models.TorrentPeers;

namespace Lantean.QBTMud.Test.Services
{
    public class PeerDataManagerTests
    {
        private readonly PeerDataManager _target;

        public PeerDataManagerTests()
        {
            _target = new PeerDataManager();
        }

        // ---------- CreatePeerList ----------

        [Fact]
        public void GIVEN_NullPeers_WHEN_CreatePeerList_THEN_EmptyPeerList()
        {
            // arrange
            var input = new QbtTorrentPeers(
                fullUpdate: false,
                peers: null,
                peersRemoved: null,
                requestId: 1,
                showFlags: null);

            // act
            var result = _target.CreatePeerList(input);

            // assert
            result.Should().NotBeNull();
            result.Peers.Should().NotBeNull();
            result.Peers.Count.Should().Be(0);
        }

        [Fact]
        public void GIVEN_MultiplePeers_WHEN_CreatePeerList_THEN_MapsAllFieldsAndKeys()
        {
            // arrange
            var p1 = new QbtPeer(
                client: "qBittorrent/4.6.0",
                connection: "TCP",
                country: "UK",
                countryCode: "GB",
                downloadSpeed: 1200,
                downloaded: 11,
                files: "file1.mkv",
                flags: "D U H",
                flagsDescription: "downloading, uploading, high priority",
                iPAddress: "1.1.1.1",
                i2pDestination: null,
                clientId: "ClientA",
                port: 6881,
                progress: 0.5f,
                relevance: 0.7f,
                uploadSpeed: 3400,
                uploaded: 22);

            var p2 = new QbtPeer(
                client: "Transmission/4.0",
                connection: "uTP",
                country: "Canada",
                countryCode: "CA",
                downloadSpeed: 2200,
                downloaded: 33,
                files: "file2.mp4",
                flags: "Q",
                flagsDescription: "queued",
                iPAddress: "2.2.2.2",
                i2pDestination: null,
                clientId: "ClientB",
                port: 51413,
                progress: 0.9f,
                relevance: 0.1f,
                uploadSpeed: 100,
                uploaded: 44);

            var dict = new Dictionary<string, QbtPeer>
            {
                ["1.1.1.1:6881"] = p1,
                ["2.2.2.2:51413"] = p2
            };

            var input = new QbtTorrentPeers(
                fullUpdate: true,
                peers: dict,
                peersRemoved: null,
                requestId: 2,
                showFlags: true);

            // act
            var result = _target.CreatePeerList(input);

            // assert
            result.Should().NotBeNull();
            result.Peers.Count.Should().Be(2);

            result.Peers.Should().ContainKey("1.1.1.1:6881");
            var a = result.Peers["1.1.1.1:6881"];
            a.Key.Should().Be("1.1.1.1:6881");
            a.Client.Should().Be("qBittorrent/4.6.0");
            a.ClientId.Should().Be("ClientA");
            a.Connection.Should().Be("TCP");
            a.Country.Should().Be("UK");
            a.CountryCode.Should().Be("GB");
            a.Downloaded.Should().Be(11);
            a.DownloadSpeed.Should().Be(1200);
            a.Files.Should().Be("file1.mkv");
            a.Flags.Should().Be("D U H");
            a.FlagsDescription.Should().Be("downloading, uploading, high priority");
            a.IPAddress.Should().Be("1.1.1.1");
            a.Port.Should().Be(6881);
            a.Progress.Should().Be(0.5f);
            a.Relevance.Should().Be(0.7f);
            a.Uploaded.Should().Be(22);
            a.UploadSpeed.Should().Be(3400);

            result.Peers.Should().ContainKey("2.2.2.2:51413");
            var b = result.Peers["2.2.2.2:51413"];
            b.Key.Should().Be("2.2.2.2:51413");
            b.Client.Should().Be("Transmission/4.0");
            b.ClientId.Should().Be("ClientB");
            b.Connection.Should().Be("uTP");
            b.Country.Should().Be("Canada");
            b.CountryCode.Should().Be("CA");
            b.Downloaded.Should().Be(33);
            b.DownloadSpeed.Should().Be(2200);
            b.Files.Should().Be("file2.mp4");
            b.Flags.Should().Be("Q");
            b.FlagsDescription.Should().Be("queued");
            b.IPAddress.Should().Be("2.2.2.2");
            b.Port.Should().Be(51413);
            b.Progress.Should().Be(0.9f);
            b.Relevance.Should().Be(0.1f);
            b.Uploaded.Should().Be(44);
            b.UploadSpeed.Should().Be(100);
        }

        [Fact]
        public void GIVEN_PeerWithNullNumerics_WHEN_CreatePeerList_THEN_DefaultsToZeros()
        {
            // arrange
            var nullish = new QbtPeer(
                client: "ClientX",
                connection: "TCP",
                country: null,
                countryCode: null,
                downloadSpeed: null,   // -> 0
                downloaded: null,      // -> 0
                files: "file.dat",
                flags: "",
                flagsDescription: "",
                iPAddress: "9.9.9.9",
                i2pDestination: null,
                clientId: "CID",
                port: null,            // -> 0
                progress: null,        // -> 0
                relevance: null,       // -> 0
                uploadSpeed: null,     // -> 0
                uploaded: null);       // -> 0

            var input = new QbtTorrentPeers(
                fullUpdate: false,
                peers: new Dictionary<string, QbtPeer> { ["9.9.9.1:0"] = nullish },
                peersRemoved: null,
                requestId: 3,
                showFlags: null);

            // act
            var result = _target.CreatePeerList(input);

            // assert
            result.Peers.Should().ContainKey("9.9.9.1:0");
            var p = result.Peers["9.9.9.1:0"];
            p.Client.Should().Be("ClientX");
            p.Connection.Should().Be("TCP");
            p.DownloadSpeed.Should().Be(0);
            p.Downloaded.Should().Be(0);
            p.Port.Should().Be(0);
            p.Progress.Should().Be(0f);
            p.Relevance.Should().Be(0f);
            p.UploadSpeed.Should().Be(0);
            p.Uploaded.Should().Be(0);
        }

        // ---------- MergeTorrentPeers ----------

        [Fact]
        public void GIVEN_NoChanges_WHEN_MergeTorrentPeers_THEN_DoesNothing()
        {
            // arrange
            var peerList = new PeerList(new Dictionary<string, Peer>
            {
                ["1.1.1.1:6881"] = new Peer(
                    key: "1.1.1.1:6881",
                    client: "qB",
                    clientId: "A",
                    connection: "TCP",
                    country: "UK",
                    countryCode: "GB",
                    downloaded: 1,
                    downloadSpeed: 2,
                    files: "f",
                    flags: "D",
                    flagsDescription: "down",
                    iPAddress: "1.1.1.1",
                    port: 6881,
                    progress: 0.1f,
                    relevance: 0.2f,
                    uploaded: 3,
                    uploadSpeed: 4)
            });

            var input = new QbtTorrentPeers(
                fullUpdate: false,
                peers: null,
                peersRemoved: null,
                requestId: 10,
                showFlags: null);

            // act
            _target.MergeTorrentPeers(input, peerList);

            // assert
            peerList.Peers.Count.Should().Be(1);
            peerList.Peers.Should().ContainKey("1.1.1.1:6881");
        }

        [Fact]
        public void GIVEN_PeersRemovedWithExistingAndMissing_WHEN_MergeTorrentPeers_THEN_RemovesExistingOnly()
        {
            // arrange
            var peerList = new PeerList(new Dictionary<string, Peer>
            {
                ["a"] = new Peer("a", "c", "id", "TCP", null, null, 0, 0, "f", "F", "FD", "10.0.0.1", 1111, 0, 0, 0, 0),
                ["b"] = new Peer("b", "c2", "id2", "uTP", null, null, 0, 0, "f2", "F2", "FD2", "10.0.0.2", 2222, 0, 0, 0, 0),
            });

            var input = new QbtTorrentPeers(
                fullUpdate: false,
                peers: null,
                peersRemoved: new List<string> { "a", "missing" },
                requestId: 11,
                showFlags: null);

            // act
            _target.MergeTorrentPeers(input, peerList);

            // assert
            peerList.Peers.Count.Should().Be(1);
            peerList.Peers.Should().ContainKey("b");
            peerList.Peers.Should().NotContainKey("a");
        }

        [Fact]
        public void GIVEN_NewPeers_WHEN_MergeTorrentPeers_THEN_AddsAllWithProperMapping()
        {
            // arrange
            var peerList = new PeerList(new Dictionary<string, Peer>());

            var q1 = new QbtPeer(
                client: "Client1",
                connection: "TCP",
                country: "US",
                countryCode: "US",
                downloadSpeed: 1000,
                downloaded: 10,
                files: "a.mkv",
                flags: "D",
                flagsDescription: "down",
                iPAddress: "3.3.3.3",
                i2pDestination: null,
                clientId: "ID1",
                port: 6000,
                progress: 0.4f,
                relevance: 0.9f,
                uploadSpeed: 50,
                uploaded: 5);

            var q2 = new QbtPeer(
                client: "Client2",
                connection: "uTP",
                country: "DE",
                countryCode: "DE",
                downloadSpeed: 2000,
                downloaded: 20,
                files: "b.mp4",
                flags: "",
                flagsDescription: "",
                iPAddress: "4.4.4.4",
                i2pDestination: null,
                clientId: "ID2",
                port: 7000,
                progress: 0.8f,
                relevance: 0.1f,
                uploadSpeed: 150,
                uploaded: 15);

            var input = new QbtTorrentPeers(
                fullUpdate: true,
                peers: new Dictionary<string, QbtPeer>
                {
                    ["3.3.3.3:6000"] = q1,
                    ["4.4.4.4:7000"] = q2
                },
                peersRemoved: null,
                requestId: 12,
                showFlags: true);

            // act
            _target.MergeTorrentPeers(input, peerList);

            // assert
            peerList.Peers.Count.Should().Be(2);

            var p1 = peerList.Peers["3.3.3.3:6000"];
            p1.Client.Should().Be("Client1");
            p1.ClientId.Should().Be("ID1");
            p1.Connection.Should().Be("TCP");
            p1.Country.Should().Be("US");
            p1.CountryCode.Should().Be("US");
            p1.Downloaded.Should().Be(10);
            p1.DownloadSpeed.Should().Be(1000);
            p1.Files.Should().Be("a.mkv");
            p1.Flags.Should().Be("D");
            p1.FlagsDescription.Should().Be("down");
            p1.IPAddress.Should().Be("3.3.3.3");
            p1.Port.Should().Be(6000);
            p1.Progress.Should().Be(0.4f);
            p1.Relevance.Should().Be(0.9f);
            p1.Uploaded.Should().Be(5);
            p1.UploadSpeed.Should().Be(50);

            var p2 = peerList.Peers["4.4.4.4:7000"];
            p2.Client.Should().Be("Client2");
            p2.ClientId.Should().Be("ID2");
            p2.Connection.Should().Be("uTP");
            p2.Country.Should().Be("DE");
            p2.CountryCode.Should().Be("DE");
            p2.Downloaded.Should().Be(20);
            p2.DownloadSpeed.Should().Be(2000);
            p2.Files.Should().Be("b.mp4");
            p2.Flags.Should().Be("");
            p2.FlagsDescription.Should().Be("");
            p2.IPAddress.Should().Be("4.4.4.4");
            p2.Port.Should().Be(7000);
            p2.Progress.Should().Be(0.8f);
            p2.Relevance.Should().Be(0.1f);
            p2.Uploaded.Should().Be(15);
            p2.UploadSpeed.Should().Be(150);
        }

        [Fact]
        public void GIVEN_ExistingPeer_AND_UpdateWithPartialNulls_WHEN_MergeTorrentPeers_THEN_OnlyNonNullFieldsChange()
        {
            // arrange
            var existing = new Peer(
                key: "5.5.5.5:6881",
                client: "OldClient",
                clientId: "OldID",
                connection: "TCP",
                country: "ES",
                countryCode: "ES",
                downloaded: 111,
                downloadSpeed: 222,
                files: "old.dat",
                flags: "X",
                flagsDescription: "old",
                iPAddress: "5.5.5.5",
                port: 6881,
                progress: 0.11f,
                relevance: 0.22f,
                uploaded: 333,
                uploadSpeed: 444);

            var peerList = new PeerList(new Dictionary<string, Peer>
            {
                [existing.Key] = existing
            });

            var update = new QbtPeer(
                client: null,                     // keep OldClient
                connection: "uTP",                // overwrite
                country: null,                    // keep ES
                countryCode: "FR",                // overwrite
                downloadSpeed: null,              // keep 222
                downloaded: 999,                  // overwrite
                files: null,                      // keep old.dat
                flags: "N",                       // overwrite
                flagsDescription: null,           // keep old
                iPAddress: null,                  // keep 5.5.5.5
                i2pDestination: null,
                clientId: "NewID",                // overwrite
                port: null,                       // keep 6881
                progress: 0.77f,                  // overwrite
                relevance: null,                  // keep 0.22
                uploadSpeed: 888,                 // overwrite
                uploaded: null);                  // keep 333

            var input = new QbtTorrentPeers(
                fullUpdate: false,
                peers: new Dictionary<string, QbtPeer> { [existing.Key] = update },
                peersRemoved: null,
                requestId: 13,
                showFlags: null);

            // act
            _target.MergeTorrentPeers(input, peerList);

            // assert
            var p = peerList.Peers[existing.Key];
            p.Client.Should().Be("OldClient");
            p.ClientId.Should().Be("NewID");
            p.Connection.Should().Be("uTP");
            p.Country.Should().Be("ES");
            p.CountryCode.Should().Be("FR");
            p.DownloadSpeed.Should().Be(222);
            p.Downloaded.Should().Be(999);
            p.Files.Should().Be("old.dat");
            p.Flags.Should().Be("N");
            p.FlagsDescription.Should().Be("old");
            p.IPAddress.Should().Be("5.5.5.5");
            p.Port.Should().Be(6881);
            p.Progress.Should().Be(0.77f);
            p.Relevance.Should().Be(0.22f);
            p.UploadSpeed.Should().Be(888);
            p.Uploaded.Should().Be(333);
        }

        [Fact]
        public void GIVEN_KeyRemovedThenReaddedInSameMerge_WHEN_MergeTorrentPeers_THEN_PresentWithNewValues()
        {
            // arrange
            var key = "6.6.6.6:6001";

            var oldPeer = new Peer(
                key: key,
                client: "Old",
                clientId: "OID",
                connection: "TCP",
                country: null,
                countryCode: null,
                downloaded: 1,
                downloadSpeed: 2,
                files: "old",
                flags: "O",
                flagsDescription: "old",
                iPAddress: "6.6.6.6",
                port: 6001,
                progress: 0.1f,
                relevance: 0.2f,
                uploaded: 3,
                uploadSpeed: 4);

            var peerList = new PeerList(new Dictionary<string, Peer> { [key] = oldPeer });

            var newPeer = new QbtPeer(
                client: "New",
                connection: "uTP",
                country: "NL",
                countryCode: "NL",
                downloadSpeed: 999,
                downloaded: 111,
                files: "new",
                flags: "N",
                flagsDescription: "new",
                iPAddress: "6.6.6.6",
                i2pDestination: null,
                clientId: "NID",
                port: 6001,
                progress: 0.9f,
                relevance: 0.8f,
                uploadSpeed: 777,
                uploaded: 333);

            var input = new QbtTorrentPeers(
                fullUpdate: false,
                peers: new Dictionary<string, QbtPeer> { [key] = newPeer },
                peersRemoved: new List<string> { key },
                requestId: 14,
                showFlags: null);

            // act
            _target.MergeTorrentPeers(input, peerList);

            // assert
            peerList.Peers.Should().ContainKey(key);
            var p = peerList.Peers[key];
            p.Client.Should().Be("New");
            p.ClientId.Should().Be("NID");
            p.Connection.Should().Be("uTP");
            p.Country.Should().Be("NL");
            p.CountryCode.Should().Be("NL");
            p.Downloaded.Should().Be(111);
            p.DownloadSpeed.Should().Be(999);
            p.Files.Should().Be("new");
            p.Flags.Should().Be("N");
            p.FlagsDescription.Should().Be("new");
            p.IPAddress.Should().Be("6.6.6.6");
            p.Port.Should().Be(6001);
            p.Progress.Should().Be(0.9f);
            p.Relevance.Should().Be(0.8f);
            p.Uploaded.Should().Be(333);
            p.UploadSpeed.Should().Be(777);
        }
    }
}