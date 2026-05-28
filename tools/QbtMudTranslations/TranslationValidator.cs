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
            ValidateLocale(locale, englishTranslations, localeTranslations, errors, invalidKeys: null);
            return errors;
        }

        public IReadOnlySet<string> GetInvalidKeys(
            string locale,
            IReadOnlyDictionary<string, string> englishTranslations,
            IReadOnlyDictionary<string, string> localeTranslations)
        {
            var invalidKeys = new HashSet<string>(StringComparer.Ordinal);
            ValidateLocale(locale, englishTranslations, localeTranslations, errors: null, invalidKeys);
            return invalidKeys;
        }

        private static void ValidateLocale(
            string locale,
            IReadOnlyDictionary<string, string> englishTranslations,
            IReadOnlyDictionary<string, string> localeTranslations,
            List<string>? errors,
            HashSet<string>? invalidKeys)
        {
            errors ??= [];

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
                    invalidKeys?.Add(entry.Key);
                    continue;
                }

                ValidatePlaceholders(locale, entry.Key, entry.Value, translation, errors, invalidKeys);
                ValidateTokenSet(locale, entry.Key, entry.Value, translation, HtmlEntityRegex(), "HTML entity", errors, invalidKeys);
                ValidateProductTerms(locale, entry.Key, entry.Value, translation, errors, invalidKeys);
            }
        }

        private static void ValidateTokenSet(
            string locale,
            string key,
            string englishValue,
            string translation,
            Regex regex,
            string tokenDescription,
            List<string> errors,
            HashSet<string>? invalidKeys)
        {
            var englishTokens = regex.Matches(englishValue).Select(match => match.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var localeTokens = regex.Matches(translation).Select(match => match.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (!englishTokens.SequenceEqual(localeTokens, StringComparer.Ordinal))
            {
                errors.Add($"{locale}: key '{key}' has mismatched {tokenDescription}s.");
                invalidKeys?.Add(key);
            }
        }

        private static void ValidateProductTerms(
            string locale,
            string key,
            string englishValue,
            string translation,
            List<string> errors,
            HashSet<string>? invalidKeys)
        {
            foreach (var productTerm in _productTerms)
            {
                var englishCount = Regex.Matches(englishValue, Regex.Escape(productTerm), RegexOptions.CultureInvariant).Count;
                var localeCount = Regex.Matches(translation, Regex.Escape(productTerm), RegexOptions.CultureInvariant).Count;
                if (englishCount != localeCount)
                {
                    errors.Add($"{locale}: key '{key}' changed protected term '{productTerm}'.");
                    invalidKeys?.Add(key);
                }
            }
        }

        private static void ValidatePlaceholders(
            string locale,
            string key,
            string englishValue,
            string translation,
            List<string> errors,
            HashSet<string>? invalidKeys)
        {
            var englishPercentLiteralValues = GetPercentLiteralValues(englishValue);
            var englishPlaceholders = PlaceholderRegex().Matches(englishValue).Select(match => match.Value).ToArray();
            var englishPlaceholderSet = englishPlaceholders.ToHashSet(StringComparer.Ordinal);
            var localePlaceholders = GetLocalePlaceholders(translation, englishPercentLiteralValues, englishPlaceholderSet).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Array.Sort(englishPlaceholders, StringComparer.Ordinal);
            if (!englishPlaceholders.SequenceEqual(localePlaceholders, StringComparer.Ordinal))
            {
                errors.Add($"{locale}: key '{key}' has mismatched placeholders.");
                invalidKeys?.Add(key);
            }
        }

        private static IReadOnlySet<string> GetPercentLiteralValues(string value)
        {
            return SuffixPercentLiteralRegex()
                .Matches(value)
                .Select(match => match.Groups[1].Value)
                .ToHashSet(StringComparer.Ordinal);
        }

        private static IReadOnlyList<string> GetLocalePlaceholders(
            string value,
            IReadOnlySet<string> englishPercentLiteralValues,
            IReadOnlySet<string> englishPlaceholderSet)
        {
            var placeholders = new List<string>();

            foreach (Match match in PlaceholderRegex().Matches(value))
            {
                var placeholderNumber = match.Groups[1].Value;
                var followedByPercentSign = match.Index + match.Length < value.Length
                    && value[match.Index + match.Length] == '%';
                var leadingPercentLiteral = match.Index == 0
                    || !char.IsDigit(value[match.Index - 1]);
                if (leadingPercentLiteral
                    && !followedByPercentSign
                    && !englishPlaceholderSet.Contains(match.Value)
                    && englishPercentLiteralValues.Contains(placeholderNumber))
                {
                    continue;
                }

                placeholders.Add(match.Value);
            }

            return placeholders;
        }

        [GeneratedRegex("%([1-9][0-9]?)(?![0-9])", RegexOptions.CultureInvariant)]
        private static partial Regex PlaceholderRegex();

        [GeneratedRegex("([1-9][0-9]?)\\s*%", RegexOptions.CultureInvariant)]
        private static partial Regex SuffixPercentLiteralRegex();

        [GeneratedRegex("&[A-Za-z0-9#]+;", RegexOptions.CultureInvariant)]
        private static partial Regex HtmlEntityRegex();
    }
}
