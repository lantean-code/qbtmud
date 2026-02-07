using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace WebUiTranslationsConverter
{
    internal static class Program
    {
        private const string FilePrefix = "webui_";
        private const string FileExtension = ".ts";
        private const string LanguagesFileName = "webui_languages.json";

        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        public static int Main(string[] args)
        {
            var options = ParseOptions(args);
            if (options is null)
            {
                PrintUsage();
                return 2;
            }

            if (!Directory.Exists(options.SourcePath))
            {
                Console.Error.WriteLine($"Source path not found: {options.SourcePath}");
                return 1;
            }

            Directory.CreateDirectory(options.OutputPath);

            var files = Directory.EnumerateFiles(options.SourcePath, "webui_*.ts", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            if (options.Mode == ConversionMode.EnglishOnly)
            {
                files = files.Where(path => string.Equals(GetLocale(path), "en", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (files.Count == 0)
            {
                Console.Error.WriteLine("No translation files matched the requested mode.");
                return 1;
            }

            var locales = CollectLocales(files);

            foreach (var file in files)
            {
                var locale = GetLocale(file);
                if (string.IsNullOrWhiteSpace(locale))
                {
                    Console.Error.WriteLine($"Unable to parse locale for file: {file}");
                    continue;
                }

                var translations = LoadTranslations(file);
                var outputFileName = $"webui_{locale}.json";
                var outputFile = BuildOutputFilePath(options.OutputPath, outputFileName);
                WriteJson(outputFile, translations);
                Console.WriteLine($"Generated {outputFile} ({translations.Count} entries)");
            }

            var languagesManifestPath = BuildOutputFilePath(options.OutputPath, LanguagesFileName);
            WriteLanguagesManifest(languagesManifestPath, locales);

            return 0;
        }

        private static ConverterOptions? ParseOptions(string[] args)
        {
            var source = string.Empty;
            var output = string.Empty;
            var mode = ConversionMode.All;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--source":
                        if (!TryReadValue(args, ref i, out source))
                            return null;
                        break;

                    case "--output":
                        if (!TryReadValue(args, ref i, out output))
                            return null;
                        break;

                    case "--mode":
                        if (!TryReadValue(args, ref i, out var modeValue))
                            return null;
                        mode = ParseMode(modeValue);
                        if (mode == ConversionMode.Unknown)
                            return null;
                        break;

                    case "--help":
                    case "-h":
                        return null;

                    default:
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            return new ConverterOptions(source, output, mode);
        }

        private static bool TryReadValue(string[] args, ref int index, out string value)
        {
            if (index + 1 >= args.Length)
            {
                value = string.Empty;
                return false;
            }

            value = args[++index];
            return !string.IsNullOrWhiteSpace(value);
        }

        private static ConversionMode ParseMode(string value)
        {
            if (string.Equals(value, "en", StringComparison.OrdinalIgnoreCase))
                return ConversionMode.EnglishOnly;
            if (string.Equals(value, "all", StringComparison.OrdinalIgnoreCase))
                return ConversionMode.All;

            return ConversionMode.Unknown;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --project tools/WebUiTranslationsConverter -- --source <path> --output <path> --mode <en|all>");
        }

        private static string GetLocale(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            if (fileName is null || !fileName.StartsWith(FilePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            var locale = fileName[FilePrefix.Length..];
            if (locale.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                locale = locale.Substring(0, locale.Length - FileExtension.Length);
            }

            return locale;
        }

        private static Dictionary<string, string> LoadTranslations(string path)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore
            };

            using var reader = XmlReader.Create(path, settings);
            var document = XDocument.Load(reader);
            var data = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var context in document.Descendants("context"))
            {
                var contextName = context.Element("name")?.Value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(contextName))
                {
                    continue;
                }

                foreach (var message in context.Elements("message"))
                {
                    var source = message.Element("source")?.Value ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(source))
                    {
                        continue;
                    }

                    var translationElement = message.Element("translation");
                    var translation = translationElement?.Value ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(translation))
                    {
                        translation = source;
                    }

                    var key = string.Concat(contextName, '|', source);

                    if (data.TryGetValue(key, out var existing))
                    {
                        if (string.Equals(existing, source, StringComparison.Ordinal) && !string.Equals(translation, source, StringComparison.Ordinal))
                        {
                            data[key] = translation;
                        }

                        continue;
                    }

                    data[key] = translation;
                }
            }

            return data;
        }

        private static List<string> CollectLocales(IEnumerable<string> files)
        {
            var locales = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var mappedLocales = files
                .Select(GetLocale)
                .Where(locale => !string.IsNullOrWhiteSpace(locale));
            var uniqueLocales = mappedLocales.Where(locale => seen.Add(locale));

            foreach (var locale in uniqueLocales)
            {
                locales.Add(locale);
            }

            return locales;
        }

        private static void WriteJson(string outputPath, Dictionary<string, string> data)
        {
            var ordered = new SortedDictionary<string, string>(data, StringComparer.Ordinal);
            var json = JsonSerializer.Serialize(ordered, _options);
            File.WriteAllText(outputPath, json);
        }

        private static string BuildOutputFilePath(string outputDirectory, string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);

            // Ensure the file name is not treated as an absolute/rooted path so that
            // Path.Combine does not ignore the outputDirectory argument.
            if (Path.IsPathRooted(safeFileName))
            {
                safeFileName = safeFileName.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            return Path.Combine(outputDirectory, safeFileName);
        }

        private static void WriteLanguagesManifest(string outputPath, List<string> locales)
        {
            var json = JsonSerializer.Serialize(locales, _options);
            File.WriteAllText(outputPath, json);
            Console.WriteLine($"Generated {outputPath} ({locales.Count} locales)");
        }

        private enum ConversionMode
        {
            Unknown = 0,
            EnglishOnly = 1,
            All = 2
        }

        private sealed record ConverterOptions(string SourcePath, string OutputPath, ConversionMode Mode);
    }
}
