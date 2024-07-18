﻿using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class TorrentOptionsDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

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

        protected void Cancel(MouseEventArgs args)
        {
            MudDialog.Cancel();
        }

        protected void Submit(MouseEventArgs args)
        {
            MudDialog.Close();
        }
    }
}