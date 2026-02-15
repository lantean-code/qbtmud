using AwesomeAssertions;
using Lantean.QBTMud.Services.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Globalization;

namespace Lantean.QBTMud.Test.Services.Localization
{
    public sealed class LanguageResourceLoaderTests
    {
        private readonly ILanguageFileLoader _fileResourceProvider;
        private readonly ILanguageEmbeddedResourceLoader _assemblyResourceProvider;
        private readonly ILanguageResourceProvider _resourceProvider;
        private readonly ILogger<LanguageResourceLoader> _logger;
        private readonly LanguageResourceLoader _target;

        public LanguageResourceLoaderTests()
        {
            _fileResourceProvider = Mock.Of<ILanguageFileLoader>();
            _assemblyResourceProvider = Mock.Of<ILanguageEmbeddedResourceLoader>();
            _resourceProvider = new LanguageResourceProvider();
            _logger = Mock.Of<ILogger<LanguageResourceLoader>>();

            var options = Options.Create(new WebUiLocalizationOptions
            {
                BasePath = "i18n",
                AliasFileName = "webui_aliases.json",
                BaseFileNameFormat = "webui_{0}.json",
                OverrideFileNameFormat = "webui_overrides_{0}.json"
            });

            _target = new LanguageResourceLoader(_fileResourceProvider, _assemblyResourceProvider, _resourceProvider, _logger, options);
        }

