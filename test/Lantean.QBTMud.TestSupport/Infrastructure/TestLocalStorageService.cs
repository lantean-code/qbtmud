using System.Text.Json;
using Lantean.QBTMud.Application.Services;

namespace Lantean.QBTMud.TestSupport.Infrastructure
{
    internal sealed class TestLocalStorageService : ILocalStorageService, ISettingsStorageService, IClientDataCacheInvalidationService, IPendingClientDataJournal
    {
        private readonly Dictionary<string, object?> _store = new(StringComparer.Ordinal);
        private readonly Lock _lock = new();
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

        public Task InvalidateClientDataCacheAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public ValueTask<Dictionary<string, JsonElement?>> ReadAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return ValueTask.FromResult(ReadPending());
            }
        }

        public ValueTask SetAsync(string key, JsonElement? value, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var pending = ReadPending();
                pending[key] = value;
                WritePending(pending);
                return ValueTask.CompletedTask;
            }
        }

        public ValueTask<Dictionary<string, JsonElement?>> CompleteAsync(IReadOnlyDictionary<string, JsonElement?> changes, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var pending = ReadPending();
                foreach (var (key, expected) in changes)
                {
                    if (pending.TryGetValue(key, out var current) && current?.GetRawText() == expected?.GetRawText())
                    {
                        pending.Remove(key);
                    }
                }
                WritePending(pending);
                return ValueTask.FromResult(pending);
            }
        }

        public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var pending = ReadPending();
                pending.Remove(key);
                WritePending(pending);
                return ValueTask.CompletedTask;
            }
        }

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            RemoveItemInternal(SettingsStorageService._pendingChangesStorageKey);
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveLocalSourceAsync(string key, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                var prefixedKey = key.StartsWith("QbtMud.", StringComparison.Ordinal) ? key : "QbtMud." + key;
                if (!ReadPending().ContainsKey(prefixedKey))
                {
                    _store.Remove(key);
                    _store.Remove(prefixedKey);
                    _store.Remove(prefixedKey["QbtMud.".Length..]);
                }
                return ValueTask.CompletedTask;
            }
        }

        private Dictionary<string, JsonElement?> ReadPending()
        {
            return _store.TryGetValue(SettingsStorageService._pendingChangesStorageKey, out var value) && value is not null
                ? JsonSerializer.Deserialize<Dictionary<string, JsonElement?>>(value is string text ? text : JsonSerializer.Serialize(value))!
                : [];
        }

        private void WritePending(Dictionary<string, JsonElement?> pending)
        {
            if (pending.Count == 0)
            {
                _store.Remove(SettingsStorageService._pendingChangesStorageKey);
            }
            else
            {
                SetItemInternal(SettingsStorageService._pendingChangesStorageKey, JsonSerializer.Serialize(pending));
            }
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
