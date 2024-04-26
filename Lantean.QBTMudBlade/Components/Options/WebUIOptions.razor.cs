﻿namespace Lantean.QBTMudBlade.Components.Options
{
    public partial class WebUIOptions : Options
    {
        protected string? Locale { get; private set; }
        protected bool PerformanceWarning { get; private set; }
        protected string? WebUiDomainList { get; private set; }
        protected string? WebUiAddress { get; private set; }
        protected int WebUiPort { get; private set; }
        protected bool WebUiUpnp { get; private set; }
        protected bool UseHttps { get; private set; }
        protected string? WebUiHttpsCertPath { get; private set; }
        protected string? WebUiHttpsKeyPath { get; private set; }
        protected string? WebUiUsername { get; private set; }
        protected string? WebUiPassword { get; private set; }
        protected bool BypassLocalAuth { get; private set; }
        protected bool BypassAuthSubnetWhitelistEnabled { get; private set; }
        protected string? BypassAuthSubnetWhitelist { get; private set; }
        protected int WebUiMaxAuthFailCount { get; private set; }
        protected int WebUiBanDuration { get; private set; }
        protected int WebUiSessionTimeout { get; private set; }
        protected bool AlternativeWebuiEnabled { get; private set; }
        protected string? AlternativeWebuiPath { get; private set; }
        protected bool WebUiClickjackingProtectionEnabled { get; private set; }
        protected bool WebUiCsrfProtectionEnabled { get; private set; }
        protected bool WebUiSecureCookieEnabled { get; private set; }
        protected bool WebUiHostHeaderValidationEnabled { get; private set; }
        protected bool WebUiUseCustomHttpHeadersEnabled { get; private set; }
        protected string? WebUiCustomHttpHeaders { get; private set; }
        protected bool WebUiReverseProxyEnabled { get; private set; }
        protected string? WebUiReverseProxiesList { get; private set; }
        protected bool DyndnsEnabled { get; private set; }
        protected int DyndnsService { get; private set; }
        protected string? DyndnsDomain { get; private set; }
        protected string? DyndnsUsername { get; private set; }
        protected string? DyndnsPassword { get; private set; }

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
            WebUiPassword = Preferences.WebUiPassword;
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

        protected async Task DyndnsServiceChanged(int value)
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
            await Task.Delay(0);
        }
    }
}
