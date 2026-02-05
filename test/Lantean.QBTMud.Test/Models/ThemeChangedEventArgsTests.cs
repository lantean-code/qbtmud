using AwesomeAssertions;
using Lantean.QBTMud.Models;
using MudBlazor;

namespace Lantean.QBTMud.Test.Models
{
    public sealed class ThemeChangedEventArgsTests
    {
        [Fact]
        public void GIVEN_Arguments_WHEN_Constructed_THEN_AssignsProperties()
        {
            var theme = new MudTheme();

            var args = new ThemeChangedEventArgs(theme, "FontFamily", "ThemeId");

            args.Theme.Should().BeSameAs(theme);
            args.FontFamily.Should().Be("FontFamily");
            args.ThemeId.Should().Be("ThemeId");
        }
    }
}
