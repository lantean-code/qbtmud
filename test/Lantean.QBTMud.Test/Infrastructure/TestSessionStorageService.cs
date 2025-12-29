using Lantean.QBTMud.Services;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Infrastructure
{
    internal sealed class TestSessionStorageService : ISessionStorageService
    {
        private readonly Dictionary<string, object?> _store = new(StringComparer.Ordinal);
        private readonly object _lock = new();
        private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

        public ValueTask<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_store.TryGetValue(key, out var value) || value is null)
                {
                    return ValueTask.FromResult<T?>(default);
                }

                if (value is T typed)
                {
                    return ValueTask.FromResult<T?>(typed);
                }

                if (value is string stringValue)
                {
                    return ValueTask.FromResult(JsonSerializer.Deserialize<T>(stringValue, _serializerOptions));
                }

                return ValueTask.FromResult(JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, _serializerOptions), _serializerOptions));
            }
        }

        public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _store.Remove(key);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _store[key] = data;
            }

            return ValueTask.CompletedTask;
        }
    }
}
