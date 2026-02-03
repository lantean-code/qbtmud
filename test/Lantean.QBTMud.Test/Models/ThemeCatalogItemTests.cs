using AwesomeAssertions;
using Lantean.QBTMud.Models;
using MudBlazor.ThemeManager;

namespace Lantean.QBTMud.Test.Models
{
    public sealed class ThemeCatalogItemTests
    {
        [Fact]
        public void GIVEN_ServerTheme_WHEN_IsReadOnlyRequested_THEN_ReturnsTrue()
        {
            var item = new ThemeCatalogItem("Id", "Name", new ThemeManagerTheme(), ThemeSource.Server, "Path");

            item.IsReadOnly.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_LocalTheme_WHEN_IsReadOnlyRequested_THEN_ReturnsFalse()
        {
            var item = new ThemeCatalogItem("Id", "Name", new ThemeManagerTheme(), ThemeSource.Local, null);

            item.IsReadOnly.Should().BeFalse();
        }
    }
}
