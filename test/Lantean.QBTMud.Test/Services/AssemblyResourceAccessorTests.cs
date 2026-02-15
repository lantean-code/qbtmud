using AwesomeAssertions;
using Lantean.QBTMud.Services;

namespace Lantean.QBTMud.Test.Services
{
    public sealed class AssemblyResourceAccessorTests
    {
        private readonly AssemblyResourceAccessor _target;

        public AssemblyResourceAccessorTests()
        {
            _target = new AssemblyResourceAccessor();
        }

        [Fact]
        public void GIVEN_AssemblyResources_WHEN_GetManifestResourceNames_THEN_ShouldIncludeEmbeddedEnglishTranslation()
        {
            var resourceNames = _target.GetManifestResourceNames();

            resourceNames.Should().Contain(name => name.EndsWith("wwwroot.i18n.webui_en.json", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void GIVEN_KnownAndUnknownResourceNames_WHEN_GetManifestResourceStream_THEN_ShouldReturnExpectedStream()
        {
            var knownResourceName = _target
                .GetManifestResourceNames()
                .First(name => name.EndsWith("wwwroot.i18n.webui_en.json", StringComparison.OrdinalIgnoreCase));

            using var knownStream = _target.GetManifestResourceStream(knownResourceName);
            using var unknownStream = _target.GetManifestResourceStream("missing-resource-name");

            knownStream.Should().NotBeNull();
            unknownStream.Should().BeNull();
        }
    }
}
