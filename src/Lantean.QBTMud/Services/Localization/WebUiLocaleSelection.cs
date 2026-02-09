using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Services.Localization
{
    internal static class WebUiLocaleSelection
    {
        internal static string ResolveLocale(string? desiredLocale, IReadOnlyList<WebUiLanguageCatalogItem> languages)
        {
            if (languages.Count == 0)
            {
                return "en";
            }

            if (string.IsNullOrWhiteSpace(desiredLocale))
            {
                return TryGetLocale("en", languages) ?? languages[0].Code;
            }

            var trimmed = desiredLocale.Trim();

            var exact = TryGetLocale(trimmed, languages);
            if (exact is not null)
            {
                return exact;
            }

            var swapped = trimmed.Replace('-', '_');
            exact = TryGetLocale(swapped, languages);
            if (exact is not null)
            {
                return exact;
            }

            swapped = trimmed.Replace('_', '-');
            exact = TryGetLocale(swapped, languages);
            if (exact is not null)
            {
                return exact;
            }

            var baseLocale = GetBaseLocale(trimmed);
            if (!string.IsNullOrWhiteSpace(baseLocale))
            {
                exact = TryGetLocale(baseLocale, languages);
                if (exact is not null)
                {
                    return exact;
                }

                exact = TryGetLocale(baseLocale.Replace('-', '_'), languages);
                if (exact is not null)
                {
                    return exact;
                }
            }

            return TryGetLocale("en", languages) ?? languages[0].Code;
        }

        private static string? TryGetLocale(string locale, IReadOnlyList<WebUiLanguageCatalogItem> languages)
        {
            for (var i = 0; i < languages.Count; i++)
            {
                var code = languages[i].Code;
                if (string.Equals(code, locale, StringComparison.OrdinalIgnoreCase))
                {
                    return code;
                }
            }

            return null;
        }

        private static string GetBaseLocale(string locale)
        {
            var atIndex = locale.IndexOf('@', StringComparison.Ordinal);
            if (atIndex > 0)
            {
                locale = locale[..atIndex];
            }

            var dashIndex = locale.IndexOf('-', StringComparison.Ordinal);
            if (dashIndex > 0)
            {
                return locale[..dashIndex];
            }

            var underscoreIndex = locale.IndexOf('_', StringComparison.Ordinal);
            if (underscoreIndex > 0)
            {
                return locale[..underscoreIndex];
            }

            return locale;
        }
    }
}
