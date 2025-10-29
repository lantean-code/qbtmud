using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class AddTorrentFileDialog
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        private List<IBrowserFile> Files { get; set; } = [];

        protected AddTorrentOptions TorrentOptions { get; set; } = default!;

        protected void UploadFiles(IReadOnlyList<IBrowserFile> files)
        {
            Files = files.ToList();
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

        protected void Remove(IBrowserFile file)
        {
            Files.Remove(file);
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}