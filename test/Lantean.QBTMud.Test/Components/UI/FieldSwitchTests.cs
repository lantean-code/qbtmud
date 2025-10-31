using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class FieldSwitchTests : IDisposable
    {
        private readonly ComponentTestContext _context;

        public FieldSwitchTests()
        {
            _context = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_LabelAndHelper_WHEN_Rendered_THEN_ShouldRenderFieldAndSwitch()
        {
            var target = _context.RenderComponent<FieldSwitch>(parameters =>
            {
                parameters.Add(p => p.Label, "Label");
                parameters.Add(p => p.HelperText, "HelperText");
                parameters.Add(p => p.Value, false);
            });

            target.Markup.Should().Contain("Label");
            target.Markup.Should().Contain("HelperText");
        }

        [Fact]
        public void GIVEN_DisabledSwitch_WHEN_Rendered_THEN_ShouldDisableInput()
        {
            var target = _context.RenderComponent<FieldSwitch>(parameters =>
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

            var target = _context.RenderComponent<FieldSwitch>(parameters =>
            {
                parameters.Add(p => p.Value, false);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, value => callbackValue = value));
            });

            target.Find("input").Change(true);

            callbackValue.Should().BeTrue();
            target.Instance.Value.Should().BeTrue();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}