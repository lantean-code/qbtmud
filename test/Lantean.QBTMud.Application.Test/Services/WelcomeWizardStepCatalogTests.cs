using AwesomeAssertions;
using Lantean.QBTMud.Application.Services;

namespace Lantean.QBTMud.Application.Test.Services
{
    public sealed class WelcomeWizardStepCatalogTests
    {
        [Fact]
        public void GIVEN_CatalogSteps_WHEN_ReadingKnownStepIds_THEN_PreservesDisplayOrder()
        {
            WelcomeWizardStepCatalog.KnownStepIds.Should().ContainInOrder(
                WelcomeWizardStepCatalog.LanguageStepId,
                WelcomeWizardStepCatalog.ThemeStepId,
                WelcomeWizardStepCatalog.NotificationsStepId,
                WelcomeWizardStepCatalog.StorageStepId);
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

        [Fact]
        public void GIVEN_ClientDataUnsupported_WHEN_GetStepsInvoked_THEN_ShouldExcludeStorageStep()
        {
            var result = WelcomeWizardStepCatalog.GetSteps(includeStorageStep: false);

            result.Select(step => step.Id).Should().NotContain(WelcomeWizardStepCatalog.StorageStepId);
        }
    }
}
