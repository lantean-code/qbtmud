using Lantean.QBTMudBlade.Models;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public abstract class SubmittableDialog : ComponentBase, IAsyncDisposable
    {
        private bool _disposedValue;

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await KeyboardService.RegisterKeypressEvent("Enter", k => Submit(k));
                await KeyboardService.Focus();
            }
        }

        protected abstract Task Submit(KeyboardEvent keyboardEvent);

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    await KeyboardService.UnregisterKeypressEvent("Enter");
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
