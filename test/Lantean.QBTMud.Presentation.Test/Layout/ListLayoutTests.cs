using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Layout;
using Lantean.QBTMud.TestSupport.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Presentation.Test.Layout
{
    public sealed class ListLayoutTests : RazorComponentTestBase<ListLayout>
    {
        private bool? _drawerCallbackValue;

        [Fact]
        public async Task GIVEN_DrawerOpenChangedDelegate_WHEN_DrawerOpenChangedInvoked_THEN_UpdatesStateAndInvokesDelegate()
        {
            var target = RenderLayout(
                drawerOpen: false,
                drawerOpenChanged: EventCallback.Factory.Create<bool>(this, value => _drawerCallbackValue = value));
            var drawer = target.FindComponent<MudDrawer>();

            await target.InvokeAsync(() => drawer.Instance.OpenChanged.InvokeAsync(true));

            target.Instance.DrawerOpen.Should().BeTrue();
            _drawerCallbackValue.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_NoDrawerOpenChangedDelegate_WHEN_DrawerOpenChangedInvoked_THEN_UpdatesStateWithoutDelegate()
        {
            var target = RenderLayout(
                drawerOpen: true,
                drawerOpenChanged: default);
            var drawer = target.FindComponent<MudDrawer>();

            await target.InvokeAsync(() => drawer.Instance.OpenChanged.InvokeAsync(false));

            target.Instance.DrawerOpen.Should().BeFalse();
            _drawerCallbackValue.Should().BeNull();
        }

        [Fact]
        public void GIVEN_RenderedLayout_WHEN_InspectingChildren_THEN_RendersFiltersNavWithoutSearchCascade()
        {
            var target = RenderLayout(
                drawerOpen: false,
                drawerOpenChanged: EventCallback.Factory.Create<bool>(this, value => _drawerCallbackValue = value));
            var filters = target.FindComponent<FiltersNav>();

            filters.Should().NotBeNull();
        }

        private IRenderedComponent<ListLayout> RenderLayout(
            bool drawerOpen,
            EventCallback<bool> drawerOpenChanged)
        {
            return TestContext.Render<ListLayout>(parameters =>
            {
                parameters.Add(p => p.Body, builder => { });
                parameters.AddCascadingValue("DrawerOpen", drawerOpen);
                parameters.AddCascadingValue("DrawerOpenChanged", drawerOpenChanged);
            });
        }
    }
}
