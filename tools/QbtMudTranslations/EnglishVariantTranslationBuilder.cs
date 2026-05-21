using System.Text.RegularExpressions;

namespace QbtMudTranslations
{
    internal static partial class EnglishVariantTranslationBuilder
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _localeReplacements =
            new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en_AU"] = CreateSharedEnglishReplacementMap(),
                ["en_GB"] = CreateSharedEnglishReplacementMap()
            };

        public static bool IsDerivedLocale(string locale)
        {
            return _localeReplacements.ContainsKey(locale);
        }

        public static Dictionary<string, string> BuildLocaleTranslations(
            string locale,
            IReadOnlyList<KeyValuePair<string, string>> englishEntries)
        {
            if (!_localeReplacements.TryGetValue(locale, out var replacements))
            {
                throw new InvalidOperationException($"Locale '{locale}' is not a supported derived English locale.");
            }

            return englishEntries.ToDictionary(
                entry => entry.Key,
                entry => TransformText(entry.Value, replacements),
                StringComparer.Ordinal);
        }

        internal static string TransformText(string sourceText, string locale)
        {
            if (!_localeReplacements.TryGetValue(locale, out var replacements))
            {
                return sourceText;
            }

            return TransformText(sourceText, replacements);
        }

        private static string TransformText(string sourceText, IReadOnlyDictionary<string, string> replacements)
        {
            return WordRegex().Replace(sourceText, match =>
            {
                var lookup = match.Value.ToLowerInvariant();
                if (!replacements.TryGetValue(lookup, out var replacement))
                {
                    return match.Value;
                }

                return MatchCase(match.Value, replacement);
            });
        }

        private static string MatchCase(string source, string replacement)
        {
            if (source.All(char.IsUpper))
            {
                return replacement.ToUpperInvariant();
            }

            if (char.IsUpper(source[0]) && source[1..].All(char.IsLower))
            {
                return char.ToUpperInvariant(replacement[0]) + replacement[1..];
            }

            return replacement;
        }

        private static IReadOnlyDictionary<string, string> CreateSharedEnglishReplacementMap()
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["analyze"] = "analyse",
                ["analyzed"] = "analysed",
                ["analyzes"] = "analyses",
                ["analyzing"] = "analysing",
                ["behavior"] = "behaviour",
                ["behaviors"] = "behaviours",
                ["catalog"] = "catalogue",
                ["cataloged"] = "catalogued",
                ["cataloging"] = "cataloguing",
                ["catalogs"] = "catalogues",
                ["center"] = "centre",
                ["centered"] = "centred",
                ["centering"] = "centring",
                ["centers"] = "centres",
                ["color"] = "colour",
                ["colored"] = "coloured",
                ["coloring"] = "colouring",
                ["colors"] = "colours",
                ["customization"] = "customisation",
                ["customizations"] = "customisations",
                ["customize"] = "customise",
                ["customized"] = "customised",
                ["customizes"] = "customises",
                ["customizing"] = "customising",
                ["favorite"] = "favourite",
                ["favorites"] = "favourites",
                ["favorited"] = "favourited",
                ["favoriting"] = "favouriting",
                ["initialize"] = "initialise",
                ["initialized"] = "initialised",
                ["initializes"] = "initialises",
                ["initializing"] = "initialising",
                ["initialization"] = "initialisation",
                ["initializations"] = "initialisations",
                ["organization"] = "organisation",
                ["organizations"] = "organisations",
                ["organize"] = "organise",
                ["organized"] = "organised",
                ["organizes"] = "organises",
                ["organizing"] = "organising"
            };
        }

        [GeneratedRegex("[A-Za-z]+", RegexOptions.CultureInvariant)]
        private static partial Regex WordRegex();
    }
}
