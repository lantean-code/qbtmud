using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class JsonTranslationDocumentTests : IDisposable
    {
        private readonly string _tempDirectoryPath;

        public JsonTranslationDocumentTests()
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
        public void GIVEN_EnglishOrderAndTranslations_WHEN_WriteOrdered_THEN_ShouldPersistEnglishKeyOrder()
        {
            var englishEntries = new List<KeyValuePair<string, string>>
            {
                new("Ctx|B", "B"),
                new("Ctx|A", "A")
            };
            var translations = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Ctx|A"] = "Translated A",
                ["Ctx|B"] = "Translated B"
            };
            var path = Path.Combine(_tempDirectoryPath, "qbtmud_de.json");

            JsonTranslationDocument.WriteOrdered(path, englishEntries, translations);

            var result = JsonTranslationDocument.LoadOrdered(path);

            result.Select(entry => entry.Key).Should().Equal("Ctx|B", "Ctx|A");
        }
    }
}
