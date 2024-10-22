using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
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

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            var options = new AddTorrentFileOptions(Files, TorrentOptions.GetTorrentOptions());
            MudDialog.Close(DialogResult.Ok(options));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}