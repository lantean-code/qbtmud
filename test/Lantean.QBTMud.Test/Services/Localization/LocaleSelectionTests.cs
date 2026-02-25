using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;

namespace Lantean.QBTMud.Test.Services.Localization
{
    public sealed class LocaleSelectionTests
    {
        [Fact]
        public void GIVEN_EmptyLanguageList_WHEN_Resolved_THEN_ReturnsEnFallback()
        {
            var result = LocaleSelection.ResolveLocale("en-US", Array.Empty<LanguageCatalogItem>());

            result.Should().Be("en");
        }

        [Fact]
        public void GIVEN_NullLocale_WHEN_Resolved_THEN_ReturnsEnWhenAvailable()
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("fr", "French"),
                new("en", "English")
            };

            var result = LocaleSelection.ResolveLocale(null, languages);

            result.Should().Be("en");
        }

        [Fact]
        public void GIVEN_ExactLocaleMatch_WHEN_Resolved_THEN_ReturnsExactCode()
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("en_GB", "English (United Kingdom)"),
                new("en", "English")
            };

            var result = LocaleSelection.ResolveLocale("en_GB", languages);

            result.Should().Be("en_GB");
        }

        [Fact]
        public void GIVEN_HyphenLocale_WHEN_Resolved_THEN_MatchesUnderscoreVariant()
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("en_GB", "English (United Kingdom)"),
                new("en", "English")
            };

            var result = LocaleSelection.ResolveLocale("en-GB", languages);

            result.Should().Be("en_GB");
        }

        [Fact]
        public void GIVEN_RegionalLocaleWithoutMatch_WHEN_Resolved_THEN_FallsBackToBaseLanguage()
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("en", "English"),
                new("fr", "French")
            };

            var result = LocaleSelection.ResolveLocale("en-US", languages);

            result.Should().Be("en");
        }

        [Fact]
        public void GIVEN_NoEnglishAndNoLocaleMatch_WHEN_Resolved_THEN_ReturnsFirstEntry()
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("fr", "French"),
                new("de", "German")
            };

            var result = LocaleSelection.ResolveLocale("en-US", languages);

            result.Should().Be("fr");
        }

        [Fact]
        public void GIVEN_NullLocaleAndNoEnglish_WHEN_Resolved_THEN_ReturnsFirstEntry()
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("fr", "French"),
                new("de", "German")
            };

            var result = LocaleSelection.ResolveLocale(null, languages);

            result.Should().Be("fr");
        }

        [Fact]
        public void GIVEN_UnderscoreLocale_WHEN_Resolved_THEN_MatchesHyphenVariant()
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("pt-BR", "Portuguese (Brazil)"),
                new("en", "English")
            };

            var result = LocaleSelection.ResolveLocale("pt_BR", languages);

            result.Should().Be("pt-BR");
        }

        [Fact]
        public void GIVEN_LocaleWithModifier_WHEN_Resolved_THEN_UsesBaseLocale()
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("_x", "Underscore Locale"),
                new("en", "English")
            };

            var result = LocaleSelection.ResolveLocale("-x@modifier", languages);

            result.Should().Be("_x");
        }

        [Fact]
        public void GIVEN_UnderscoreRegionalLocale_WHEN_Resolved_THEN_FallsBackToUnderscoreBase()
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("fr", "French"),
                new("en", "English")
            };

            var result = LocaleSelection.ResolveLocale("fr_CA", languages);

            result.Should().Be("fr");
        }

        [Fact]
        public void GIVEN_PlainUnknownLocale_WHEN_Resolved_THEN_FallsBackToEnglish()
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("en", "English"),
                new("fr", "French")
            };

            var result = LocaleSelection.ResolveLocale("zz", languages);

            result.Should().Be("en");
        }
    }
}
