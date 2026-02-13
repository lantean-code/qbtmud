using Lantean.QBTMud.Theming;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Lantean.QBTMud.Services
{
    internal sealed class BrowserStorageService : IBrowserStorageService
    {
        private const string StorageKeyPrefix = "QbtMud.";
        private static readonly JsonSerializerOptions _serializerOptions = ThemeSerialization.CreateSerializerOptions(writeIndented: false);

        private readonly IJSRuntime _jsRuntime;
        private readonly string _storageName;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserStorageService"/> class.
        /// </summary>
        /// <param name="jsRuntime">The JavaScript runtime used to access browser storage.</param>
        /// <param name="storageName">The browser storage object name.</param>
        public BrowserStorageService(IJSRuntime jsRuntime, string storageName)
        {
            _jsRuntime = jsRuntime;
            _storageName = storageName;
        }

        /// <inheritdoc />
        public async ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken)
        {
            var value = await GetItemValueAsync(key, cancellationToken);
            if (value is null)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value, _serializerOptions);
        }

        /// <inheritdoc />
        public async ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken)
        {
            return await GetItemValueAsync(key, cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Serialize(data, _serializerOptions);
            var prefixedKey = GetPrefixedKey(key);
            await _jsRuntime.InvokeAsync<object?>($"{_storageName}.setItem", cancellationToken, prefixedKey, payload);
        }

        /// <inheritdoc />
        public async ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken)
        {
            var prefixedKey = GetPrefixedKey(key);
            await _jsRuntime.InvokeAsync<object?>($"{_storageName}.setItem", cancellationToken, prefixedKey, data);
        }

        /// <inheritdoc />
        public async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken)
        {
            var prefixedKey = GetPrefixedKey(key);
            await _jsRuntime.InvokeAsync<object?>($"{_storageName}.removeItem", cancellationToken, prefixedKey);

            if (string.Equals(prefixedKey, key, StringComparison.Ordinal))
            {
                return;
            }

            await _jsRuntime.InvokeAsync<object?>($"{_storageName}.removeItem", cancellationToken, key);
        }

        private static string GetPrefixedKey(string key)
        {
            if (key.StartsWith(StorageKeyPrefix, StringComparison.Ordinal))
            {
                return key;
            }

            return string.Concat(StorageKeyPrefix, key);
        }

        private async ValueTask<string?> GetItemValueAsync(string key, CancellationToken cancellationToken)
        {
            var prefixedKey = GetPrefixedKey(key);
            var value = await _jsRuntime.InvokeAsync<string?>($"{_storageName}.getItem", cancellationToken, prefixedKey);
            if (value is not null || string.Equals(prefixedKey, key, StringComparison.Ordinal))
            {
                return value;
            }

            var legacyValue = await _jsRuntime.InvokeAsync<string?>($"{_storageName}.getItem", cancellationToken, key);
            if (legacyValue is null)
            {
                return null;
            }

            await _jsRuntime.InvokeAsync<object?>($"{_storageName}.setItem", cancellationToken, prefixedKey, legacyValue);
            await _jsRuntime.InvokeAsync<object?>($"{_storageName}.removeItem", cancellationToken, key);

            return legacyValue;
        }
    }
}
