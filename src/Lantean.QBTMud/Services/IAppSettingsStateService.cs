using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Exposes application-settings change notifications to UI consumers.
    /// </summary>
    public interface IAppSettingsStateService
    {
        /// <summary>
        /// Raised when application settings change.
        /// </summary>
        event EventHandler<AppSettingsChangedEventArgs>? SettingsChanged;
    }
}
