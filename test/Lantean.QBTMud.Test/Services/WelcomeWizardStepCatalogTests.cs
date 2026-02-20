using AwesomeAssertions;
using Lantean.QBTMud.Services;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class WelcomeWizardStepCatalogTests
    {
        [Fact]
        public void GIVEN_CatalogSteps_WHEN_ReadingKnownStepIds_THEN_PreservesDisplayOrder()
        {
            WelcomeWizardStepCatalog.KnownStepIds.Should().ContainInOrder(
                WelcomeWizardStepCatalog.LanguageStepId,
                WelcomeWizardStepCatalog.ThemeStepId,
                WelcomeWizardStepCatalog.NotificationsStepId);
        }

        [Fact]
        public void GIVEN_LegacyAcknowledgedSteps_WHEN_ReadingCatalog_THEN_ExcludesNotificationsStep()
        {
            WelcomeWizardStepCatalog.LegacyAcknowledgedStepIds.Should().ContainInOrder(
                WelcomeWizardStepCatalog.LanguageStepId,
                WelcomeWizardStepCatalog.ThemeStepId);
            WelcomeWizardStepCatalog.LegacyAcknowledgedStepIds.Should().NotContain(WelcomeWizardStepCatalog.NotificationsStepId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("welcome.unknown.v1")]
        public void GIVEN_NullWhitespaceOrUnknownStepId_WHEN_IsKnownStepIdInvoked_THEN_ReturnsFalse(string? stepId)
        {
            var result = WelcomeWizardStepCatalog.IsKnownStepId(stepId);

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_KnownStepIdWithWhitespace_WHEN_IsKnownStepIdInvoked_THEN_ReturnsTrue()
        {
            var result = WelcomeWizardStepCatalog.IsKnownStepId($" {WelcomeWizardStepCatalog.NotificationsStepId} ");

            result.Should().BeTrue();
        }
    }
}
