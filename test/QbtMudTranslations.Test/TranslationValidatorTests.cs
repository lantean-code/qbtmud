using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class TranslationValidatorTests
    {
        [Fact]
        public void GIVEN_MissingAndExtraKeys_WHEN_ValidateLocale_THEN_ShouldReportShapeErrors()
        {
            var englishTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "One",
                ["Ctx|Two"] = "Two"
            };
            var localeTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Uno",
                ["Ctx|Three"] = "Tres"
            };

            var target = new TranslationValidator();

            var result = target.ValidateLocale("es", englishTranslations, localeTranslations);

            result.Should().Contain(error => error.Contains("missing key 'Ctx|Two'", StringComparison.Ordinal));
            result.Should().Contain(error => error.Contains("extra key 'Ctx|Three'", StringComparison.Ordinal));
        }

        [Fact]
        public void GIVEN_PlaceholdersEntitiesAndProductTerms_WHEN_ValidateLocale_THEN_ShouldReportMismatch()
        {
            var englishTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Use qBittorrent with qbtmud &quot;%1&quot;"
            };
            var localeTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Usa qBitorrent con qbtmud \"%2\""
            };

            var target = new TranslationValidator();

            var result = target.ValidateLocale("es", englishTranslations, localeTranslations);

            result.Should().Contain(error => error.Contains("placeholder", StringComparison.Ordinal));
            result.Should().Contain(error => error.Contains("HTML entity", StringComparison.Ordinal));
            result.Should().Contain(error => error.Contains("qBittorrent", StringComparison.Ordinal));
        }

        [Fact]
        public void GIVEN_PercentageFormattedAsPercentThenDigits_WHEN_ValidateLocale_THEN_ShouldNotTreatItAsPlaceholder()
        {
            var englishTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Less Than 100% Availability"
            };
            var localeTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Erabilgarritasuna %100 baino txikiagoa"
            };

            var target = new TranslationValidator();

            var result = target.ValidateLocale("eu", englishTranslations, localeTranslations);

            result.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_EnglishUsesPlaceholderFollowedByPercentSign_WHEN_LocaleDropsPlaceholder_THEN_ShouldReportPlaceholderMismatch()
        {
            var englishTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "%1% complete - %2 downloaded, %3 in progress"
            };
            var localeTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Tamamlandı - %2 indirildi, %3 devam ediyor"
            };

            var target = new TranslationValidator();

            var result = target.ValidateLocale("tr", englishTranslations, localeTranslations);

            result.Should().Contain(error => error.Contains("placeholder", StringComparison.Ordinal));
        }

        [Fact]
        public void GIVEN_EnglishUsesPlaceholderFollowedByPercentSign_WHEN_LocaleKeepsPlaceholderWithoutTrailingPercentSign_THEN_ShouldNotTreatItAsLiteralPercent()
        {
            var englishTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Pieces progress for torrent %1: %2% complete. %3 downloaded, %4 downloading, %5 pending."
            };
            var localeTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Fortschritt für Torrent %1: %2 abgeschlossen. %3 heruntergeladen, %4 wird heruntergeladen, %5 ausstehend."
            };

            var target = new TranslationValidator();

            var result = target.ValidateLocale("de", englishTranslations, localeTranslations);

            result.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_PercentageFormattedAsDigitsThenPercentInEnglishAndPercentThenDigitsInLocale_WHEN_ValidateLocale_THEN_ShouldNotTreatItAsPlaceholder()
        {
            var englishTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Less than 80% Availability"
            };
            var localeTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "%80'in altında kullanılabilirlik"
            };

            var target = new TranslationValidator();

            var result = target.ValidateLocale("tr", englishTranslations, localeTranslations);

            result.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_LocaleContainsLiteralPercentageAndMissingExpectedPlaceholder_WHEN_ValidateLocale_THEN_ShouldStillReportPlaceholderMismatch()
        {
            var englishTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Done %1 and 80% complete"
            };
            var localeTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "%80 tamamlandı"
            };

            var target = new TranslationValidator();

            var result = target.ValidateLocale("tr", englishTranslations, localeTranslations);

            result.Should().Contain(error => error.Contains("placeholder", StringComparison.Ordinal));
        }

        [Fact]
        public void GIVEN_LocaleIntroducesUnexpectedPlaceholder_WHEN_ValidateLocale_THEN_ShouldReportPlaceholderMismatch()
        {
            var englishTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Done %1"
            };
            var localeTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Fertig %1 %2"
            };

            var target = new TranslationValidator();

            var result = target.ValidateLocale("de", englishTranslations, localeTranslations);

            result.Should().Contain(error => error.Contains("placeholder", StringComparison.Ordinal));
        }

        [Fact]
        public void GIVEN_EnglishHasNoPlaceholderAndLocaleAddsOne_WHEN_ValidateLocale_THEN_ShouldReportPlaceholderMismatch()
        {
            var englishTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Done"
            };
            var localeTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|One"] = "Fertig %1"
            };

            var target = new TranslationValidator();

            var result = target.ValidateLocale("de", englishTranslations, localeTranslations);

            result.Should().Contain(error => error.Contains("placeholder", StringComparison.Ordinal));
        }
    }
}
