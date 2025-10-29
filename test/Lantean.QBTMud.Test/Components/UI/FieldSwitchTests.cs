using System;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class FieldSwitchTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public FieldSwitchTests()
        {
            _target = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_LabelAndHelper_WHEN_Rendered_THEN_ShouldRenderFieldAndSwitch()
        {
            var cut = _target.RenderComponent<FieldSwitch>(parameters =>
            {
                parameters.Add(p => p.Label, "Label");
                parameters.Add(p => p.HelperText, "HelperText");
                parameters.Add(p => p.Value, false);
            });

            cut.Markup.Should().Contain("Label");
            cut.Markup.Should().Contain("HelperText");
        }

        [Fact]
        public void GIVEN_DisabledSwitch_WHEN_Rendered_THEN_ShouldDisableInput()
        {
            var cut = _target.RenderComponent<FieldSwitch>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
                parameters.Add(p => p.Value, false);
            });

            var input = cut.Find("input");
            input.HasAttribute("disabled").Should().BeTrue();
        }

        [Fact]
        public void GIVEN_ValueChanged_WHEN_Toggled_THEN_ShouldUpdateValueAndInvokeCallback()
        {
            var callbackValue = false;

            var cut = _target.RenderComponent<FieldSwitch>(parameters =>
            {
                parameters.Add(p => p.Value, false);
                parameters.Add(p => p.ValueChanged, EventCallback.Factory.Create<bool>(this, value => callbackValue = value));
            });

            cut.Find("input").Change(true);

            callbackValue.Should().BeTrue();
            cut.Instance.Value.Should().BeTrue();
        }

        public void Dispose()
        {
            _target.Dispose();
        }
    }
}
