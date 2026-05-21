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

            var resolvedLocale = TryResolveLocaleCandidate(desiredLocale, languages);
            if (resolvedLocale is not null)
            {
                return resolvedLocale;
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

            foreach (var candidate in GetLocaleCandidates(locale))
            {
                var exact = TryGetLocale(candidate, languages);
                if (exact is not null)
                {
                    return exact;
                }
            }

            return null;
        }

        private static IEnumerable<string> GetLocaleCandidates(string locale)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var trimmed = locale.Trim();

            if (seen.Add(trimmed))
            {
                yield return trimmed;
            }

            var underscored = trimmed.Replace('-', '_');
            if (seen.Add(underscored))
            {
                yield return underscored;
            }

            var hyphenated = trimmed.Replace('_', '-');
            if (seen.Add(hyphenated))
            {
                yield return hyphenated;
            }

            foreach (var modifierCandidate in WebUiLocaleScriptMapper.GetLocaleCandidatesForScript(hyphenated))
            {
                if (seen.Add(modifierCandidate))
                {
                    yield return modifierCandidate;
                }
            }

            var baseLocale = GetBaseLocale(trimmed);
            if (!string.IsNullOrWhiteSpace(baseLocale) && seen.Add(baseLocale))
            {
                yield return baseLocale;
            }

            var underscoredBaseLocale = baseLocale.Replace('-', '_');
            if (!string.IsNullOrWhiteSpace(underscoredBaseLocale) && seen.Add(underscoredBaseLocale))
            {
                yield return underscoredBaseLocale;
            }
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
