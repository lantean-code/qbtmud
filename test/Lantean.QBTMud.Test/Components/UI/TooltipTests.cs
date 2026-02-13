using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class TooltipTests : RazorComponentTestBase<Tooltip>
    {
        [Fact]
        public void GIVEN_DefaultSettings_WHEN_HoverPointerUnavailable_THEN_UsesTouchSafeTriggers()
        {
            TestContext.JSInterop.Setup<bool>("qbt.supportsHoverPointer", _ => true)
                .SetResult(false);

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
            });

            var tooltip = target.FindComponent<MudTooltip>();

            tooltip.Instance.ShowOnHover.Should().BeFalse();
            tooltip.Instance.ShowOnFocus.Should().BeFalse();
            tooltip.Instance.ShowOnClick.Should().BeFalse();
            TestContext.JSInterop.Invocations.Should().ContainSingle(invocation => invocation.Identifier == "qbt.supportsHoverPointer");
        }

        [Fact]
        public void GIVEN_DefaultSettings_WHEN_HoverPointerAvailable_THEN_UsesDesktopTriggers()
        {
            TestContext.JSInterop.Setup<bool>("qbt.supportsHoverPointer", _ => true)
                .SetResult(true);

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
            });

            target.WaitForAssertion(() =>
            {
                var tooltip = target.FindComponent<MudTooltip>();
                tooltip.Instance.ShowOnHover.Should().BeTrue();
                tooltip.Instance.ShowOnFocus.Should().BeTrue();
                tooltip.Instance.ShowOnClick.Should().BeFalse();
            });

            TestContext.JSInterop.Invocations.Should().ContainSingle(invocation => invocation.Identifier == "qbt.supportsHoverPointer");
        }

        [Fact]
        public void GIVEN_CustomTouchTriggers_WHEN_HoverPointerUnavailable_THEN_UsesTouchOverrides()
        {
            TestContext.JSInterop.Setup<bool>("qbt.supportsHoverPointer", _ => true)
                .SetResult(false);

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
                parameters.Add(p => p.ShowOnHover, true);
                parameters.Add(p => p.ShowOnFocus, false);
                parameters.Add(p => p.ShowOnClick, false);
                parameters.Add(p => p.ShowOnHoverOnTouch, true);
                parameters.Add(p => p.ShowOnFocusOnTouch, true);
                parameters.Add(p => p.ShowOnClickOnTouch, true);
            });

            var tooltip = target.FindComponent<MudTooltip>();

            tooltip.Instance.ShowOnHover.Should().BeTrue();
            tooltip.Instance.ShowOnFocus.Should().BeTrue();
            tooltip.Instance.ShowOnClick.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_TouchOptimizationDisabled_WHEN_Rendered_THEN_UsesConfiguredDesktopTriggersWithoutInterop()
        {
            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
                parameters.Add(p => p.UseTouchOptimizedBehavior, false);
                parameters.Add(p => p.ShowOnHover, false);
                parameters.Add(p => p.ShowOnFocus, false);
                parameters.Add(p => p.ShowOnClick, true);
            });

            var tooltip = target.FindComponent<MudTooltip>();

            tooltip.Instance.ShowOnHover.Should().BeFalse();
            tooltip.Instance.ShowOnFocus.Should().BeFalse();
            tooltip.Instance.ShowOnClick.Should().BeTrue();
            TestContext.JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == "qbt.supportsHoverPointer");
        }

        [Fact]
        public void GIVEN_JsInteropThrows_WHEN_Rendered_THEN_FallsBackToTouchSafeBehavior()
        {
            TestContext.JSInterop.Setup<bool>("qbt.supportsHoverPointer", _ => true)
                .SetException(new JSException("Error"));

            var target = RenderTooltip(parameters =>
            {
                parameters.Add(p => p.Text, "Text");
            });

            var tooltip = target.FindComponent<MudTooltip>();

            tooltip.Instance.ShowOnHover.Should().BeFalse();
            tooltip.Instance.ShowOnFocus.Should().BeFalse();
            tooltip.Instance.ShowOnClick.Should().BeFalse();
            TestContext.JSInterop.Invocations.Should().ContainSingle(invocation => invocation.Identifier == "qbt.supportsHoverPointer");
        }

        [Fact]
        public void GIVEN_UnmatchedAttributes_WHEN_Rendered_THEN_ForwardsAttributesToMudTooltip()
        {
            var target = RenderTooltip(parameters =>
            {
                parameters.AddUnmatched("data-test-id", "TooltipComponent");
                parameters.Add(p => p.Text, "Text");
            });

            var tooltip = target.FindComponent<MudTooltip>();

            HasTestId(tooltip, "TooltipComponent").Should().BeTrue();
        }

        private IRenderedComponent<Tooltip> RenderTooltip(Action<ComponentParameterCollectionBuilder<Tooltip>> configure)
        {
            return TestContext.Render<Tooltip>(parameters =>
            {
                parameters.Add(p => p.ChildContent, (RenderFragment)(builder =>
                {
                    builder.OpenElement(0, "button");
                    builder.AddContent(1, "Action");
                    builder.CloseElement();
                }));

                configure(parameters);
            });
        }
    }
}
