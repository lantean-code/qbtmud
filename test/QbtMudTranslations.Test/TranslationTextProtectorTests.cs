using AwesomeAssertions;
using QbtMudTranslations;

namespace QbtMudTranslations.Test
{
    public sealed class TranslationTextProtectorTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GIVEN_ProtectedFragmentsFollowedByDigits_WHEN_RoundTripped_THEN_ShouldPreserveBoundariesAndLiteralMarkup(bool useAzure)
        {
            ITranslationTextProtector target = useAzure ? new AzureTranslationTextProtector() : new GoogleTranslationTextProtector();
            var source = "&quot;123 &amp;4 <span>Literal</span> &lt;div&gt;";
            var protectedText = target.Protect([source]).Single();

            var result = target.Restore(protectedText.ProtectedText, protectedText.TokenMap);

            result.Should().Be(source);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GIVEN_MoreThanTenProtectedFragments_WHEN_ProviderTextRestored_THEN_ShouldPreserveEveryFragment(bool useAzure)
        {
            ITranslationTextProtector target = useAzure ? new AzureTranslationTextProtector() : new GoogleTranslationTextProtector();
            var sourceText = "Prefix" + string.Concat(Enumerable.Range(1, 12).Select(index => $"%{index}")) + "&quot;Suffix";
            var protectedText = target.Protect([sourceText]).Single();

            var result = target.Restore(protectedText.ProtectedText, protectedText.TokenMap);

            protectedText.TokenMap.Should().HaveCount(13);
            result.Should().Be(sourceText);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void GIVEN_OverlappingNumericTokenIdentities_WHEN_Restored_THEN_ShouldMatchCompleteIdentitiesRegardlessOfMapOrder(bool useAzure, bool reverseMap)
        {
            ITranslationTextProtector target = useAzure ? new AzureTranslationTextProtector() : new GoogleTranslationTextProtector();
            IReadOnlyDictionary<string, string> tokenMap = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["QBTMUDTOKEN1"] = "%1",
                ["QBTMUDTOKEN10"] = "&quot;",
                ["QBTMUDTOKEN101"] = "qbtmud",
                ["QBTMUDTOKEN1001"] = "%99",
                ["QBTMUDTOKEN01"] = "qBittorrent"
            };
            if (reverseMap)
            {
                tokenMap = tokenMap.Reverse().ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
            }

            var result = target.Restore("<div>QBTMUDTOKEN1QBTMUDTOKEN10|QBTMUDTOKEN101|QBTMUDTOKEN1001|QBTMUDTOKEN01|QBTMUDTOKEN100|QBTMUDTOKEN|qbtmudtoken1</div>", tokenMap);

            result.Should().Be("%1&quot;|qbtmud|%99|qBittorrent|QBTMUDTOKEN100|QBTMUDTOKEN|qbtmudtoken1");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GIVEN_FragmentContainingTokenTextAndReplacementMetacharacters_WHEN_Restored_THEN_ShouldInsertFragmentLiterally(bool useAzure)
        {
            ITranslationTextProtector target = useAzure ? new AzureTranslationTextProtector() : new GoogleTranslationTextProtector();
            var tokenMap = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["QBTMUDTOKEN0"] = "QBTMUDTOKEN1 $1 $& &quot;",
                ["QBTMUDTOKEN1"] = "%1"
            };

            var result = target.Restore("<div>QBTMUDTOKEN0 QBTMUDTOKEN1</div>", tokenMap);

            result.Should().Be("QBTMUDTOKEN1 $1 $& &quot; %1");
        }
    }
}
