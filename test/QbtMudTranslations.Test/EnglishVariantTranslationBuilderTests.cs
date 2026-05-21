using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class EnglishVariantTranslationBuilderTests
    {
        [Theory]
        [InlineData("en_AU")]
        [InlineData("en_GB")]
        public void GIVEN_DerivedEnglishLocale_WHEN_IsDerivedLocale_THEN_ShouldReturnTrue(string locale)
        {
            var result = EnglishVariantTranslationBuilder.IsDerivedLocale(locale);

            result.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_NonDerivedLocale_WHEN_IsDerivedLocale_THEN_ShouldReturnFalse()
        {
            var result = EnglishVariantTranslationBuilder.IsDerivedLocale("en");

            result.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_DerivedEnglishLocale_WHEN_TransformText_THEN_ShouldApplyConfiguredSpellingsAndPreserveSimpleCase()
        {
            var result = EnglishVariantTranslationBuilder.TransformText(
                "Color, Colors, COLOR, customize, Favorite, behavior, initialize and organize.",
                "en_GB");

            result.Should().Be("Colour, Colours, COLOUR, customise, Favourite, behaviour, initialise and organise.");
        }

        [Fact]
        public void GIVEN_DerivedEnglishLocale_WHEN_TransformText_THEN_ShouldLeaveAmbiguousTermsUnchanged()
        {
            var result = EnglishVariantTranslationBuilder.TransformText(
                "External program, license terms, and practice mode.",
                "en_AU");

            result.Should().Be("External program, license terms, and practice mode.");
        }

        [Fact]
        public void GIVEN_UnsupportedLocale_WHEN_TransformText_THEN_ShouldReturnOriginalText()
        {
            var result = EnglishVariantTranslationBuilder.TransformText("Colors", "en");

            result.Should().Be("Colors");
        }

        [Fact]
        public void GIVEN_UnsupportedLocale_WHEN_BuildLocaleTranslations_THEN_ShouldThrow()
        {
            var action = () => EnglishVariantTranslationBuilder.BuildLocaleTranslations(
                "en",
                [new KeyValuePair<string, string>("Ctx|A", "Colors")]);

            action.Should().Throw<InvalidOperationException>().WithMessage("*supported derived English locale*");
        }
    }
}
