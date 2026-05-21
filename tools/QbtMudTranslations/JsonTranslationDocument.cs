using System.Text;
using System.Text.Json;

namespace QbtMudTranslations
{
    internal static class JsonTranslationDocument
    {
        public static List<KeyValuePair<string, string>> LoadOrdered(string path)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"Translation file must contain a JSON object: {path}");
            }

            var entries = new List<KeyValuePair<string, string>>();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                entries.Add(new KeyValuePair<string, string>(property.Name, property.Value.GetString() ?? string.Empty));
            }

            return entries;
        }

        public static Dictionary<string, string> LoadDictionary(string path)
        {
            return LoadOrdered(path).ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
        }

        public static void WriteOrdered(
            string path,
            IReadOnlyList<KeyValuePair<string, string>> englishEntries,
            IReadOnlyDictionary<string, string> translations)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = true
            }))
            {
                writer.WriteStartObject();
                foreach (var entry in englishEntries)
                {
                    if (!translations.TryGetValue(entry.Key, out var translation))
                    {
                        throw new InvalidOperationException($"Missing translation value for key '{entry.Key}'.");
                    }

                    writer.WriteString(entry.Key, translation);
                }

                writer.WriteEndObject();
            }

            var json = Encoding.UTF8.GetString(stream.ToArray()).Replace("\n", "\r\n", StringComparison.Ordinal);
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }
    }
}
