using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class AddTorrentLinkDialog : SubmittableDialog
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? Url { get; set; }

        protected MudTextField<string?>? UrlsTextField { get; set; }

        protected string? Urls { get; set; }

        protected AddTorrentOptions TorrentOptions { get; set; } = default!;

        protected override void OnInitialized()
        {
            if (Url is not null)
            {
                Urls = Url;
            }
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            if (Urls is null)
            {
                MudDialog.Cancel();
                return;
            }
            var options = new AddTorrentLinkOptions(Urls, TorrentOptions.GetTorrentOptions());
            MudDialog.Close(DialogResult.Ok(options));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }
    }
}
