using System.Text.RegularExpressions;

namespace QbtMudTranslations
{
    internal sealed partial class TranslationValidator
    {
        private static readonly string[] _productTerms =
        [
            "qBittorrent",
            "qbtmud"
        ];

        public IReadOnlyList<string> ValidateLocale(
            string locale,
            IReadOnlyDictionary<string, string> englishTranslations,
            IReadOnlyDictionary<string, string> localeTranslations)
        {
            var errors = new List<string>();

            var missingKeys = englishTranslations.Keys.Except(localeTranslations.Keys, StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToList();
            foreach (var missingKey in missingKeys)
            {
                errors.Add($"{locale}: missing key '{missingKey}'.");
            }

            var extraKeys = localeTranslations.Keys.Except(englishTranslations.Keys, StringComparer.Ordinal).OrderBy(key => key, StringComparer.Ordinal).ToList();
            foreach (var extraKey in extraKeys)
            {
                errors.Add($"{locale}: extra key '{extraKey}'.");
            }

            foreach (var entry in englishTranslations)
            {
                if (!localeTranslations.TryGetValue(entry.Key, out var translation))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(translation))
                {
                    errors.Add($"{locale}: key '{entry.Key}' has an empty translation.");
                    continue;
                }

                ValidatePlaceholders(locale, entry.Key, entry.Value, translation, errors);
                ValidateTokenSet(locale, entry.Key, entry.Value, translation, HtmlEntityRegex(), "HTML entity", errors);
                ValidateProductTerms(locale, entry.Key, entry.Value, translation, errors);
            }

            return errors;
        }

        private static void ValidateTokenSet(
            string locale,
            string key,
            string englishValue,
            string translation,
            Regex regex,
            string tokenDescription,
            List<string> errors)
        {
            var englishTokens = regex.Matches(englishValue).Select(match => match.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var localeTokens = regex.Matches(translation).Select(match => match.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!englishTokens.SequenceEqual(localeTokens, StringComparer.Ordinal))
            {
                errors.Add($"{locale}: key '{key}' has mismatched {tokenDescription}s.");
            }
        }

        private static void ValidateProductTerms(
            string locale,
            string key,
            string englishValue,
            string translation,
            List<string> errors)
        {
            foreach (var productTerm in _productTerms)
            {
                var englishCount = Regex.Matches(englishValue, Regex.Escape(productTerm), RegexOptions.CultureInvariant).Count;
                var localeCount = Regex.Matches(translation, Regex.Escape(productTerm), RegexOptions.CultureInvariant).Count;
                if (englishCount != localeCount)
                {
                    errors.Add($"{locale}: key '{key}' changed protected term '{productTerm}'.");
                }
            }
        }

        private static void ValidatePlaceholders(
            string locale,
            string key,
            string englishValue,
            string translation,
            List<string> errors)
        {
            var expectedPlaceholders = PlaceholderRegex()
                .Matches(englishValue)
                .Select(match => match.Value)
                .GroupBy(value => value, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
            if (expectedPlaceholders.Count == 0)
            {
                return;
            }

            foreach (var expectedPlaceholder in expectedPlaceholders)
            {
                var localeCount = Regex.Matches(translation, Regex.Escape(expectedPlaceholder.Key), RegexOptions.CultureInvariant).Count;
                if (localeCount != expectedPlaceholder.Value)
                {
                    errors.Add($"{locale}: key '{key}' has mismatched placeholders.");
                    return;
                }
            }
        }

        [GeneratedRegex("%([1-9][0-9]?)(?![0-9])", RegexOptions.CultureInvariant)]
        private static partial Regex PlaceholderRegex();

        [GeneratedRegex("&[A-Za-z0-9#]+;", RegexOptions.CultureInvariant)]
        private static partial Regex HtmlEntityRegex();
    }
}
