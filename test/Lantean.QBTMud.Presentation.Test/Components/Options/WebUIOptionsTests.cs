using System.Net;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Options;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Presentation.Test.Components.Options
{
    public sealed class WebUIOptionsTests : RazorComponentTestBase<WebUIOptions>
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("WebUiApiKey")]
        public void GIVEN_ApiKeyManagementSupported_WHEN_Rendered_THEN_ShouldShowReadOnlyMaskedKeyAndAvailableActions(string? apiKey)
        {
            var target = RenderApiKeyTarget(apiKey);

            var field = FindTextField(target, "WebUiApiKey").Instance;
            field.GetState(x => x.Value).Should().Be(apiKey ?? string.Empty);
            field.ReadOnly.Should().BeTrue();
            field.InputType.Should().Be(InputType.Password);
            FindButton(target, "ApiKeyRotate").Instance.Disabled.Should().BeFalse();
            FindButton(target, "ApiKeyDelete").Instance.Disabled.Should().Be(string.IsNullOrEmpty(apiKey));
            FindComponentByTestId<MudIconButton>(target, "ApiKeyCopy").Instance.Disabled.Should().Be(string.IsNullOrEmpty(apiKey));
            FindComponentByTestId<MudIconButton>(target, "ApiKeyVisibility").Instance.Disabled.Should().Be(string.IsNullOrEmpty(apiKey));
        }

        [Fact]
        public void GIVEN_ApiKeyManagementUnsupported_WHEN_Rendered_THEN_ShouldHideApiKeyControls()
        {
            var target = RenderApiKeyTarget("WebUiApiKey", supported: false);

            target.FindComponents<MudTextField<string>>().Should().NotContain(component => HasTestId(component, "WebUiApiKey"));
            target.FindComponents<MudButton>().Should().NotContain(component => HasTestId(component, "ApiKeyRotate"));
            target.FindComponents<MudButton>().Should().NotContain(component => HasTestId(component, "ApiKeyDelete"));
        }

        [Fact]
        public async Task GIVEN_ExistingApiKey_WHEN_VisibilityToggledAndCopied_THEN_ShouldRevealMaskAndCopyKey()
        {
            var target = RenderApiKeyTarget("WebUiApiKey");
            var visibility = FindComponentByTestId<MudIconButton>(target, "ApiKeyVisibility");

            await target.InvokeAsync(() => visibility.Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            FindTextField(target, "WebUiApiKey").Instance.InputType.Should().Be(InputType.Text);

            await target.InvokeAsync(() => visibility.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            await target.InvokeAsync(() => FindComponentByTestId<MudIconButton>(target, "ApiKeyCopy").Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            FindTextField(target, "WebUiApiKey").Instance.InputType.Should().Be(InputType.Password);
            Mock.Get(TestContext.Services.GetRequiredService<IClipboardService>()).Verify(service => service.WriteToClipboard("WebUiApiKey"), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("WebUiApiKey")]
        public async Task GIVEN_ConfirmedGenerationOrRotation_WHEN_Clicked_THEN_ShouldReplaceKeyImmediatelyAndPreserveItOnReset(string apiKey)
        {
            var target = RenderApiKeyTarget(apiKey);
            var apiClient = Mock.Get(TestContext.Services.GetRequiredService<IApiClient>());
            apiClient.Setup(client => client.RotateAPIKeyAsync(It.IsAny<CancellationToken>())).ReturnsSuccessAsync(new ApiKey("Key"));
            if (apiKey.Length > 0)
            {
                await target.InvokeAsync(() => FindComponentByTestId<MudIconButton>(target, "ApiKeyVisibility").Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            }

            await target.InvokeAsync(() => FindButton(target, "ApiKeyRotate").Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            await target.Instance.ResetAsync();

            FindTextField(target, "WebUiApiKey").Instance.GetState(x => x.Value).Should().Be("Key");
            FindTextField(target, "WebUiApiKey").Instance.InputType.Should().Be(InputType.Password);
            await target.InvokeAsync(() => FindComponentByTestId<MudIconButton>(target, "ApiKeyCopy").Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            Mock.Get(TestContext.Services.GetRequiredService<IClipboardService>()).Verify(service => service.WriteToClipboard("Key"), Times.Once);
            target.Instance.UpdatePreferences.WebUiApiKey.Should().BeNull();
            var title = apiKey.Length == 0 ? "Generate API key" : "Rotate API key";
            var confirmation = apiKey.Length == 0
                ? "Generate an API key? This key can be used to interact with qBittorrent's API."
                : "Rotate this API key? The current key will immediately stop working and a new key will be generated.";
            Mock.Get(TestContext.Services.GetRequiredService<IDialogWorkflow>()).Verify(service => service.ShowConfirmDialog(title, confirmation), Times.Once);
            apiClient.Verify(client => client.RotateAPIKeyAsync(It.IsAny<CancellationToken>()), Times.Once);
            apiClient.Verify(client => client.SetApplicationPreferencesAsync(It.IsAny<UpdatePreferences>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_ConfirmedDeletion_WHEN_Clicked_THEN_ShouldClearKeyImmediatelyAndPreserveDeletionOnReset()
        {
            var target = RenderApiKeyTarget("WebUiApiKey");
            var apiClient = Mock.Get(TestContext.Services.GetRequiredService<IApiClient>());
            apiClient.Setup(client => client.DeleteAPIKeyAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ApiResult.CreateSuccess());
            await target.InvokeAsync(() => FindComponentByTestId<MudIconButton>(target, "ApiKeyVisibility").Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            await target.InvokeAsync(() => FindButton(target, "ApiKeyDelete").Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            await target.Instance.ResetAsync();

            FindTextField(target, "WebUiApiKey").Instance.GetState(x => x.Value).Should().BeEmpty();
            FindTextField(target, "WebUiApiKey").Instance.InputType.Should().Be(InputType.Password);
            FindButton(target, "ApiKeyDelete").Instance.Disabled.Should().BeTrue();
            FindComponentByTestId<MudIconButton>(target, "ApiKeyCopy").Instance.Disabled.Should().BeTrue();
            FindButton(target, "ApiKeyRotate").Instance.Disabled.Should().BeFalse();
            target.Instance.UpdatePreferences.WebUiApiKey.Should().BeNull();
            Mock.Get(TestContext.Services.GetRequiredService<IDialogWorkflow>()).Verify(service => service.ShowConfirmDialog("Delete API key", "Delete this API key? The current key will immediately stop working."), Times.Once);
            apiClient.Verify(client => client.DeleteAPIKeyAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(false, "")]
        [InlineData(false, "WebUiApiKey")]
        [InlineData(true, "WebUiApiKey")]
        public async Task GIVEN_ConfirmationDeclined_WHEN_KeyActionClicked_THEN_ShouldKeepKeyAndSkipApiCall(bool delete, string apiKey)
        {
            var target = RenderApiKeyTarget(apiKey);
            Mock.Get(TestContext.Services.GetRequiredService<IDialogWorkflow>())
                .Setup(service => service.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);

            await target.InvokeAsync(() => FindButton(target, delete ? "ApiKeyDelete" : "ApiKeyRotate").Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            FindTextField(target, "WebUiApiKey").Instance.GetState(x => x.Value).Should().Be(apiKey);
            FindButton(target, "ApiKeyRotate").Instance.Disabled.Should().BeFalse();
            var apiClient = Mock.Get(TestContext.Services.GetRequiredService<IApiClient>());
            apiClient.Verify(client => client.RotateAPIKeyAsync(It.IsAny<CancellationToken>()), Times.Never);
            apiClient.Verify(client => client.DeleteAPIKeyAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_ApiFailure_WHEN_KeyActionClicked_THEN_ShouldKeepKeyAndShowFailureFeedback(bool delete)
        {
            var target = RenderApiKeyTarget("WebUiApiKey");
            var apiClient = Mock.Get(TestContext.Services.GetRequiredService<IApiClient>());
            apiClient.Setup(client => client.RotateAPIKeyAsync(It.IsAny<CancellationToken>())).ReturnsFailure(ApiFailureKind.ServerError, "Failure", HttpStatusCode.InternalServerError);
            apiClient.Setup(client => client.DeleteAPIKeyAsync(It.IsAny<CancellationToken>())).ReturnsFailure(ApiFailureKind.ServerError, "Failure", HttpStatusCode.InternalServerError);

            await target.InvokeAsync(() => FindButton(target, delete ? "ApiKeyDelete" : "ApiKeyRotate").Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            FindTextField(target, "WebUiApiKey").Instance.GetState(x => x.Value).Should().Be("WebUiApiKey");
            FindButton(target, "ApiKeyRotate").Instance.Disabled.Should().BeFalse();
            FindButton(target, "ApiKeyDelete").Instance.Disabled.Should().BeFalse();
            var message = delete ? "Unable to delete API key." : "Unable to rotate API key.";
            Mock.Get(TestContext.Services.GetRequiredService<IApiFeedbackWorkflow>()).Verify(service => service.HandleFailureAsync(
                It.Is<ApiResultBase>(result => result.IsFailure),
                It.Is<Func<string?, string>>(builder => builder(null) == message),
                Severity.Error,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GIVEN_PendingConfirmation_WHEN_KeyActionsClickedAgain_THEN_ShouldDisableControlsAndPreventDuplicateOperations(bool delete)
        {
            var target = RenderApiKeyTarget("WebUiApiKey");
            var confirmation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var dialog = Mock.Get(TestContext.Services.GetRequiredService<IDialogWorkflow>());
            dialog.Setup(service => service.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(confirmation.Task);
            var rotate = FindButton(target, "ApiKeyRotate");
            var remove = FindButton(target, "ApiKeyDelete");

            var pending = target.InvokeAsync(() => (delete ? remove : rotate).Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            target.WaitForAssertion(() => rotate.Instance.Disabled.Should().BeTrue());
            remove.Instance.Disabled.Should().BeTrue();
            FindComponentByTestId<MudIconButton>(target, "ApiKeyCopy").Instance.Disabled.Should().BeTrue();
            FindComponentByTestId<MudIconButton>(target, "ApiKeyVisibility").Instance.Disabled.Should().BeTrue();
            await target.InvokeAsync(() => rotate.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            await target.InvokeAsync(() => remove.Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            confirmation.SetResult(false);
            await pending;

            rotate.Instance.Disabled.Should().BeFalse();
            dialog.Verify(service => service.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        private IRenderedComponent<WebUIOptions> RenderApiKeyTarget(string? apiKey, bool supported = true)
        {
            var capabilities = new Mock<IWebApiCapabilityService>();
            capabilities.Setup(service => service.GetCapabilityStateAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WebApiCapabilityState(new Version(2, 14, 1), true, supportsApiKeyManagement: supported));
            var dialog = new Mock<IDialogWorkflow>();
            dialog.Setup(service => service.ShowConfirmDialog(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            TestContext.Services.AddSingleton(capabilities.Object);
            TestContext.Services.AddSingleton(dialog.Object);
            TestContext.Services.AddSingleton(new Mock<IApiClient>().Object);
            TestContext.Services.AddSingleton(new Mock<IApiFeedbackWorkflow>().Object);
            TestContext.Services.AddSingleton(new Mock<IClipboardService>().Object);
            TestContext.Render<MudPopoverProvider>();

            return TestContext.Render<WebUIOptions>(parameters =>
            {
                parameters.Add(component => component.Preferences, apiKey is null ? null : PreferencesFactory.CreatePreferences(spec => spec.WebUiApiKey = apiKey));
                parameters.Add(component => component.UpdatePreferences, new UpdatePreferences());
            });
        }

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
