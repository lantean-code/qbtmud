using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Components.Dialogs
{
    public abstract class SubmittableDialog : ComponentBase, IAsyncDisposable
    {
        private static readonly KeyboardEvent _ctrlEnterKey = new("Enter") { CtrlKey = true };

        private static readonly KeyboardEvent _enterKey = new("Enter");

        private bool _disposedValue;

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        protected virtual DialogSubmitTriggers SubmitTriggers => DialogSubmitTriggers.CtrlEnter;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                if (SubmitTriggers.HasFlag(DialogSubmitTriggers.Enter))
                {
                    await KeyboardService.RegisterKeypressEvent(_enterKey, Submit);
                }

                if (SubmitTriggers.HasFlag(DialogSubmitTriggers.CtrlEnter))
                {
                    await KeyboardService.RegisterKeypressEvent(_ctrlEnterKey, Submit);
                }

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
                    if (SubmitTriggers.HasFlag(DialogSubmitTriggers.Enter))
                    {
                        await KeyboardService.UnregisterKeypressEvent(_enterKey);
                    }

                    if (SubmitTriggers.HasFlag(DialogSubmitTriggers.CtrlEnter))
                    {
                        await KeyboardService.UnregisterKeypressEvent(_ctrlEnterKey);
                    }

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
