using AwesomeAssertions;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Test.Models
{
    public sealed class ThemeDefinitionTests
    {
        [Fact]
        public void GIVEN_DefaultInstance_WHEN_Created_THEN_SetsDefaults()
        {
            var definition = new ThemeDefinition();

            definition.Id.Should().NotBeNullOrWhiteSpace();
            definition.Name.Should().BeEmpty();
            definition.Theme.Should().NotBeNull();
        }
    }
}
