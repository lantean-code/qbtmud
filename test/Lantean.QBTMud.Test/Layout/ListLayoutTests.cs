using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Layout;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Test.Layout
{
    public sealed class ListLayoutTests : RazorComponentTestBase<ListLayout>
    {
        private readonly IRenderedComponent<ListLayout> _target;
        private bool? _drawerCallbackValue;

        public ListLayoutTests()
        {
            _target = RenderLayout(
                drawerOpen: false,
                drawerOpenChanged: EventCallback.Factory.Create<bool>(this, value => _drawerCallbackValue = value),
                statusChanged: EventCallback.Factory.Create<Status>(this, _ => { }),
                categoryChanged: EventCallback.Factory.Create<string>(this, _ => { }),
                tagChanged: EventCallback.Factory.Create<string>(this, _ => { }),
                trackerChanged: EventCallback.Factory.Create<string>(this, _ => { }),
                searchTermChanged: EventCallback.Factory.Create<FilterSearchState>(this, _ => { }));
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
            var target = RenderLayout(
                drawerOpen: true,
                drawerOpenChanged: default,
                statusChanged: EventCallback.Factory.Create<Status>(this, _ => { }),
                categoryChanged: EventCallback.Factory.Create<string>(this, _ => { }),
                tagChanged: EventCallback.Factory.Create<string>(this, _ => { }),
                trackerChanged: EventCallback.Factory.Create<string>(this, _ => { }),
                searchTermChanged: EventCallback.Factory.Create<FilterSearchState>(this, _ => { }));
            var drawer = target.FindComponent<MudDrawer>();

            await target.InvokeAsync(() => drawer.Instance.OpenChanged.InvokeAsync(false));

            target.Instance.DrawerOpen.Should().BeFalse();
            _drawerCallbackValue.Should().BeNull();
        }

        [Fact]
        public void GIVEN_RenderedLayout_WHEN_InspectingChildren_THEN_RendersFiltersAndSearchCascade()
        {
            var filters = _target.FindComponent<FiltersNav>();
            var searchCascade = _target.FindComponents<CascadingValue<EventCallback<FilterSearchState>>>()
                .Single(component => string.Equals(component.Instance.Name, "SearchTermChanged", StringComparison.Ordinal));

            filters.Instance.StatusChanged.HasDelegate.Should().BeTrue();
            filters.Instance.CategoryChanged.HasDelegate.Should().BeTrue();
            filters.Instance.TagChanged.HasDelegate.Should().BeTrue();
            filters.Instance.TrackerChanged.HasDelegate.Should().BeTrue();
            searchCascade.Instance.Value.HasDelegate.Should().BeTrue();
        }

        private IRenderedComponent<ListLayout> RenderLayout(
            bool drawerOpen,
            EventCallback<bool> drawerOpenChanged,
            EventCallback<Status> statusChanged,
            EventCallback<string> categoryChanged,
            EventCallback<string> tagChanged,
            EventCallback<string> trackerChanged,
            EventCallback<FilterSearchState> searchTermChanged)
        {
            return TestContext.Render<ListLayout>(parameters =>
            {
                parameters.Add(p => p.Body, builder => { });
                parameters.AddCascadingValue("DrawerOpen", drawerOpen);
                parameters.AddCascadingValue("DrawerOpenChanged", drawerOpenChanged);
                parameters.AddCascadingValue("StatusChanged", statusChanged);
                parameters.AddCascadingValue("CategoryChanged", categoryChanged);
                parameters.AddCascadingValue("TagChanged", tagChanged);
                parameters.AddCascadingValue("TrackerChanged", trackerChanged);
                parameters.AddCascadingValue("SearchTermChanged", searchTermChanged);
            });
        }
    }
}
