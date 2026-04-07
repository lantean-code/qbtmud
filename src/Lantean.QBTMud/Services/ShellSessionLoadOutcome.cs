namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Represents the outcome of a shell session load attempt.
    /// </summary>
    public enum ShellSessionLoadOutcome
    {
        Ready,
        AuthenticationRequired,
        LostConnection,
        RetryableFailure
    }
}
