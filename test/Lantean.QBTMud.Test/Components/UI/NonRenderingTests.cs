using AwesomeAssertions;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class NonRenderingTests : IDisposable
    {
        private readonly ComponentTestContext _context;

        public NonRenderingTests()
        {
            _context = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_ChildContent_WHEN_Rendered_THEN_ShouldRenderChildContent()
        {
            var target = _context.RenderComponent<NonRendering>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            target.Markup.Should().Be("ChildContent");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}