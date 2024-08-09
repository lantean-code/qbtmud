using Lantean.QBTMudBlade.Models;
using Microsoft.JSInterop;
using System.Collections.Concurrent;

namespace Lantean.QBTMudBlade.Services
{
    public class KeyboardService : IKeyboardService
    {
        private readonly IJSRuntime _jSRuntime;

        private DotNetObjectReference<KeyboardService>? _dotNetObjectReference;

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
    }
}
