using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class SubMenuDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public TorrentAction? ParentAction { get; set; }

        [Parameter]
        public MainData MainData { get; set; } = default!;

        [Parameter]
        public QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        protected bool MultiAction => ParentAction?.MultiAction ?? false;

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