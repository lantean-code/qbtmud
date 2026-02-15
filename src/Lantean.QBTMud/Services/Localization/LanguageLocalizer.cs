using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Lantean.QBTMud.Services.Localization
{
    /// <summary>
    /// Provides qBittorrent-style translation lookup using context and source keys.
    /// </summary>
    public sealed class LanguageLocalizer : ILanguageLocalizer
    {
        private static readonly Regex PlaceholderRegex = new("%([1-9][0-9]?)", RegexOptions.Compiled);
        private static readonly ConcurrentDictionary<string, string> NormalizedPlaceholderCache = new(StringComparer.Ordinal);

        private readonly ILanguageResourceProvider _resourceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageLocalizer"/> class.
        /// </summary>
        /// <param name="resourceProvider">The provider containing localization dictionaries.</param>
        public LanguageLocalizer(ILanguageResourceProvider resourceProvider)
        {
            _resourceProvider = resourceProvider;
        }

        /// <inheritdoc />
        public string Translate(string context, string source, params object[] arguments)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return string.Empty;
            }

            var formatArguments = arguments ?? Array.Empty<object>();
            var resources = _resourceProvider.Resources;
            var key = CreateKey(context, source);

            if (resources.Aliases.TryGetValue(key, out var aliasKey) && !string.IsNullOrWhiteSpace(aliasKey))
            {
                key = aliasKey;
            }

            if (resources.Overrides.TryGetValue(key, out var overrideTranslation))
            {
                return FormatTranslation(overrideTranslation, formatArguments, source);
            }

            if (resources.Translations.TryGetValue(key, out var translation))
            {
                return FormatTranslation(translation, formatArguments, source);
            }

            return FormatTranslation(source, formatArguments, source);
        }

        private static string CreateKey(string context, string source)
        {
            if (string.IsNullOrWhiteSpace(context))
            {
                return source;
            }

            return string.Concat(context, '|', source);
        }

        private static string FormatTranslation(string translation, object[] arguments, string source)
        {
            var value = string.IsNullOrWhiteSpace(translation) ? source : translation;
            if (arguments.Length == 0)
            {
                return value;
            }

            var normalized = NormalizePlaceholders(value);
            try
            {
                return string.Format(CultureInfo.CurrentCulture, normalized, arguments);
            }
            catch (FormatException)
            {
                return value;
            }
        }

        private static string NormalizePlaceholders(string value)
        {
            if (!value.Contains('%', StringComparison.Ordinal))
            {
                return value;
            }

            return NormalizedPlaceholderCache.GetOrAdd(value, static cachedValue => PlaceholderRegex.Replace(cachedValue, match =>
            {
                var index = int.Parse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                return string.Concat("{", index - 1, "}");
            }));
        }
    }
}
