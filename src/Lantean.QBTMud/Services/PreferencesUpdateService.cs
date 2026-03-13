using Lantean.QBitTorrentClient.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Default in-app publisher for updated application preferences.
    /// </summary>
    public sealed class PreferencesUpdateService : IPreferencesUpdateService
    {
        /// <inheritdoc />
        public event PreferencesUpdatedAsyncHandler? PreferencesUpdated;

        /// <inheritdoc />
        public async ValueTask PublishAsync(Preferences preferences)
        {
            ArgumentNullException.ThrowIfNull(preferences);

            var preferencesUpdated = PreferencesUpdated;
            if (preferencesUpdated is null)
            {
                return;
            }

            var invocationList = preferencesUpdated.GetInvocationList();
            for (var i = 0; i < invocationList.Length; i++)
            {
                var handler = (PreferencesUpdatedAsyncHandler)invocationList[i];
                await handler(preferences);
            }
        }
    }
}
