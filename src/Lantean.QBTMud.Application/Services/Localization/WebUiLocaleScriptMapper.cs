namespace Lantean.QBTMud.Application.Services.Localization
{
    internal static class WebUiLocaleScriptMapper
    {
        internal static IEnumerable<string> GetLocaleCandidatesForScript(string locale)
        {
            var normalized = locale.Replace('_', '-');
            var segments = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 2)
            {
                yield break;
            }

            var script = NormalizeScriptTag(segments[1]);
            if (string.IsNullOrWhiteSpace(script))
            {
                yield break;
            }

            var baseLocale = segments[0];
            yield return string.Concat(baseLocale, "@", script);

            var alias = GetScriptAlias(script);
            if (!string.IsNullOrWhiteSpace(alias))
            {
                yield return string.Concat(baseLocale, "@", alias);
            }
        }

        internal static string NormalizeLocaleForCulture(string locale)
        {
            var normalized = locale.Replace('_', '-');
            var atIndex = normalized.IndexOf('@', StringComparison.Ordinal);
            if (atIndex < 0)
            {
                return normalized;
            }

            var basePart = normalized[..atIndex];
            var scriptPart = normalized[(atIndex + 1)..];
            if (string.IsNullOrWhiteSpace(scriptPart))
            {
                return basePart;
            }

            var script = NormalizeScriptTag(scriptPart);
            if (string.IsNullOrWhiteSpace(script))
            {
                return basePart;
            }

            return string.Concat(basePart, "-", script);
        }

        private static string NormalizeScriptTag(string script)
        {
            if (string.Equals(script, "latin", StringComparison.OrdinalIgnoreCase))
            {
                return "Latn";
            }

            if (string.Equals(script, "cyrillic", StringComparison.OrdinalIgnoreCase))
            {
                return "Cyrl";
            }

            if (script.Length == 4 && script.All(char.IsLetter))
            {
                return string.Concat(char.ToUpperInvariant(script[0]), script.Substring(1).ToLowerInvariant());
            }

            return string.Empty;
        }

        private static string GetScriptAlias(string script)
        {
            if (string.Equals(script, "Latn", StringComparison.Ordinal))
            {
                return "latin";
            }

            if (string.Equals(script, "Cyrl", StringComparison.Ordinal))
            {
                return "cyrillic";
            }

            return string.Empty;
        }
    }
}
