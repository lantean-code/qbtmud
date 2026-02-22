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

        private MudFileUpload<IReadOnlyList<IBrowserFile>>? FileUpload { get; set; }

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
            if (Files.Count == 0)
            {
                MudDialog.Cancel();
                return;
            }

            var options = new AddTorrentFileOptions(Files, TorrentOptions.GetTorrentOptions());
            MudDialog.Close(DialogResult.Ok(options));
        }

        protected async Task RemoveAsync(IBrowserFile file)
        {
            await FileUpload!.RemoveFileAsync(file);
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}
