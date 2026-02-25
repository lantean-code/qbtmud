using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class EnhancedErrorBoundaryTests : RazorComponentTestBase
    {
        private readonly IRenderedComponent<EnhancedErrorBoundary> _target;

        public EnhancedErrorBoundaryTests()
        {
            _target = RenderBoundary(disabled: false, exception: null);
        }

        [Fact]
        public void GIVEN_NewBoundary_WHEN_Rendered_THEN_ShouldNotBeErrored()
        {
            _target.Instance.HasErrored.Should().BeFalse();
            _target.Instance.Errors.Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_DisabledBoundary_WHEN_ChildThrows_THEN_ShouldRethrowAndCaptureError()
        {
            var exception = new InvalidOperationException("Boom");
            var target = RenderBoundary(disabled: true, exception);
            var button = target.Find("#throw-button");

            var act = () => button.Click();

            act.Should().Throw<InvalidOperationException>().WithMessage("Boom");
            target.Instance.Errors.Should().ContainSingle().Which.Should().BeSameAs(exception);
        }

        private IRenderedComponent<EnhancedErrorBoundary> RenderBoundary(bool disabled, Exception? exception)
        {
            return TestContext.Render<EnhancedErrorBoundary>(parameters =>
            {
                parameters.Add(parameter => parameter.Disabled, disabled);
                parameters.AddChildContent(builder =>
                {
                    if (exception is null)
                    {
                        return;
                    }

                    builder.OpenComponent<ThrowOnClick>(0);
                    builder.AddAttribute(1, nameof(ThrowOnClick.Exception), exception);
                    builder.CloseComponent();
                });
            });
        }

        private sealed class ThrowOnClick : ComponentBase
        {
            [Parameter]
            [EditorRequired]
            public Exception Exception { get; set; } = default!;

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "id", "throw-button");
                builder.AddAttribute(2, "type", "button");
                builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, OnClick));
                builder.AddContent(4, "Throw");
                builder.CloseElement();
            }

            private Task OnClick(MouseEventArgs args)
            {
                throw Exception;
            }
        }
    }
}
