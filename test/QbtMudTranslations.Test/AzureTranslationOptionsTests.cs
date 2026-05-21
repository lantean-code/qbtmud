using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class AzureTranslationOptionsTests : IDisposable
    {
        public void Dispose()
        {
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_AZURE_API_KEY", null);
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_AZURE_BASE_URL", null);
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_AZURE_REGION", null);
        }

        [Fact]
        public void GIVEN_ApiKeyIsMissing_WHEN_FromEnvironment_THEN_ShouldReturnNull()
        {
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_AZURE_REGION", "westeurope");

            var result = AzureTranslationOptions.FromEnvironment();

            result.Should().BeNull();
        }

        [Fact]
        public void GIVEN_RegionIsMissing_WHEN_FromEnvironment_THEN_ShouldReturnNull()
        {
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_AZURE_API_KEY", "ApiKey");

            var result = AzureTranslationOptions.FromEnvironment();

            result.Should().BeNull();
        }

        [Fact]
        public void GIVEN_BaseUrlWithoutTrailingSlash_WHEN_FromEnvironment_THEN_ShouldNormalizeAndUseConfiguredValues()
        {
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_AZURE_API_KEY", "ApiKey");
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_AZURE_BASE_URL", "https://example.test");
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_AZURE_REGION", "westeurope");

            var result = AzureTranslationOptions.FromEnvironment();

            result.Should().NotBeNull();
            result!.ApiKey.Should().Be("ApiKey");
            result.BaseUrl.Should().Be("https://example.test/");
            result.Region.Should().Be("westeurope");
        }
    }
}
