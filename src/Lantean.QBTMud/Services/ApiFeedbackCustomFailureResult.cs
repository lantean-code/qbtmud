namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Indicates how <see cref="IApiFeedbackWorkflow"/> should proceed after a caller-specific failure callback runs.
    /// </summary>
    public enum ApiFeedbackCustomFailureResult
    {
        /// <summary>
        /// Continue with the normal workflow handling after the custom callback completes.
        /// </summary>
        ContinueWithWorkflow = 0,

        /// <summary>
        /// Stop workflow handling because the caller has fully handled the failure.
        /// </summary>
        StopHandling = 1,
    }
}
