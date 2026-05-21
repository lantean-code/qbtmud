using System.Globalization;
using AwesomeAssertions;
using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Test.Services.Localization
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
        public void GIVEN_NullLocaleAndCurrentUiCultureMatch_WHEN_Resolved_THEN_ReturnsCurrentUiCultureLocale()
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("en", "English"),
                new("fr", "French")
            };
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;

            try
            {
                var culture = new CultureInfo("fr-FR");
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var result = LocaleSelection.ResolveLocale(null, languages);

                result.Should().Be("fr");
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }

        [Theory]
        [InlineData("sr-Latn-RS", "sr@latin")]
        [InlineData("uz-Latn-UZ", "uz@Latn")]
        [InlineData("sr-Cyrl-RS", "sr@cyrillic")]
        public void GIVEN_NullLocaleAndCurrentUiCultureUsesScriptTag_WHEN_Resolved_THEN_ReturnsMatchingScriptLocale(string cultureName, string expectedLocale)
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("en", "English"),
                new("sr@latin", "Serbian (Latin)"),
                new("sr@cyrillic", "Serbian (Cyrillic)"),
                new("uz@Latn", "Uzbek (Latin)")
            };
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;

            try
            {
                var culture = new CultureInfo(cultureName);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                var result = LocaleSelection.ResolveLocale(null, languages);

                result.Should().Be(expectedLocale);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
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

        [Theory]
        [InlineData("sr-Latn-RS", "sr@latin")]
        [InlineData("uz-Latn-UZ", "uz@Latn")]
        [InlineData("sr-Cyrl-RS", "sr@cyrillic")]
        public void GIVEN_RegionalLocaleWithScriptTag_WHEN_Resolved_THEN_UsesMatchingScriptLocale(string desiredLocale, string expectedLocale)
        {
            var languages = new List<LanguageCatalogItem>
            {
                new("en", "English"),
                new("sr@latin", "Serbian (Latin)"),
                new("sr@cyrillic", "Serbian (Cyrillic)"),
                new("uz@Latn", "Uzbek (Latin)")
            };

            var result = LocaleSelection.ResolveLocale(desiredLocale, languages);

            result.Should().Be(expectedLocale);
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
