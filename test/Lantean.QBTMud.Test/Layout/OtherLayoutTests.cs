using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Layout;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Test.Layout
{
    public sealed class OtherLayoutTests : RazorComponentTestBase<OtherLayout>
    {
        private readonly IRenderedComponent<OtherLayout> _target;
        private bool? _drawerCallbackValue;

        public OtherLayoutTests()
        {
            _target = RenderLayout(
                drawerOpen: false,
                drawerOpenChanged: EventCallback.Factory.Create<bool>(this, value => _drawerCallbackValue = value));
        }

        [Fact]
        public async Task GIVEN_DrawerOpenChangedDelegate_WHEN_DrawerOpenChangedInvoked_THEN_UpdatesStateAndInvokesDelegate()
        {
            var drawer = _target.FindComponent<MudDrawer>();

            await _target.InvokeAsync(() => drawer.Instance.OpenChanged.InvokeAsync(true));

            _target.Instance.DrawerOpen.Should().BeTrue();
            _drawerCallbackValue.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NoDrawerOpenChangedDelegate_WHEN_DrawerOpenChangedInvoked_THEN_UpdatesStateWithoutDelegate()
        {
            var target = RenderLayout(drawerOpen: true, drawerOpenChanged: default);
            var drawer = target.FindComponent<MudDrawer>();

            await target.InvokeAsync(() => drawer.Instance.OpenChanged.InvokeAsync(false));

            target.Instance.DrawerOpen.Should().BeFalse();
            _drawerCallbackValue.Should().BeNull();
        }

        [Fact]
        public void GIVEN_RenderedLayout_WHEN_InspectingChildren_THEN_RendersDrawerAndApplicationActions()
        {
            _target.FindComponent<MudDrawer>();
            var actions = _target.FindComponent<ApplicationActions>();

            _target.Instance.DrawerOpen.Should().BeFalse();
            actions.Instance.IsMenu.Should().BeFalse();
            actions.Instance.Preferences.Should().BeNull();
        }

        private IRenderedComponent<OtherLayout> RenderLayout(bool drawerOpen, EventCallback<bool> drawerOpenChanged)
        {
            return TestContext.Render<OtherLayout>(parameters =>
            {
                parameters.Add(p => p.Body, builder => { });
                parameters.AddCascadingValue("DrawerOpen", drawerOpen);
                parameters.AddCascadingValue("DrawerOpenChanged", drawerOpenChanged);
            });
        }
    }
}
