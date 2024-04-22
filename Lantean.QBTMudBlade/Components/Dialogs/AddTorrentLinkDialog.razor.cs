using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class AddTorrentLinkDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        protected string? Urls { get; set; }

        protected AddTorrentOptions TorrentOptions { get; set; } = default!;

        protected void Cancel(MouseEventArgs args)
        {
            MudDialog.Cancel();
        }

        protected void Submit(MouseEventArgs args)
        {
            if (Urls is null)
            {
                MudDialog.Cancel();
                return;
            }
            var options = new AddTorrentLinkOptions(Urls, TorrentOptions.GetTorrentOptions());
            MudDialog.Close(DialogResult.Ok(options));
        }
    }
}