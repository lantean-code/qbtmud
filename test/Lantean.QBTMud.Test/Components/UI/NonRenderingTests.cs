using System;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class NonRenderingTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public NonRenderingTests()
        {
            _target = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_ChildContent_WHEN_Rendered_THEN_ShouldRenderChildContent()
        {
            var cut = _target.RenderComponent<NonRendering>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            cut.Markup.Should().Be("ChildContent");
        }

        public void Dispose()
        {
            _target.Dispose();
        }
    }
}
