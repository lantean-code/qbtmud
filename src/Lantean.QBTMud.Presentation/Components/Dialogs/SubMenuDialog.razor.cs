using Lantean.QBTMud.Core.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudCategory = Lantean.QBTMud.Core.Models.Category;
using MudTorrent = Lantean.QBTMud.Core.Models.Torrent;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class SubMenuDialog
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public UIAction? ParentAction { get; set; }

        [Parameter]
        public Dictionary<string, MudTorrent> Torrents { get; set; } = default!;

        [Parameter]
        public QBittorrentPreferences? Preferences { get; set; }

        [Parameter]
        public IEnumerable<string> Hashes { get; set; } = [];

        [Parameter]
        public HashSet<string> Tags { get; set; } = default!;

        [Parameter]
        public Dictionary<string, MudCategory> Categories { get; set; } = default!;

        protected Task CloseDialog()
        {
            MudDialog.Close();

            return Task.CompletedTask;
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }
    }
}
