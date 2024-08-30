using Lantean.QBitTorrentClient;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMudBlade.Pages
{
    public partial class About
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter(Name = "Version")]
        public string? Version { get; set; }

        protected string? QtVersion { get; private set; }

        protected string? LibtorrentVersion { get; private set; }

        protected string? BoostVersion { get; private set; }

        protected string? OpensslVersion { get; private set; }

        protected string? ZlibVersion { get; private set; }

        protected string? QBittorrentVersion { get; private set; }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("/");
        }

        protected override async Task OnInitializedAsync()
        {
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
            QBittorrentVersion =  $"{Version} ({info.Bitness}-bit)";
        }
    }
}