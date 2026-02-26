using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class CustomNavLinkTests : RazorComponentTestBase<CustomNavLink>
    {
        [Fact]
        public void GIVEN_ActiveIconLink_WHEN_Rendered_THEN_ShouldRenderIconAndClasses()
        {
            var target = TestContext.Render<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.Active, true);
                parameters.Add(p => p.Class, "Class");
                parameters.Add(p => p.Icon, "Icon");
                parameters.Add(p => p.IconColor, Color.Default);
                parameters.Add(p => p.OnLongPress, EventCallback.Factory.Create<LongPressEventArgs>(this, _ => Task.CompletedTask));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            var root = target.FindAll("div").First();
            root.ClassList.Should().Contain("mud-nav-item");
            root.ClassList.Should().Contain("mud-ripple");
            root.ClassList.Should().Contain("Class");

            var link = target.Find(".mud-nav-link");
            link.ClassList.Should().Contain("active");
            link.ClassList.Should().Contain("unselectable");

            var icon = target.FindComponent<MudIcon>();
            icon.Instance.Class.Should().Contain("mud-nav-link-icon");
            icon.Instance.Class.Should().Contain("mud-nav-link-icon-default");

            link.TextContent.Should().Contain("ChildContent");
        }

        [Fact]
        public void GIVEN_DisabledLink_WHEN_Clicked_THEN_ShouldNotInvokeOnClick()
        {
            var clicked = false;

            var target = TestContext.Render<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
                parameters.Add(p => p.DisableRipple, true);
                parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicked = true));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            target.Find(".mud-nav-link").Click();

            clicked.Should().BeFalse();

            var root = target.FindAll("div").First();
            root.ClassList.Should().NotContain("mud-ripple");

            var link = target.Find(".mud-nav-link");
            link.ClassList.Should().NotContain("unselectable");
        }

        [Fact]
        public void GIVEN_EnabledLink_WHEN_Clicked_THEN_ShouldInvokeOnClick()
        {
            var clicked = false;

            var target = TestContext.Render<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicked = true));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            target.Find(".mud-nav-link").Click();

            clicked.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_CustomIconColor_WHEN_Rendered_THEN_ShouldOmitDefaultIconClass()
        {
            var target = TestContext.Render<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.Icon, "Icon");
                parameters.Add(p => p.IconColor, Color.Primary);
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            var icon = target.FindComponent<MudIcon>();
            icon.Instance.Class.Should().Contain("mud-nav-link-icon");
            icon.Instance.Class.Should().NotContain("mud-nav-link-icon-default");
        }

        [Fact]
        public async Task GIVEN_LongPressHandler_WHEN_LongPressEventRaised_THEN_ShouldInvokeCallback()
        {
            var pressed = false;

            var target = TestContext.Render<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.OnLongPress, EventCallback.Factory.Create<LongPressEventArgs>(this, _ =>
                {
                    pressed = true;
                    return Task.CompletedTask;
                }));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            await target.Find(".mud-nav-link").TriggerEventAsync("onlongpress", new LongPressEventArgs());

            pressed.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_TestContextMenuHandler_WHENTestContextMenuRaised_THEN_ShouldInvokeCallback()
        {
            var invoked = false;

            var target = TestContext.Render<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.OnContextMenu, EventCallback.Factory.Create<MouseEventArgs>(this, _ =>
                {
                    invoked = true;
                    return Task.CompletedTask;
                }));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            await target.Find(".mud-nav-link").TriggerEventAsync("oncontextmenu", new MouseEventArgs());

            invoked.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_TargetAndContextMenu_WHEN_Rendered_THEN_ParametersAreSet()
        {
            RenderFragment contextMenu = builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddContent(1, "ContextMenu");
                builder.CloseElement();
            };

            var target = TestContext.Render<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.Target, "_blank");
                parameters.Add(p => p.ContextMenu, contextMenu);
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            target.Instance.Target.Should().Be("_blank");
            target.Instance.ContextMenu.Should().NotBeNull();
        }
    }
}
