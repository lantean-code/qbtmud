using System;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class TickSwitchTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public TickSwitchTests()
        {
            _target = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_ValueTrue_WHEN_Rendered_THEN_ShouldUseSuccessIcon()
        {
            var cut = _target.RenderComponent<TickSwitch<bool>>(parameters =>
            {
                parameters.Add(p => p.Value, true);
            });

            cut.Instance.ThumbIcon.Should().Be(Icons.Material.Filled.Done);
            cut.Instance.ThumbIconColor.Should().Be(Color.Success);
        }

        [Fact]
        public void GIVEN_ValueFalse_WHEN_Rendered_THEN_ShouldUseErrorIcon()
        {
            var cut = _target.RenderComponent<TickSwitch<bool>>(parameters =>
            {
                parameters.Add(p => p.Value, false);
            });

            cut.Instance.ThumbIcon.Should().Be(Icons.Material.Filled.Close);
            cut.Instance.ThumbIconColor.Should().Be(Color.Error);
        }

        public void Dispose()
        {
            _target.Dispose();
        }
    }
}
