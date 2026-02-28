namespace Lantean.QBTMud.Services.Localization
{
    internal static class WebUiLocaleNormalizer
    {
        internal static string Normalize(string locale)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(locale);

            var trimmedLocale = locale.Trim();
            var baseLocale = GetBaseLocale(trimmedLocale);

            if (string.Equals(baseLocale, "C", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(baseLocale, "POSIX", StringComparison.OrdinalIgnoreCase))
            {
                return "en";
            }

            return trimmedLocale;
        }

        private static string GetBaseLocale(string locale)
        {
            var trimmedLocale = locale.Trim();
            var separatorIndex = trimmedLocale.IndexOfAny(['.', '@', '-', '_']);
            if (separatorIndex <= 0)
            {
                return trimmedLocale;
            }

            return trimmedLocale[..separatorIndex];
        }
    }
}
