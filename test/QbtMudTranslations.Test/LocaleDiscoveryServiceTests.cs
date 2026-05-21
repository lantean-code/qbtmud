using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class LocaleDiscoveryServiceTests : IDisposable
    {
        private readonly string _tempDirectoryPath;

        public LocaleDiscoveryServiceTests()
        {
            _tempDirectoryPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectoryPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectoryPath))
            {
                Directory.Delete(_tempDirectoryPath, recursive: true);
            }
        }

        [Fact]
        public void GIVEN_UpstreamTranslationDirectory_WHEN_GetSupportedLocales_THEN_ShouldReturnSortedDistinctLocaleCodes()
        {
            File.WriteAllText(Path.Combine(_tempDirectoryPath, "webui_pt_BR.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_tempDirectoryPath, "webui_en.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_tempDirectoryPath, "webui_en.ts.bak"), string.Empty);
            File.WriteAllText(Path.Combine(_tempDirectoryPath, "webui_pt_BR.ts.copy"), string.Empty);
            File.WriteAllText(Path.Combine(_tempDirectoryPath, "webui_de.ts"), string.Empty);
            File.WriteAllText(Path.Combine(_tempDirectoryPath, "webui_de.ts"), string.Empty);

            var target = new LocaleDiscoveryService();

            var result = target.GetSupportedLocales(_tempDirectoryPath);

            result.Should().Equal("de", "en", "pt_BR");
        }
    }
}
