namespace Lantean.QBT.Services
{
    public interface IStorageService
    {
        Task ClearAsync(CancellationToken cancellationToken = default);

        Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);

        Task<bool> ContainsKeyAsync(string key, CancellationToken cancellationToken = default);

        Task RemoveItemAsync(string key, CancellationToken cancellationToken = default);

        Task RemoveItemsAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

        Task SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default);
    }
}
