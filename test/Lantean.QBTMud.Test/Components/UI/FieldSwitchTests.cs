using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class FieldSwitchTests : RazorComponentTestBase<FieldSwitch>
    {
        [Fact]
        public void GIVEN_LabelAndHelper_WHEN_Rendered_THEN_ShouldRenderFieldAndSwitch()
        {
            var target = TestContext.Render<FieldSwitch>(parameters =>
            {
                parameters.Add(p => p.Label, "Label");
                parameters.Add(p => p.HelperText, "HelperText");
                parameters.Add(p => p.Value, false);
            });

            var field = target.FindComponent<MudField>();
            field.Instance.Label.Should().Be("Label");
            field.Instance.HelperText.Should().Be("HelperText");
        }

        [Fact]
        public void GIVEN_DisabledSwitch_WHEN_Rendered_THEN_ShouldDisableInput()
        {
            var target = TestContext.Render<FieldSwitch>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
                parameters.Add(p => p.Value, false);
            });

            var input = target.Find("input");
            input.HasAttribute("disabled").Should().BeTrue();
        }

        [Fact]
        public void GIVEN_ValueChanged_WHEN_Toggled_THEN_ShouldUpdateValueAndInvokeCallback()
        {
            var callbackValue = false;

            var target = TestContext.Render<FieldSwitch>(parameters =>
            {
                parameters.Add(p => p.Value, false);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, value => callbackValue = value));
            });

            target.Find("input").Change(true);

            callbackValue.Should().BeTrue();
            target.Instance.Value.Should().BeTrue();
        }
    }
}
