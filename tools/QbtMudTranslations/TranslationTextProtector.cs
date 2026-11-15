using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace QbtMudTranslations
{
    internal static partial class TranslationTextProtector
    {
        private static readonly string[] _protectedTerms =
        [
            "qBittorrent",
            "qbtmud"
        ];

        public static IReadOnlyList<ProtectedTranslationText> ProtectForAzure(IReadOnlyList<string> sourceTexts)
        {
            return sourceTexts.Select(ProtectForAzure).ToArray();
        }

        public static IReadOnlyList<ProtectedTranslationText> ProtectForGoogle(IReadOnlyList<string> sourceTexts)
        {
            return sourceTexts.Select(ProtectForGoogle).ToArray();
        }

        public static ProtectedTranslationText ProtectForAzure(string sourceText)
        {
            return ProtectAsHtml(sourceText, useAzureDynamicDictionary: true);
        }

        public static ProtectedTranslationText ProtectForGoogle(string sourceText)
        {
            return ProtectAsHtml(sourceText, useAzureDynamicDictionary: false);
        }

        /// <summary>
        /// Removes protection markup and restores complete protected tokens in translated text.
        /// </summary>
        /// <param name="translatedText">The translated HTML text.</param>
        /// <param name="tokenMap">The original fragments indexed by protected token.</param>
        /// <returns>The translation with the original protected fragments restored.</returns>
        public static string RestoreFromHtml(string translatedText, IReadOnlyDictionary<string, string> tokenMap)
        {
            var restoredText = ProtectedTokenRegex().Replace(translatedText, match =>
            {
                return tokenMap.TryGetValue(match.Value, out var fragment) ? WebUtility.HtmlEncode(fragment) : match.Value;
            });
            restoredText = StripProtectionTagsRegex().Replace(restoredText, string.Empty);
            return WebUtility.HtmlDecode(restoredText);
        }

        private static ProtectedTranslationText ProtectAsHtml(string sourceText, bool useAzureDynamicDictionary)
        {
            var tokenMap = new Dictionary<string, string>(StringComparer.Ordinal);
            var protectedTextBuilder = new StringBuilder(sourceText.Length + 32);
            var tokenIndex = 0;
            protectedTextBuilder.Append("<div>");

            var currentIndex = 0;
            foreach (Match match in ProtectedFragmentRegex().Matches(sourceText))
            {
                protectedTextBuilder.Append(WebUtility.HtmlEncode(sourceText[currentIndex..match.Index]));
                protectedTextBuilder.Append(BuildProtectedFragment(match.Value, tokenMap, ref tokenIndex, useAzureDynamicDictionary));
                currentIndex = match.Index + match.Length;
            }

            protectedTextBuilder.Append(WebUtility.HtmlEncode(sourceText[currentIndex..]));
            protectedTextBuilder.Append("</div>");

            return new ProtectedTranslationText
            {
                SourceText = sourceText,
                ProtectedText = protectedTextBuilder.ToString(),
                TokenMap = tokenMap
            };
        }

        private static string BuildProtectedFragment(
            string fragment,
            Dictionary<string, string> tokenMap,
            ref int tokenIndex,
            bool useAzureDynamicDictionary)
        {
            if (useAzureDynamicDictionary && _protectedTerms.Contains(fragment, StringComparer.Ordinal))
            {
                var encodedFragment = WebUtility.HtmlEncode(fragment);
                return $"""<mstrans:dictionary translation="{encodedFragment}">{encodedFragment}</mstrans:dictionary>""";
            }

            var token = AddToken(fragment, tokenMap, ref tokenIndex);
            return $"""<span class="notranslate" translate="no">{token}</span>""";
        }

        private static string AddToken(string value, Dictionary<string, string> tokenMap, ref int tokenIndex)
        {
            var token = $"QBTMUDTOKEN{tokenIndex++}";
            tokenMap[token] = value;
            return token;
        }

        [GeneratedRegex("""qBittorrent|qbtmud|%([1-9][0-9]?)(?![0-9])|&[A-Za-z0-9#]+;""", RegexOptions.CultureInvariant)]
        private static partial Regex ProtectedFragmentRegex();

        [GeneratedRegex("QBTMUDTOKEN[0-9]+", RegexOptions.CultureInvariant)]
        private static partial Regex ProtectedTokenRegex();

        [GeneratedRegex("""</?(?:div|span)\b[^>]*>""", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
        private static partial Regex StripProtectionTagsRegex();
    }
}
