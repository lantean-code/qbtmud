using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class GoogleTranslationTextProtectorTests
    {
        [Fact]
        public void GIVEN_ProtectedTermsPlaceholdersAndEntities_WHEN_Protect_THEN_ShouldUseNoTranslateMarkup()
        {
            var target = new GoogleTranslationTextProtector();

            var result = target.Protect(["Use qBittorrent with qbtmud &quot;%1&quot;"]);

            result.Should().ContainSingle();
            result[0].ProtectedText.Should().Be(
                "<div>Use <span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN0</span> with <span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN1</span> <span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN2</span><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN3</span><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN4</span></div>");
        }

        [Fact]
        public void GIVEN_HtmlTranslationResponse_WHEN_Restore_THEN_ShouldReturnPlainTranslation()
        {
            var target = new GoogleTranslationTextProtector();
            var protectedText = target.Protect(["Use qBittorrent with qbtmud &quot;%1&quot;"]).Single();

            var result = target.Restore(
                "<div>Utiliza <span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN0</span> amb <span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN1</span> <span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN2</span><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN3</span><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN4</span></div>",
                protectedText.TokenMap);

            result.Should().Be("Utiliza qBittorrent amb qbtmud &quot;%1&quot;");
        }
    }
}
