using Lantean.QBTMud.Core.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudMainData = Lantean.QBTMud.Core.Models.MainData;

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
        public MudMainData MainData { get; set; } = default!;

        [Parameter]
        public QBittorrentPreferences? Preferences { get; set; }

        protected bool AutomaticTorrentManagement { get; set; }

        protected string? SavePath { get; set; }

        protected string? TempPath { get; set; }

        protected override Task OnInitializedAsync()
        {
            if (!MainData.Torrents.TryGetValue(Hash, out var torrent))
            {
                return Task.CompletedTask;
            }

            AutomaticTorrentManagement = torrent.AutomaticTorrentManagement;
            SavePath = torrent.SavePath;
            TempPath = Preferences?.TempPath;

            return Task.CompletedTask;
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