        [Fact]
        public void GIVEN_LoaderNotInitialized_WHEN_ResourcesRead_THEN_ShouldReturnEmptyResources()
        {
            _resourceProvider.Resources.Aliases.Should().BeEmpty();
            _resourceProvider.Resources.Overrides.Should().BeEmpty();
            _resourceProvider.Resources.Translations.Should().BeEmpty();
            _resourceProvider.Resources.LoadedCultureName.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_SameLocaleLoadedTwice_WHEN_LoadLocaleAsync_THEN_ShouldLoadOnlyOnce()
        {
            var translations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|Source"] = "Translated"
            };

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(new Dictionary<string, string>(StringComparer.Ordinal)));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_de-DE.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(translations));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_overrides_de-DE.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(new Dictionary<string, string>(StringComparer.Ordinal)));

            await _target.LoadLocaleAsync("de-DE", TestContext.Current.CancellationToken);
            await _target.LoadLocaleAsync("de-DE", TestContext.Current.CancellationToken);

            _resourceProvider.Resources.Translations.Should().ContainKey("Ctx|Source").WhoseValue.Should().Be("Translated");
            _resourceProvider.Resources.LoadedCultureName.Should().Be("de-DE");

            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_de-DE.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_overrides_de-DE.json", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CultureChanges_WHEN_EnsureInitialized_THEN_ShouldReloadResourcesForNewCulture()
        {
            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(new Dictionary<string, string>(StringComparer.Ordinal)));

            Mock.Get(_assemblyResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_en.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Ctx|Source"] = "English"
                }));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_overrides_en.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(new Dictionary<string, string>(StringComparer.Ordinal)));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_fr-FR.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Ctx|Source"] = "Francais"
                }));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_overrides_fr-FR.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(new Dictionary<string, string>(StringComparer.Ordinal)));

            await WithCultureAsync(new CultureInfo("en-US"), async () =>
            {
                await _target.EnsureInitialized(TestContext.Current.CancellationToken);
            });

            await WithCultureAsync(new CultureInfo("fr-FR"), async () =>
            {
                await _target.EnsureInitialized(TestContext.Current.CancellationToken);
            });

            _resourceProvider.Resources.Translations.Should().ContainKey("Ctx|Source").WhoseValue.Should().Be("Francais");
            _resourceProvider.Resources.LoadedCultureName.Should().Be("fr-FR");

            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()), Times.Exactly(2));
            Mock.Get(_assemblyResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_en.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_fr-FR.json", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_EnglishLocaleAndEmbeddedTranslations_WHEN_LoadLocaleAsync_THEN_ShouldUseEmbeddedAndEnglishOverrides()
        {
            var aliases = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|Source"] = "Ctx|Alias"
            };
            var translations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|Alias"] = "Translated"
            };
            var overrides = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|Alias"] = "Override"
            };

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(aliases));

            Mock.Get(_assemblyResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_en.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(translations));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_overrides_en.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(overrides));

            await _target.LoadLocaleAsync("en-US", TestContext.Current.CancellationToken);

            _resourceProvider.Resources.Aliases.Should().ContainKey("Ctx|Source").WhoseValue.Should().Be("Ctx|Alias");
            _resourceProvider.Resources.Translations.Should().ContainKey("Ctx|Alias").WhoseValue.Should().Be("Translated");
            _resourceProvider.Resources.Overrides.Should().ContainKey("Ctx|Alias").WhoseValue.Should().Be("Override");
            _resourceProvider.Resources.LoadedCultureName.Should().Be("en-US");

            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_en-US.json", It.IsAny<CancellationToken>()), Times.Never);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_en_US.json", It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_EnglishLocaleWithoutEmbeddedTranslations_WHEN_LoadLocaleAsync_THEN_ShouldFallbackToCandidateLocales()
        {
            var translations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|Source"] = "Translated"
            };
            var overrides = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|Source"] = "Override"
            };

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_assemblyResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_en.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_en-US.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_en_US.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(translations));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_overrides_en_US.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(overrides));

            await _target.LoadLocaleAsync("en-US", TestContext.Current.CancellationToken);

            _resourceProvider.Resources.Aliases.Should().BeEmpty();
            _resourceProvider.Resources.Translations.Should().ContainKey("Ctx|Source").WhoseValue.Should().Be("Translated");
            _resourceProvider.Resources.Overrides.Should().ContainKey("Ctx|Source").WhoseValue.Should().Be("Override");
            _resourceProvider.Resources.LoadedCultureName.Should().Be("en-US");

            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_en-US.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_en_US.json", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_NonEnglishLocale_WHEN_PrimaryLocalesMissing_THEN_ShouldUseBaseLocaleAndEmptyOverrides()
        {
            var aliases = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|Source"] = "Ctx|Alias"
            };
            var translations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|Alias"] = "Translated"
            };

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(aliases));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_fr-CA.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_fr_CA.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_fr.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(translations));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_overrides_fr.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            await _target.LoadLocaleAsync("fr-CA", TestContext.Current.CancellationToken);

            _resourceProvider.Resources.Aliases.Should().ContainKey("Ctx|Source").WhoseValue.Should().Be("Ctx|Alias");
            _resourceProvider.Resources.Translations.Should().ContainKey("Ctx|Alias").WhoseValue.Should().Be("Translated");
            _resourceProvider.Resources.Overrides.Should().BeEmpty();
            _resourceProvider.Resources.LoadedCultureName.Should().Be("fr-CA");

            Mock.Get(_assemblyResourceProvider).Verify(provider => provider.LoadDictionaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_fr-CA.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_fr_CA.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_fr.json", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_NoTranslationCandidates_WHEN_LoadLocaleAsync_THEN_ShouldReturnEmptyDictionaries()
        {
            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            await _target.LoadLocaleAsync("sv-SE", TestContext.Current.CancellationToken);

            _resourceProvider.Resources.Aliases.Should().BeEmpty();
            _resourceProvider.Resources.Translations.Should().BeEmpty();
            _resourceProvider.Resources.Overrides.Should().BeEmpty();
            _resourceProvider.Resources.LoadedCultureName.Should().Be("sv-SE");

            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_sv-SE.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_sv_SE.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_sv.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_en.json", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_EnglishLocaleNameWithoutRegion_WHEN_LoadLocaleAsync_THEN_ShouldDeduplicateCandidateLocales()
        {
            var translations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|Source"] = "Translated"
            };

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_assemblyResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_en.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_en.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(translations));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_overrides_en.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            await _target.LoadLocaleAsync("en", TestContext.Current.CancellationToken);

            _resourceProvider.Resources.Translations.Should().ContainKey("Ctx|Source").WhoseValue.Should().Be("Translated");
            _resourceProvider.Resources.Overrides.Should().BeEmpty();

            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_en.json", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_UnderscoreLocale_WHEN_LoadLocaleAsync_THEN_ShouldTryDashVariantAfterUnderscore()
        {
            var translations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|Source"] = "Translated"
            };

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_pt_BR.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_pt-BR.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(translations));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_overrides_pt-BR.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            await _target.LoadLocaleAsync("pt_BR", TestContext.Current.CancellationToken);

            _resourceProvider.Resources.Translations.Should().ContainKey("Ctx|Source").WhoseValue.Should().Be("Translated");
            _resourceProvider.Resources.LoadedCultureName.Should().Be("pt_BR");

            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_pt_BR.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_pt-BR.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_overrides_pt-BR.json", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_LocaleWithScriptSuffix_WHEN_LoadLocaleAsync_THEN_ShouldResolveBaseLocaleAfterAtSign()
        {
            var translations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|Source"] = "Translated"
            };

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_sr@latin.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_sr.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(translations));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_overrides_sr.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            await _target.LoadLocaleAsync("sr@latin", TestContext.Current.CancellationToken);

            _resourceProvider.Resources.Translations.Should().ContainKey("Ctx|Source").WhoseValue.Should().Be("Translated");
            _resourceProvider.Resources.LoadedCultureName.Should().Be("sr@latin");

            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_sr@latin.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_sr.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_overrides_sr.json", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_AtSignOnlyLocale_WHEN_LoadLocaleAsync_THEN_ShouldFallBackToEnglishCandidate()
        {
            var translations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|Source"] = "Translated"
            };

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_@.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_en.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(translations));

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_overrides_en.json", It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<Dictionary<string, string>?>(null));

            await _target.LoadLocaleAsync("@", TestContext.Current.CancellationToken);

            _resourceProvider.Resources.Translations.Should().ContainKey("Ctx|Source").WhoseValue.Should().Be("Translated");
            _resourceProvider.Resources.LoadedCultureName.Should().Be("@");

            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_@.json", It.IsAny<CancellationToken>()), Times.Once);
            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync("webui_en.json", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_CultureAlreadyLoaded_WHEN_EnsureInitialized_THEN_ShouldNotReload()
        {
            var culture = new CultureInfo("fr-FR");
            var previousCulture = CultureInfo.CurrentCulture;
            var previousUiCulture = CultureInfo.CurrentUICulture;

            _resourceProvider.SetResources(new LanguageResources(
                new Dictionary<string, string>(StringComparer.Ordinal),
                new Dictionary<string, string>(StringComparer.Ordinal),
                new Dictionary<string, string>(StringComparer.Ordinal),
                "fr-FR"));

            try
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                await _target.EnsureInitialized(TestContext.Current.CancellationToken);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.CurrentUICulture = previousUiCulture;
            }

            Mock.Get(_fileResourceProvider).Verify(provider => provider.LoadDictionaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Mock.Get(_assemblyResourceProvider).Verify(provider => provider.LoadDictionaryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GIVEN_CanceledToken_WHEN_LoadLocaleAsync_THEN_ShouldPropagateOperationCanceledException()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            Mock.Get(_fileResourceProvider)
                .Setup(provider => provider.LoadDictionaryAsync("webui_aliases.json", It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException(cancellationTokenSource.Token));

            Func<Task> action = async () =>
            {
                await _target.LoadLocaleAsync("en-US", cancellationTokenSource.Token);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task GIVEN_WhitespaceLocale_WHEN_LoadLocaleAsync_THEN_ShouldThrowArgumentException()
        {
            Func<Task> action = async () =>
            {
                await _target.LoadLocaleAsync(" ", TestContext.Current.CancellationToken);
            };

            await action.Should().ThrowAsync<ArgumentException>();
        }

        private static async Task WithCultureAsync(CultureInfo culture, Func<Task> action)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;

            try
            {
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                await action();
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
            }
        }
    }
}
