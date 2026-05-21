using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class GoogleTranslationOptionsTests : IDisposable
    {
        public void Dispose()
        {
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_GOOGLE_API_KEY", null);
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_GOOGLE_BASE_URL", null);
        }

        [Fact]
        public void GIVEN_ApiKeyIsMissing_WHEN_FromEnvironment_THEN_ShouldReturnNull()
        {
            var result = GoogleTranslationOptions.FromEnvironment();

            result.Should().BeNull();
        }

        [Fact]
        public void GIVEN_BaseUrlWithoutTrailingSlash_WHEN_FromEnvironment_THEN_ShouldNormalizeAndUseConfiguredValues()
        {
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_GOOGLE_API_KEY", "ApiKey");
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_GOOGLE_BASE_URL", "https://example.test/v2");

            var result = GoogleTranslationOptions.FromEnvironment();

            result.Should().NotBeNull();
            result!.ApiKey.Should().Be("ApiKey");
            result.BaseUrl.Should().Be("https://example.test/v2/");
        }

        [Fact]
        public void GIVEN_OnlyApiKey_WHEN_FromEnvironment_THEN_ShouldUseDefaultBaseUrl()
        {
            Environment.SetEnvironmentVariable("QBTMUD_TRANSLATIONS_GOOGLE_API_KEY", "ApiKey");

            var result = GoogleTranslationOptions.FromEnvironment();

            result.Should().NotBeNull();
            result!.BaseUrl.Should().Be("https://translation.googleapis.com/language/translate/v2/");
        }
    }
}
