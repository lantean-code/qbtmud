using Microsoft.JSInterop;
using System.Text.Json;

namespace Lantean.QBTMud.Services
{
    internal sealed class BrowserStorageService
    {
        private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

        private readonly IJSRuntime _jsRuntime;
        private readonly string _storageName;

        public BrowserStorageService(IJSRuntime jsRuntime, string storageName)
        {
            _jsRuntime = jsRuntime;
            _storageName = storageName;
        }

        internal async ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken)
        {
            var value = await _jsRuntime.InvokeAsync<string?>($"{_storageName}.getItem", cancellationToken, key);
            if (value is null)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value, _serializerOptions);
        }

        internal ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken)
        {
            return _jsRuntime.InvokeAsync<string?>($"{_storageName}.getItem", cancellationToken, key);
        }

        internal async ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Serialize(data, _serializerOptions);
            await _jsRuntime.InvokeAsync<object?>($"{_storageName}.setItem", cancellationToken, key, payload);
        }

        internal async ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken)
        {
            await _jsRuntime.InvokeAsync<object?>($"{_storageName}.setItem", cancellationToken, key, data);
        }

        internal async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken)
        {
            await _jsRuntime.InvokeAsync<object?>($"{_storageName}.removeItem", cancellationToken, key);
        }
    }
}
