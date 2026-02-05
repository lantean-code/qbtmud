using AwesomeAssertions;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Theming;
using MudBlazor;

namespace Lantean.QBTMud.Test.Theming
{
    public sealed class ThemeFontHelperTests
    {
        [Fact]
        public void GIVEN_WhitespaceFont_WHEN_Applied_THEN_DoesNotModifyTheme()
        {
            var theme = new ThemeDefinition
            {
                FontFamily = "FontFamily",
                Theme = new MudTheme()
            };
            var original = theme.FontFamily;

            ThemeFontHelper.ApplyFont(theme, " ");

            theme.FontFamily.Should().Be(original);
        }

        [Fact]
        public void GIVEN_FontFamily_WHEN_Applied_THEN_TypographyUpdated()
        {
            var theme = new ThemeDefinition
            {
                FontFamily = "FontFamily",
                Theme = new MudTheme()
            };

            ThemeFontHelper.ApplyFont(theme, "Nunito Sans");

            theme.FontFamily.Should().Be("Nunito Sans");
            theme.Theme.Typography.Body1.FontFamily.Should().Contain("Nunito Sans");
            theme.Theme.Typography.H1.FontFamily.Should().Contain("Nunito Sans");
            theme.Theme.Typography.Button.FontFamily.Should().Contain("Nunito Sans");
        }
    }
}
