namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides local storage keys used by the welcome wizard.
    /// </summary>
    public static class WelcomeWizardStorageKeys
    {
        /// <summary>
        /// Gets the legacy key indicating the user has completed the welcome wizard.
        /// </summary>
        public const string Completed = "WelcomeWizard.Completed.v1";

        /// <summary>
        /// Gets the versioned state key used for incremental wizard progress.
        /// </summary>
        public const string State = "WelcomeWizard.State.v2";
    }
}
