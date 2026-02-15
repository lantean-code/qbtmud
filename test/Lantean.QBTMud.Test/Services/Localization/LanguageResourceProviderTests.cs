using AwesomeAssertions;
using Lantean.QBTMud.Services.Localization;

namespace Lantean.QBTMud.Test.Services.Localization
{
    public sealed class LanguageResourceProviderTests
    {
        private readonly LanguageResourceProvider _target;

        public LanguageResourceProviderTests()
        {
            _target = new LanguageResourceProvider();
        }

        [Fact]
        public void GIVEN_NewProvider_WHEN_ResourcesRead_THEN_ShouldReturnEmptyResources()
        {
            _target.Resources.Aliases.Should().BeEmpty();
            _target.Resources.Overrides.Should().BeEmpty();
            _target.Resources.Translations.Should().BeEmpty();
            _target.Resources.LoadedCultureName.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_ResourcesProvided_WHEN_SetResources_THEN_ShouldStoreProvidedResources()
        {
            var resources = new LanguageResources(
                new Dictionary<string, string>(StringComparer.Ordinal) { ["Alias"] = "Value" },
                new Dictionary<string, string>(StringComparer.Ordinal) { ["Override"] = "Value" },
                new Dictionary<string, string>(StringComparer.Ordinal) { ["Translation"] = "Value" },
                "fr-FR");

            _target.SetResources(resources);

            _target.Resources.Should().BeSameAs(resources);
        }

        [Fact]
        public void GIVEN_NullResources_WHEN_SetResources_THEN_ShouldThrowArgumentNullException()
        {
            Action action = () => _target.SetResources(null!);

            action.Should().Throw<ArgumentNullException>();
        }
    }
}
