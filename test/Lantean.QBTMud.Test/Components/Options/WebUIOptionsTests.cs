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
using MudBlazor;
using Xunit;

namespace Lantean.QBTMud.Test.Components.Options
{
    public sealed class WebUIOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public WebUIOptionsTests()
        {
            _target = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            var preferences = DeserializePreferences();

            _target.RenderComponent<MudPopoverProvider>();
            var update = new UpdatePreferences();

            var cut = _target.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Host").Instance.Value.Should().Be("example.com");
            cut.FindComponents<MudNumericField<int>>().First(f => f.Instance.Label == "Port").Instance.Value.Should().Be(9090);

            cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Use HTTPS instead of HTTP").Instance.Value.Should().BeTrue();
            cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Certificate").Instance.Disabled.Should().BeFalse();

            cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Bypass authentication for clients in whitelisted IP subnets").Instance.Value.Should().BeTrue();
            cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "10.0.0.0/8").Instance.Disabled.Should().BeFalse();

            cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable Host header validation").Instance.Value.Should().BeTrue();
            cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "domain1\n.domain2").Instance.Disabled.Should().BeFalse();

            cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Add custom HTTP headers").Instance.Value.Should().BeTrue();
            cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "X-Test: 1").Instance.Disabled.Should().BeFalse();

            cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable reverse proxy support").Instance.Value.Should().BeTrue();
            cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "proxy1").Instance.Disabled.Should().BeFalse();

            cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Update my dynamic domain name").Instance.Value.Should().BeTrue();
            cut.FindComponents<MudSelect<int>>().First().Instance.Value.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_WebUiSettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var hostField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Host");
            await cut.InvokeAsync(() => hostField.Instance.ValueChanged.InvokeAsync("localhost"));

            var portField = cut.FindComponents<MudNumericField<int>>().First(f => f.Instance.Label == "Port");
            await cut.InvokeAsync(() => portField.Instance.ValueChanged.InvokeAsync(8081));

            var upnpSwitch = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Use UPnP / NAT-PMP to forward the port from my router");
            await cut.InvokeAsync(() => upnpSwitch.Instance.ValueChanged.InvokeAsync(false));

            var httpsSwitch = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Use HTTPS instead of HTTP");
            await cut.InvokeAsync(() => httpsSwitch.Instance.ValueChanged.InvokeAsync(false));

            var certField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Certificate");
            certField.Instance.Disabled.Should().BeTrue();
            await cut.InvokeAsync(() => certField.Instance.ValueChanged.InvokeAsync("/newcert.pem"));

            var keyField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Key");
            await cut.InvokeAsync(() => keyField.Instance.ValueChanged.InvokeAsync("/newkey.pem"));

            update.WebUiAddress.Should().Be("localhost");
            update.WebUiPort.Should().Be(8081);
            update.WebUiUpnp.Should().BeFalse();
            update.UseHttps.Should().BeFalse();
            update.WebUiHttpsCertPath.Should().Be("/newcert.pem");
            update.WebUiHttpsKeyPath.Should().Be("/newkey.pem");

            events.Should().NotBeEmpty();
            events.Should().AllSatisfy(evt => evt.Should().BeSameAs(update));
        }

        [Fact]
        public async Task GIVEN_AuthenticationSettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var usernameField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Username" && tf.Instance.Value == "admin");
            await cut.InvokeAsync(() => usernameField.Instance.ValueChanged.InvokeAsync("root"));

            var passwordField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Password" && tf.Instance.Value == "secret!");
            await cut.InvokeAsync(() => passwordField.Instance.ValueChanged.InvokeAsync("newpass"));

            var localBypass = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Bypass authentication for clients on localhost");
            await cut.InvokeAsync(() => localBypass.Instance.ValueChanged.InvokeAsync(false));

            var subnetBypass = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Bypass authentication for clients in whitelisted IP subnets");
            await cut.InvokeAsync(() => subnetBypass.Instance.ValueChanged.InvokeAsync(false));

            var subnetField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "10.0.0.0/8");
            subnetField.Instance.Disabled.Should().BeTrue();
            await cut.InvokeAsync(() => subnetField.Instance.ValueChanged.InvokeAsync("192.168.0.0/16"));

            var failCountField = cut.FindComponents<MudNumericField<int>>().First(f => f.Instance.Label == "Ban client after consecutive failures");
            await cut.InvokeAsync(() => failCountField.Instance.ValueChanged.InvokeAsync(7));

            var banDurationField = cut.FindComponents<MudNumericField<int>>().First(f => f.Instance.Label == "ban for");
            await cut.InvokeAsync(() => banDurationField.Instance.ValueChanged.InvokeAsync(120));

            var sessionField = cut.FindComponents<MudNumericField<int>>().First(f => f.Instance.Label == "Session timeout");
            await cut.InvokeAsync(() => sessionField.Instance.ValueChanged.InvokeAsync(7200));

            update.WebUiUsername.Should().Be("root");
            update.WebUiPassword.Should().Be("newpass");
            update.BypassLocalAuth.Should().BeFalse();
            update.BypassAuthSubnetWhitelistEnabled.Should().BeFalse();
            update.BypassAuthSubnetWhitelist.Should().Be("192.168.0.0/16");
            update.WebUiMaxAuthFailCount.Should().Be(7);
            update.WebUiBanDuration.Should().Be(120);
            update.WebUiSessionTimeout.Should().Be(7200);

            events.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_SecurityAndHeaders_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var altSwitch = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Use alternative Web UI");
            await cut.InvokeAsync(() => altSwitch.Instance.ValueChanged.InvokeAsync(false));

            var altPathField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Files location");
            await cut.InvokeAsync(() => altPathField.Instance.ValueChanged.InvokeAsync("/alt/ui"));

            var clickSwitch = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable clickjacking protection");
            await cut.InvokeAsync(() => clickSwitch.Instance.ValueChanged.InvokeAsync(false));

            var csrfSwitch = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable Cross-Site Request Forgery (CSRF) protection");
            await cut.InvokeAsync(() => csrfSwitch.Instance.ValueChanged.InvokeAsync(false));

            var secureSwitch = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable cookie Secure flag (requires HTTPS)");
            await cut.InvokeAsync(() => secureSwitch.Instance.ValueChanged.InvokeAsync(false));

            var hostSwitch = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable Host header validation");
            await cut.InvokeAsync(() => hostSwitch.Instance.ValueChanged.InvokeAsync(false));

            var domainField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "domain1\n.domain2");
            domainField.Instance.Disabled.Should().BeTrue();
            await cut.InvokeAsync(() => domainField.Instance.ValueChanged.InvokeAsync("example.org"));

            var headerSwitch = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Add custom HTTP headers");
            await cut.InvokeAsync(() => headerSwitch.Instance.ValueChanged.InvokeAsync(false));

            var headersField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "X-Test: 1");
            headersField.Instance.Disabled.Should().BeTrue();
            await cut.InvokeAsync(() => headersField.Instance.ValueChanged.InvokeAsync("X-New: 2"));

            var reverseSwitch = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable reverse proxy support");
            await cut.InvokeAsync(() => reverseSwitch.Instance.ValueChanged.InvokeAsync(false));

            var reverseField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "proxy1");
            reverseField.Instance.Disabled.Should().BeTrue();
            await cut.InvokeAsync(() => reverseField.Instance.ValueChanged.InvokeAsync("proxy2"));

            update.AlternativeWebuiEnabled.Should().BeFalse();
            update.AlternativeWebuiPath.Should().Be("/alt/ui");
            update.WebUiClickjackingProtectionEnabled.Should().BeFalse();
            update.WebUiCsrfProtectionEnabled.Should().BeFalse();
            update.WebUiSecureCookieEnabled.Should().BeFalse();
            update.WebUiHostHeaderValidationEnabled.Should().BeFalse();
            update.WebUiDomainList.Should().Be("example.org");
            update.WebUiUseCustomHttpHeadersEnabled.Should().BeFalse();
            update.WebUiCustomHttpHeaders.Should().Be("X-New: 2");
            update.WebUiReverseProxyEnabled.Should().BeFalse();
            update.WebUiReverseProxiesList.Should().Be("proxy2");

            events.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_DyndnsSettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            _target.RenderComponent<MudPopoverProvider>();

            var cut = _target.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var enableSwitch = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Update my dynamic domain name");
            await cut.InvokeAsync(() => enableSwitch.Instance.ValueChanged.InvokeAsync(false));

            var serviceSelect = cut.FindComponents<MudSelect<int>>().First();
            serviceSelect.Instance.Disabled.Should().BeTrue();
            await cut.InvokeAsync(() => serviceSelect.Instance.ValueChanged.InvokeAsync(1));

            var domainField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Domain name");
            domainField.Instance.Disabled.Should().BeTrue();
            await cut.InvokeAsync(() => domainField.Instance.ValueChanged.InvokeAsync("newdomain"));

            var userField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Username" && tf.Instance.Value == "user");
            userField.Instance.Disabled.Should().BeTrue();
            await cut.InvokeAsync(() => userField.Instance.ValueChanged.InvokeAsync("newuser"));

            var passField = cut.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Password" && tf.Instance.Value == "pass");
            passField.Instance.Disabled.Should().BeTrue();
            await cut.InvokeAsync(() => passField.Instance.ValueChanged.InvokeAsync("newpass"));

            update.DyndnsEnabled.Should().BeFalse();
            update.DyndnsService.Should().Be(1);
            update.DyndnsDomain.Should().Be("newdomain");
            update.DyndnsUsername.Should().Be("newuser");
            update.DyndnsPassword.Should().Be("newpass");

            events.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_RegisterButton_WHEN_Clicked_THEN_ShouldInvokeJs()
        {
            _target.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var cut = _target.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("Register", StringComparison.Ordinal)).Click());

            var calls = _target.JSInterop.Invocations.Where(i => i.Identifier == "qbt.open").ToList();
            calls.Should().HaveCount(1);
            calls[0].Arguments[0].Should().Be("https://www.dyndns.com/account/services/hosts/add.html");

            var enableSwitch = cut.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Update my dynamic domain name");
            await cut.InvokeAsync(() => enableSwitch.Instance.ValueChanged.InvokeAsync(false));
            await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("Register", StringComparison.Ordinal)).Click());
            calls = _target.JSInterop.Invocations.Where(i => i.Identifier == "qbt.open").ToList();
            calls.Should().HaveCount(1);

            await cut.InvokeAsync(() => enableSwitch.Instance.ValueChanged.InvokeAsync(true));
            var serviceSelect = cut.FindComponents<MudSelect<int>>().First();
            await cut.InvokeAsync(() => serviceSelect.Instance.ValueChanged.InvokeAsync(1));
            await cut.InvokeAsync(() => cut.FindAll("button").First(b => b.TextContent.Contains("Register", StringComparison.Ordinal)).Click());

            calls = _target.JSInterop.Invocations.Where(i => i.Identifier == "qbt.open").ToList();
            calls.Should().HaveCount(2);
            calls[1].Arguments[0].Should().Be("http://www.no-ip.com/services/managed_dns/free_dynamic_dns.html");
        }

        private static Preferences DeserializePreferences()
        {
            const string json = """
            {
                "locale": "en",
                "performance_warning": false,
                "web_ui_domain_list": "domain1\n.domain2",
                "web_ui_address": "example.com",
                "web_ui_port": 9090,
                "web_ui_upnp": true,
                "use_https": true,
                "web_ui_https_cert_path": "/cert.pem",
                "web_ui_https_key_path": "/key.pem",
                "web_ui_username": "admin",
                "web_ui_password": "secret!",
                "bypass_local_auth": true,
                "bypass_auth_subnet_whitelist_enabled": true,
                "bypass_auth_subnet_whitelist": "10.0.0.0/8",
                "web_ui_max_auth_fail_count": 5,
                "web_ui_ban_duration": 60,
                "web_ui_session_timeout": 3600,
                "alternative_webui_enabled": true,
                "alternative_webui_path": "/var/ui",
                "web_ui_clickjacking_protection_enabled": true,
                "web_ui_csrf_protection_enabled": true,
                "web_ui_secure_cookie_enabled": true,
                "web_ui_host_header_validation_enabled": true,
                "web_ui_use_custom_http_headers_enabled": true,
                "web_ui_custom_http_headers": "X-Test: 1",
                "web_ui_reverse_proxy_enabled": true,
                "web_ui_reverse_proxies_list": "proxy1",
                "dyndns_enabled": true,
                "dyndns_service": 0,
                "dyndns_domain": "example.com",
                "dyndns_username": "user",
                "dyndns_password": "pass"
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
