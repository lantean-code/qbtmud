using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class CloudLocaleMapperTests
    {
        [Theory]
        [InlineData("sr@latin", "sr-Latn")]
        [InlineData("mn_MN", "mn-Cyrl")]
        [InlineData("zh_HK", "yue")]
        [InlineData("pt_PT", "pt-PT")]
        [InlineData("uz@Latn", "uz")]
        [InlineData("ne_NP", "ne")]
        public void GIVEN_AzureMappedLocale_WHEN_TryResolveAzureLocale_THEN_ShouldReturnExpectedLocale(
            string locale,
            string expectedLocale)
        {
            var result = CloudLocaleMapper.TryResolveAzureLocale(locale, out var targetLocale);

            result.Should().BeTrue();
            targetLocale.Should().Be(expectedLocale);
        }

        [Theory]
        [InlineData("eo", "eo")]
        [InlineData("ltg", "ltg")]
        [InlineData("oc", "oc")]
        public void GIVEN_GooglePrimaryLocale_WHEN_TryResolveLocales_THEN_ShouldSkipAzureAndUseGoogle(
            string locale,
            string expectedGoogleLocale)
        {
            var azureResult = CloudLocaleMapper.TryResolveAzureLocale(locale, out var azureLocale);
            var googleResult = CloudLocaleMapper.TryResolveGoogleLocale(locale, out var googleLocale);

            azureResult.Should().BeFalse();
            azureLocale.Should().BeNull();
            googleResult.Should().BeTrue();
            googleLocale.Should().Be(expectedGoogleLocale);
        }

        [Theory]
        [InlineData("en")]
        [InlineData("en_AU")]
        [InlineData("en_GB")]
        public void GIVEN_EnglishLocale_WHEN_IsEnglishLocale_THEN_ShouldReturnTrue(string locale)
        {
            var result = CloudLocaleMapper.IsEnglishLocale(locale);

            result.Should().BeTrue();
        }
    }
}
