using Lantean.QBTMud.Application.Services.Localization;
using Microsoft.AspNetCore.Components;
using AppSettingsModel = Lantean.QBTMud.Core.Models.AppSettings;

namespace Lantean.QBTMud.Components.AppSettingsTabs
{
    public partial class GeneralAppSettingsTab
    {
        [Parameter]
        [EditorRequired]
        public AppSettingsModel Settings { get; set; } = default!;

        [Parameter]
        public EventCallback SettingsChanged { get; set; }

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        private async Task OnSpeedHistoryEnabledChanged(bool value)
        {
            if (Settings.SpeedHistoryEnabled == value)
            {
                return;
            }

            Settings.SpeedHistoryEnabled = value;
            await SettingsChanged.InvokeAsync();
        }

        private string TranslateSettings(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppSettings", source, arguments);
        }
    }
}
