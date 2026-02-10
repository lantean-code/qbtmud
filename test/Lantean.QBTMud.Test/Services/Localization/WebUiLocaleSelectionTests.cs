using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;

namespace Lantean.QBTMud.Test.Services.Localization
{
    public sealed class WebUiLocaleSelectionTests
    {
        [Fact]
        public void GIVEN_EmptyLanguageList_WHEN_Resolved_THEN_ReturnsEnFallback()
        {
            var result = WebUiLocaleSelection.ResolveLocale("en-US", Array.Empty<WebUiLanguageCatalogItem>());

            result.Should().Be("en");
        }

        [Fact]
        public void GIVEN_NullLocale_WHEN_Resolved_THEN_ReturnsEnWhenAvailable()
        {
            var languages = new List<WebUiLanguageCatalogItem>
            {
                new("fr", "French"),
                new("en", "English")
            };

            var result = WebUiLocaleSelection.ResolveLocale(null, languages);

            result.Should().Be("en");
        }

        [Fact]
        public void GIVEN_ExactLocaleMatch_WHEN_Resolved_THEN_ReturnsExactCode()
        {
            var languages = new List<WebUiLanguageCatalogItem>
            {
                new("en_GB", "English (United Kingdom)"),
                new("en", "English")
            };

            var result = WebUiLocaleSelection.ResolveLocale("en_GB", languages);

            result.Should().Be("en_GB");
        }

        [Fact]
        public void GIVEN_HyphenLocale_WHEN_Resolved_THEN_MatchesUnderscoreVariant()
        {
            var languages = new List<WebUiLanguageCatalogItem>
            {
                new("en_GB", "English (United Kingdom)"),
                new("en", "English")
            };

            var result = WebUiLocaleSelection.ResolveLocale("en-GB", languages);

            result.Should().Be("en_GB");
        }

        [Fact]
        public void GIVEN_RegionalLocaleWithoutMatch_WHEN_Resolved_THEN_FallsBackToBaseLanguage()
        {
            var languages = new List<WebUiLanguageCatalogItem>
            {
                new("en", "English"),
                new("fr", "French")
            };

            var result = WebUiLocaleSelection.ResolveLocale("en-US", languages);

            result.Should().Be("en");
        }

        [Fact]
        public void GIVEN_NoEnglishAndNoLocaleMatch_WHEN_Resolved_THEN_ReturnsFirstEntry()
        {
            var languages = new List<WebUiLanguageCatalogItem>
            {
                new("fr", "French"),
                new("de", "German")
            };

            var result = WebUiLocaleSelection.ResolveLocale("en-US", languages);

            result.Should().Be("fr");
        }
    }
}
