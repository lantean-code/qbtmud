using AwesomeAssertions;
using Lantean.QBTMud.Models;
using MudBlazor;

namespace Lantean.QBTMud.Test.Models
{
    public sealed class WizardAccentColorTests
    {
        [Fact]
        public void GIVEN_PaletteColor_WHEN_FromPaletteInvoked_THEN_CreatesPaletteAccent()
        {
            var result = WizardAccentColor.FromPalette(Color.Info);

            result.Kind.Should().Be(WizardAccentColorKind.Palette);
            result.PaletteColor.Should().Be(Color.Info);
            result.CssColor.Should().BeNull();
        }

        [Fact]
        public void GIVEN_CssColor_WHEN_FromCssInvoked_THEN_CreatesCssAccent()
        {
            var result = WizardAccentColor.FromCss("#44AAFF");

            result.Kind.Should().Be(WizardAccentColorKind.Css);
            result.PaletteColor.Should().BeNull();
            result.CssColor.Should().Be("#44AAFF");
        }

        [Fact]
        public void GIVEN_WhitespaceCssColor_WHEN_FromCssInvoked_THEN_ThrowsArgumentException()
        {
            var action = () => WizardAccentColor.FromCss("  ");

            action.Should().Throw<ArgumentException>()
                .WithMessage("*cannot be null or whitespace*")
                .WithParameterName("cssColor");
        }
    }
}
