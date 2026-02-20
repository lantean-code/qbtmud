using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Pages
{
    public partial class About
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IAppBuildInfoService AppBuildInfoService { get; set; } = default!;

        [Inject]
        protected IAppUpdateService AppUpdateService { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter(Name = "Version")]
        public string? Version { get; set; }

        protected string? QtVersion { get; private set; }

        protected string? LibtorrentVersion { get; private set; }

        protected string? BoostVersion { get; private set; }

        protected string? OpensslVersion { get; private set; }

        protected string? ZlibVersion { get; private set; }

        protected int? Bitness { get; private set; }

        protected string? QBittorrentVersion { get; private set; }

        protected AppBuildInfo? QbtMudBuildInfo { get; private set; }

        protected AppUpdateStatus? QbtMudUpdateStatus { get; private set; }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected override async Task OnInitializedAsync()
        {
            QbtMudBuildInfo = AppBuildInfoService.GetCurrentBuildInfo();

            var info = await ApiClient.GetBuildInfo();
            if (Version is null)
            {
                Version = await ApiClient.GetApplicationVersion();
            }

            QtVersion = info.QTVersion;
            LibtorrentVersion = info.LibTorrentVersion;
            BoostVersion = info.BoostVersion;
            OpensslVersion = info.OpenSSLVersion;
            ZlibVersion = info.ZLibVersion;
            QBittorrentVersion = Version;
            Bitness = info.Bitness;

            try
            {
                QbtMudUpdateStatus = await AppUpdateService.GetUpdateStatusAsync();
            }
            catch
            {
                QbtMudUpdateStatus = null;
            }
        }

        protected string GetQbtMudUpdateState()
        {
            if (QbtMudUpdateStatus is null)
            {
                return TranslateAbout("Not available");
            }

            return QbtMudUpdateStatus.IsUpdateAvailable
                ? TranslateAbout("Update available")
                : TranslateAbout("Up to date");
        }

        private string TranslateAbout(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppAbout", source, arguments);
        }
    }
}
