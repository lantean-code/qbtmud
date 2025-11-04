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
            var target = TestContext.RenderComponent<SortLabel>(parameters =>
            {
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "Label"));
            });

            var span = target.Find("span");
            span.ClassList.Should().Contain("mud-button-root");
            span.ClassList.Should().Contain("mud-table-sort-label");
            span.TextContent.Should().Contain("Label");

            var icon = target.Find(".mud-icon-root");
            icon.ClassList.Should().Contain("mud-table-sort-label-icon");
        }

        [Fact]
        public void GIVEN_AppendIconTrue_WHEN_Rendered_THEN_ShouldDisplayIconAfterContent()
        {
            var target = TestContext.RenderComponent<SortLabel>(parameters =>
            {
                parameters.Add(p => p.AppendIcon, true);
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "Label"));
            });

            target.Markup.Should().Contain("Label");
            var html = target.Markup;
            html.IndexOf("mud-table-sort-label-icon", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("Label", StringComparison.Ordinal));
        }

        [Fact]
        public async Task GIVEN_EnabledLabel_WHEN_Toggled_THEN_ShouldCycleThroughSortDirections()
        {
            var sortDirection = SortDirection.None;

            var target = TestContext.RenderComponent<SortLabel>(parameters =>
            {
                parameters.Add(p => p.AllowUnsorted, true);
                parameters.Add(p => p.SortDirection, sortDirection);
                parameters.Add(p => p.SortDirectionChanged, EventCallback.Factory.Create<SortDirection>(this, value => sortDirection = value));
            });

            await target.Find("span").TriggerEventAsync("onclick", new MouseEventArgs());
            sortDirection.Should().Be(SortDirection.Ascending);

            target.SetParametersAndRender(parameterBuilder =>
            {
                parameterBuilder.Add(p => p.SortDirection, sortDirection);
            });

            await target.Find("span").TriggerEventAsync("onclick", new MouseEventArgs());
            sortDirection.Should().Be(SortDirection.Descending);

            target.SetParametersAndRender(parameterBuilder =>
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

            var target = TestContext.RenderComponent<SortLabel>(parameters =>
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
            var target = TestContext.RenderComponent<SortLabel>(parameters =>
            {
                parameters.Add(p => p.SortDirection, SortDirection.Ascending);
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "Label"));
            });

            var icon = target.Find(".mud-icon-root");
            icon.ClassList.Should().Contain("mud-direction-asc");
        }

        [Fact]
        public void GIVEN_SortDirectionDescending_WHEN_Rendered_THEN_ShouldUseDescendingIconClass()
        {
            var target = TestContext.RenderComponent<SortLabel>(parameters =>
            {
                parameters.Add(p => p.SortDirection, SortDirection.Descending);
                parameters.Add(p => p.ChildContent, builder => builder.AddContent(0, "Label"));
            });

            var icon = target.Find(".mud-icon-root");
            icon.ClassList.Should().Contain("mud-direction-desc");
        }
    }
}
