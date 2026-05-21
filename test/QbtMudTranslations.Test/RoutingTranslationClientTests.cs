using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class RoutingTranslationClientTests
    {
        [Fact]
        public async Task GIVEN_EnglishLocale_WHEN_TranslateAsync_THEN_ShouldReturnSourceTextsWithoutInvokingBackends()
        {
            var azureTranslationClient = new Mock<ITranslationClient>();
            var googleTranslationClient = new Mock<ITranslationClient>();
            var target = new RoutingTranslationClient(azureTranslationClient.Object, googleTranslationClient.Object);

            var result = await target.TranslateAsync("en_GB", ["Source"], TestContext.Current.CancellationToken);

            result.Should().ContainSingle().Which.Should().Be("Source");
            azureTranslationClient.Verify(
                mock => mock.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            googleTranslationClient.Verify(
                mock => mock.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_AzureSupportedLocale_WHEN_TranslateAsync_THEN_ShouldUseAzureClientWithMappedLocale()
        {
            var azureTranslationClient = new Mock<ITranslationClient>();
            azureTranslationClient
                .Setup(mock => mock.TranslateAsync(
                    "pt",
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["Azure"]);
            var googleTranslationClient = new Mock<ITranslationClient>();
            var target = new RoutingTranslationClient(azureTranslationClient.Object, googleTranslationClient.Object);

            var result = await target.TranslateAsync("pt_BR", ["Source"], TestContext.Current.CancellationToken);

            result.Should().ContainSingle().Which.Should().Be("Azure");
            azureTranslationClient.Verify(
                mock => mock.TranslateAsync(
                    "pt",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Source"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            googleTranslationClient.Verify(
                mock => mock.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_GooglePrimaryLocale_WHEN_TranslateAsync_THEN_ShouldUseGoogleClient()
        {
            var azureTranslationClient = new Mock<ITranslationClient>();
            var googleTranslationClient = new Mock<ITranslationClient>();
            googleTranslationClient
                .Setup(mock => mock.TranslateAsync(
                    "ltg",
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["Google"]);
            var target = new RoutingTranslationClient(azureTranslationClient.Object, googleTranslationClient.Object);

            var result = await target.TranslateAsync("ltg", ["Source"], TestContext.Current.CancellationToken);

            result.Should().ContainSingle().Which.Should().Be("Google");
            azureTranslationClient.Verify(
                mock => mock.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            googleTranslationClient.Verify(
                mock => mock.TranslateAsync(
                    "ltg",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Source"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_AzureClientRejectsLocale_WHEN_TranslateAsync_THEN_ShouldRetryWithGoogle()
        {
            var azureTranslationClient = new Mock<ITranslationClient>();
            azureTranslationClient
                .Setup(mock => mock.TranslateAsync(
                    "be",
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TranslationBackendRejectedException("Rejected", System.Net.HttpStatusCode.BadRequest));
            var googleTranslationClient = new Mock<ITranslationClient>();
            googleTranslationClient
                .Setup(mock => mock.TranslateAsync(
                    "be",
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["Google"]);
            var target = new RoutingTranslationClient(azureTranslationClient.Object, googleTranslationClient.Object);

            var result = await target.TranslateAsync("be", ["Source"], TestContext.Current.CancellationToken);

            result.Should().ContainSingle().Which.Should().Be("Google");
            azureTranslationClient.Verify(
                mock => mock.TranslateAsync(
                    "be",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Source"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            googleTranslationClient.Verify(
                mock => mock.TranslateAsync(
                    "be",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Source"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_AzureClientThrowsServerError_WHEN_TranslateAsync_THEN_ShouldNotRetryWithGoogle()
        {
            var azureTranslationClient = new Mock<ITranslationClient>();
            azureTranslationClient
                .Setup(mock => mock.TranslateAsync(
                    "be",
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Server error"));
            var googleTranslationClient = new Mock<ITranslationClient>();
            var target = new RoutingTranslationClient(azureTranslationClient.Object, googleTranslationClient.Object);

            var action = async () => await target.TranslateAsync("be", ["Source"], TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("Server error");
            googleTranslationClient.Verify(
                mock => mock.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_GooglePrimaryLocaleWithoutGoogleClient_WHEN_TranslateAsync_THEN_ShouldThrow()
        {
            var target = new RoutingTranslationClient(azureTranslationClient: null, googleTranslationClient: null);

            var action = async () => await target.TranslateAsync("ltg", ["Source"], TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*ltg*");
        }

        [Fact]
        public async Task GIVEN_AzureSupportedLocaleWithoutAzureClient_WHEN_TranslateAsync_THEN_ShouldThrow()
        {
            var googleTranslationClient = new Mock<ITranslationClient>();
            var target = new RoutingTranslationClient(azureTranslationClient: null, googleTranslationClient.Object);

            var action = async () => await target.TranslateAsync("be", ["Source"], TestContext.Current.CancellationToken);

            await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Azure Translator*");
            googleTranslationClient.Verify(
                mock => mock.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
