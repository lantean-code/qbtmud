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

        private string TranslateSettings(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppSettings", source, arguments);
        }
    }
}
