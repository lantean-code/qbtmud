using Blazored.LocalStorage;
using System.Text.Json;

namespace Lantean.QBTMud.Test.Infrastructure
{
    internal sealed class TestLocalStorageService : ILocalStorageService
    {
        private readonly Dictionary<string, object?> _store = new(StringComparer.Ordinal);
        private readonly object _lock = new();
        private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
        private int _writeCount;

        public event EventHandler<ChangingEventArgs>? Changing;

        public event EventHandler<ChangedEventArgs>? Changed;

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

        public ValueTask ClearAsync(CancellationToken cancellationToken = default)
        {
            List<string> keys;
            lock (_lock)
            {
                keys = _store.Keys.ToList();
            }

            foreach (var key in keys)
            {
                RemoveItemInternal(key, raiseEvents: true);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return new ValueTask<bool>(_store.ContainsKey(key));
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

        public ValueTask<string?> KeyAsync(int index, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (index < 0 || index >= _store.Count)
                {
                    return ValueTask.FromResult<string?>(null);
                }

                return ValueTask.FromResult<string?>(_store.Keys.ElementAt(index));
            }
        }

        public ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return ValueTask.FromResult<IEnumerable<string>>(_store.Keys.ToList());
            }
        }

        public ValueTask<int> LengthAsync(CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                return ValueTask.FromResult(_store.Count);
            }
        }

        public ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            RemoveItemInternal(key, raiseEvents: true);
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                RemoveItemInternal(key, raiseEvents: true);
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default)
        {
            SetItemInternal(key, data, raiseEvents: true);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            SetItemInternal(key, data, raiseEvents: true);
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

        private void SetItemInternal(string key, object? newValue, bool raiseEvents)
        {
            object? oldValue;
            lock (_lock)
            {
                _store.TryGetValue(key, out oldValue);
            }

            if (raiseEvents)
            {
                var changingArgs = new ChangingEventArgs
                {
                    Key = key,
                    OldValue = oldValue,
                    NewValue = newValue
                };
                Changing?.Invoke(this, changingArgs);
                if (changingArgs.Cancel)
                {
                    return;
                }

                newValue = changingArgs.NewValue;
            }

            lock (_lock)
            {
                _store[key] = newValue;
                ++_writeCount;
            }

            if (raiseEvents)
            {
                Changed?.Invoke(this, new ChangedEventArgs
                {
                    Key = key,
                    OldValue = oldValue,
                    NewValue = newValue
                });
            }
        }

        private void RemoveItemInternal(string key, bool raiseEvents)
        {
            object? oldValue;
            lock (_lock)
            {
                if (!_store.TryGetValue(key, out oldValue))
                {
                    return;
                }
            }

            if (raiseEvents)
            {
                var changingArgs = new ChangingEventArgs
                {
                    Key = key,
                    OldValue = oldValue,
                    NewValue = null
                };
                Changing?.Invoke(this, changingArgs);
                if (changingArgs.Cancel)
                {
                    return;
                }
            }

            lock (_lock)
            {
                _store.Remove(key);
            }

            if (raiseEvents)
            {
                var args = new ChangedEventArgs
                {
                    Key = key,
                    OldValue = oldValue,
                    NewValue = null
                };
                Changed?.Invoke(this, args);
            }
        }
    }
}
