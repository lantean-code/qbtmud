using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class TorrentOptionsDialog
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public string Hash { get; set; } = default!;

        [CascadingParameter]
        public MainData MainData { get; set; } = default!;

        [CascadingParameter]
        public QBitTorrentClient.Models.Preferences Preferences { get; set; } = default!;

        protected bool AutomaticTorrentManagement { get; set; }

        protected string? SavePath { get; set; }

        protected string? TempPath { get; set; }

        protected override void OnInitialized()
        {
            if (!MainData.Torrents.TryGetValue(Hash, out var torrent))
            {
                return;
            }

            var tempPath = Preferences.TempPath;

            AutomaticTorrentManagement = torrent.AutomaticTorrentManagement;
            SavePath = torrent.SavePath;
            TempPath = tempPath;
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            MudDialog.Close();
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}