using Lantean.QBTMud.Interop;

namespace Lantean.QBTMud.Models
{
    /// <summary>
    /// Describes a browser notification permission change.
    /// </summary>
    public sealed class BrowserNotificationPermissionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BrowserNotificationPermissionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="permission">The updated browser notification permission.</param>
        public BrowserNotificationPermissionChangedEventArgs(BrowserNotificationPermission permission)
        {
            Permission = permission;
        }

        /// <summary>
        /// Gets the updated browser notification permission.
        /// </summary>
        public BrowserNotificationPermission Permission { get; }
    }
}
