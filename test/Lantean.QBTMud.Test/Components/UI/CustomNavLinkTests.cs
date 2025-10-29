using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class CustomNavLinkTests : IDisposable
    {
        private readonly ComponentTestContext _target;

        public CustomNavLinkTests()
        {
            _target = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_ActiveIconLink_WHEN_Rendered_THEN_ShouldRenderIconAndClasses()
        {
            var cut = _target.RenderComponent<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.Active, true);
                parameters.Add(p => p.Class, "Class");
                parameters.Add(p => p.Icon, "Icon");
                parameters.Add(p => p.IconColor, Color.Default);
                parameters.Add(p => p.OnLongPress, EventCallback.Factory.Create<LongPressEventArgs>(this, _ => Task.CompletedTask));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            var container = cut.Find(".mud-nav-item");
            var containerClasses = container.GetAttribute("class");
            containerClasses.Should().Contain("mud-nav-item");
            containerClasses.Should().Contain("mud-ripple");
            containerClasses.Should().Contain("Class");

            var link = cut.Find(".mud-nav-link");
            var linkClasses = link.GetAttribute("class");
            linkClasses.Should().Contain("mud-nav-link");
            linkClasses.Should().Contain("active");
            linkClasses.Should().Contain("unselectable");

            var icon = cut.Find(".mud-nav-link-icon");
            var iconClasses = icon.GetAttribute("class");
            iconClasses.Should().Contain("mud-nav-link-icon");
            iconClasses.Should().Contain("mud-nav-link-icon-default");

            cut.Markup.Should().Contain("ChildContent");
        }

        [Fact]
        public void GIVEN_DisabledLink_WHEN_Clicked_THEN_ShouldNotInvokeOnClick()
        {
            var clicked = false;

            var cut = _target.RenderComponent<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
                parameters.Add(p => p.DisableRipple, true);
                parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicked = true));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            cut.Find(".mud-nav-link").Click();

            clicked.Should().BeFalse();

            var containerClasses = cut.Find(".mud-nav-item").GetAttribute("class");
            containerClasses.Should().NotContain("mud-ripple");

            var linkClasses = cut.Find(".mud-nav-link").GetAttribute("class");
            linkClasses.Should().NotContain("unselectable");
        }

        [Fact]
        public void GIVEN_EnabledLink_WHEN_Clicked_THEN_ShouldInvokeOnClick()
        {
            var clicked = false;

            var cut = _target.RenderComponent<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.OnClick, EventCallback.Factory.Create<MouseEventArgs>(this, _ => clicked = true));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            cut.Find(".mud-nav-link").Click();

            clicked.Should().BeTrue();
        }

        [Fact]
        public void GIVEN_CustomIconColor_WHEN_Rendered_THEN_ShouldOmitDefaultIconClass()
        {
            var cut = _target.RenderComponent<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.Icon, "Icon");
                parameters.Add(p => p.IconColor, Color.Primary);
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            var icon = cut.Find(".mud-nav-link-icon");
            var iconClasses = icon.GetAttribute("class");
            iconClasses.Should().Contain("mud-nav-link-icon");
            iconClasses.Should().NotContain("mud-nav-link-icon-default");
        }

        [Fact]
        public async Task GIVEN_LongPressHandler_WHEN_LongPressEventRaised_THEN_ShouldInvokeCallback()
        {
            var pressed = false;

            var cut = _target.RenderComponent<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.OnLongPress, EventCallback.Factory.Create<LongPressEventArgs>(this, _ =>
                {
                    pressed = true;
                    return Task.CompletedTask;
                }));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            await cut.Find(".mud-nav-link").TriggerEventAsync("onlongpress", new LongPressEventArgs());

            pressed.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ContextMenuHandler_WHEN_ContextMenuRaised_THEN_ShouldInvokeCallback()
        {
            var invoked = false;

            var cut = _target.RenderComponent<CustomNavLink>(parameters =>
            {
                parameters.Add(p => p.OnContextMenu, EventCallback.Factory.Create<MouseEventArgs>(this, _ =>
                {
                    invoked = true;
                    return Task.CompletedTask;
                }));
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "ChildContent"));
            });

            await cut.Find(".mud-nav-link").TriggerEventAsync("oncontextmenu", new MouseEventArgs());

            invoked.Should().BeTrue();
        }

        public void Dispose()
        {
            _target.Dispose();
        }
    }
}
