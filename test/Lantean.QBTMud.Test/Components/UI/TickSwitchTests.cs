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
    }
}
