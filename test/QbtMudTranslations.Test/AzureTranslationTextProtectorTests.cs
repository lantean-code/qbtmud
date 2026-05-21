using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class AzureTranslationTextProtectorTests
    {
        [Fact]
        public void GIVEN_ProtectedTermsPlaceholdersAndEntities_WHEN_Protect_THEN_ShouldUseAzureNativeMarkup()
        {
            var target = new AzureTranslationTextProtector();

            var result = target.Protect(["Use qBittorrent with qbtmud &quot;%1&quot;"]);

            result.Should().ContainSingle();
            result[0].ProtectedText.Should().Be(
                "<div>Use <mstrans:dictionary translation=\"qBittorrent\">qBittorrent</mstrans:dictionary> with <mstrans:dictionary translation=\"qbtmud\">qbtmud</mstrans:dictionary> <span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN0</span><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN1</span><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN2</span></div>");
        }

        [Fact]
        public void GIVEN_HtmlTranslationResponse_WHEN_Restore_THEN_ShouldReturnPlainTranslation()
        {
            var target = new AzureTranslationTextProtector();
            var protectedText = target.Protect(["Use qBittorrent with qbtmud &quot;%1&quot;"]).Single();

            var result = target.Restore(
                "<div>Utiliza qBittorrent con qbtmud <span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN0</span><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN1</span><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN2</span></div>",
                protectedText.TokenMap);

            result.Should().Be("Utiliza qBittorrent con qbtmud &quot;%1&quot;");
        }
    }
}
