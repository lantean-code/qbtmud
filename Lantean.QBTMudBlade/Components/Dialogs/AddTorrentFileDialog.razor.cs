using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class AddTorrentFileDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        protected IReadOnlyList<IBrowserFile> Files { get; set; } = [];

        protected AddTorrentOptions TorrentOptions { get; set; } = default!;

        protected void UploadFiles(IReadOnlyList<IBrowserFile> files)
        {
            Files = files;
        }

        protected void Cancel(MouseEventArgs args)
        {
            MudDialog.Cancel();
        }

        protected void Submit(MouseEventArgs args)
        {
            var options = new AddTorrentFileOptions(Files, TorrentOptions.GetTorrentOptions());
            MudDialog.Close(DialogResult.Ok(options));
        }
    }
}