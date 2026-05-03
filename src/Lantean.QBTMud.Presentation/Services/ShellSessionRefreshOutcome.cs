namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Represents the outcome of a shell session refresh attempt.
    /// </summary>
    public enum ShellSessionRefreshOutcome
    {
        Updated,
        NoChange,
        AuthenticationRequired,
        LostConnection,
        RetryableFailure
    }
}
