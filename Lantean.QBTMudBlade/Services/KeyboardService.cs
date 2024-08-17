using Lantean.QBTMudBlade.Models;
using Microsoft.JSInterop;
using System.Collections.Concurrent;

namespace Lantean.QBTMudBlade.Services
{
    public class KeyboardService : IKeyboardService, IAsyncDisposable
    {
        private readonly IJSRuntime _jSRuntime;

        private DotNetObjectReference<KeyboardService>? _dotNetObjectReference;
        private bool _disposedValue;
        private readonly ConcurrentDictionary<string, Func<KeyboardEvent, Task>> _keyboardHandlers = new();

        public KeyboardService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }

        public async Task RegisterKeypressEvent(KeyboardEvent criteria, Func<KeyboardEvent, Task> onKeyPress)
        {
            await _jSRuntime.InvokeVoidAsync("qbt.registerKeypressEvent", criteria, GetObjectReference());
            _keyboardHandlers.TryAdd(criteria, onKeyPress);
        }

        private DotNetObjectReference<KeyboardService> GetObjectReference()
        {
            _dotNetObjectReference ??= DotNetObjectReference.Create(this);

            return _dotNetObjectReference;
        }

        [JSInvokable]
        public async Task HandleKeyPressEvent(KeyboardEvent keyboardEvent)
        {
            if (!_keyboardHandlers.TryGetValue(keyboardEvent, out var handler))
            {
                return;
            }

            await handler(keyboardEvent);
        }

        public async Task UnregisterKeypressEvent(KeyboardEvent criteria)
        {
            await _jSRuntime.InvokeVoidAsync("qbt.unregisterKeypressEvent", criteria, GetObjectReference());
            _keyboardHandlers.Remove(criteria, out var _);
        }

        public async Task Focus()
        {
            await _jSRuntime.InvokeVoidAsync("qbt.keyPressFocusInstance", GetObjectReference());
        }

        public async Task UnFocus()
        {
            await _jSRuntime.InvokeVoidAsync("qbt.keyPressUnFocusInstance", GetObjectReference());
        }

        protected async virtual ValueTask DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    await UnFocus();
                    foreach (var key in _keyboardHandlers.Keys)
                    {
                        await _jSRuntime.InvokeVoidAsync("qbt.unregisterKeypressEvent", key, GetObjectReference());
                    }

                    _keyboardHandlers.Clear();
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
