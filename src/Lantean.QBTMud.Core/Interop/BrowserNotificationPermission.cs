namespace Lantean.QBTMud.Core.Interop
{
    /// <summary>
    /// Represents browser notification permission values.
    /// </summary>
    public enum BrowserNotificationPermission
    {
        /// <summary>
        /// Browser notification permission could not be determined.
        /// </summary>
        Unknown,

        /// <summary>
        /// Browser notification API is unavailable.
        /// </summary>
        Unsupported,

        /// <summary>
        /// Browser notification API requires a secure context.
        /// </summary>
        Insecure,

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
