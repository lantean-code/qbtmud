using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Stores runtime app settings shared by authenticated UI components.
    /// </summary>
    public interface IAppSettingsStateService
    {
        /// <summary>
        /// Occurs when the runtime app-settings snapshot changes.
        /// </summary>
        event EventHandler<AppSettingsChangedEventArgs>? Changed;

        /// <summary>
        /// Gets the current runtime app-settings snapshot.
        /// </summary>
        AppSettings? Current { get; }

        /// <summary>
        /// Sets the current runtime app-settings snapshot.
        /// </summary>
        /// <param name="settings">The runtime app-settings snapshot.</param>
        /// <returns><see langword="true"/> when the snapshot changed; otherwise, <see langword="false"/>.</returns>
        bool SetSettings(AppSettings? settings);
    }
}
