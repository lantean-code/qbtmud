using AwesomeAssertions;
using Bunit;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class ConnectionOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _context;

        public ConnectionOptionsTests()
        {
            _context = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = _context.RenderComponent<ConnectionOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            target.FindComponent<MudSelect<int>>().Instance.Value.Should().Be(2);
            target.FindComponent<MudNumericField<int>>().Instance.Value.Should().Be(8999);

            var switches = target.FindComponents<FieldSwitch>();
            var switchLabels = switches.Select(s => s.Instance.Label).ToList();

            switchLabels.Should().Contain("Use UPnp / NAT-PMP port forwarding from my router");
            switches[switchLabels.IndexOf("Use UPnp / NAT-PMP port forwarding from my router")].Instance.Value.Should().BeTrue();

            switchLabels.Should().Contain("Global maximum number of connections");
            switches[switchLabels.IndexOf("Global maximum number of connections")].Instance.Value.Should().BeTrue();

            switchLabels.Should().Contain("Maximum number of connections per torrent");
            switches[switchLabels.IndexOf("Maximum number of connections per torrent")].Instance.Value.Should().BeFalse();

            switchLabels.Should().Contain("Global maximum number of upload slots");
            switches[switchLabels.IndexOf("Global maximum number of upload slots")].Instance.Value.Should().BeTrue();

            switchLabels.Should().Contain("I2P (Experimental)");
            switches[switchLabels.IndexOf("I2P (Experimental)")].Instance.Value.Should().BeTrue();

            target.FindComponents<MudTextField<string>>()
                .First(tf => tf.Instance.Label == "Host" && tf.Instance.Value == "i2p.local")
                .Instance.Disabled.Should().BeFalse();

            switchLabels.Should().Contain("Use proxy for peer connections");
            switches[switchLabels.IndexOf("Use proxy for peer connections")].Instance.Disabled.Should().BeTrue();

            target.FindComponents<MudTextField<string>>()
                .First(tf => tf.Instance.Value == "127.0.0.1")
                .Instance.Disabled.Should().BeFalse();

            target.FindComponents<MudTextField<string>>()
                .First(tf => tf.Instance.Value == "user")
                .Instance.Disabled.Should().BeFalse();

            switches.First(s => s.Instance.Label == "IP Filter").Instance.Value.Should().BeTrue();

            target.FindComponents<MudTextField<string>>()
                .First(tf => tf.Instance.Label == "Filter path (.dat, .p2p, .p2b)")
                .Instance.Disabled.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_SwitchesAndInputs_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<ConnectionOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var listenField = target.FindComponent<MudNumericField<int>>();
            await target.InvokeAsync(() => listenField.Instance.ValueChanged.InvokeAsync(7000));

            var upnpSwitch = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Use UPnp / NAT-PMP port forwarding from my router");
            await target.InvokeAsync(() => upnpSwitch.Instance.ValueChanged.InvokeAsync(false));

            var maxConnField = target.FindComponents<MudNumericField<int>>()
                .First(n => n.Instance.Label == "Connections" && n.Instance.Value == 600);
            await target.InvokeAsync(() => maxConnField.Instance.ValueChanged.InvokeAsync(450));

            var proxyTypeSelect = target.FindComponents<MudSelect<string>>().First(s => s.Instance.Label == "Type");
            await target.InvokeAsync(() => proxyTypeSelect.Instance.ValueChanged.InvokeAsync("None"));

            update.ListenPort.Should().Be(7000);
            update.Upnp.Should().BeFalse();
            update.MaxConnec.Should().Be(450);
            update.ProxyType.Should().Be("None");

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_I2PSettings_WHEN_EnabledAndValuesChanged_THEN_ShouldUpdatePreferences()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializeCustomPreferences("""
            {
                "bittorrent_protocol": 0,
                "listen_port": 5000,
                "i2p_enabled": false,
                "i2p_address": "",
                "i2p_port": 0,
                "i2p_mixed_mode": false
            }
            """);

            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<ConnectionOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var i2pHostField = target.FindComponents<MudTextField<string>>()
                .First(tf => tf.Instance.Label == "Host" && tf.Instance.Value == string.Empty);
            i2pHostField.Instance.Disabled.Should().BeTrue();

            var i2pPortField = target.FindComponents<MudNumericField<int>>()
                .First(n => n.Instance.Label == "Slots" && n.Instance.Value == 0 && n.Instance.Disabled);

            var i2pSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "I2P (Experimental)");
            await target.InvokeAsync(() => i2pSwitch.Instance.ValueChanged.InvokeAsync(true));

            update.I2pEnabled.Should().BeTrue();

            i2pHostField.Instance.Disabled.Should().BeFalse();
            i2pPortField = target.FindComponents<MudNumericField<int>>()
                .First(n => n.Instance.Label == "Slots" && !n.Instance.Disabled);

            await target.InvokeAsync(() => i2pHostField.Instance.ValueChanged.InvokeAsync("i2p.example"));
            update.I2pAddress.Should().Be("i2p.example");

            await target.InvokeAsync(() => i2pPortField.Instance.ValueChanged.InvokeAsync(7654));
            update.I2pPort.Should().Be(7654);

            var mixedSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Mixed mode");
            await target.InvokeAsync(() => mixedSwitch.Instance.ValueChanged.InvokeAsync(true));
            update.I2pMixedMode.Should().BeTrue();

            events.Should().HaveCount(4);
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_ProxySettings_WHEN_TypeAuthAndFlagsChanged_THEN_ShouldRespectRules()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<ConnectionOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var typeSelect = target.FindComponents<MudSelect<string>>().First(s => s.Instance.Label == "Type");
            await target.InvokeAsync(() => typeSelect.Instance.ValueChanged.InvokeAsync("None"));

            update.ProxyType.Should().Be("None");

            var proxyHostField = target.FindComponents<MudTextField<string>>()
                .First(tf => tf.Instance.Value == "127.0.0.1");
            proxyHostField.Instance.Disabled.Should().BeTrue();

            var proxyPeerSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Use proxy for peer connections");
            proxyPeerSwitch.Instance.Disabled.Should().BeTrue();

            await target.InvokeAsync(() => typeSelect.Instance.ValueChanged.InvokeAsync("SOCKS5"));
            proxyHostField.Instance.Disabled.Should().BeFalse();

            var authSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Authentication");
            await target.InvokeAsync(() => authSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.ProxyAuthEnabled.Should().BeFalse();

            await target.InvokeAsync(() => proxyHostField.Instance.ValueChanged.InvokeAsync("10.0.0.5"));
            update.ProxyIp.Should().Be("10.0.0.5");

            var proxyPortField = target.FindComponents<MudNumericField<int>>()
                .First(n => n.Instance.Label == "Port" && !n.Instance.Disabled);
            await target.InvokeAsync(() => proxyPortField.Instance.ValueChanged.InvokeAsync(8888));
            update.ProxyPort.Should().Be(8888);

            var proxyUserField = target.FindComponents<MudTextField<string>>()
                .First(tf => tf.Instance.Label == "Username");
            await target.InvokeAsync(() => proxyUserField.Instance.ValueChanged.InvokeAsync("proxyuser"));
            update.ProxyUsername.Should().Be("proxyuser");

            var proxyPasswordField = target.FindComponents<MudTextField<string>>()
                .First(tf => tf.Instance.Label == "Password");
            await target.InvokeAsync(() => proxyPasswordField.Instance.ValueChanged.InvokeAsync("proxypass"));
            update.ProxyPassword.Should().Be("proxypass");

            var hostLookupSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Perform hostname lookup via proxy");
            await target.InvokeAsync(() => hostLookupSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.ProxyHostnameLookup.Should().BeFalse();

            var proxyBittorrentSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Use proxy for BitTorrent purposes");
            await target.InvokeAsync(() => proxyBittorrentSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.ProxyBittorrent.Should().BeFalse();

            var proxyRssSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Use proxy for RSS purposes");
            await target.InvokeAsync(() => proxyRssSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.ProxyRss.Should().BeFalse();

            var proxyMiscSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Use proxy for general purposes");
            await target.InvokeAsync(() => proxyMiscSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.ProxyMisc.Should().BeFalse();

            events.Should().HaveCount(11);
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_ConnectionLimitSwitches_WHEN_Disabled_THEN_ShouldDisableInputs()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializeCustomPreferences("""
            {
                "listen_port": 6881,
                "upnp": true,
                "max_connec": 800,
                "max_connec_per_torrent": 300,
                "max_uploads": 50,
                "max_uploads_per_torrent": 12
            }
            """);

            var target = _context.RenderComponent<ConnectionOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, new UpdatePreferences());
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var switches = target.FindComponents<FieldSwitch>();
            var maxConnectionsSwitch = switches.Single(s => s.Instance.Label == "Global maximum number of connections");
            await target.InvokeAsync(() => maxConnectionsSwitch.Instance.ValueChanged.InvokeAsync(false));

            var maxConnectionsField = target.FindComponents<MudNumericField<int>>()
                .First(n => n.Instance.Value == 800);
            maxConnectionsField.Instance.Disabled.Should().BeTrue();

            var perTorrentSwitch = switches.Single(s => s.Instance.Label == "Maximum number of connections per torrent");
            await target.InvokeAsync(() => perTorrentSwitch.Instance.ValueChanged.InvokeAsync(false));

            var perTorrentField = target.FindComponents<MudNumericField<int>>()
                .First(n => n.Instance.Value == 300);
            perTorrentField.Instance.Disabled.Should().BeTrue();

            var maxUploadsSwitch = switches.Single(s => s.Instance.Label == "Global maximum number of upload slots");
            await target.InvokeAsync(() => maxUploadsSwitch.Instance.ValueChanged.InvokeAsync(false));

            var maxUploadsField = target.FindComponents<MudNumericField<int>>()
                .First(n => n.Instance.Value == 50);
            maxUploadsField.Instance.Disabled.Should().BeTrue();

            var uploadsPerTorrentSwitch = switches.Single(s => s.Instance.Label == "Maximum number of upload slots per torrent");
            await target.InvokeAsync(() => uploadsPerTorrentSwitch.Instance.ValueChanged.InvokeAsync(false));

            var uploadsPerTorrentField = target.FindComponents<MudNumericField<int>>()
                .First(n => n.Instance.Value == 12);
            uploadsPerTorrentField.Instance.Disabled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ProtocolAndIpFilter_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            var target = _context.RenderComponent<ConnectionOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var protocolSelect = target.FindComponent<MudSelect<int>>();
            await target.InvokeAsync(() => protocolSelect.Instance.ValueChanged.InvokeAsync(1));
            update.BittorrentProtocol.Should().Be(1);

            var ipFilterSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "IP Filter");
            await target.InvokeAsync(() => ipFilterSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.IpFilterEnabled.Should().BeFalse();

            var filterPathField = target.FindComponents<MudTextField<string>>()
                .First(tf => tf.Instance.Label == "Filter path (.dat, .p2p, .p2b)");
            await target.InvokeAsync(() => filterPathField.Instance.ValueChanged.InvokeAsync("/new/filter.dat"));
            update.IpFilterPath.Should().Be("/new/filter.dat");

            var trackersSwitch = target.FindComponents<FieldSwitch>().Single(s => s.Instance.Label == "Apply to trackers");
            await target.InvokeAsync(() => trackersSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.IpFilterTrackers.Should().BeFalse();

            var bannedField = target.FindComponents<MudTextField<string>>()
                .First(tf => tf.Instance.Label == "Manually banned IP addresses");
            await target.InvokeAsync(() => bannedField.Instance.ValueChanged.InvokeAsync("10.0.0.2"));
            update.BannedIPs.Should().Be("10.0.0.2");

            events.Should().HaveCount(5);
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_GenericPopoverTrigger_WHEN_Clicked_THEN_ShouldUpdatePort()
        {
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();

            var target = _context.RenderComponent<ConnectionOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var numericField = target.FindComponent<MudNumericField<int>>();
            await target.InvokeAsync(() => numericField.Instance.OnAdornmentClick.InvokeAsync());

            update.ListenPort.Should().BeGreaterThanOrEqualTo(1024);
            update.ListenPort.Should().BeLessThanOrEqualTo(65535);
        }

        private static Preferences DeserializePreferences()
        {
            const string json = """
            {
                "bittorrent_protocol": 2,
                "listen_port": 8999,
                "upnp": true,
                "max_connec": 600,
                "max_connec_per_torrent": 0,
                "max_uploads": 30,
                "max_uploads_per_torrent": 5,
                "i2p_enabled": true,
                "i2p_address": "i2p.local",
                "i2p_port": 4444,
                "i2p_mixed_mode": true,
                "proxy_type": "SOCKS5",
                "proxy_ip": "127.0.0.1",
                "proxy_port": 1080,
                "proxy_auth_enabled": true,
                "proxy_username": "user",
                "proxy_password": "pass",
                "proxy_hostname_lookup": true,
                "proxy_bittorrent": true,
                "proxy_peer_connections": false,
                "proxy_rss": true,
                "proxy_misc": true,
                "ip_filter_enabled": true,
                "ip_filter_path": "/filters/ipfilter.dat",
                "ip_filter_trackers": true,
                "banned_IPs": "10.0.0.1"
            }
            """;

            return JsonSerializer.Deserialize<Preferences>(json, SerializerOptions.Options)!;
        }

        private static Preferences DeserializeCustomPreferences(string json)
        {
            return JsonSerializer.Deserialize<Preferences>(json, SerializerOptions.Options)!;
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}