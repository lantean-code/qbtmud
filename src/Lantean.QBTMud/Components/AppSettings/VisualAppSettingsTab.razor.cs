using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using AppSettingsModel = Lantean.QBTMud.Models.AppSettings;

namespace Lantean.QBTMud.Components.AppSettingsTabs
{
    public partial class VisualAppSettingsTab
    {
        [Parameter]
        [EditorRequired]
        public AppSettingsModel Settings { get; set; } = default!;

        [Parameter]
        public EventCallback SettingsChanged { get; set; }

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        private async Task OnThemeModePreferenceChanged(ThemeModePreference value)
        {
            if (Settings.ThemeModePreference == value)
            {
                return;
            }

            Settings.ThemeModePreference = value;
            await SettingsChanged.InvokeAsync();
        }

        private async Task OnThemeRepositoryIndexUrlChanged(string value)
        {
            if (string.Equals(Settings.ThemeRepositoryIndexUrl, value, StringComparison.Ordinal))
            {
                return;
            }

            Settings.ThemeRepositoryIndexUrl = value;
            await SettingsChanged.InvokeAsync();
        }

        private bool IsThemeRepositoryIndexUrlValid
        {
            get
            {
                var value = Settings.ThemeRepositoryIndexUrl;
                if (string.IsNullOrWhiteSpace(value))
                {
                    return true;
                }

                if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
                {
                    return false;
                }

                return string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            }
        }

        private string ThemeRepositoryIndexUrlErrorText
        {
            get
            {
                return TranslateSettings("Enter a valid HTTPS URL or leave blank.");
            }
        }

        private string TranslateSettings(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppSettings", source, arguments);
        }
    }
}
