using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Components.Options
{
    public partial class WebUIOptions : Options
    {
        protected int WebUiSessionsCountLimit { get; private set; }

        // Key operations take effect immediately and must survive resetting pending preferences.
        private string? _apiKeyOverride;

        [Parameter]
        public string? ApiKey { get; set; }

        [Parameter]
        public EventCallback<string> ApiKeyChanged { get; set; }

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IApiFeedbackWorkflow ApiFeedbackWorkflow { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected IClipboardService ClipboardService { get; set; } = default!;

        [Inject]
        protected IWebApiCapabilityService WebApiCapabilityService { get; set; } = default!;

        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        protected bool SupportsApiKeyManagement { get; private set; }
        protected bool ApiKeyBusy { get; private set; }
        protected bool ApiKeyVisible { get; private set; }
        protected string WebUiApiKey => _apiKeyOverride ?? ApiKey ?? Preferences?.WebUiApiKey ?? string.Empty;
        protected bool HasApiKey => !string.IsNullOrEmpty(WebUiApiKey);

        protected string? Locale { get; private set; }
        protected bool? PerformanceWarning { get; private set; }
        protected string? WebUiDomainList { get; private set; }
        protected string? WebUiAddress { get; private set; }
        protected int WebUiPort { get; private set; }
        protected bool? WebUiUpnp { get; private set; }
        protected bool? UseHttps { get; private set; }
        protected string? WebUiHttpsCertPath { get; private set; }
        protected string? WebUiHttpsKeyPath { get; private set; }
        protected string? WebUiUsername { get; private set; }
        protected string? WebUiPassword { get; private set; }
        protected bool? BypassLocalAuth { get; private set; }
        protected bool? BypassAuthSubnetWhitelistEnabled { get; private set; }
        protected string? BypassAuthSubnetWhitelist { get; private set; }
        protected int WebUiMaxAuthFailCount { get; private set; }
        protected int WebUiBanDuration { get; private set; }
        protected int WebUiSessionTimeout { get; private set; }
        protected bool? AlternativeWebuiEnabled { get; private set; }
        protected string? AlternativeWebuiPath { get; private set; }
        protected bool? WebUiClickjackingProtectionEnabled { get; private set; }
        protected bool? WebUiCsrfProtectionEnabled { get; private set; }
        protected bool? WebUiSecureCookieEnabled { get; private set; }
        protected bool? WebUiHostHeaderValidationEnabled { get; private set; }
        protected bool? WebUiUseCustomHttpHeadersEnabled { get; private set; }
        protected string? WebUiCustomHttpHeaders { get; private set; }
        protected bool? WebUiReverseProxyEnabled { get; private set; }
        protected string? WebUiReverseProxiesList { get; private set; }
        protected bool? DyndnsEnabled { get; private set; }
        protected DyndnsService DyndnsService { get; private set; }
        protected string? DyndnsDomain { get; private set; }
        protected string? DyndnsUsername { get; private set; }
        protected string? DyndnsPassword { get; private set; }

        protected Func<int, string?> WebUiPortValidation => WebUiPortValidationFunc;

        protected Func<string?, string?> WebUiHttpsCertPathValidation => WebUiHttpsCertPathValidationFunc;

        protected Func<string?, string?> WebUiHttpsKeyPathValidation => WebUiHttpsKeyPathValidationFunc;

        protected Func<string?, string?> WebUiUsernameValidation => WebUiUsernameValidationFunc;

        protected Func<string?, string?> WebUiPasswordValidation => WebUiPasswordValidationFunc;

        protected Func<string?, string?> AlternativeWebuiPathValidation => AlternativeWebuiPathValidationFunc;

        protected override async Task OnInitializedAsync()
        {
            var capabilities = await WebApiCapabilityService.GetCapabilityStateAsync();
            SupportsApiKeyManagement = capabilities.SupportsApiKeyManagement;
        }

        protected void ToggleApiKeyVisibility()
        {
            ApiKeyVisible = !ApiKeyVisible;
        }

        protected async Task CopyApiKey()
        {
            await ClipboardService.WriteToClipboard(WebUiApiKey);
        }

        protected async Task RotateApiKey()
        {
            if (ApiKeyBusy)
            {
                return;
            }

            ApiKeyBusy = true;
            try
            {
                var title = HasApiKey ? "Rotate API key" : "Generate API key";
                var confirmation = HasApiKey
                    ? "Rotate this API key? The current key will immediately stop working and a new key will be generated."
                    : "Generate an API key? This key can be used to interact with qBittorrent's API.";
                if (!await DialogWorkflow.ShowConfirmDialog(
                    LanguageLocalizer.Translate("OptionsDialog", title),
                    LanguageLocalizer.Translate("confirmRotateAPIKeyDialog", confirmation)))
                {
                    return;
                }

                var result = await ApiClient.RotateAPIKeyAsync();
                if (result.IsFailure)
                {
                    await ApiFeedbackWorkflow.HandleFailureAsync(result, _ => LanguageLocalizer.Translate("HttpServer", "Unable to rotate API key."));
                    return;
                }

                _apiKeyOverride = result.Value.Key;
                ApiKeyVisible = false;
                await ApiKeyChanged.InvokeAsync(_apiKeyOverride);
            }
            finally
            {
                ApiKeyBusy = false;
            }
        }

        protected async Task DeleteApiKey()
        {
            if (ApiKeyBusy)
            {
                return;
            }

            ApiKeyBusy = true;
            try
            {
                if (!await DialogWorkflow.ShowConfirmDialog(
                    LanguageLocalizer.Translate("OptionsDialog", "Delete API key"),
                    LanguageLocalizer.Translate("confirmRotateAPIKeyDialog", "Delete this API key? The current key will immediately stop working.")))
                {
                    return;
                }

                var result = await ApiClient.DeleteAPIKeyAsync();
                if (result.IsFailure)
                {
                    await ApiFeedbackWorkflow.HandleFailureAsync(result, _ => LanguageLocalizer.Translate("HttpServer", "Unable to delete API key."));
                    return;
                }

                _apiKeyOverride = string.Empty;
                ApiKeyVisible = false;
                await ApiKeyChanged.InvokeAsync(_apiKeyOverride);
            }
            finally
            {
                ApiKeyBusy = false;
            }
        }

        private string? WebUiPortValidationFunc(int value)
        {
            if (value < 1 || value > MaxPortValue)
            {
                return LanguageLocalizer.Translate("HttpServer", "The port used for the WebUI must be between 1 and 65535.");
            }

            return null;
        }

        private string? WebUiUsernameValidationFunc(string? value)
        {
            if (value is null || value.Length < 3)
            {
                return LanguageLocalizer.Translate("OptionsDialog", "The WebUI username must be at least 3 characters long.");
            }

            return null;
        }

        private string? WebUiPasswordValidationFunc(string? value)
        {
            if (value is null || value.Length < 6)
            {
                return LanguageLocalizer.Translate("OptionsDialog", "The WebUI password must be at least 6 characters long.");
            }

            return null;
        }

        protected string? WebUiHttpsCertPathValidationFunc(string? value)
        {
            if (UseHttps != true)
            {
                return null;
            }

            if (value is not null && value.Length > 0)
            {
                return null;
            }

            return LanguageLocalizer.Translate("OptionsDialog", "HTTPS certificate should not be empty.");
        }

        protected string? WebUiHttpsKeyPathValidationFunc(string? value)
        {
            if (UseHttps != true)
            {
                return null;
            }

            if (value is not null && value.Length > 0)
            {
                return null;
            }

            return LanguageLocalizer.Translate("OptionsDialog", "HTTPS key should not be empty.");
        }

        protected string? AlternativeWebuiPathValidationFunc(string? value)
        {
            if (AlternativeWebuiEnabled != true)
            {
                return null;
            }

            if (value is not null && value.Length > 0)
            {
                return null;
            }

            return LanguageLocalizer.Translate("OptionsDialog", "The alternative WebUI files location cannot be blank.");
        }


        protected async Task WebUiSessionsCountLimitChanged(int value)
        {
            WebUiSessionsCountLimit = value;
            UpdatePreferences.WebUiSessionsCountLimit = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected override bool SetOptions()
        {
            if (Preferences is null)
            {
                return false;
            }

            Locale = Preferences.Locale;
            PerformanceWarning = Preferences.PerformanceWarning;
            WebUiDomainList = Preferences.WebUiDomainList;
            WebUiAddress = Preferences.WebUiAddress;
            WebUiPort = Preferences.WebUiPort;
            WebUiUpnp = Preferences.WebUiUpnp;
            UseHttps = Preferences.UseHttps;
            WebUiHttpsCertPath = Preferences.WebUiHttpsCertPath;
            WebUiHttpsKeyPath = Preferences.WebUiHttpsKeyPath;
            WebUiUsername = Preferences.WebUiUsername;
            WebUiPassword = null;
            BypassLocalAuth = Preferences.BypassLocalAuth;
            BypassAuthSubnetWhitelistEnabled = Preferences.BypassAuthSubnetWhitelistEnabled;
            BypassAuthSubnetWhitelist = Preferences.BypassAuthSubnetWhitelist;
            WebUiMaxAuthFailCount = Preferences.WebUiMaxAuthFailCount;
            WebUiBanDuration = Preferences.WebUiBanDuration;
            WebUiSessionTimeout = Preferences.WebUiSessionTimeout;
            AlternativeWebuiEnabled = Preferences.AlternativeWebuiEnabled;
            AlternativeWebuiPath = Preferences.AlternativeWebuiPath;
            WebUiClickjackingProtectionEnabled = Preferences.WebUiClickjackingProtectionEnabled;
            WebUiCsrfProtectionEnabled = Preferences.WebUiCsrfProtectionEnabled;
            WebUiSecureCookieEnabled = Preferences.WebUiSecureCookieEnabled;
            WebUiHostHeaderValidationEnabled = Preferences.WebUiHostHeaderValidationEnabled;
            WebUiUseCustomHttpHeadersEnabled = Preferences.WebUiUseCustomHttpHeadersEnabled;
            WebUiCustomHttpHeaders = Preferences.WebUiCustomHttpHeaders;
            WebUiReverseProxyEnabled = Preferences.WebUiReverseProxyEnabled;
            WebUiReverseProxiesList = Preferences.WebUiReverseProxiesList;
            DyndnsEnabled = Preferences.DyndnsEnabled;
            DyndnsService = Preferences.DyndnsService;
            DyndnsDomain = Preferences.DyndnsDomain;
            DyndnsUsername = Preferences.DyndnsUsername;
            DyndnsPassword = Preferences.DyndnsPassword;

            WebUiSessionsCountLimit = Preferences.WebUiSessionsCountLimit;

            return true;
        }

        protected async Task LocaleChanged(string value)
        {
            Locale = value;
            UpdatePreferences.Locale = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task PerformanceWarningChanged(bool value)
        {
            PerformanceWarning = value;
            UpdatePreferences.PerformanceWarning = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiDomainListChanged(string value)
        {
            WebUiDomainList = value;
            UpdatePreferences.WebUiDomainList = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiAddressChanged(string value)
        {
            WebUiAddress = value;
            UpdatePreferences.WebUiAddress = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiPortChanged(int value)
        {
            WebUiPort = value;
            UpdatePreferences.WebUiPort = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiUpnpChanged(bool value)
        {
            WebUiUpnp = value;
            UpdatePreferences.WebUiUpnp = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task UseHttpsChanged(bool value)
        {
            UseHttps = value;
            UpdatePreferences.UseHttps = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiHttpsCertPathChanged(string value)
        {
            WebUiHttpsCertPath = value;
            UpdatePreferences.WebUiHttpsCertPath = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiHttpsKeyPathChanged(string value)
        {
            WebUiHttpsKeyPath = value;
            UpdatePreferences.WebUiHttpsKeyPath = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiUsernameChanged(string value)
        {
            WebUiUsername = value;
            UpdatePreferences.WebUiUsername = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiPasswordChanged(string value)
        {
            WebUiPassword = value;
            UpdatePreferences.WebUiPassword = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task BypassLocalAuthChanged(bool value)
        {
            BypassLocalAuth = value;
            UpdatePreferences.BypassLocalAuth = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task BypassAuthSubnetWhitelistEnabledChanged(bool value)
        {
            BypassAuthSubnetWhitelistEnabled = value;
            UpdatePreferences.BypassAuthSubnetWhitelistEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task BypassAuthSubnetWhitelistChanged(string value)
        {
            BypassAuthSubnetWhitelist = value;
            UpdatePreferences.BypassAuthSubnetWhitelist = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiMaxAuthFailCountChanged(int value)
        {
            WebUiMaxAuthFailCount = value;
            UpdatePreferences.WebUiMaxAuthFailCount = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiBanDurationChanged(int value)
        {
            WebUiBanDuration = value;
            UpdatePreferences.WebUiBanDuration = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiSessionTimeoutChanged(int value)
        {
            WebUiSessionTimeout = value;
            UpdatePreferences.WebUiSessionTimeout = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AlternativeWebuiEnabledChanged(bool value)
        {
            AlternativeWebuiEnabled = value;
            UpdatePreferences.AlternativeWebuiEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task AlternativeWebuiPathChanged(string value)
        {
            AlternativeWebuiPath = value;
            UpdatePreferences.AlternativeWebuiPath = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiClickjackingProtectionEnabledChanged(bool value)
        {
            WebUiClickjackingProtectionEnabled = value;
            UpdatePreferences.WebUiClickjackingProtectionEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiCsrfProtectionEnabledChanged(bool value)
        {
            WebUiCsrfProtectionEnabled = value;
            UpdatePreferences.WebUiCsrfProtectionEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiSecureCookieEnabledChanged(bool value)
        {
            WebUiSecureCookieEnabled = value;
            UpdatePreferences.WebUiSecureCookieEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiHostHeaderValidationEnabledChanged(bool value)
        {
            WebUiHostHeaderValidationEnabled = value;
            UpdatePreferences.WebUiHostHeaderValidationEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiUseCustomHttpHeadersEnabledChanged(bool value)
        {
            WebUiUseCustomHttpHeadersEnabled = value;
            UpdatePreferences.WebUiUseCustomHttpHeadersEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiCustomHttpHeadersChanged(string value)
        {
            WebUiCustomHttpHeaders = value;
            UpdatePreferences.WebUiCustomHttpHeaders = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiReverseProxyEnabledChanged(bool value)
        {
            WebUiReverseProxyEnabled = value;
            UpdatePreferences.WebUiReverseProxyEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task WebUiReverseProxiesListChanged(string value)
        {
            WebUiReverseProxiesList = value;
            UpdatePreferences.WebUiReverseProxiesList = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DyndnsEnabledChanged(bool value)
        {
            DyndnsEnabled = value;
            UpdatePreferences.DyndnsEnabled = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DyndnsServiceChanged(DyndnsService value)
        {
            DyndnsService = value;
            UpdatePreferences.DyndnsService = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DyndnsDomainChanged(string value)
        {
            DyndnsDomain = value;
            UpdatePreferences.DyndnsDomain = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DyndnsUsernameChanged(string value)
        {
            DyndnsUsername = value;
            UpdatePreferences.DyndnsUsername = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task DyndnsPasswordChanged(string value)
        {
            DyndnsPassword = value;
            UpdatePreferences.DyndnsPassword = value;
            await PreferencesChanged.InvokeAsync(UpdatePreferences);
        }

        protected async Task RegisterDyndnsService()
        {
            if (DyndnsEnabled != true)
            {
                return;
            }

#pragma warning disable S1075 // URIs should not be hardcoded
            var url = DyndnsService switch
            {
                DyndnsService.DynDns => "https://www.dyndns.com/account/services/hosts/add.html",
                DyndnsService.NoIp => "http://www.no-ip.com/services/managed_dns/free_dynamic_dns.html",
                _ => throw new InvalidOperationException($"DyndnsService value of {DyndnsService} is not supported."),
            };
#pragma warning restore S1075 // URIs should not be hardcoded
            await JSRuntime.Open(url, true);
        }
    }
}
