using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class TranslationSyncServiceTests : IDisposable
    {
        private readonly string _tempDirectoryPath;
        private readonly string _upstreamDirectoryPath;
        private readonly string _outputDirectoryPath;
        private readonly string _englishFilePath;

        public TranslationSyncServiceTests()
        {
            _tempDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            _upstreamDirectoryPath = Path.Combine(_tempDirectoryPath, "upstream");
            _outputDirectoryPath = Path.Combine(_tempDirectoryPath, "output");
            _englishFilePath = Path.Combine(_outputDirectoryPath, "qbtmud_en.json");

            Directory.CreateDirectory(_upstreamDirectoryPath);
            Directory.CreateDirectory(_outputDirectoryPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectoryPath))
            {
                Directory.Delete(_tempDirectoryPath, recursive: true);
            }
        }

        [Fact]
        public async Task GIVEN_SyncMode_WHEN_RunAsync_THEN_ShouldWriteAllDiscoveredLocaleFiles()
        {
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_de.ts"), string.Empty);
            File.WriteAllText(_englishFilePath, "{\n  \"Ctx|B\": \"B\",\n  \"Ctx|A\": \"A\"\n}");
            var translationClient = new Mock<ITranslationClient>();
            translationClient
                .Setup(mock => mock.TranslateAsync(
                    "de",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 2 && texts[0] == "B" && texts[1] == "A"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["B-DE", "A-DE"]);

            var target = new TranslationSyncService(
                new LocaleDiscoveryService(),
                translationClient.Object,
                new TranslationValidator(),
                new TranslationProviderAvailabilityValidator(hasAzureProvider: true, hasGoogleProvider: true),
                maxValidationRetries: 1);
            var options = new ToolOptions
            {
                EnglishFilePath = _englishFilePath,
                OutputDirectoryPath = _outputDirectoryPath,
                UpstreamTranslationsPath = _upstreamDirectoryPath,
                Mode = TranslationToolMode.Sync
            };

            var result = await target.RunAsync(options, TestContext.Current.CancellationToken);

            result.Should().Be(0);
            JsonTranslationDocument.LoadOrdered(Path.Combine(_outputDirectoryPath, "qbtmud_de.json"))
                .Select(entry => entry.Value)
                .Should()
                .Equal("B-DE", "A-DE");
            JsonTranslationDocument.LoadDictionary(Path.Combine(_outputDirectoryPath, "qbtmud_en.json"))
                .Should()
                .Contain(new KeyValuePair<string, string>("Ctx|A", "A"));
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    "de",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 2 && texts[0] == "B" && texts[1] == "A"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_DerivedEnglishLocale_WHEN_RunAsync_THEN_ShouldWriteSubstitutedSpellingsWithoutInvokingTranslationClient()
        {
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en_GB.ts"), string.Empty);
            File.WriteAllText(_englishFilePath, "{\n  \"Ctx|A\": \"Colors are easy to customize.\",\n  \"Ctx|B\": \"Program and license stay the same.\"\n}");
            var translationClient = new Mock<ITranslationClient>(MockBehavior.Strict);

            var target = new TranslationSyncService(
                new LocaleDiscoveryService(),
                translationClient.Object,
                new TranslationValidator(),
                new TranslationProviderAvailabilityValidator(hasAzureProvider: false, hasGoogleProvider: false),
                maxValidationRetries: 1);
            var options = new ToolOptions
            {
                EnglishFilePath = _englishFilePath,
                OutputDirectoryPath = _outputDirectoryPath,
                UpstreamTranslationsPath = _upstreamDirectoryPath,
                Mode = TranslationToolMode.Sync
            };

            var result = await target.RunAsync(options, TestContext.Current.CancellationToken);

            result.Should().Be(0);
            JsonTranslationDocument.LoadDictionary(Path.Combine(_outputDirectoryPath, "qbtmud_en_GB.json"))
                .Should()
                .Contain(new KeyValuePair<string, string>("Ctx|A", "Colours are easy to customise."))
                .And.Contain(new KeyValuePair<string, string>("Ctx|B", "Program and license stay the same."));
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_DerivedEnglishLocaleAndExistingLocaleFile_WHEN_RunAsync_THEN_ShouldRegenerateValuesFromEnglishSource()
        {
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en_AU.ts"), string.Empty);
            File.WriteAllText(_englishFilePath, "{\n  \"Ctx|A\": \"Dark Colors\",\n  \"Ctx|B\": \"Customize storage\"\n}");
            File.WriteAllText(Path.Combine(_outputDirectoryPath, "qbtmud_en_AU.json"), "{\n  \"Ctx|A\": \"Stale value\",\n  \"Ctx|B\": \"Another stale value\"\n}");
            var translationClient = new Mock<ITranslationClient>(MockBehavior.Strict);

            var target = new TranslationSyncService(
                new LocaleDiscoveryService(),
                translationClient.Object,
                new TranslationValidator(),
                new TranslationProviderAvailabilityValidator(hasAzureProvider: false, hasGoogleProvider: false),
                maxValidationRetries: 1);
            var options = new ToolOptions
            {
                EnglishFilePath = _englishFilePath,
                OutputDirectoryPath = _outputDirectoryPath,
                UpstreamTranslationsPath = _upstreamDirectoryPath,
                Mode = TranslationToolMode.Sync
            };

            var result = await target.RunAsync(options, TestContext.Current.CancellationToken);

            result.Should().Be(0);
            JsonTranslationDocument.LoadDictionary(Path.Combine(_outputDirectoryPath, "qbtmud_en_AU.json"))
                .Should()
                .Contain(new KeyValuePair<string, string>("Ctx|A", "Dark Colours"))
                .And.Contain(new KeyValuePair<string, string>("Ctx|B", "Customise storage"));
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_SyncModeAndExistingLocaleFileHasMissingKeys_WHEN_RunAsync_THEN_ShouldTranslateOnlyMissingKeys()
        {
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_de.ts"), string.Empty);
            File.WriteAllText(_englishFilePath, "{\n  \"Ctx|B\": \"B\",\n  \"Ctx|A\": \"A\"\n}");
            File.WriteAllText(Path.Combine(_outputDirectoryPath, "qbtmud_de.json"), "{\n  \"Ctx|B\": \"B-EXISTING\"\n}");
            var translationClient = new Mock<ITranslationClient>();
            translationClient
                .Setup(mock => mock.TranslateAsync(
                    "de",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "A"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["A-DE"]);

            var target = new TranslationSyncService(
                new LocaleDiscoveryService(),
                translationClient.Object,
                new TranslationValidator(),
                new TranslationProviderAvailabilityValidator(hasAzureProvider: true, hasGoogleProvider: true),
                maxValidationRetries: 1);
            var options = new ToolOptions
            {
                EnglishFilePath = _englishFilePath,
                OutputDirectoryPath = _outputDirectoryPath,
                UpstreamTranslationsPath = _upstreamDirectoryPath,
                Mode = TranslationToolMode.Sync
            };

            var result = await target.RunAsync(options, TestContext.Current.CancellationToken);

            result.Should().Be(0);
            JsonTranslationDocument.LoadDictionary(Path.Combine(_outputDirectoryPath, "qbtmud_de.json"))
                .Should()
                .Contain(new KeyValuePair<string, string>("Ctx|B", "B-EXISTING"))
                .And.Contain(new KeyValuePair<string, string>("Ctx|A", "A-DE"));
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    "de",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "A"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_SyncModeAndExistingLocaleFileIsComplete_WHEN_RunAsync_THEN_ShouldNotRetranslateExistingKeys()
        {
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_de.ts"), string.Empty);
            File.WriteAllText(_englishFilePath, "{\n  \"Ctx|B\": \"B\",\n  \"Ctx|A\": \"A\"\n}");
            File.WriteAllText(Path.Combine(_outputDirectoryPath, "qbtmud_de.json"), "{\n  \"Ctx|B\": \"B-EXISTING\",\n  \"Ctx|A\": \"A-EXISTING\"\n}");
            var translationClient = new Mock<ITranslationClient>(MockBehavior.Strict);

            var target = new TranslationSyncService(
                new LocaleDiscoveryService(),
                translationClient.Object,
                new TranslationValidator(),
                new TranslationProviderAvailabilityValidator(hasAzureProvider: true, hasGoogleProvider: true),
                maxValidationRetries: 1);
            var options = new ToolOptions
            {
                EnglishFilePath = _englishFilePath,
                OutputDirectoryPath = _outputDirectoryPath,
                UpstreamTranslationsPath = _upstreamDirectoryPath,
                Mode = TranslationToolMode.Sync
            };

            var result = await target.RunAsync(options, TestContext.Current.CancellationToken);

            result.Should().Be(0);
            JsonTranslationDocument.LoadDictionary(Path.Combine(_outputDirectoryPath, "qbtmud_de.json"))
                .Should()
                .Contain(new KeyValuePair<string, string>("Ctx|B", "B-EXISTING"))
                .And.Contain(new KeyValuePair<string, string>("Ctx|A", "A-EXISTING"));
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_ValidateModeAndMissingLocaleFile_WHEN_RunAsync_THEN_ShouldFail()
        {
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_fr.ts"), string.Empty);
            File.WriteAllText(_englishFilePath, "{\n  \"Ctx|A\": \"A\"\n}");
            File.WriteAllText(Path.Combine(_outputDirectoryPath, "qbtmud_en.json"), "{\n  \"Ctx|A\": \"A\"\n}");
            var translationClient = new Mock<ITranslationClient>(MockBehavior.Strict);

            var target = new TranslationSyncService(
                new LocaleDiscoveryService(),
                translationClient.Object,
                new TranslationValidator(),
                new TranslationProviderAvailabilityValidator(hasAzureProvider: false, hasGoogleProvider: false),
                maxValidationRetries: 1);
            var options = new ToolOptions
            {
                EnglishFilePath = _englishFilePath,
                OutputDirectoryPath = _outputDirectoryPath,
                UpstreamTranslationsPath = _upstreamDirectoryPath,
                Mode = TranslationToolMode.Validate
            };

            var result = await target.RunAsync(options, TestContext.Current.CancellationToken);

            result.Should().Be(1);
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_SyncModeAndAzureOnlyLocaleSet_WHEN_GoogleConfigurationIsMissing_THEN_ShouldStillTranslate()
        {
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_fr.ts"), string.Empty);
            File.WriteAllText(_englishFilePath, "{\n  \"Ctx|A\": \"A\"\n}");
            var translationClient = new Mock<ITranslationClient>();
            translationClient
                .Setup(mock => mock.TranslateAsync(
                    "fr",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "A"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["A-FR"]);

            var target = new TranslationSyncService(
                new LocaleDiscoveryService(),
                translationClient.Object,
                new TranslationValidator(),
                new TranslationProviderAvailabilityValidator(hasAzureProvider: true, hasGoogleProvider: false),
                maxValidationRetries: 1);
            var options = new ToolOptions
            {
                EnglishFilePath = _englishFilePath,
                OutputDirectoryPath = _outputDirectoryPath,
                UpstreamTranslationsPath = _upstreamDirectoryPath,
                Mode = TranslationToolMode.Sync
            };

            var result = await target.RunAsync(options, TestContext.Current.CancellationToken);

            result.Should().Be(0);
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    "fr",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "A"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_SyncModeAndGoogleOnlyLocaleSet_WHEN_GoogleConfigurationIsMissing_THEN_ShouldFailBeforeTranslating()
        {
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_ltg.ts"), string.Empty);
            File.WriteAllText(_englishFilePath, "{\n  \"Ctx|A\": \"A\"\n}");
            var translationClient = new Mock<ITranslationClient>(MockBehavior.Strict);

            var target = new TranslationSyncService(
                new LocaleDiscoveryService(),
                translationClient.Object,
                new TranslationValidator(),
                new TranslationProviderAvailabilityValidator(hasAzureProvider: true, hasGoogleProvider: false),
                maxValidationRetries: 1);
            var options = new ToolOptions
            {
                EnglishFilePath = _englishFilePath,
                OutputDirectoryPath = _outputDirectoryPath,
                UpstreamTranslationsPath = _upstreamDirectoryPath,
                Mode = TranslationToolMode.Sync
            };

            var result = await target.RunAsync(options, TestContext.Current.CancellationToken);

            result.Should().Be(1);
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    It.IsAny<string>(),
                    It.IsAny<IReadOnlyList<string>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GIVEN_SyncModeAndFirstTranslationAttemptFailsValidation_WHEN_RunAsync_THEN_ShouldRetryUntilValidationPasses()
        {
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_ja.ts"), string.Empty);
            File.WriteAllText(_englishFilePath, "{\n  \"Ctx|A\": \"Applied to all items in %1; cleared %2 item overrides.\"\n}");
            var translationClient = new Mock<ITranslationClient>();
            translationClient
                .SetupSequence(mock => mock.TranslateAsync(
                    "ja",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["%1のすべての項目に適用しました。%2 %2項目の上書き設定をクリアしました。"])
                .ReturnsAsync(["%1のすべての項目に適用しました。%2項目の上書き設定をクリアしました。"]);

            var target = new TranslationSyncService(
                new LocaleDiscoveryService(),
                translationClient.Object,
                new TranslationValidator(),
                new TranslationProviderAvailabilityValidator(hasAzureProvider: true, hasGoogleProvider: true),
                maxValidationRetries: 1);
            var options = new ToolOptions
            {
                EnglishFilePath = _englishFilePath,
                OutputDirectoryPath = _outputDirectoryPath,
                UpstreamTranslationsPath = _upstreamDirectoryPath,
                Mode = TranslationToolMode.Sync
            };

            var result = await target.RunAsync(options, TestContext.Current.CancellationToken);

            result.Should().Be(0);
            JsonTranslationDocument.LoadDictionary(Path.Combine(_outputDirectoryPath, "qbtmud_ja.json"))
                .Should()
                .Contain(new KeyValuePair<string, string>("Ctx|A", "%1のすべての項目に適用しました。%2項目の上書き設定をクリアしました。"));
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    "ja",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GIVEN_SyncModeAndValidationFailure_WHEN_MaxValidationRetriesIsZero_THEN_ShouldFailWithoutRetrying()
        {
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_ja.ts"), string.Empty);
            File.WriteAllText(_englishFilePath, "{\n  \"Ctx|A\": \"Applied to all items in %1; cleared %2 item overrides.\"\n}");
            var translationClient = new Mock<ITranslationClient>();
            translationClient
                .Setup(mock => mock.TranslateAsync(
                    "ja",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["%1のすべての項目に適用しました。%2 %2項目の上書き設定をクリアしました。"]);

            var target = new TranslationSyncService(
                new LocaleDiscoveryService(),
                translationClient.Object,
                new TranslationValidator(),
                new TranslationProviderAvailabilityValidator(hasAzureProvider: true, hasGoogleProvider: true),
                maxValidationRetries: 0);
            var options = new ToolOptions
            {
                EnglishFilePath = _englishFilePath,
                OutputDirectoryPath = _outputDirectoryPath,
                UpstreamTranslationsPath = _upstreamDirectoryPath,
                Mode = TranslationToolMode.Sync
            };

            var result = await target.RunAsync(options, TestContext.Current.CancellationToken);

            result.Should().Be(1);
            File.Exists(Path.Combine(_outputDirectoryPath, "qbtmud_ja.json")).Should().BeFalse();
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    "ja",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_SyncModeAndMiddleLocaleFailsValidation_WHEN_RunAsync_THEN_ShouldWriteSuccessfulLocalesBeforeRetryingFailuresAtEnd()
        {
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_de.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_fr.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_it.ts"), string.Empty);
            File.WriteAllText(_englishFilePath, "{\n  \"Ctx|A\": \"Applied to all items in %1; cleared %2 item overrides.\"\n}");
            var translationClient = new Mock<ITranslationClient>();
            translationClient
                .Setup(mock => mock.TranslateAsync(
                    "de",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["Angewendet auf alle Elemente in %1; %2 Elementüberschreibungen wurden gelöscht."]);
            translationClient
                .SetupSequence(mock => mock.TranslateAsync(
                    "fr",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["Appliqué à tous les éléments dans %1 ; %2 %2 surcharges d'éléments effacées."])
                .ReturnsAsync(["Appliqué à tous les éléments dans %1 ; %2 surcharges d'éléments effacées."]);
            translationClient
                .Setup(mock => mock.TranslateAsync(
                    "it",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["Applicato a tutti gli elementi in %1; rimosse %2 sostituzioni degli elementi."]);

            var target = new TranslationSyncService(
                new LocaleDiscoveryService(),
                translationClient.Object,
                new TranslationValidator(),
                new TranslationProviderAvailabilityValidator(hasAzureProvider: true, hasGoogleProvider: true),
                maxValidationRetries: 1);
            var options = new ToolOptions
            {
                EnglishFilePath = _englishFilePath,
                OutputDirectoryPath = _outputDirectoryPath,
                UpstreamTranslationsPath = _upstreamDirectoryPath,
                Mode = TranslationToolMode.Sync
            };

            var result = await target.RunAsync(options, TestContext.Current.CancellationToken);

            result.Should().Be(0);
            File.Exists(Path.Combine(_outputDirectoryPath, "qbtmud_de.json")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDirectoryPath, "qbtmud_fr.json")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDirectoryPath, "qbtmud_it.json")).Should().BeTrue();
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    "de",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    "fr",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    "it",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GIVEN_SyncModeAndLocaleStillFailsAfterRetries_WHEN_RunAsync_THEN_ShouldKeepSuccessfulLocaleFilesAndReturnFailure()
        {
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_de.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_fr.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_upstreamDirectoryPath, "webui_it.ts"), string.Empty);
            File.WriteAllText(_englishFilePath, "{\n  \"Ctx|A\": \"Applied to all items in %1; cleared %2 item overrides.\"\n}");
            var translationClient = new Mock<ITranslationClient>();
            translationClient
                .Setup(mock => mock.TranslateAsync(
                    "de",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["Angewendet auf alle Elemente in %1; %2 Elementüberschreibungen wurden gelöscht."]);
            translationClient
                .Setup(mock => mock.TranslateAsync(
                    "fr",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["Appliqué à tous les éléments dans %1 ; %2 %2 surcharges d'éléments effacées."]);
            translationClient
                .Setup(mock => mock.TranslateAsync(
                    "it",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(["Applicato a tutti gli elementi in %1; rimosse %2 sostituzioni degli elementi."]);

            var target = new TranslationSyncService(
                new LocaleDiscoveryService(),
                translationClient.Object,
                new TranslationValidator(),
                new TranslationProviderAvailabilityValidator(hasAzureProvider: true, hasGoogleProvider: true),
                maxValidationRetries: 0);
            var options = new ToolOptions
            {
                EnglishFilePath = _englishFilePath,
                OutputDirectoryPath = _outputDirectoryPath,
                UpstreamTranslationsPath = _upstreamDirectoryPath,
                Mode = TranslationToolMode.Sync
            };

            var result = await target.RunAsync(options, TestContext.Current.CancellationToken);

            result.Should().Be(1);
            File.Exists(Path.Combine(_outputDirectoryPath, "qbtmud_de.json")).Should().BeTrue();
            File.Exists(Path.Combine(_outputDirectoryPath, "qbtmud_fr.json")).Should().BeFalse();
            File.Exists(Path.Combine(_outputDirectoryPath, "qbtmud_it.json")).Should().BeTrue();
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    "de",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    "fr",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            translationClient.Verify(
                mock => mock.TranslateAsync(
                    "it",
                    It.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "Applied to all items in %1; cleared %2 item overrides."),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
