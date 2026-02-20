namespace Lantean.QBTMud.Interop
{
    /// <summary>
    /// Represents browser notification permission values.
    /// </summary>
    public enum BrowserNotificationPermission
    {
        /// <summary>
        /// Browser notification API is unavailable.
        /// </summary>
        Unsupported,

        /// <summary>
        /// Permission has not been granted or denied.
        /// </summary>
        Default,

        /// <summary>
        /// Permission has been granted.
        /// </summary>
        Granted,

        /// <summary>
        /// Permission has been denied.
        /// </summary>
        Denied
    }
}
