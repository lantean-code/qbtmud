using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.TestSupport.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Lantean.QBTMud.Presentation.Test.Components.UI
{
    public sealed class TdExtendedTests : RazorComponentTestBase<TdExtended>
    {
        [Fact]
        public async Task GIVEN_LongPressHandler_WHEN_LongPressRaised_THEN_ShouldInvokeCallback()
        {
            var invoked = false;

            var target = TestContext.Render<TdExtended>(parameters =>
            {
                parameters.Add(p => p.OnLongPress, EventCallback.Factory.Create<CellLongPressEventArgs>(this, _ =>
                {
                    invoked = true;
                    return Task.CompletedTask;
                }));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            await target.Find("td").TriggerEventAsync("onlongpress", new LongPressEventArgs());

            invoked.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_TestContextMenuHandler_WHENTestContextMenuRaised_THEN_ShouldInvokeCallback()
        {
            var invoked = false;

            var target = TestContext.Render<TdExtended>(parameters =>
            {
                parameters.Add(p => p.OnContextMenu, EventCallback.Factory.Create<CellMouseEventArgs>(this, _ =>
                {
                    invoked = true;
                    return Task.CompletedTask;
                }));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            await target.Find("td").TriggerEventAsync("oncontextmenu", new MouseEventArgs());

            invoked.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_NoHandlers_WHEN_Rendered_THEN_ShouldNotPreventNativeInteractions()
        {
            var target = TestContext.Render<TdExtended>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            target.Instance.OnLongPress.HasDelegate.Should().BeFalse();
            target.Instance.OnContextMenu.HasDelegate.Should().BeFalse();
        }
    }
}
