using System.Collections.Concurrent;

namespace Lantean.QBT.Services
{
    public sealed class InMemoryStorageService : IStorageService
    {
        private readonly ConcurrentDictionary<string, object> _items = [];

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            _items.Clear();

            return Task.CompletedTask;
        }

        public Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            var containsKey = _items.ContainsKey(key);

            return Task.FromResult(containsKey);
        }

        public Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            T? result = default;
            if (_items.TryGetValue(key, out var item) && item is T typedItem)
            {
                result = typedItem;
            }
            return Task.FromResult(result);
        }

        public Task RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            _items.TryRemove(key, out _);

            return Task.CompletedTask;
        }

        public Task RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                _items.TryRemove(key, out _);
            }

            return Task.CompletedTask;
        }

        public Task SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(data);

            _items.AddOrUpdate(key, data, (key, value) => data);

            return Task.CompletedTask;
        }
    }
}
