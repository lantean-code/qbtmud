using AwesomeAssertions;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class TickSwitchTests : IDisposable
    {
        private readonly ComponentTestContext _context;

        public TickSwitchTests()
        {
            _context = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_ValueTrue_WHEN_Rendered_THEN_ShouldUseSuccessIcon()
        {
            var target = _context.RenderComponent<TickSwitch<bool>>(parameters =>
            {
                parameters.Add(p => p.Value, true);
            });

            target.Instance.ThumbIcon.Should().Be(Icons.Material.Filled.Done);
            target.Instance.ThumbIconColor.Should().Be(Color.Success);
        }

        [Fact]
        public void GIVEN_ValueFalse_WHEN_Rendered_THEN_ShouldUseErrorIcon()
        {
            var target = _context.RenderComponent<TickSwitch<bool>>(parameters =>
            {
                parameters.Add(p => p.Value, false);
            });

            target.Instance.ThumbIcon.Should().Be(Icons.Material.Filled.Close);
            target.Instance.ThumbIconColor.Should().Be(Color.Error);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}