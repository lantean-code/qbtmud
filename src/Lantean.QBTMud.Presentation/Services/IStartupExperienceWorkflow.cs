namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Coordinates welcome wizard and startup update-check experiences.
    /// </summary>
    public interface IStartupExperienceWorkflow
    {
        /// <summary>
        /// Runs the welcome wizard flow when needed.
        /// </summary>
        /// <param name="initialLocale">The initial locale to seed into the wizard.</param>
        /// <param name="useFullScreenDialog">A value indicating whether the wizard should use a full-screen dialog.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns><see langword="true"/> when the PWA install prompt flow may continue; otherwise, <see langword="false"/>.</returns>
        Task<bool> RunWelcomeWizardAsync(string? initialLocale, bool useFullScreenDialog, CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs the startup update check when enabled.
        /// </summary>
        /// <param name="updateChecksEnabled">A value indicating whether update checks are enabled.</param>
        /// <param name="dismissedReleaseTag">The dismissed release tag, if any.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        Task RunUpdateCheckAsync(bool updateChecksEnabled, string? dismissedReleaseTag, CancellationToken cancellationToken = default);
    }
}
