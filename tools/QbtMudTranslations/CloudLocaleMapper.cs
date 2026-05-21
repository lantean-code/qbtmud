namespace QbtMudTranslations
{
    internal static class CloudLocaleMapper
    {
        private static readonly HashSet<string> _englishLocales = new(StringComparer.OrdinalIgnoreCase)
        {
            "en",
            "en_AU",
            "en_GB"
        };

        private static readonly HashSet<string> _googlePrimaryLocales = new(StringComparer.OrdinalIgnoreCase)
        {
            "eo",
            "ltg",
            "oc"
        };

        private static readonly Dictionary<string, string> _azureOverrides = new(StringComparer.OrdinalIgnoreCase)
        {
            ["az@latin"] = "az",
            ["en_AU"] = "en",
            ["en_GB"] = "en",
            ["hi_IN"] = "hi",
            ["lv_LV"] = "lv",
            ["mn_MN"] = "mn-Cyrl",
            ["ms_MY"] = "ms",
            ["ne_NP"] = "ne",
            ["pt_BR"] = "pt",
            ["pt_PT"] = "pt-PT",
            ["sr"] = "sr-Cyrl",
            ["sr@latin"] = "sr-Latn",
            ["uz@Latn"] = "uz",
            ["zh_CN"] = "zh-Hans",
            ["zh_HK"] = "yue",
            ["zh_TW"] = "zh-Hant"
        };

        private static readonly Dictionary<string, string> _googleOverrides = new(StringComparer.OrdinalIgnoreCase)
        {
            ["az@latin"] = "az",
            ["hi_IN"] = "hi",
            ["lv_LV"] = "lv",
            ["mn_MN"] = "mn",
            ["ms_MY"] = "ms",
            ["ne_NP"] = "ne",
            ["pt_BR"] = "pt-BR",
            ["pt_PT"] = "pt-PT",
            ["sr@latin"] = "sr-Latn",
            ["uz@Latn"] = "uz"
        };

        public static bool IsEnglishLocale(string locale)
        {
            return _englishLocales.Contains(locale);
        }

        public static bool TryResolveAzureLocale(string locale, out string? targetLocale)
        {
            if (IsEnglishLocale(locale) || _googlePrimaryLocales.Contains(locale))
            {
                targetLocale = null;
                return false;
            }

            if (_azureOverrides.TryGetValue(locale, out var overrideLocale))
            {
                targetLocale = overrideLocale;
                return true;
            }

            targetLocale = Normalize(locale);
            return true;
        }

        public static bool TryResolveGoogleLocale(string locale, out string? targetLocale)
        {
            if (IsEnglishLocale(locale))
            {
                targetLocale = null;
                return false;
            }

            if (_googleOverrides.TryGetValue(locale, out var overrideLocale))
            {
                targetLocale = overrideLocale;
                return true;
            }

            targetLocale = Normalize(locale);
            return true;
        }

        private static string Normalize(string locale)
        {
            return locale.Replace('_', '-');
        }
    }
}
