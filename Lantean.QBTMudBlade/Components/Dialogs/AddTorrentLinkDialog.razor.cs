using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class AddTorrentLinkDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        protected MudTextField<string?>? UrlsTextField { get; set; }

        protected string? Urls { get; set; }

        protected AddTorrentOptions TorrentOptions { get; set; } = default!;

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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && UrlsTextField is not null)
            {
                await UrlsTextField.FocusAsync();
            }
        }
    }
}