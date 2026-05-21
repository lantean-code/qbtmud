using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class TranslationProviderAvailabilityValidatorTests
    {
        [Fact]
        public void GIVEN_AzureOnlyLocaleSet_WHEN_GoogleIsMissing_THEN_ShouldNotRequireGoogle()
        {
            var target = new TranslationProviderAvailabilityValidator(hasAzureProvider: true, hasGoogleProvider: false);

            var result = target.ValidateSyncConfiguration(["en", "fr"]);

            result.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_GoogleOnlyLocaleSet_WHEN_GoogleIsMissing_THEN_ShouldRequireGoogle()
        {
            var target = new TranslationProviderAvailabilityValidator(hasAzureProvider: true, hasGoogleProvider: false);

            var result = target.ValidateSyncConfiguration(["en", "ltg"]);

            result.Should().ContainSingle()
                .Which.Should().Be("Sync mode requires Google Cloud Translation configuration. Set QBTMUD_TRANSLATIONS_GOOGLE_API_KEY.");
        }

        [Fact]
        public void GIVEN_MixedLocaleSet_WHEN_GoogleIsMissing_THEN_ShouldRequireGoogleOnlyOnce()
        {
            var target = new TranslationProviderAvailabilityValidator(hasAzureProvider: true, hasGoogleProvider: false);

            var result = target.ValidateSyncConfiguration(["en", "fr", "ltg"]);

            result.Should().ContainSingle()
                .Which.Should().Be("Sync mode requires Google Cloud Translation configuration. Set QBTMUD_TRANSLATIONS_GOOGLE_API_KEY.");
        }

        [Fact]
        public void GIVEN_AzureOnlyLocaleSet_WHEN_AzureIsMissing_THEN_ShouldRequireAzure()
        {
            var target = new TranslationProviderAvailabilityValidator(hasAzureProvider: false, hasGoogleProvider: true);

            var result = target.ValidateSyncConfiguration(["en", "fr"]);

            result.Should().ContainSingle()
                .Which.Should().Be("Sync mode requires Azure Translator configuration. Set QBTMUD_TRANSLATIONS_AZURE_API_KEY and QBTMUD_TRANSLATIONS_AZURE_REGION.");
        }
    }
}
