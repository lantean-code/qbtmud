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
    public sealed class WebUIOptionsTests : RazorComponentTestBase<WebUIOptions>
    {
        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            var preferences = DeserializePreferences();

            TestContext.RenderComponent<MudPopoverProvider>();
            var update = new UpdatePreferences();

            var target = TestContext.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            FindTextField(target, "WebUiAddress").Instance.Value.Should().Be("example.com");
            FindNumeric(target, "WebUiPort").Instance.Value.Should().Be(9090);

            FindSwitch(target, "UseHttps").Instance.Value.Should().BeTrue();
            FindTextField(target, "WebUiHttpsCertPath").Instance.Disabled.Should().BeFalse();

            FindSwitch(target, "BypassAuthSubnetWhitelistEnabled").Instance.Value.Should().BeTrue();
            FindTextField(target, "BypassAuthSubnetWhitelist").Instance.Disabled.Should().BeFalse();

            FindSwitch(target, "WebUiHostHeaderValidationEnabled").Instance.Value.Should().BeTrue();
            FindTextField(target, "WebUiDomainList").Instance.Disabled.Should().BeFalse();

            FindSwitch(target, "WebUiUseCustomHttpHeadersEnabled").Instance.Value.Should().BeTrue();
            FindTextField(target, "WebUiCustomHttpHeaders").Instance.Disabled.Should().BeFalse();

            FindSwitch(target, "WebUiReverseProxyEnabled").Instance.Value.Should().BeTrue();
            FindTextField(target, "WebUiReverseProxiesList").Instance.Disabled.Should().BeFalse();

            FindSwitch(target, "DyndnsEnabled").Instance.Value.Should().BeTrue();
            FindSelect<int>(target, "DyndnsService").Instance.Value.Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_WebUiSettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            TestContext.RenderComponent<MudPopoverProvider>();

            var target = TestContext.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            await target.InvokeAsync(() => FindTextField(target, "WebUiAddress").Instance.ValueChanged.InvokeAsync("localhost"));
            await target.InvokeAsync(() => FindNumeric(target, "WebUiPort").Instance.ValueChanged.InvokeAsync(8081));
            await target.InvokeAsync(() => FindSwitch(target, "WebUiUpnp").Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => FindSwitch(target, "UseHttps").Instance.ValueChanged.InvokeAsync(false));

            var certField = FindTextField(target, "WebUiHttpsCertPath");
            certField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => certField.Instance.ValueChanged.InvokeAsync("/newcert.pem"));

            var keyField = FindTextField(target, "WebUiHttpsKeyPath");
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

            TestContext.RenderComponent<MudPopoverProvider>();

            var target = TestContext.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var usernameField = FindTextField(target, "WebUiUsername");
            await target.InvokeAsync(() => usernameField.Instance.ValueChanged.InvokeAsync("root"));

            var passwordField = FindTextField(target, "WebUiPassword");
            await target.InvokeAsync(() => passwordField.Instance.ValueChanged.InvokeAsync("newpass"));

            var localBypass = FindSwitch(target, "BypassLocalAuth");
            await target.InvokeAsync(() => localBypass.Instance.ValueChanged.InvokeAsync(false));

            var subnetBypass = FindSwitch(target, "BypassAuthSubnetWhitelistEnabled");
            await target.InvokeAsync(() => subnetBypass.Instance.ValueChanged.InvokeAsync(false));

            var subnetField = FindTextField(target, "BypassAuthSubnetWhitelist");
            subnetField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => subnetField.Instance.ValueChanged.InvokeAsync("192.168.0.0/16"));

            var failCountField = FindNumeric(target, "WebUiMaxAuthFailCount");
            await target.InvokeAsync(() => failCountField.Instance.ValueChanged.InvokeAsync(7));

            var banDurationField = FindNumeric(target, "WebUiBanDuration");
            await target.InvokeAsync(() => banDurationField.Instance.ValueChanged.InvokeAsync(120));

            var sessionField = FindNumeric(target, "WebUiSessionTimeout");
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

            TestContext.RenderComponent<MudPopoverProvider>();

            var target = TestContext.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var altSwitch = FindSwitch(target, "AlternativeWebuiEnabled");
            await target.InvokeAsync(() => altSwitch.Instance.ValueChanged.InvokeAsync(false));

            var altPathField = FindTextField(target, "AlternativeWebuiPath");
            await target.InvokeAsync(() => altPathField.Instance.ValueChanged.InvokeAsync("/alt/ui"));

            var clickSwitch = FindSwitch(target, "WebUiClickjackingProtectionEnabled");
            await target.InvokeAsync(() => clickSwitch.Instance.ValueChanged.InvokeAsync(false));

            var csrfSwitch = FindSwitch(target, "WebUiCsrfProtectionEnabled");
            await target.InvokeAsync(() => csrfSwitch.Instance.ValueChanged.InvokeAsync(false));

            var secureSwitch = FindSwitch(target, "WebUiSecureCookieEnabled");
            await target.InvokeAsync(() => secureSwitch.Instance.ValueChanged.InvokeAsync(false));

            var hostSwitch = FindSwitch(target, "WebUiHostHeaderValidationEnabled");
            await target.InvokeAsync(() => hostSwitch.Instance.ValueChanged.InvokeAsync(false));

            var domainField = FindTextField(target, "WebUiDomainList");
            domainField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => domainField.Instance.ValueChanged.InvokeAsync("example.org"));

            var headerSwitch = FindSwitch(target, "WebUiUseCustomHttpHeadersEnabled");
            await target.InvokeAsync(() => headerSwitch.Instance.ValueChanged.InvokeAsync(false));

            var headersField = FindTextField(target, "WebUiCustomHttpHeaders");
            headersField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => headersField.Instance.ValueChanged.InvokeAsync("X-New: 2"));

            var reverseSwitch = FindSwitch(target, "WebUiReverseProxyEnabled");
            await target.InvokeAsync(() => reverseSwitch.Instance.ValueChanged.InvokeAsync(false));

            var reverseField = FindTextField(target, "WebUiReverseProxiesList");
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

            TestContext.RenderComponent<MudPopoverProvider>();

            var target = TestContext.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var enableSwitch = FindSwitch(target, "DyndnsEnabled");
            await target.InvokeAsync(() => enableSwitch.Instance.ValueChanged.InvokeAsync(false));

            var serviceSelect = FindSelect<int>(target, "DyndnsService");
            serviceSelect.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => serviceSelect.Instance.ValueChanged.InvokeAsync(1));

            var domainField = FindTextField(target, "DyndnsDomain");
            domainField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => domainField.Instance.ValueChanged.InvokeAsync("newdomain"));

            var userField = FindTextField(target, "DyndnsUsername");
            userField.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => userField.Instance.ValueChanged.InvokeAsync("newuser"));

            var passField = FindTextField(target, "DyndnsPassword");
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
            TestContext.RenderComponent<MudPopoverProvider>();

            var preferences = DeserializePreferences();
            var update = new UpdatePreferences();
            var target = TestContext.RenderComponent<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            await target.InvokeAsync(() => target.FindAll("button").First(b => b.TextContent.Contains("Register", StringComparison.Ordinal)).Click());

            var calls = TestContext.JSInterop.Invocations.Where(i => i.Identifier == "qbt.open").ToList();
            calls.Should().HaveCount(1);
            calls[0].Arguments[0].Should().Be("https://www.dyndns.com/account/services/hosts/add.html");

            var enableSwitch = FindSwitch(target, "DyndnsEnabled");
            await target.InvokeAsync(() => enableSwitch.Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => target.FindAll("button").First(b => b.TextContent.Contains("Register", StringComparison.Ordinal)).Click());
            calls = TestContext.JSInterop.Invocations.Where(i => i.Identifier == "qbt.open").ToList();
            calls.Should().HaveCount(1);

            await target.InvokeAsync(() => enableSwitch.Instance.ValueChanged.InvokeAsync(true));
            var serviceSelect = FindSelect<int>(target, "DyndnsService");
            await target.InvokeAsync(() => serviceSelect.Instance.ValueChanged.InvokeAsync(1));
            await target.InvokeAsync(() => target.FindAll("button").First(b => b.TextContent.Contains("Register", StringComparison.Ordinal)).Click());

            calls = TestContext.JSInterop.Invocations.Where(i => i.Identifier == "qbt.open").ToList();
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

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<WebUIOptions> target, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(target, testId);
        }

        private static IRenderedComponent<MudNumericField<int>> FindNumeric(IRenderedComponent<WebUIOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(target, testId);
        }

        private static IRenderedComponent<FieldSwitch> FindSwitch(IRenderedComponent<WebUIOptions> target, string testId)
        {
            return FindComponentByTestId<FieldSwitch>(target, testId);
        }

        private static IRenderedComponent<MudSelect<T>> FindSelect<T>(IRenderedComponent<WebUIOptions> target, string testId)
        {
            return FindComponentByTestId<MudSelect<T>>(target, testId);
        }
    }
}
