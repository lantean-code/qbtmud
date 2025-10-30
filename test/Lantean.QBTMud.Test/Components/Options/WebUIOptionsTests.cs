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
    public sealed class WebUIOptionsTests : IDisposable
    {
        private readonly ComponentTestContext _context;

        public WebUIOptionsTests()
        {
            _context = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            var preferences = DeserializePreferences();

            _context.RenderComponent<MudPopoverProvider>();
            var update = new UpdatePreferences();

            var target = _context.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Host").Instance.Value.Should().Be("example.com");
            target.FindComponents<MudNumericField<int>>().First(f => f.Instance.Label == "Port").Instance.Value.Should().Be(9090);

            target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Use HTTPS instead of HTTP").Instance.Value.Should().BeTrue();
            target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Certificate").Instance.Disabled.Should().BeFalse();

            target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Bypass authentication for clients in whitelisted IP subnets").Instance.Value.Should().BeTrue();
            target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "10.0.0.0/8").Instance.Disabled.Should().BeFalse();

            target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable Host header validation").Instance.Value.Should().BeTrue();
            target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "domain1\n.domain2").Instance.Disabled.Should().BeFalse();

            target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Add custom HTTP headers").Instance.Value.Should().BeTrue();
            target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "X-Test: 1").Instance.Disabled.Should().BeFalse();

            target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable reverse proxy support").Instance.Value.Should().BeTrue();
            target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "proxy1").Instance.Disabled.Should().BeFalse();

            target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Update my dynamic domain name").Instance.Value.Should().BeTrue();
            target.FindComponents<MudSelect<int>>()[0].Instance.Value.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_WebUiSettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            _context.RenderComponent<MudPopoverProvider>();

            var target = _context.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var hostField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Host");
            await target.InvokeAsync(() => hostField.Instance.ValueChanged.InvokeAsync("localhost"));

            var portField = target.FindComponents<MudNumericField<int>>().First(f => f.Instance.Label == "Port");
            await target.InvokeAsync(() => portField.Instance.ValueChanged.InvokeAsync(8081));

            var upnpSwitch = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Use UPnP / NAT-PMP to forward the port from my router");
            await target.InvokeAsync(() => upnpSwitch.Instance.ValueChanged.InvokeAsync(false));

            var httpsSwitch = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Use HTTPS instead of HTTP");
            await target.InvokeAsync(() => httpsSwitch.Instance.ValueChanged.InvokeAsync(false));

            var certField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Certificate");
            certField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => certField.Instance.ValueChanged.InvokeAsync("/newcert.pem"));

            var keyField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Key");
            await target.InvokeAsync(() => keyField.Instance.ValueChanged.InvokeAsync("/newkey.pem"));

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

            _context.RenderComponent<MudPopoverProvider>();

            var target = _context.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var usernameField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Username" && tf.Instance.Value == "admin");
            await target.InvokeAsync(() => usernameField.Instance.ValueChanged.InvokeAsync("root"));

            var passwordField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Password" && tf.Instance.Value == "secret!");
            await target.InvokeAsync(() => passwordField.Instance.ValueChanged.InvokeAsync("newpass"));

            var localBypass = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Bypass authentication for clients on localhost");
            await target.InvokeAsync(() => localBypass.Instance.ValueChanged.InvokeAsync(false));

            var subnetBypass = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Bypass authentication for clients in whitelisted IP subnets");
            await target.InvokeAsync(() => subnetBypass.Instance.ValueChanged.InvokeAsync(false));

            var subnetField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "10.0.0.0/8");
            subnetField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => subnetField.Instance.ValueChanged.InvokeAsync("192.168.0.0/16"));

            var failCountField = target.FindComponents<MudNumericField<int>>().First(f => f.Instance.Label == "Ban client after consecutive failures");
            await target.InvokeAsync(() => failCountField.Instance.ValueChanged.InvokeAsync(7));

            var banDurationField = target.FindComponents<MudNumericField<int>>().First(f => f.Instance.Label == "ban for");
            await target.InvokeAsync(() => banDurationField.Instance.ValueChanged.InvokeAsync(120));

            var sessionField = target.FindComponents<MudNumericField<int>>().First(f => f.Instance.Label == "Session timeout");
            await target.InvokeAsync(() => sessionField.Instance.ValueChanged.InvokeAsync(7200));

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

            _context.RenderComponent<MudPopoverProvider>();

            var target = _context.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var altSwitch = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Use alternative Web UI");
            await target.InvokeAsync(() => altSwitch.Instance.ValueChanged.InvokeAsync(false));

            var altPathField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Files location");
            await target.InvokeAsync(() => altPathField.Instance.ValueChanged.InvokeAsync("/alt/ui"));

            var clickSwitch = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable clickjacking protection");
            await target.InvokeAsync(() => clickSwitch.Instance.ValueChanged.InvokeAsync(false));

            var csrfSwitch = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable Cross-Site Request Forgery (CSRF) protection");
            await target.InvokeAsync(() => csrfSwitch.Instance.ValueChanged.InvokeAsync(false));

            var secureSwitch = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable cookie Secure flag (requires HTTPS)");
            await target.InvokeAsync(() => secureSwitch.Instance.ValueChanged.InvokeAsync(false));

            var hostSwitch = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable Host header validation");
            await target.InvokeAsync(() => hostSwitch.Instance.ValueChanged.InvokeAsync(false));

            var domainField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "domain1\n.domain2");
            domainField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => domainField.Instance.ValueChanged.InvokeAsync("example.org"));

            var headerSwitch = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Add custom HTTP headers");
            await target.InvokeAsync(() => headerSwitch.Instance.ValueChanged.InvokeAsync(false));

            var headersField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "X-Test: 1");
            headersField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => headersField.Instance.ValueChanged.InvokeAsync("X-New: 2"));

            var reverseSwitch = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Enable reverse proxy support");
            await target.InvokeAsync(() => reverseSwitch.Instance.ValueChanged.InvokeAsync(false));

            var reverseField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Value == "proxy1");
            reverseField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => reverseField.Instance.ValueChanged.InvokeAsync("proxy2"));

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

            _context.RenderComponent<MudPopoverProvider>();

            var target = _context.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var enableSwitch = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Update my dynamic domain name");
            await target.InvokeAsync(() => enableSwitch.Instance.ValueChanged.InvokeAsync(false));

            var serviceSelect = target.FindComponents<MudSelect<int>>()[0];
            serviceSelect.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => serviceSelect.Instance.ValueChanged.InvokeAsync(1));

            var domainField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Domain name");
            domainField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => domainField.Instance.ValueChanged.InvokeAsync("newdomain"));

            var userField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Username" && tf.Instance.Value == "user");
            userField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => userField.Instance.ValueChanged.InvokeAsync("newuser"));

            var passField = target.FindComponents<MudTextField<string>>().First(tf => tf.Instance.Label == "Password" && tf.Instance.Value == "pass");
            passField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => passField.Instance.ValueChanged.InvokeAsync("newpass"));

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
            _context.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var target = _context.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            await target.InvokeAsync(() => target.FindAll("button").First(b => b.TextContent.Contains("Register", StringComparison.Ordinal)).Click());

            var calls = _context.JSInterop.Invocations.Where(i => i.Identifier == "qbt.open").ToList();
            calls.Should().HaveCount(1);
            calls[0].Arguments[0].Should().Be("https://www.dyndns.com/account/services/hosts/add.html");

            var enableSwitch = target.FindComponents<FieldSwitch>().First(s => s.Instance.Label == "Update my dynamic domain name");
            await target.InvokeAsync(() => enableSwitch.Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => target.FindAll("button").First(b => b.TextContent.Contains("Register", StringComparison.Ordinal)).Click());
            calls = _context.JSInterop.Invocations.Where(i => i.Identifier == "qbt.open").ToList();
            calls.Should().HaveCount(1);

            await target.InvokeAsync(() => enableSwitch.Instance.ValueChanged.InvokeAsync(true));
            var serviceSelect = target.FindComponents<MudSelect<int>>()[0];
            await target.InvokeAsync(() => serviceSelect.Instance.ValueChanged.InvokeAsync(1));
            await target.InvokeAsync(() => target.FindAll("button").First(b => b.TextContent.Contains("Register", StringComparison.Ordinal)).Click());

            calls = _context.JSInterop.Invocations.Where(i => i.Identifier == "qbt.open").ToList();
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
            _context.Dispose();
        }
    }
}