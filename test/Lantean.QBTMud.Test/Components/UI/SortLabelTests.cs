using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class SortLabelTests : RazorComponentTestBase<SortLabel>
    {
        [Fact]
        public void GIVEN_DefaultSettings_WHEN_Rendered_THEN_ShouldDisplayLabelAndIcon()
        {
            var target = TestContext.Render<SortLabel>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "Label"));
            });

            var labelHost = target.Find("span");
            labelHost.ClassList.Should().Contain("mud-table-sort-label");
            labelHost.TextContent.Should().Contain("Label");

            var icon = target.FindComponent<MudIcon>();
            icon.Instance.Class.Should().Contain("mud-table-sort-label-icon");
        }

        [Fact]
        public void GIVEN_AppendIconTrue_WHEN_Rendered_THEN_ShouldDisplayIconAfterContent()
        {
            var target = TestContext.Render<SortLabel>(parameters =>
            {
                parameters.Add(p => p.AppendIcon, true);
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "Label"));
            });

            var labelHost = target.Find("span");
            labelHost.TextContent.Trim().Should().Be("Label");
            labelHost.FirstElementChild.Should().NotBeNull();
            labelHost.FirstElementChild!.ClassList.Should().Contain("mud-table-sort-label-icon");
        }

        [Fact]
        public async Task GIVEN_EnabledLabel_WHEN_Toggled_THEN_ShouldCycleThroughSortDirections()
        {
            var sortDirection = SortDirection.None;

            var target = TestContext.Render<SortLabel>(parameters =>
            {
                parameters.Add(p => p.AllowUnsorted, true);
                parameters.Add(p => p.SortDirection, sortDirection);
                parameters.Add(p => p.SortDirectionChanged, EventCallback.Factory.Create<SortDirection>(this, value => sortDirection = value));
            });

            await target.Find("span").TriggerEventAsync("onclick", new MouseEventArgs());
            sortDirection.Should().Be(SortDirection.Ascending);

            target.Render(parameterBuilder =>
            {
                parameterBuilder.Add(p => p.SortDirection, sortDirection);
            });

            await target.Find("span").TriggerEventAsync("onclick", new MouseEventArgs());
            sortDirection.Should().Be(SortDirection.Descending);

            target.Render(parameterBuilder =>
            {
                parameterBuilder.Add(p => p.SortDirection, sortDirection);
            });

            await target.Find("span").TriggerEventAsync("onclick", new MouseEventArgs());
            sortDirection.Should().Be(SortDirection.None);
        }

        [Fact]
        public async Task GIVEN_DisabledLabel_WHEN_Clicked_THEN_ShouldNotInvokeCallback()
        {
            var sortDirection = SortDirection.Ascending;

            var target = TestContext.Render<SortLabel>(parameters =>
            {
                parameters.Add(p => p.Enabled, false);
                parameters.Add(p => p.SortDirection, sortDirection);
                parameters.Add(p => p.SortDirectionChanged, EventCallback.Factory.Create<SortDirection>(this, value => sortDirection = value));
            });

            await target.Find("span").TriggerEventAsync("onclick", new MouseEventArgs());

            sortDirection.Should().Be(SortDirection.Ascending);
        }

        [Fact]
        public void GIVEN_SortDirectionAscending_WHEN_Rendered_THEN_ShouldUseAscendingIconClass()
        {
            var target = TestContext.Render<SortLabel>(parameters =>
            {
                parameters.Add(p => p.SortDirection, SortDirection.Ascending);
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "Label"));
            });

            var icon = target.FindComponent<MudIcon>();
            icon.Instance.Class.Should().Contain("mud-direction-asc");
        }

        [Fact]
        public void GIVEN_SortDirectionDescending_WHEN_Rendered_THEN_ShouldUseDescendingIconClass()
        {
            var target = TestContext.Render<SortLabel>(parameters =>
            {
                parameters.Add(p => p.SortDirection, SortDirection.Descending);
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "Label"));
            });

            var icon = target.FindComponent<MudIcon>();
            icon.Instance.Class.Should().Contain("mud-direction-desc");
        }

        [Fact]
        public async Task GIVEN_DescendingDirectionAndSortingRequired_WHEN_Toggled_THEN_ShouldWrapToAscending()
        {
            var sortDirection = SortDirection.Descending;

            var target = TestContext.Render<SortLabel>(parameters =>
            {
                parameters.Add(p => p.AllowUnsorted, false);
                parameters.Add(p => p.SortDirection, sortDirection);
                parameters.Add(p => p.SortDirectionChanged, EventCallback.Factory.Create<SortDirection>(this, value => sortDirection = value));
            });

            await target.Find("span").TriggerEventAsync("onclick", new MouseEventArgs());

            sortDirection.Should().Be(SortDirection.Ascending);
        }

        [Fact]
        public async Task GIVEN_InvalidSortDirection_WHEN_Toggled_THEN_ShouldFallbackToNone()
        {
            var callbackDirection = SortDirection.Ascending;

            var target = TestContext.Render<SortLabel>(parameters =>
            {
                parameters.Add(p => p.SortDirection, (SortDirection)999);
                parameters.Add(p => p.SortDirectionChanged, EventCallback.Factory.Create<SortDirection>(this, value => callbackDirection = value));
            });

            await target.Find("span").TriggerEventAsync("onclick", new MouseEventArgs());

            callbackDirection.Should().Be(SortDirection.None);
        }

        [Fact]
        public void GIVEN_InvalidSortDirection_WHEN_Rendered_THEN_ShouldUseDefaultIconClass()
        {
            var target = TestContext.Render<SortLabel>(parameters =>
            {
                parameters.Add(p => p.SortDirection, (SortDirection)123);
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "Label"));
            });

            var icon = target.FindComponent<MudIcon>();
            icon.Instance.Class.Should().Be("mud-table-sort-label-icon");
        }
    }
}
