using Blazored.LocalStorage;
using Lantean.QBT.Services;

namespace Lantean.QBTMud.Services
{
    public class LocalStorageStorageService : IStorageService
    {
        private readonly ILocalStorageService _localStorageService;

        public LocalStorageStorageService(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            await _localStorageService.ClearAsync(cancellationToken);
        }

        public async Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            return await _localStorageService.ContainKeyAsync(key, cancellationToken);
        }

        public async Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            return await _localStorageService.GetItemAsync<T>(key, cancellationToken);
        }

        public async Task RemoveItemAsync(string key, CancellationToken cancellationToken = default)
        {
            await _localStorageService.RemoveItemAsync(key, cancellationToken);
        }

        public async Task RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            await _localStorageService.RemoveItemsAsync(keys, cancellationToken);
        }

        public async Task SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            await _localStorageService.SetItemAsync(key, data, cancellationToken);
        }
    }
}
