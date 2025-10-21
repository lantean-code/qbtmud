using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class AddTorrentOptions
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Parameter]
        public bool ShowCookieOption { get; set; }

        protected bool Expanded { get; set; }

        protected bool TorrentManagementMode { get; set; }

        protected string SavePath { get; set; } = default!;

        protected string? Cookie { get; set; }

        protected string? RenameTorrent { get; set; }

        protected IEnumerable<string> Categories { get; set; } = [];

        protected string? Category { get; set; }

        protected bool StartTorrent { get; set; } = true;

        protected bool AddToTopOfQueue { get; set; } = true;

        protected string StopCondition { get; set; } = "None";

        protected bool SkipHashCheck { get; set; } = false;

        protected string ContentLayout { get; set; } = "Original";

        protected bool DownloadInSequentialOrder { get; set; } = false;

        protected bool DownloadFirstAndLastPiecesFirst { get; set; } = false;

        protected long DownloadLimit { get; set; }

        protected long UploadLimit { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var categories = await ApiClient.GetAllCategories();
            Categories = categories.Select(c => c.Key).ToList();

            var preferences = await ApiClient.GetApplicationPreferences();

            TorrentManagementMode = preferences.AutoTmmEnabled;
            SavePath = preferences.SavePath;
            StartTorrent = !preferences.AddStoppedEnabled;
            AddToTopOfQueue = preferences.AddToTopOfQueue;
            StopCondition = preferences.TorrentStopCondition;
            ContentLayout = preferences.TorrentContentLayout;
        }

        public TorrentOptions GetTorrentOptions()
        {
            return new TorrentOptions(
                TorrentManagementMode,
                SavePath,
                Cookie,
                RenameTorrent,
                Category,
                StartTorrent,
                AddToTopOfQueue,
                StopCondition,
                SkipHashCheck,
                ContentLayout,
                DownloadInSequentialOrder,
                DownloadFirstAndLastPiecesFirst,
                DownloadLimit,
                UploadLimit);
        }
    }
}
