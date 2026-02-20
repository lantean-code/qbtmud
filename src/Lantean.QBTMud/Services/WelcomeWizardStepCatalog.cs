using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services
{
    /// <summary>
    /// Provides the ordered catalog of welcome wizard steps.
    /// </summary>
    public static class WelcomeWizardStepCatalog
    {
        /// <summary>
        /// Gets the language step identifier.
        /// </summary>
        public const string LanguageStepId = "welcome.language.v1";

        /// <summary>
        /// Gets the theme step identifier.
        /// </summary>
        public const string ThemeStepId = "welcome.theme.v1";

        /// <summary>
        /// Gets the notifications step identifier.
        /// </summary>
        public const string NotificationsStepId = "welcome.notifications.v1";

        /// <summary>
        /// Gets all wizard steps in display order.
        /// </summary>
        public static IReadOnlyList<WelcomeWizardStepDefinition> Steps { get; } =
        [
            new WelcomeWizardStepDefinition(LanguageStepId, 0),
            new WelcomeWizardStepDefinition(ThemeStepId, 1),
            new WelcomeWizardStepDefinition(NotificationsStepId, 2)
        ];

        /// <summary>
        /// Gets the step identifiers acknowledged for users migrated from legacy completion state.
        /// </summary>
        public static IReadOnlyList<string> LegacyAcknowledgedStepIds { get; } =
        [
            LanguageStepId,
            ThemeStepId
        ];

        /// <summary>
        /// Gets the full list of known step identifiers.
        /// </summary>
        public static IReadOnlyList<string> KnownStepIds { get; } =
            Steps.Select(step => step.Id).ToList();

        /// <summary>
        /// Determines whether the specified step identifier exists in the catalog.
        /// </summary>
        /// <param name="stepId">The step identifier to check.</param>
        /// <returns><see langword="true"/> when known; otherwise <see langword="false"/>.</returns>
        public static bool IsKnownStepId(string? stepId)
        {
            if (string.IsNullOrWhiteSpace(stepId))
            {
                return false;
            }

            return KnownStepIds.Contains(stepId.Trim(), StringComparer.Ordinal);
        }
    }
}
