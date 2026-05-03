using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Provides storage-type routing and migration for qbtmud settings keys.
    /// </summary>
    public interface IStorageRoutingService
    {
        /// <summary>
        /// Gets the persisted routing settings.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The routing settings.</returns>
        Task<StorageRoutingSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves routing settings and migrates changed items across storage types before persisting.
        /// </summary>
        /// <param name="settings">The routing settings to save.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The persisted routing settings.</returns>
        Task<StorageRoutingSettings> SaveSettingsAsync(StorageRoutingSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves the effective storage type for a runtime key using provided settings and capability state.
        /// </summary>
        /// <param name="key">The runtime storage key without qbtmud prefix.</param>
        /// <param name="settings">The routing settings.</param>
        /// <param name="supportsClientData">Whether ClientData is currently supported.</param>
        /// <returns>The effective storage type.</returns>
        StorageType ResolveEffectiveStorageType(string key, StorageRoutingSettings settings, bool supportsClientData);
    }
}
