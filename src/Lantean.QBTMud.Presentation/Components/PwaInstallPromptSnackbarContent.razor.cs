using Lantean.QBTMud.Application.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Lantean.QBTMud.Components
{
    public partial class PwaInstallPromptSnackbarContent
    {
        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Parameter]
        public bool CanPromptInstall { get; set; }

        [Parameter]
        public bool ShowIosInstructions { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnInstallClicked { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> OnDismissClicked { get; set; }

        protected string BuildMessage()
        {
            var bodyText = ShowIosInstructions
                ? Translate("Install qBittorrent Web UI from your browser menu for quicker access.")
                : Translate("Install qBittorrent Web UI for quicker access and a native-like experience.");

            if (!ShowIosInstructions)
            {
                return bodyText;
            }

            return $"{bodyText} {Translate("On iPhone or iPad, tap Share, then Add to Home Screen.")}";
        }

        protected string Translate(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppPwaInstallPrompt", source, arguments);
        }
    }
}
