using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class AddTorrentLinkDialog : IAsyncDisposable
    {
        private bool _disposedValue;

        private readonly KeyboardEvent _ctrlEnterKey = new KeyboardEvent("Enter")
        {
            CtrlKey = true,
        };

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await KeyboardService.RegisterKeypressEvent(_ctrlEnterKey, Submit);
                await KeyboardService.Focus();
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

        protected Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    await KeyboardService.UnregisterKeypressEvent(_ctrlEnterKey);
                    await KeyboardService.UnFocus();
                }

                _disposedValue = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}