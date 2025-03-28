﻿using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class SubMenuDialog
    {
        [CascadingParameter]
        IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public UIAction? ParentAction { get; set; }

        [Parameter]
        public Dictionary<string, Torrent> Torrents { get; set; } = default!;

        [Parameter]
        public QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        [Parameter]
        public IEnumerable<string> Hashes { get; set; } = [];

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