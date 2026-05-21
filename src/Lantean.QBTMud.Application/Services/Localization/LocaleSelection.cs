using System.Globalization;
using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Services.Localization
{
    internal static class LocaleSelection
    {
        internal static string ResolveLocale(string? desiredLocale, IReadOnlyList<LanguageCatalogItem> languages)
        {
            if (languages.Count == 0)
            {
                return "en";
            }

            if (string.IsNullOrWhiteSpace(desiredLocale))
            {
                var currentLocale = TryResolveCurrentCultureLocale(languages);
                if (currentLocale is not null)
                {
                    return currentLocale;
                }

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

        private static string? TryResolveCurrentCultureLocale(IReadOnlyList<LanguageCatalogItem> languages)
        {
            var currentUiCultureLocale = TryResolveLocaleCandidate(CultureInfo.CurrentUICulture.Name, languages);
            if (currentUiCultureLocale is not null)
            {
                return currentUiCultureLocale;
            }

            return TryResolveLocaleCandidate(CultureInfo.CurrentCulture.Name, languages);
        }

        private static string? TryResolveLocaleCandidate(string? locale, IReadOnlyList<LanguageCatalogItem> languages)
        {
            if (string.IsNullOrWhiteSpace(locale))
            {
                return null;
            }

            var trimmed = locale.Trim();

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

            return null;
        }

        private static string? TryGetLocale(string locale, IReadOnlyList<LanguageCatalogItem> languages)
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
