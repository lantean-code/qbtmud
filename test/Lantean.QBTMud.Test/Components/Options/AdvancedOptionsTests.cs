using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Moq;
using MudBlazor;
using Xunit;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class AdvancedOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public AdvancedOptionsTests()
        {
            _target = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            var api = _target.AddSingletonMock<IApiClient>();
            api.Setup(a => a.GetNetworkInterfaces())
                .ReturnsAsync(new List<NetworkInterface>
                {
                    new NetworkInterface("Any", string.Empty),
                    new NetworkInterface("Ethernet", "eth0")
                });
            api.Setup(a => a.GetNetworkInterfaceAddressList(It.IsAny<string>()))
                .ReturnsAsync(Array.Empty<string>());

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var resumeSelect = FindSelect<string>(cut, "Resume data storage type (requires restart)");
            resumeSelect.Instance.Value.Should().Be("SQLite");

            FindNumeric(cut, "Physical memory (RAM) usage limit (applied if libtorrent >= 2.0)").Instance.Value.Should().Be(512);
            FindSelect<string>(cut, "Network interface").Instance.Value.Should().Be("eth0");
            FindSelect<string>(cut, "Optional IP address to bind to").Instance.Value.Should().Be("10.0.0.2");
            FindNumeric(cut, "Save resume data interval").Instance.Value.Should().Be(15);
            FindNumeric(cut, ".torrent file size limit").Instance.Value.Should().Be(150);
            FindSwitch(cut, "Recheck torrents on completion").Instance.Value.Should().BeTrue();
            FindNumeric(cut, "Refresh interval").Instance.Value.Should().Be(1500);
            FindSwitch(cut, "Resolve peer countries").Instance.Value.Should().BeTrue();
            FindSwitch(cut, "Enable embedded tracker").Instance.Value.Should().BeTrue();
            FindNumeric(cut, "Embedded tracker port").Instance.Value.Should().Be(19000);
            FindSwitch(cut, "Enable port forwarding for embedded tracker").Instance.Value.Should().BeTrue();
            cut.Markup.Should().Contain("Ethernet");
        }

        [Fact]
        public async Task GIVEN_NetworkInterface_WHEN_Changed_THEN_ShouldRefreshAddresses()
        {
            var api = _target.AddSingletonMock<IApiClient>();
            api.Setup(a => a.GetNetworkInterfaces())
                .ReturnsAsync(new List<NetworkInterface>
                {
                    new NetworkInterface("Any", string.Empty),
                    new NetworkInterface("Ethernet", "eth0")
                });
            api.Setup(a => a.GetNetworkInterfaceAddressList("eth0"))
                .ReturnsAsync(new[] { "192.168.0.10", "fe80::1" });
            api.Setup(a => a.GetNetworkInterfaceAddressList(""))
                .ReturnsAsync(Array.Empty<string>());

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var interfaceSelect = FindSelect<string>(cut, "Network interface");
            await cut.InvokeAsync(() => interfaceSelect.Instance.ValueChanged.InvokeAsync("eth0"));

            update.CurrentNetworkInterface.Should().Be("eth0");
            raised.Last().Should().BeSameAs(update);

            var addressSelect = FindSelect<string>(cut, "Optional IP address to bind to");
            await cut.InvokeAsync(() => addressSelect.Instance.ValueChanged.InvokeAsync("::"));
            update.CurrentInterfaceAddress.Should().Be("::");
            raised.Last().Should().BeSameAs(update);
            api.Verify(a => a.GetNetworkInterfaceAddressList("eth0"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CoreAdvancedSettings_WHEN_Modified_THEN_ShouldUpdatePreferences()
        {
            var api = _target.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfaces()).ReturnsAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressList(It.IsAny<string>())).ReturnsAsync(Array.Empty<string>());

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            await cut.InvokeAsync(() => FindSelect<string>(cut, "Resume data storage type (requires restart)").Instance.ValueChanged.InvokeAsync("Legacy"));
            await cut.InvokeAsync(() => FindNumeric(cut, "Physical memory (RAM) usage limit (applied if libtorrent >= 2.0)").Instance.ValueChanged.InvokeAsync(768));
            await cut.InvokeAsync(() => FindNumeric(cut, "Save resume data interval").Instance.ValueChanged.InvokeAsync(20));
            await cut.InvokeAsync(() => FindNumeric(cut, ".torrent file size limit").Instance.ValueChanged.InvokeAsync(175));
            await cut.InvokeAsync(() => FindSwitch(cut, "Recheck torrents on completion").Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => FindSwitch(cut, "Confirm torrent recheck").Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => FindNumeric(cut, "Refresh interval").Instance.ValueChanged.InvokeAsync(2000));
            await cut.InvokeAsync(() => FindSwitch(cut, "Resolve peer countries").Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => FindSwitch(cut, "Reannounce to all trackers when IP or port changed").Instance.ValueChanged.InvokeAsync(false));

            update.ResumeDataStorageType.Should().Be("Legacy");
            update.MemoryWorkingSetLimit.Should().Be(768);
            update.SaveResumeDataInterval.Should().Be(20);
            update.TorrentFileSizeLimit.Should().Be(175 * 1024 * 1024);
            update.RecheckCompletedTorrents.Should().BeFalse();
            update.ConfirmTorrentRecheck.Should().BeFalse();
            update.RefreshInterval.Should().Be(2000);
            update.ResolvePeerCountries.Should().BeFalse();
            update.ReannounceWhenAddressChanged.Should().BeFalse();
            raised.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_DiskSettings_WHEN_Modified_THEN_ShouldUpdatePreferences()
        {
            var api = _target.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfaces()).ReturnsAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressList(It.IsAny<string>())).ReturnsAsync(Array.Empty<string>());

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            _target.RenderComponent<MudPopoverProvider>();

            var raised = new List<UpdatePreferences>();
            var cut = _target.RenderComponent<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            await cut.InvokeAsync(() => FindNumeric(cut, "Bdecode depth limit").Instance.ValueChanged.InvokeAsync(120));
            await cut.InvokeAsync(() => FindNumeric(cut, "Bdecode token limit").Instance.ValueChanged.InvokeAsync(240));
            await cut.InvokeAsync(() => FindNumeric(cut, "Asynchronous I/O threads").Instance.ValueChanged.InvokeAsync(6));
            await cut.InvokeAsync(() => FindNumeric(cut, "Hashing threads (requires libtorrent >= 2.0)").Instance.ValueChanged.InvokeAsync(8));
            await cut.InvokeAsync(() => FindNumeric(cut, "File pool size").Instance.ValueChanged.InvokeAsync(1024));
            await cut.InvokeAsync(() => FindNumeric(cut, "Outstanding memory when checking torrents").Instance.ValueChanged.InvokeAsync(256));
            await cut.InvokeAsync(() => FindNumeric(cut, "Disk cache (requires libtorrent < 2.0)").Instance.ValueChanged.InvokeAsync(384));
            await cut.InvokeAsync(() => FindNumeric(cut, "Disk cache expiry interval (requires libtorrent < 2.0)").Instance.ValueChanged.InvokeAsync(120));
            await cut.InvokeAsync(() => FindNumeric(cut, "Disk queue size").Instance.ValueChanged.InvokeAsync(10240));
            await cut.InvokeAsync(() => FindSelect<int>(cut, "Disk IO type (libtorrent >= 2.0; requires restart)").Instance.ValueChanged.InvokeAsync(1));
            await cut.InvokeAsync(() => FindSelect<int>(cut, "Disk IO read mode").Instance.ValueChanged.InvokeAsync(1));
            await cut.InvokeAsync(() => FindSelect<int>(cut, "Disk IO write mode").Instance.ValueChanged.InvokeAsync(2));
            await cut.InvokeAsync(() => FindSwitch(cut, "Coalesce reads & writes (requires libtorrent < 2.0)").Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => FindSwitch(cut, "Use piece extent affinity").Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => FindSwitch(cut, "Send upload piece suggestions").Instance.ValueChanged.InvokeAsync(true));

            update.BdecodeDepthLimit.Should().Be(120);
            update.BdecodeTokenLimit.Should().Be(240);
            update.AsyncIoThreads.Should().Be(6);
            update.HashingThreads.Should().Be(8);
            update.FilePoolSize.Should().Be(1024);
            update.CheckingMemoryUse.Should().Be(256);
            update.DiskCache.Should().Be(384);
            update.DiskCacheTtl.Should().Be(120);
            update.DiskQueueSize.Should().Be(10240 * 1024);
            update.DiskIoType.Should().Be(1);
            update.DiskIoReadMode.Should().Be(1);
            update.DiskIoWriteMode.Should().Be(2);
            update.EnableCoalesceReadWrite.Should().BeFalse();
            update.EnablePieceExtentAffinity.Should().BeFalse();
            update.EnableUploadSuggestions.Should().BeTrue();
            raised.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_BufferAndConnectionSettings_WHEN_Modified_THEN_ShouldUpdatePreferences()
        {
            var api = _target.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfaces()).ReturnsAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressList(It.IsAny<string>())).ReturnsAsync(Array.Empty<string>());

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            await cut.InvokeAsync(() => FindNumeric(cut, "Send buffer watermark").Instance.ValueChanged.InvokeAsync(256));
            await cut.InvokeAsync(() => FindNumeric(cut, "Send buffer low watermark").Instance.ValueChanged.InvokeAsync(32));
            await cut.InvokeAsync(() => FindNumeric(cut, "Send buffer watermark factor").Instance.ValueChanged.InvokeAsync(200));
            await cut.InvokeAsync(() => FindNumeric(cut, "Outgoing connections per second").Instance.ValueChanged.InvokeAsync(500));
            await cut.InvokeAsync(() => FindNumeric(cut, "Socket send buffer size [0: system default]").Instance.ValueChanged.InvokeAsync(256));
            await cut.InvokeAsync(() => FindNumeric(cut, "Socket receive buffer size [0: system default]").Instance.ValueChanged.InvokeAsync(256));
            await cut.InvokeAsync(() => FindNumeric(cut, "Socket backlog size").Instance.ValueChanged.InvokeAsync(100));
            await cut.InvokeAsync(() => FindNumeric(cut, "Outgoing ports (Min) [0: disabled]").Instance.ValueChanged.InvokeAsync(10000));
            await cut.InvokeAsync(() => FindNumeric(cut, "Outgoing ports (Max) [0: disabled]").Instance.ValueChanged.InvokeAsync(20000));
            await cut.InvokeAsync(() => FindNumeric(cut, "UPnP lease duration [0: permanent lease]").Instance.ValueChanged.InvokeAsync(1200));
            await cut.InvokeAsync(() => FindNumeric(cut, "Type of service (ToS) for connections to peers").Instance.ValueChanged.InvokeAsync(16));
            await cut.InvokeAsync(() => FindSelect<int>(cut, "μTP-TCP mixed mode algorithm").Instance.ValueChanged.InvokeAsync(1));
            await cut.InvokeAsync(() => FindSwitch(cut, "Support internationalized domain name (IDN)").Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => FindSwitch(cut, "Allow multiple connections from the same IP address").Instance.ValueChanged.InvokeAsync(true));
            await cut.InvokeAsync(() => FindSwitch(cut, "Validate HTTPS tracker certificate").Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => FindSwitch(cut, "Server-side request forgery (SSRF) mitigation").Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => FindSwitch(cut, "Disallow connection to peers on privileged ports").Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => FindSwitch(cut, "Enable embedded tracker").Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => FindNumeric(cut, "Embedded tracker port").Instance.ValueChanged.InvokeAsync(20000));
            await cut.InvokeAsync(() => FindSwitch(cut, "Enable port forwarding for embedded tracker").Instance.ValueChanged.InvokeAsync(false));

            update.SendBufferWatermark.Should().Be(256);
            update.SendBufferLowWatermark.Should().Be(32);
            update.SendBufferWatermarkFactor.Should().Be(200);
            update.ConnectionSpeed.Should().Be(500);
            update.SocketSendBufferSize.Should().Be(256 * 1024);
            update.SocketReceiveBufferSize.Should().Be(256 * 1024);
            update.SocketBacklogSize.Should().Be(100);
            update.OutgoingPortsMin.Should().Be(10000);
            update.OutgoingPortsMax.Should().Be(20000);
            update.UpnpLeaseDuration.Should().Be(1200);
            update.PeerTos.Should().Be(16);
            update.UtpTcpMixedMode.Should().Be(1);
            update.IdnSupportEnabled.Should().BeFalse();
            update.EnableMultiConnectionsFromSameIp.Should().BeTrue();
            update.ValidateHttpsTrackerCertificate.Should().BeFalse();
            update.SsrfMitigation.Should().BeFalse();
            update.BlockPeersOnPrivilegedPorts.Should().BeFalse();
            update.EnableEmbeddedTracker.Should().BeFalse();
            update.EmbeddedTrackerPort.Should().Be(20000);
            update.EmbeddedTrackerPortForwarding.Should().BeFalse();
            raised.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_TrackerSettings_WHEN_Modified_THEN_ShouldUpdatePreferences()
        {
            var api = _target.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfaces()).ReturnsAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressList(It.IsAny<string>())).ReturnsAsync(Array.Empty<string>());

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            await cut.InvokeAsync(() => FindSelect<int>(cut, "Upload slots behavior").Instance.ValueChanged.InvokeAsync(1));
            await cut.InvokeAsync(() => FindSelect<int>(cut, "Upload choking algorithm").Instance.ValueChanged.InvokeAsync(2));
            await cut.InvokeAsync(() => FindSwitch(cut, "Always announce to all trackers in a tier").Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => FindSwitch(cut, "Always announce to all tiers").Instance.ValueChanged.InvokeAsync(true));
            await cut.InvokeAsync(() => FindTextField(cut, "IP address reported to trackers (requires restart)").Instance.ValueChanged.InvokeAsync("203.0.113.5"));
            await cut.InvokeAsync(() => FindNumeric(cut, "Max concurrent HTTP announces").Instance.ValueChanged.InvokeAsync(80));
            await cut.InvokeAsync(() => FindNumeric(cut, "Stop tracker timeout [0: disabled]").Instance.ValueChanged.InvokeAsync(45));
            await cut.InvokeAsync(() => FindNumeric(cut, "Peer turnover disconnect percentage:").Instance.ValueChanged.InvokeAsync(12));
            await cut.InvokeAsync(() => FindNumeric(cut, "Peer turnover threshold percentage").Instance.ValueChanged.InvokeAsync(25));
            await cut.InvokeAsync(() => FindNumeric(cut, "Peer turnover disconnect interval").Instance.ValueChanged.InvokeAsync(120));
            await cut.InvokeAsync(() => FindNumeric(cut, "Maximum outstanding requests to a single peer").Instance.ValueChanged.InvokeAsync(200));
            await cut.InvokeAsync(() => FindNumeric(cut, "I2P inbound quantity (requires libtorrent >= 2.0)").Instance.ValueChanged.InvokeAsync(6));
            await cut.InvokeAsync(() => FindNumeric(cut, "I2P outbound quantity (requires libtorrent >= 2.0)").Instance.ValueChanged.InvokeAsync(4));
            await cut.InvokeAsync(() => FindNumeric(cut, "I2P inbound length (requires libtorrent >= 2.0)").Instance.ValueChanged.InvokeAsync(3));
            await cut.InvokeAsync(() => FindNumeric(cut, "I2P outbound length (requires libtorrent >= 2.0)").Instance.ValueChanged.InvokeAsync(2));

            update.UploadSlotsBehavior.Should().Be(1);
            update.UploadChokingAlgorithm.Should().Be(2);
            update.AnnounceToAllTrackers.Should().BeFalse();
            update.AnnounceToAllTiers.Should().BeTrue();
            update.AnnounceIp.Should().Be("203.0.113.5");
            update.MaxConcurrentHttpAnnounces.Should().Be(80);
            update.StopTrackerTimeout.Should().Be(45);
            update.PeerTurnover.Should().Be(12);
            update.PeerTurnoverCutoff.Should().Be(25);
            update.PeerTurnoverInterval.Should().Be(120);
            update.RequestQueueSize.Should().Be(200);
            update.I2pInboundQuantity.Should().Be(6);
            update.I2pOutboundQuantity.Should().Be(4);
            update.I2pInboundLength.Should().Be(3);
            update.I2pOutboundLength.Should().Be(2);
            raised.Should().NotBeEmpty();
        }

        private static IRenderedComponent<MudNumericField<int>> FindNumeric(IRenderedComponent<AdvancedOptions> cut, string label)
        {
            return cut.FindComponents<MudNumericField<int>>().First(field => field.Instance.Label == label);
        }

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<AdvancedOptions> cut, string label)
        {
            return cut.FindComponents<MudTextField<string>>().First(field => field.Instance.Label == label);
        }

        private static IRenderedComponent<FieldSwitch> FindSwitch(IRenderedComponent<AdvancedOptions> cut, string label)
        {
            return cut.FindComponents<FieldSwitch>().First(field => field.Instance.Label == label);
        }

        private static IRenderedComponent<MudSelect<T>> FindSelect<T>(IRenderedComponent<AdvancedOptions> cut, string label)
        {
            return cut.FindComponents<MudSelect<T>>().First(field => field.Instance.Label == label);
        }

        private static Preferences DeserializePreferences()
        {
            const string json = """
            {
                "resume_data_storage_type": "SQLite",
                "memory_working_set_limit": 512,
                "current_network_interface": "eth0",
                "current_interface_address": "10.0.0.2",
                "save_resume_data_interval": 15,
                "torrent_file_size_limit": 157286400,
                "recheck_completed_torrents": true,
                "confirm_torrent_recheck": true,
                "app_instance_name": "Instance",
                "refresh_interval": 1500,
                "resolve_peer_countries": true,
                "reannounce_when_address_changed": true,
                "bdecode_depth_limit": 100,
                "bdecode_token_limit": 200,
                "async_io_threads": 4,
                "hashing_threads": 4,
                "file_pool_size": 512,
                "checking_memory_use": 128,
                "disk_cache": 256,
                "disk_cache_ttl": 60,
                "disk_queue_size": 8192,
                "disk_io_type": 0,
                "disk_io_read_mode": 0,
                "disk_io_write_mode": 0,
                "enable_coalesce_read_write": true,
                "enable_piece_extent_affinity": true,
                "enable_upload_suggestions": false,
                "send_buffer_watermark": 192,
                "send_buffer_low_watermark": 16,
                "send_buffer_watermark_factor": 150,
                "connection_speed": 300,
                "socket_send_buffer_size": 128,
                "socket_receive_buffer_size": 128,
                "socket_backlog_size": 50,
                "outgoing_ports_min": 0,
                "outgoing_ports_max": 0,
                "upnp_lease_duration": 600,
                "peer_tos": 8,
                "utp_tcp_mixed_mode": 0,
                "idn_support_enabled": true,
                "enable_multi_connections_from_same_ip": false,
                "validate_https_tracker_certificate": true,
                "ssrf_mitigation": true,
                "block_peers_on_privileged_ports": true,
                "enable_embedded_tracker": true,
                "embedded_tracker_port": 19000,
                "embedded_tracker_port_forwarding": true,
                "mark_of_the_web": false,
                "python_executable_path": "/usr/bin/python",
                "upload_slots_behavior": 0,
                "upload_choking_algorithm": 1,
                "announce_to_all_trackers": true,
                "announce_to_all_tiers": false,
                "announce_ip": "198.51.100.5",
                "max_concurrent_http_announces": 60,
                "stop_tracker_timeout": 30,
                "peer_turnover": 10,
                "peer_turnover_cutoff": 20,
                "peer_turnover_interval": 90,
                "request_queue_size": 150,
                "dht_bootstrap_nodes": "node.example.com",
                "i2p_inbound_quantity": 3,
                "i2p_outbound_quantity": 2,
                "i2p_inbound_length": 1,
                "i2p_outbound_length": 1
            }
            """;

            return JsonSerializer.Deserialize<Preferences>(json, SerializerOptions.Options)!;
        }

        public void Dispose()
        {
            _target.Dispose();
        }
    }
}
