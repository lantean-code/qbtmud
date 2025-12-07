using Lantean.QBTMud.Models;
using Microsoft.JSInterop;
using System.Collections.Concurrent;

namespace Lantean.QBTMud.Services
{
    public class KeyboardService : IKeyboardService, IAsyncDisposable
    {
        private readonly IJSRuntime _jSRuntime;

        private DotNetObjectReference<KeyboardService>? _dotNetObjectReference;
        private bool _disposedValue;
        private readonly ConcurrentDictionary<string, KeyboardHandlerRegistration> _keyboardHandlers = new();

        public KeyboardService(IJSRuntime jSRuntime)
        {
            _jSRuntime = jSRuntime;
        }

        public async Task RegisterKeypressEvent(KeyboardEvent criteria, Func<KeyboardEvent, Task> onKeyPress)
        {
            await _jSRuntime.InvokeVoidAsync("qbt.registerKeypressEvent", criteria, GetObjectReference());
            var handlerKey = GetHandlerKey(criteria);
            _keyboardHandlers.AddOrUpdate(handlerKey, _ => new KeyboardHandlerRegistration(criteria, onKeyPress), (_, _) => new KeyboardHandlerRegistration(criteria, onKeyPress));
        }

        private DotNetObjectReference<KeyboardService> GetObjectReference()
        {
            _dotNetObjectReference ??= DotNetObjectReference.Create(this);

            return _dotNetObjectReference;
        }

        [JSInvokable]
        public async Task HandleKeyPressEvent(KeyboardEvent keyboardEvent)
        {
            var handlerKey = GetHandlerKey(keyboardEvent);
            if (!_keyboardHandlers.TryGetValue(handlerKey, out var registration))
            {
                return;
            }

            await registration.Handler(keyboardEvent);
        }

        public async Task UnregisterKeypressEvent(KeyboardEvent criteria)
        {
            await _jSRuntime.InvokeVoidAsync("qbt.unregisterKeypressEvent", criteria, GetObjectReference());
            var handlerKey = GetHandlerKey(criteria);
            _keyboardHandlers.TryRemove(handlerKey, out _);
        }

        public async Task Focus()
        {
            await _jSRuntime.InvokeVoidAsync("qbt.keyPressFocusInstance", GetObjectReference());
        }

        public async Task UnFocus()
        {
            await _jSRuntime.InvokeVoidAsync("qbt.keyPressUnFocusInstance", GetObjectReference());
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    await UnFocus();
                    foreach (var registration in _keyboardHandlers.Values)
                    {
                        await _jSRuntime.InvokeVoidAsync("qbt.unregisterKeypressEvent", registration.Criteria, GetObjectReference());
                    }

                    _keyboardHandlers.Clear();

                    if (_dotNetObjectReference is not null)
                    {
                        _dotNetObjectReference.Dispose();
                    }
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

        private static string GetHandlerKey(KeyboardEvent keyboardEvent)
        {
            return keyboardEvent.GetCanonicalKey();
        }

        private sealed record KeyboardHandlerRegistration(KeyboardEvent Criteria, Func<KeyboardEvent, Task> Handler);
    }
}
