using AwesomeAssertions;
using Lantean.QBTMud.Services.Localization;

namespace Lantean.QBTMud.Test.Services.Localization
{
    public sealed class WebUiLocaleNormalizerTests
    {
        [Fact]
        public void GIVEN_RegularLocale_WHEN_Normalized_THEN_ShouldReturnTrimmedLocale()
        {
            var result = WebUiLocaleNormalizer.Normalize(" fr_FR ");

            result.Should().Be("fr_FR");
        }

        [Theory]
        [InlineData("C")]
        [InlineData("POSIX")]
        [InlineData("C.UTF-8")]
        [InlineData("POSIX@latin")]
        public void GIVEN_SystemDefaultLocaleMarker_WHEN_Normalized_THEN_ShouldReturnEnglish(string locale)
        {
            var result = WebUiLocaleNormalizer.Normalize(locale);

            result.Should().Be("en");
        }

        [Fact]
        public void GIVEN_WhitespaceLocale_WHEN_Normalized_THEN_ShouldThrowArgumentException()
        {
            var action = () => WebUiLocaleNormalizer.Normalize(" ");

            action.Should().Throw<ArgumentException>();
        }
    }
}
