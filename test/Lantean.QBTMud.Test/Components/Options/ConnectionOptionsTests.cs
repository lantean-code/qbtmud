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

            FindSelect<int>(target, "BittorrentProtocol").Instance.Value.Should().Be(2);
            FindNumericInt(target, "ListenPort").Instance.Value.Should().Be(8999);

            FindSwitch(target, "Upnp").Instance.Value.Should().BeTrue();
            FindSwitch(target, "MaxConnecEnabled").Instance.Value.Should().BeTrue();
            FindSwitch(target, "MaxConnecPerTorrentEnabled").Instance.Value.Should().BeFalse();
            FindSwitch(target, "MaxUploadsEnabled").Instance.Value.Should().BeTrue();
            FindSwitch(target, "I2pEnabled").Instance.Value.Should().BeTrue();

            FindTextField(target, "I2pAddress").Instance.Disabled.Should().BeFalse();

            FindSwitch(target, "ProxyPeerConnections").Instance.Disabled.Should().BeTrue();
            FindTextField(target, "ProxyIp").Instance.Disabled.Should().BeFalse();
            FindTextField(target, "ProxyUsername").Instance.Disabled.Should().BeFalse();

            FindSwitch(target, "IpFilterEnabled").Instance.Value.Should().BeTrue();
            FindTextField(target, "IpFilterPath").Instance.Disabled.Should().BeFalse();
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

            var listenField = FindNumericInt(target, "ListenPort");
            await target.InvokeAsync(() => listenField.Instance.ValueChanged.InvokeAsync(7000));

            var upnpSwitch = FindSwitch(target, "Upnp");
            await target.InvokeAsync(() => upnpSwitch.Instance.ValueChanged.InvokeAsync(false));

            var maxConnField = FindNumericInt(target, "MaxConnec");
            await target.InvokeAsync(() => maxConnField.Instance.ValueChanged.InvokeAsync(450));

            var proxyTypeSelect = FindSelect<string>(target, "ProxyType");
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

            var i2pHostField = FindTextField(target, "I2pAddress");
            i2pHostField.Instance.Disabled.Should().BeTrue();

            var i2pPortField = FindNumericInt(target, "I2pPort");
            i2pPortField.Instance.Disabled.Should().BeTrue();

            var i2pSwitch = FindSwitch(target, "I2pEnabled");
            await target.InvokeAsync(() => i2pSwitch.Instance.ValueChanged.InvokeAsync(true));

            update.I2pEnabled.Should().BeTrue();

            i2pHostField.Instance.Disabled.Should().BeFalse();
            i2pPortField = FindNumericInt(target, "I2pPort");
            i2pPortField.Instance.Disabled.Should().BeFalse();

            await target.InvokeAsync(() => i2pHostField.Instance.ValueChanged.InvokeAsync("i2p.example"));
            update.I2pAddress.Should().Be("i2p.example");

            await target.InvokeAsync(() => i2pPortField.Instance.ValueChanged.InvokeAsync(7654));
            update.I2pPort.Should().Be(7654);

            var mixedSwitch = FindSwitch(target, "I2pMixedMode");
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

            var typeSelect = FindSelect<string>(target, "ProxyType");
            await target.InvokeAsync(() => typeSelect.Instance.ValueChanged.InvokeAsync("None"));

            update.ProxyType.Should().Be("None");

            var proxyHostField = FindTextField(target, "ProxyIp");
            proxyHostField.Instance.Disabled.Should().BeTrue();

            var proxyPeerSwitch = FindSwitch(target, "ProxyPeerConnections");
            proxyPeerSwitch.Instance.Disabled.Should().BeTrue();

            await target.InvokeAsync(() => typeSelect.Instance.ValueChanged.InvokeAsync("SOCKS5"));
            proxyHostField.Instance.Disabled.Should().BeFalse();

            var authSwitch = FindSwitch(target, "ProxyAuthEnabled");
            await target.InvokeAsync(() => authSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.ProxyAuthEnabled.Should().BeFalse();

            await target.InvokeAsync(() => proxyHostField.Instance.ValueChanged.InvokeAsync("10.0.0.5"));
            update.ProxyIp.Should().Be("10.0.0.5");

            var proxyPortField = FindNumericInt(target, "ProxyPort");
            await target.InvokeAsync(() => proxyPortField.Instance.ValueChanged.InvokeAsync(8888));
            update.ProxyPort.Should().Be(8888);

            var proxyUserField = FindTextField(target, "ProxyUsername");
            await target.InvokeAsync(() => proxyUserField.Instance.ValueChanged.InvokeAsync("proxyuser"));
            update.ProxyUsername.Should().Be("proxyuser");

            var proxyPasswordField = FindTextField(target, "ProxyPassword");
            await target.InvokeAsync(() => proxyPasswordField.Instance.ValueChanged.InvokeAsync("proxypass"));
            update.ProxyPassword.Should().Be("proxypass");

            var hostLookupSwitch = FindSwitch(target, "ProxyHostnameLookup");
            await target.InvokeAsync(() => hostLookupSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.ProxyHostnameLookup.Should().BeFalse();

            var proxyBittorrentSwitch = FindSwitch(target, "ProxyBittorrent");
            await target.InvokeAsync(() => proxyBittorrentSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.ProxyBittorrent.Should().BeFalse();

            var proxyRssSwitch = FindSwitch(target, "ProxyRss");
            await target.InvokeAsync(() => proxyRssSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.ProxyRss.Should().BeFalse();

            var proxyMiscSwitch = FindSwitch(target, "ProxyMisc");
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

            var maxConnectionsSwitch = FindSwitch(target, "MaxConnecEnabled");
            await target.InvokeAsync(() => maxConnectionsSwitch.Instance.ValueChanged.InvokeAsync(false));
            FindNumericInt(target, "MaxConnec").Instance.Disabled.Should().BeTrue();

            var perTorrentSwitch = FindSwitch(target, "MaxConnecPerTorrentEnabled");
            await target.InvokeAsync(() => perTorrentSwitch.Instance.ValueChanged.InvokeAsync(false));
            FindNumericInt(target, "MaxConnecPerTorrent").Instance.Disabled.Should().BeTrue();

            var maxUploadsSwitch = FindSwitch(target, "MaxUploadsEnabled");
            await target.InvokeAsync(() => maxUploadsSwitch.Instance.ValueChanged.InvokeAsync(false));
            FindNumericInt(target, "MaxUploads").Instance.Disabled.Should().BeTrue();

            var uploadsPerTorrentSwitch = FindSwitch(target, "MaxUploadsPerTorrentEnabled");
            await target.InvokeAsync(() => uploadsPerTorrentSwitch.Instance.ValueChanged.InvokeAsync(false));
            FindNumericInt(target, "MaxUploadsPerTorrent").Instance.Disabled.Should().BeTrue();
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

            var protocolSelect = FindSelect<int>(target, "BittorrentProtocol");
            await target.InvokeAsync(() => protocolSelect.Instance.ValueChanged.InvokeAsync(1));
            update.BittorrentProtocol.Should().Be(1);

            var ipFilterSwitch = FindSwitch(target, "IpFilterEnabled");
            await target.InvokeAsync(() => ipFilterSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.IpFilterEnabled.Should().BeFalse();

            var filterPathField = FindTextField(target, "IpFilterPath");
            await target.InvokeAsync(() => filterPathField.Instance.ValueChanged.InvokeAsync("/new/filter.dat"));
            update.IpFilterPath.Should().Be("/new/filter.dat");

            var trackersSwitch = FindSwitch(target, "IpFilterTrackers");
            await target.InvokeAsync(() => trackersSwitch.Instance.ValueChanged.InvokeAsync(false));
            update.IpFilterTrackers.Should().BeFalse();

            var bannedField = FindTextField(target, "BannedIPs");
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

            var numericField = FindNumericInt(target, "ListenPort");
            await target.InvokeAsync(() => numericField.Instance.OnAdornmentClick.InvokeAsync());

            update.ListenPort.Should().BeGreaterThanOrEqualTo(1024);
            update.ListenPort.Should().BeLessThanOrEqualTo(65535);
        }

        private static IRenderedComponent<TComponent> FindComponentByTestId<TComponent>(IRenderedComponent<ConnectionOptions> target, string testId)
            where TComponent : IComponent
        {
            return target.FindComponents<TComponent>()
                .Single(component => component.FindAll($"[data-test-id='{testId}']").Count > 0);
        }

        private static IRenderedComponent<FieldSwitch> FindSwitch(IRenderedComponent<ConnectionOptions> target, string testId)
        {
            return FindComponentByTestId<FieldSwitch>(target, testId);
        }

        private static IRenderedComponent<MudNumericField<int>> FindNumericInt(IRenderedComponent<ConnectionOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(target, testId);
        }

        private static IRenderedComponent<MudSelect<T>> FindSelect<T>(IRenderedComponent<ConnectionOptions> target, string testId)
        {
            return FindComponentByTestId<MudSelect<T>>(target, testId);
        }

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<ConnectionOptions> target, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(target, testId);
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
