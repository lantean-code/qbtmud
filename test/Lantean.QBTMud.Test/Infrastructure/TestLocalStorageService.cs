using Lantean.QBTMud.Services;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Infrastructure
{
    internal sealed class TestLocalStorageService : ILocalStorageService
    {
        private readonly Dictionary<string, object?> _store = new(StringComparer.Ordinal);
        private readonly object _lock = new();
        private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
        private int _writeCount;

        public TestLocalStorageService(IReadOnlyDictionary<string, object?>? initialValues = null)
        {
            if (initialValues is null)
            {
                return;
            }

            foreach (var entry in initialValues)
            {
                _store[entry.Key] = entry.Value;
            }
        }

        public int WriteCount
        {
            get
            {
                lock (_lock)
                {
                    return _writeCount;
                }
            }
        }

        public ValueTask<string?> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_store.TryGetValue(key, out var value) || value is null)
                {
                    return ValueTask.FromResult<string?>(null);
                }

                if (value is string s)
                {
                    return ValueTask.FromResult<string?>(s);
                }

                return ValueTask.FromResult<string?>(JsonSerializer.Serialize(value, _serializerOptions));
            }
        }

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
            RemoveItemInternal(key);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
        {
            SetItemInternal(key, data);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            SetItemInternal(key, data);
            return ValueTask.CompletedTask;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _store.Clear();
            }
        }

        public IReadOnlyDictionary<string, object?> Snapshot()
        {
            lock (_lock)
            {
                return new Dictionary<string, object?>(_store, StringComparer.Ordinal);
            }
        }

        private void SetItemInternal(string key, object? newValue)
        {
            lock (_lock)
            {
                _store[key] = newValue;
                ++_writeCount;
            }
        }

        private void RemoveItemInternal(string key)
        {
            lock (_lock)
            {
                _store.Remove(key);
            }
        }
    }
}
