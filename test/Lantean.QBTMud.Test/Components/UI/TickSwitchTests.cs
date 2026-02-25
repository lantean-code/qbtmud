using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class TickSwitchTests : RazorComponentTestBase
    {
        [Fact]
        public void GIVEN_ValueTrue_WHEN_Rendered_THEN_ShouldUseSuccessIcon()
        {
            var target = TestContext.Render<TickSwitch<bool>>(parameters =>
            {
                parameters.Add(p => p.Value, true);
            });

            target.Instance.ThumbIcon.Should().Be(Icons.Material.Filled.Done);
            target.Instance.ThumbIconColor.Should().Be(Color.Success);
        }

        [Fact]
        public void GIVEN_ValueFalse_WHEN_Rendered_THEN_ShouldUseErrorIcon()
        {
            var target = TestContext.Render<TickSwitch<bool>>(parameters =>
            {
                parameters.Add(p => p.Value, false);
            });

            target.Instance.ThumbIcon.Should().Be(Icons.Material.Filled.Close);
            target.Instance.ThumbIconColor.Should().Be(Color.Error);
        }

        [Fact]
        public void GIVEN_NullThenTrueValue_WHEN_ReRendered_THEN_ShouldRestoreVisibilityAndIcon()
        {
            var target = TestContext.Render<TickSwitch<bool?>>();

            target.Instance.Style.Should().Contain("display:none;");

            target.Render(parameters =>
            {
                parameters.Add(p => p.Value, true);
            });

            target.Instance.Style.Should().BeNull();
            target.Instance.ThumbIcon.Should().Be(Icons.Material.Filled.Done);
            target.Instance.ThumbIconColor.Should().Be(Color.Success);
        }

        [Fact]
        public void GIVEN_NullValueWithStyleEndingSemicolon_WHEN_ReRenderedWithNull_THEN_ShouldNotDuplicateHiddenStyle()
        {
            var target = TestContext.Render<TickSwitch<bool?>>(parameters =>
            {
                parameters.Add(p => p.Value, (bool?)null);
                parameters.Add(p => p.Style, "opacity:0.5;");
            });

            target.Instance.Style.Should().Be("opacity:0.5;display:none;");

            target.Render();

            target.Instance.Style.Should().Be("opacity:0.5;display:none;");
        }

        [Fact]
        public void GIVEN_NullValueWithStyleWithoutSemicolon_WHEN_Rendered_THEN_ShouldAppendSeparatorBeforeHiddenStyle()
        {
            var target = TestContext.Render<TickSwitch<bool?>>(parameters =>
            {
                parameters.Add(p => p.Value, (bool?)null);
                parameters.Add(p => p.Style, "opacity:0.5");
            });

            target.Instance.Style.Should().Be("opacity:0.5;display:none;");
        }

        [Fact]
        public void GIVEN_StringValue_WHEN_Rendered_THEN_ShouldKeepProvidedStyle()
        {
            var target = TestContext.Render<TickSwitch<string>>(parameters =>
            {
                parameters.Add(p => p.Value, "Value");
                parameters.Add(p => p.Style, "opacity:0.5;");
            });

            target.Instance.Style.Should().Be("opacity:0.5;");
        }
    }
}
