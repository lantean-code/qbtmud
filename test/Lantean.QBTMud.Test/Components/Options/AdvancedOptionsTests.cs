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
using System.Text.Json;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class AdvancedOptionsTests : RazorComponentTestBase<AdvancedOptions>
    {
        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            var api = TestContext.AddSingletonMock<IApiClient>();
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

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var resumeSelect = FindSelect<string>(target, "ResumeDataStorageType");
            resumeSelect.Instance.Value.Should().Be("SQLite");

            FindNumeric(target, "MemoryWorkingSetLimit").Instance.Value.Should().Be(512);
            FindSelect<string>(target, "CurrentNetworkInterface").Instance.Value.Should().Be("eth0");
            FindSelect<string>(target, "CurrentInterfaceAddress").Instance.Value.Should().Be("10.0.0.2");
            FindNumeric(target, "SaveResumeDataInterval").Instance.Value.Should().Be(15);
            FindNumeric(target, "TorrentFileSizeLimit").Instance.Value.Should().Be(150);
            FindSwitch(target, "RecheckCompletedTorrents").Instance.Value.Should().BeTrue();
            FindNumeric(target, "RefreshInterval").Instance.Value.Should().Be(1500);
            FindSwitch(target, "ResolvePeerCountries").Instance.Value.Should().BeTrue();
            FindSwitch(target, "EnableEmbeddedTracker").Instance.Value.Should().BeTrue();
            FindNumeric(target, "EmbeddedTrackerPort").Instance.Value.Should().Be(19000);
            FindSwitch(target, "EmbeddedTrackerPortForwarding").Instance.Value.Should().BeTrue();
            target.Markup.Should().Contain("Ethernet");
        }

        [Fact]
        public async Task GIVEN_NetworkInterface_WHEN_Changed_THEN_ShouldRefreshAddresses()
        {
            var api = TestContext.AddSingletonMock<IApiClient>();
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

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            var interfaceSelect = FindSelect<string>(target, "CurrentNetworkInterface");
            await target.InvokeAsync(() => interfaceSelect.Instance.ValueChanged.InvokeAsync("eth0"));

            update.CurrentNetworkInterface.Should().Be("eth0");
            raised[^1].Should().BeSameAs(update);

            var addressSelect = FindSelect<string>(target, "CurrentInterfaceAddress");
            await target.InvokeAsync(() => addressSelect.Instance.ValueChanged.InvokeAsync("::"));
            update.CurrentInterfaceAddress.Should().Be("::");
            raised[^1].Should().BeSameAs(update);
            api.Verify(a => a.GetNetworkInterfaceAddressList("eth0"), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CoreAdvancedSettings_WHEN_Modified_THEN_ShouldUpdatePreferences()
        {
            var api = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfaces()).ReturnsAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressList(It.IsAny<string>())).ReturnsAsync(Array.Empty<string>());

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            await target.InvokeAsync(() => FindSelect<string>(target, "ResumeDataStorageType").Instance.ValueChanged.InvokeAsync("Legacy"));
            await target.InvokeAsync(() => FindNumeric(target, "MemoryWorkingSetLimit").Instance.ValueChanged.InvokeAsync(768));
            await target.InvokeAsync(() => FindNumeric(target, "SaveResumeDataInterval").Instance.ValueChanged.InvokeAsync(20));
            await target.InvokeAsync(() => FindNumeric(target, "TorrentFileSizeLimit").Instance.ValueChanged.InvokeAsync(175));
            await target.InvokeAsync(() => FindSwitch(target, "RecheckCompletedTorrents").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "ConfirmTorrentRecheck").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindNumeric(target, "RefreshInterval").Instance.ValueChanged.InvokeAsync(2000));
            await target.InvokeAsync(() => FindSwitch(target, "ResolvePeerCountries").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "ReannounceWhenAddressChanged").Instance.ValueChanged.InvokeAsync(false));

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
            var api = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfaces()).ReturnsAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressList(It.IsAny<string>())).ReturnsAsync(Array.Empty<string>());

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            TestContext.Render<MudPopoverProvider>();

            var raised = new List<UpdatePreferences>();
            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            await target.InvokeAsync(() => FindNumeric(target, "BdecodeDepthLimit").Instance.ValueChanged.InvokeAsync(120));
            await target.InvokeAsync(() => FindNumeric(target, "BdecodeTokenLimit").Instance.ValueChanged.InvokeAsync(240));
            await target.InvokeAsync(() => FindNumeric(target, "AsyncIoThreads").Instance.ValueChanged.InvokeAsync(6));
            await target.InvokeAsync(() => FindNumeric(target, "HashingThreads").Instance.ValueChanged.InvokeAsync(8));
            await target.InvokeAsync(() => FindNumeric(target, "FilePoolSize").Instance.ValueChanged.InvokeAsync(1024));
            await target.InvokeAsync(() => FindNumeric(target, "CheckingMemoryUse").Instance.ValueChanged.InvokeAsync(256));
            await target.InvokeAsync(() => FindNumeric(target, "DiskCache").Instance.ValueChanged.InvokeAsync(384));
            await target.InvokeAsync(() => FindNumeric(target, "DiskCacheTtl").Instance.ValueChanged.InvokeAsync(120));
            await target.InvokeAsync(() => FindNumeric(target, "DiskQueueSize").Instance.ValueChanged.InvokeAsync(10240));
            await target.InvokeAsync(() => FindSelect<int>(target, "DiskIoType").Instance.ValueChanged.InvokeAsync(1));
            await target.InvokeAsync(() => FindSelect<int>(target, "DiskIoReadMode").Instance.ValueChanged.InvokeAsync(1));
            await target.InvokeAsync(() => FindSelect<int>(target, "DiskIoWriteMode").Instance.ValueChanged.InvokeAsync(2));
            await target.InvokeAsync(() => FindSwitch(target, "EnableCoalesceReadWrite").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "EnablePieceExtentAffinity").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "EnableUploadSuggestions").Instance.ValueChanged.InvokeAsync(true));

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
            var api = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfaces()).ReturnsAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressList(It.IsAny<string>())).ReturnsAsync(Array.Empty<string>());

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            await target.InvokeAsync(() => FindNumeric(target, "SendBufferWatermark").Instance.ValueChanged.InvokeAsync(256));
            await target.InvokeAsync(() => FindNumeric(target, "SendBufferLowWatermark").Instance.ValueChanged.InvokeAsync(32));
            await target.InvokeAsync(() => FindNumeric(target, "SendBufferWatermarkFactor").Instance.ValueChanged.InvokeAsync(200));
            await target.InvokeAsync(() => FindNumeric(target, "ConnectionSpeed").Instance.ValueChanged.InvokeAsync(500));
            await target.InvokeAsync(() => FindNumeric(target, "SocketSendBufferSize").Instance.ValueChanged.InvokeAsync(256));
            await target.InvokeAsync(() => FindNumeric(target, "SocketReceiveBufferSize").Instance.ValueChanged.InvokeAsync(256));
            await target.InvokeAsync(() => FindNumeric(target, "SocketBacklogSize").Instance.ValueChanged.InvokeAsync(100));
            await target.InvokeAsync(() => FindNumeric(target, "OutgoingPortsMin").Instance.ValueChanged.InvokeAsync(10000));
            await target.InvokeAsync(() => FindNumeric(target, "OutgoingPortsMax").Instance.ValueChanged.InvokeAsync(20000));
            await target.InvokeAsync(() => FindNumeric(target, "UpnpLeaseDuration").Instance.ValueChanged.InvokeAsync(1200));
            await target.InvokeAsync(() => FindNumeric(target, "PeerTos").Instance.ValueChanged.InvokeAsync(16));
            await target.InvokeAsync(() => FindSelect<int>(target, "UtpTcpMixedMode").Instance.ValueChanged.InvokeAsync(1));
            await target.InvokeAsync(() => FindSwitch(target, "IdnSupportEnabled").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "EnableMultiConnectionsFromSameIp").Instance.ValueChanged.InvokeAsync(true));
            await target.InvokeAsync(() => FindSwitch(target, "ValidateHttpsTrackerCertificate").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "SsrfMitigation").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "BlockPeersOnPrivilegedPorts").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "EnableEmbeddedTracker").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindNumeric(target, "EmbeddedTrackerPort").Instance.ValueChanged.InvokeAsync(20000));
            await target.InvokeAsync(() => FindSwitch(target, "EmbeddedTrackerPortForwarding").Instance.ValueChanged.InvokeAsync(false));

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
            var api = TestContext.AddSingletonMock<IApiClient>(MockBehavior.Loose);
            api.Setup(a => a.GetNetworkInterfaces()).ReturnsAsync(Array.Empty<NetworkInterface>());
            api.Setup(a => a.GetNetworkInterfaceAddressList(It.IsAny<string>())).ReturnsAsync(Array.Empty<string>());

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var raised = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<AdvancedOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => raised.Add(value)));
            });

            await target.InvokeAsync(() => FindSelect<int>(target, "UploadSlotsBehavior").Instance.ValueChanged.InvokeAsync(1));
            await target.InvokeAsync(() => FindSelect<int>(target, "UploadChokingAlgorithm").Instance.ValueChanged.InvokeAsync(2));
            await target.InvokeAsync(() => FindSwitch(target, "AnnounceToAllTrackers").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "AnnounceToAllTiers").Instance.ValueChanged.InvokeAsync(true));
            await target.InvokeAsync(() => FindTextField(target, "AnnounceIp").Instance.ValueChanged.InvokeAsync("203.0.113.5"));
            await target.InvokeAsync(() => FindNumeric(target, "MaxConcurrentHttpAnnounces").Instance.ValueChanged.InvokeAsync(80));
            await target.InvokeAsync(() => FindNumeric(target, "StopTrackerTimeout").Instance.ValueChanged.InvokeAsync(45));
            await target.InvokeAsync(() => FindNumeric(target, "PeerTurnover").Instance.ValueChanged.InvokeAsync(12));
            await target.InvokeAsync(() => FindNumeric(target, "PeerTurnoverCutoff").Instance.ValueChanged.InvokeAsync(25));
            await target.InvokeAsync(() => FindNumeric(target, "PeerTurnoverInterval").Instance.ValueChanged.InvokeAsync(120));
            await target.InvokeAsync(() => FindNumeric(target, "RequestQueueSize").Instance.ValueChanged.InvokeAsync(200));
            await target.InvokeAsync(() => FindNumeric(target, "I2pInboundQuantity").Instance.ValueChanged.InvokeAsync(6));
            await target.InvokeAsync(() => FindNumeric(target, "I2pOutboundQuantity").Instance.ValueChanged.InvokeAsync(4));
            await target.InvokeAsync(() => FindNumeric(target, "I2pInboundLength").Instance.ValueChanged.InvokeAsync(3));
            await target.InvokeAsync(() => FindNumeric(target, "I2pOutboundLength").Instance.ValueChanged.InvokeAsync(2));

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

        private static IRenderedComponent<MudNumericField<int>> FindNumeric(IRenderedComponent<AdvancedOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(target, testId);
        }

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<AdvancedOptions> target, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(target, testId);
        }

        private static IRenderedComponent<FieldSwitch> FindSwitch(IRenderedComponent<AdvancedOptions> target, string testId)
        {
            return FindComponentByTestId<FieldSwitch>(target, testId);
        }

        private static IRenderedComponent<MudSelect<T>> FindSelect<T>(IRenderedComponent<AdvancedOptions> target, string testId)
        {
            return FindComponentByTestId<MudSelect<T>>(target, testId);
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
    }
}
