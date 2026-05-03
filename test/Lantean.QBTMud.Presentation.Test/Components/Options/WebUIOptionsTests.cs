using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Presentation.Test.Components.Options
{
    public sealed class WebUIOptionsTests : RazorComponentTestBase<WebUIOptions>
    {
        [Fact]
        public void GIVEN_Preferences_WHEN_Rendered_THEN_ShouldReflectState()
        {
            var preferences = CreatePreferences();

            TestContext.Render<MudPopoverProvider>();
            var update = new UpdatePreferences();

            var target = TestContext.Render<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            FindTextField(target, "WebUiAddress").Instance.GetState(x => x.Value).Should().Be("example.com");
            FindNumeric(target, "WebUiPort").Instance.GetState(x => x.Value).Should().Be(9090);

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
            FindSelect<DyndnsService>(target, "DyndnsService").Instance.GetState(x => x.Value).Should().Be(DyndnsService.DynDns);
        }

        [Fact]
        public async Task GIVEN_WebUiSettings_WHEN_Changed_THEN_ShouldUpdatePreferences()
        {
            var preferences = CreatePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<WebUIOptions>(parameters =>
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
            var preferences = CreatePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<WebUIOptions>(parameters =>
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
            var preferences = CreatePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<WebUIOptions>(parameters =>
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
            var preferences = CreatePreferences();
            var update = new UpdatePreferences();
            var events = new List<UpdatePreferences>();

            TestContext.Render<MudPopoverProvider>();

            var target = TestContext.Render<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, value => events.Add(value)));
            });

            var enableSwitch = FindSwitch(target, "DyndnsEnabled");
            await target.InvokeAsync(() => enableSwitch.Instance.ValueChanged.InvokeAsync(false));

            var serviceSelect = FindSelect<DyndnsService>(target, "DyndnsService");
            serviceSelect.Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => serviceSelect.Instance.ValueChanged.InvokeAsync(DyndnsService.NoIp));

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
            update.DyndnsService.Should().Be(DyndnsService.NoIp);
            update.DyndnsDomain.Should().Be("newdomain");
            update.DyndnsUsername.Should().Be("newuser");
            update.DyndnsPassword.Should().Be("newpass");

            events.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_RegisterButton_WHEN_Clicked_THEN_ShouldInvokeJs()
        {
            TestContext.Render<MudPopoverProvider>();
            var openInvocation = TestContext.JSInterop.SetupVoid("qbt.open", _ => true);
            openInvocation.SetVoidResult();

            var preferences = CreatePreferences();
            var update = new UpdatePreferences();
            var target = TestContext.Render<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var registerButton = FindComponentByTestId<MudButton>(target, "DyndnsRegister");
            await target.InvokeAsync(() => registerButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            var calls = openInvocation.Invocations.ToList();
            calls.Should().HaveCount(1);
            calls.Select(call => call.Arguments.OfType<string>().First())
                .Should()
                .Equal("https://www.dyndns.com/account/services/hosts/add.html");

            var enableSwitch = FindSwitch(target, "DyndnsEnabled");
            await target.InvokeAsync(() => enableSwitch.Instance.ValueChanged.InvokeAsync(false));
            await target.InvokeAsync(() => registerButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            calls = openInvocation.Invocations.ToList();
            calls.Should().HaveCount(1);

            await target.InvokeAsync(() => enableSwitch.Instance.ValueChanged.InvokeAsync(true));
            var serviceSelect = FindSelect<DyndnsService>(target, "DyndnsService");
            await target.InvokeAsync(() => serviceSelect.Instance.ValueChanged.InvokeAsync(DyndnsService.NoIp));
            await target.InvokeAsync(() => registerButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            calls = openInvocation.Invocations.ToList();
            calls.Should().HaveCount(2);
            calls.Select(call => call.Arguments.OfType<string>().First())
                .Should()
                .Equal(
                    "https://www.dyndns.com/account/services/hosts/add.html",
                    "http://www.no-ip.com/services/managed_dns/free_dynamic_dns.html");
        }

        [Fact]
        public async Task GIVEN_ValidationDelegates_WHEN_InvalidValuesProvided_THEN_ShouldReturnValidationMessages()
        {
            var preferences = CreatePreferences();
            var update = new UpdatePreferences();
            var target = TestContext.Render<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, update);
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var portValidation = FindNumeric(target, "WebUiPort").Instance.Validation.Should().BeOfType<Func<int, string?>>().Subject;
            portValidation(0).Should().Be("The port used for the WebUI must be between 1 and 65535.");
            portValidation(8080).Should().BeNull();

            var usernameValidation = FindTextField(target, "WebUiUsername").Instance.Validation.Should().BeOfType<Func<string?, string?>>().Subject;
            usernameValidation("ab").Should().Be("The WebUI username must be at least 3 characters long.");
            usernameValidation("admin").Should().BeNull();

            var passwordValidation = FindTextField(target, "WebUiPassword").Instance.Validation.Should().BeOfType<Func<string?, string?>>().Subject;
            passwordValidation("12345").Should().Be("The WebUI password must be at least 6 characters long.");
            passwordValidation("123456").Should().BeNull();

            var certValidation = FindTextField(target, "WebUiHttpsCertPath").Instance.Validation.Should().BeOfType<Func<string?, string?>>().Subject;
            certValidation(string.Empty).Should().Be("HTTPS certificate should not be empty.");
            certValidation("/cert.pem").Should().BeNull();

            var keyValidation = FindTextField(target, "WebUiHttpsKeyPath").Instance.Validation.Should().BeOfType<Func<string?, string?>>().Subject;
            keyValidation(string.Empty).Should().Be("HTTPS key should not be empty.");
            keyValidation("/key.pem").Should().BeNull();

            var altPathValidation = FindTextField(target, "AlternativeWebuiPath").Instance.Validation.Should().BeOfType<Func<string?, string?>>().Subject;
            altPathValidation(string.Empty).Should().Be("The alternative WebUI files location cannot be blank.");
            altPathValidation("/var/ui").Should().BeNull();

            await target.InvokeAsync(() => FindSwitch(target, "UseHttps").Instance.ValueChanged.InvokeAsync(false));
            certValidation(string.Empty).Should().BeNull();
            keyValidation(string.Empty).Should().BeNull();

            await target.InvokeAsync(() => FindSwitch(target, "AlternativeWebuiEnabled").Instance.ValueChanged.InvokeAsync(false));
            altPathValidation(string.Empty).Should().BeNull();
        }

        [Fact]
        public void GIVEN_NullPreferences_WHEN_Rendered_THEN_ShouldNotPopulateValuesOrThrow()
        {
            var target = TestContext.Render<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, null);
                parameters.Add(p => p.UpdatePreferences, new UpdatePreferences());
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            FindTextField(target, "WebUiAddress").Instance.GetState(x => x.Value).Should().BeNull();
            FindNumeric(target, "WebUiPort").Instance.GetState(x => x.Value).Should().Be(0);
        }

        [Fact]
        public async Task GIVEN_UnsupportedDyndnsService_WHEN_RegisterClicked_THEN_ShouldThrowInvalidOperationException()
        {
            TestContext.Render<MudPopoverProvider>();
            var preferences = CreateUnsupportedDyndnsPreferences();
            var target = TestContext.Render<WebUIOptions>(parameters =>
            {
                parameters.Add(p => p.Preferences, preferences);
                parameters.Add(p => p.UpdatePreferences, new UpdatePreferences());
                parameters.Add(p => p.PreferencesChanged, EventCallback.Factory.Create<UpdatePreferences>(this, _ => { }));
            });

            var registerButton = FindComponentByTestId<MudButton>(target, "DyndnsRegister");
            Func<Task> action = async () => await target.InvokeAsync(() => registerButton.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            await action.Should().ThrowAsync<InvalidOperationException>();
        }

        private static Preferences CreateUnsupportedDyndnsPreferences()
        {
            return PreferencesFactory.CreatePreferences(spec =>
            {
                spec.DyndnsEnabled = true;
                spec.DyndnsService = (DyndnsService)2;
            });
        }

        private static Preferences CreatePreferences()
        {
            return PreferencesFactory.CreatePreferences(spec =>
            {
                spec.AlternativeWebuiEnabled = true;
                spec.AlternativeWebuiPath = "/var/ui";
                spec.BypassAuthSubnetWhitelist = "10.0.0.0/8";
                spec.BypassAuthSubnetWhitelistEnabled = true;
                spec.BypassLocalAuth = true;
                spec.DyndnsDomain = "example.com";
                spec.DyndnsEnabled = true;
                spec.DyndnsPassword = "pass";
                spec.DyndnsService = DyndnsService.DynDns;
                spec.DyndnsUsername = "user";
                spec.Locale = "en";
                spec.PerformanceWarning = false;
                spec.UseHttps = true;
                spec.WebUiAddress = "example.com";
                spec.WebUiBanDuration = 60;
                spec.WebUiClickjackingProtectionEnabled = true;
                spec.WebUiCsrfProtectionEnabled = true;
                spec.WebUiCustomHttpHeaders = "X-Test: 1";
                spec.WebUiDomainList = "domain1\n.domain2";
                spec.WebUiHostHeaderValidationEnabled = true;
                spec.WebUiHttpsCertPath = "/cert.pem";
                spec.WebUiHttpsKeyPath = "/key.pem";
                spec.WebUiMaxAuthFailCount = 5;
                spec.WebUiPassword = "secret!";
                spec.WebUiPort = 9090;
                spec.WebUiReverseProxiesList = "proxy1";
                spec.WebUiReverseProxyEnabled = true;
                spec.WebUiSecureCookieEnabled = true;
                spec.WebUiSessionTimeout = 3600;
                spec.WebUiUpnp = true;
                spec.WebUiUseCustomHttpHeadersEnabled = true;
                spec.WebUiUsername = "admin";
            });
        }

        private static IRenderedComponent<MudTextField<string>> FindTextField(IRenderedComponent<WebUIOptions> target, string testId)
        {
            return FindComponentByTestId<MudTextField<string>>(target, testId);
        }

        private static IRenderedComponent<MudNumericField<int>> FindNumeric(IRenderedComponent<WebUIOptions> target, string testId)
        {
            return FindComponentByTestId<MudNumericField<int>>(target, testId);
        }

        private static IRenderedComponent<MudSelect<T>> FindSelect<T>(IRenderedComponent<WebUIOptions> target, string testId)
        {
            return FindComponentByTestId<MudSelect<T>>(target, testId);
        }
    }
}
