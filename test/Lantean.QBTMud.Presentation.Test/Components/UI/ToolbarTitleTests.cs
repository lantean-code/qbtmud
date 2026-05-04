using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Presentation.Test.Components.UI
{
    public sealed class ToolbarTitleTests : RazorComponentTestBase<ToolbarTitle>
    {
        [Fact]
        public void GIVEN_TitleWithoutCustomClass_WHEN_Rendered_THEN_RendersDefaultTitleClassAndContent()
        {
            var target = TestContext.Render<ToolbarTitle>(parameters =>
            {
                parameters.Add(p => p.ChildContent, CreateChildContent("Title"));
            });

            var text = target.FindComponent<MudText>();

            text.Instance.Class.Should().Be("toolbar-title");
            GetChildContentText(text.Instance.ChildContent).Should().Be("Title");
        }

        [Fact]
        public void GIVEN_TitleWithCustomClass_WHEN_Rendered_THEN_RendersCombinedTitleClassAndContent()
        {
            var target = TestContext.Render<ToolbarTitle>(parameters =>
            {
                parameters.Add(p => p.Class, "px-5");
                parameters.Add(p => p.ChildContent, CreateChildContent("CustomTitle"));
            });

            var text = target.FindComponent<MudText>();

            text.Instance.Class.Should().Be("toolbar-title px-5");
            GetChildContentText(text.Instance.ChildContent).Should().Be("CustomTitle");
        }

        private static RenderFragment CreateChildContent(string value)
        {
            return builder =>
            {
                builder.AddContent(0, value);
            };
        }
    }
}
