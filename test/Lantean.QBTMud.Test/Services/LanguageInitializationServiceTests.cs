using AwesomeAssertions;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using System.Globalization;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class LanguageInitializationServiceTests : IDisposable
    {
        private readonly CultureInfo _originalCurrentCulture;
        private readonly CultureInfo _originalCurrentUiCulture;
        private readonly CultureInfo? _originalDefaultThreadCurrentCulture;
        private readonly CultureInfo? _originalDefaultThreadCurrentUiCulture;
        private readonly ILocalStorageService _localStorage;
        private readonly ILanguageResourceLoader _languageResourceLoader;
        private readonly ILogger<LanguageInitializationService> _logger;
        private readonly LanguageInitializationService _target;

        public LanguageInitializationServiceTests()
        {
            _originalCurrentCulture = CultureInfo.CurrentCulture;
            _originalCurrentUiCulture = CultureInfo.CurrentUICulture;
            _originalDefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentCulture;
            _originalDefaultThreadCurrentUiCulture = CultureInfo.DefaultThreadCurrentUICulture;

            _localStorage = Mock.Of<ILocalStorageService>();
            _languageResourceLoader = Mock.Of<ILanguageResourceLoader>();
            _logger = Mock.Of<ILogger<LanguageInitializationService>>();

            Mock.Get(_localStorage)
                .Setup(storage => storage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<string?>("fr_FR"));

            Mock.Get(_languageResourceLoader)
                .Setup(loader => loader.LoadLocaleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            _target = new LanguageInitializationService(_localStorage, _languageResourceLoader, _logger);
        }

        [Fact]
        public async Task GIVEN_StoredLocale_WHEN_EnsureLanguageResourcesInitialized_THEN_ShouldLoadStoredLocale()
        {
            await _target.EnsureLanguageResourcesInitialized(TestContext.Current.CancellationToken);

            Mock.Get(_languageResourceLoader).Verify(loader => loader.LoadLocaleAsync("fr_FR", It.IsAny<CancellationToken>()), Times.Once);
            CultureInfo.CurrentUICulture.Name.Should().Be("fr-FR");
        }

        [Fact]
        public async Task GIVEN_StoredLocaleWithWhitespace_WHEN_EnsureLanguageResourcesInitialized_THEN_ShouldTrimStoredLocale()
        {
            Mock.Get(_localStorage)
                .Setup(storage => storage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<string?>(" fr_FR "));

            await _target.EnsureLanguageResourcesInitialized(TestContext.Current.CancellationToken);

            Mock.Get(_languageResourceLoader).Verify(loader => loader.LoadLocaleAsync("fr_FR", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StoredLocaleMissing_WHEN_EnsureLanguageResourcesInitialized_THEN_ShouldUseCurrentUiCulture()
        {
            Mock.Get(_localStorage)
                .Setup(storage => storage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<string?>(null));

            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

            await _target.EnsureLanguageResourcesInitialized(TestContext.Current.CancellationToken);

            Mock.Get(_languageResourceLoader).Verify(loader => loader.LoadLocaleAsync("de-DE", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StoredLocaleMissingAndCurrentUiCultureInvariant_WHEN_EnsureLanguageResourcesInitialized_THEN_ShouldFallbackToCurrentCulture()
        {
            Mock.Get(_localStorage)
                .Setup(storage => storage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<string?>(null));

            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = new CultureInfo("es-ES");

            await _target.EnsureLanguageResourcesInitialized(TestContext.Current.CancellationToken);

            Mock.Get(_languageResourceLoader).Verify(loader => loader.LoadLocaleAsync("es-ES", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StoredLocaleMissingAndAllCulturesInvariant_WHEN_EnsureLanguageResourcesInitialized_THEN_ShouldFallbackToEnglish()
        {
            Mock.Get(_localStorage)
                .Setup(storage => storage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<string?>(null));

            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            await _target.EnsureLanguageResourcesInitialized(TestContext.Current.CancellationToken);

            Mock.Get(_languageResourceLoader).Verify(loader => loader.LoadLocaleAsync("en", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_StoredLocaleHasEmptyBasePart_WHEN_EnsureLanguageResourcesInitialized_THEN_ShouldSkipCultureApplyAndStillLoadLocale()
        {
            Mock.Get(_localStorage)
                .Setup(storage => storage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<string?>("@"));

            var originalCulture = new CultureInfo("de-DE");
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalCulture;
            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalCulture;

            await _target.EnsureLanguageResourcesInitialized(TestContext.Current.CancellationToken);

            CultureInfo.CurrentCulture.Name.Should().Be("de-DE");
            CultureInfo.CurrentUICulture.Name.Should().Be("de-DE");
            Mock.Get(_languageResourceLoader).Verify(loader => loader.LoadLocaleAsync("@", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("sr@latin", "sr-Latn")]
        [InlineData("sr@cyrillic", "sr-Cyrl")]
        [InlineData("sr@aBcD", "sr-Abcd")]
        [InlineData("sr@x", "sr")]
        public async Task GIVEN_StoredLocaleWithScriptTag_WHEN_EnsureLanguageResourcesInitialized_THEN_ShouldNormalizeCultureBeforeLoad(string locale, string expectedCulture)
        {
            Mock.Get(_localStorage)
                .Setup(storage => storage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<string?>(locale));

            await _target.EnsureLanguageResourcesInitialized(TestContext.Current.CancellationToken);

            CultureInfo.CurrentUICulture.Name.Should().Be(expectedCulture);
            Mock.Get(_languageResourceLoader).Verify(loader => loader.LoadLocaleAsync(locale, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_InvalidStoredLocale_WHEN_EnsureLanguageResourcesInitialized_THEN_ShouldIgnoreCultureFailureAndLoadLocale()
        {
            Mock.Get(_localStorage)
                .Setup(storage => storage.GetItemAsStringAsync(LanguageStorageKeys.PreferredLocale, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult<string?>("invalid$$"));

            var originalCulture = new CultureInfo("en-US");
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalCulture;
            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalCulture;

            await _target.EnsureLanguageResourcesInitialized(TestContext.Current.CancellationToken);

            CultureInfo.CurrentUICulture.Name.Should().Be("en-US");
            Mock.Get(_languageResourceLoader).Verify(loader => loader.LoadLocaleAsync("invalid$$", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GIVEN_LanguageResourceLoaderThrowsOperationCanceled_WHEN_EnsureLanguageResourcesInitialized_THEN_ShouldPropagateOperationCanceledException()
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            Mock.Get(_languageResourceLoader)
                .Setup(loader => loader.LoadLocaleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));

            Func<Task> action = async () =>
            {
                await _target.EnsureLanguageResourcesInitialized(cancellationTokenSource.Token);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _originalCurrentCulture;
            CultureInfo.CurrentUICulture = _originalCurrentUiCulture;
            CultureInfo.DefaultThreadCurrentCulture = _originalDefaultThreadCurrentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _originalDefaultThreadCurrentUiCulture;
        }
    }
}
