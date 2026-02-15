using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Components
{
    public partial class TorrentsListNav
    {
        private const string _appContext = "AppTorrentsListNav";

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Parameter]
        public IEnumerable<Torrent>? Torrents { get; set; }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        private string TranslateApp(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate(_appContext, source, arguments);
        }
    }
}
