using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class ProtectedTranslationClientTests
    {
        [Fact]
        public async Task GIVEN_HtmlProtectedBackendResponse_WHEN_TranslateAsync_THEN_ShouldRestoreProtectedFragments()
        {
            var translationBackend = new Mock<ITranslationBackend>();
            translationBackend
                .Setup(mock => mock.TranslateAsync(
                    "oc",
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    "<div>Utiliza <span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN0</span> amb <span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN1</span> <span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN2</span><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN3</span><span class=\"notranslate\" translate=\"no\">QBTMUDTOKEN4</span></div>"
                ]);
            var target = new ProtectedTranslationClient(translationBackend.Object, new GoogleTranslationTextProtector());

            var result = await target.TranslateAsync("oc", ["Use qBittorrent with qbtmud &quot;%1&quot;"], TestContext.Current.CancellationToken);

            result.Should().ContainSingle().Which.Should().Be("Utiliza qBittorrent amb qbtmud &quot;%1&quot;");
        }

        [Fact]
        public async Task GIVEN_BackendCountMismatch_WHEN_TranslateAsync_THEN_ShouldThrow()
        {
            var translationBackend = new Mock<ITranslationBackend>();
            translationBackend
                .Setup(mock => mock.TranslateAsync(
                    "oc",
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);
            var target = new ProtectedTranslationClient(translationBackend.Object, new GoogleTranslationTextProtector());

            var action = async () => await target.TranslateAsync("oc", ["Source"], TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
