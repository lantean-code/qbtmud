using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.UI;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Presentation.Test.Components.UI
{
    public sealed class ContentPanelTests : RazorComponentTestBase<ContentPanel>
    {
        [Fact]
        public void GIVEN_DefaultParameters_WHEN_Rendered_THEN_RendersPanelBodyAndChildContent()
        {
            var target = TestContext.Render<ContentPanel>(parameters =>
            {
                parameters.Add(p => p.ChildContent, CreateChildContent("Body"));
            });

            var root = GetRootElement(target);

            root.ClassName.Should().Be("content-panel");
            root.Children.Should().ContainSingle();
            root.Children[0].ClassName.Should().Be("content-panel__body");
            root.TextContent.Trim().Should().Be("Body");
        }

        [Fact]
        public void GIVEN_AllLayoutParameters_WHEN_Rendered_THEN_CombinesClassesAndUsesContainer()
        {
            var target = TestContext.Render<ContentPanel>(parameters =>
            {
                parameters.Add(p => p.ToolbarContent, CreateChildContent("Toolbar"));
                parameters.Add(p => p.ChildContent, CreateChildContent("Body"));
                parameters.Add(p => p.ToolbarScroll, true);
                parameters.Add(p => p.UseContainer, true);
                parameters.Add(p => p.ContainerMaxWidth, MaxWidth.Large);
                parameters.Add(p => p.RootClass, "root-extra");
                parameters.Add(p => p.ToolbarClass, "toolbar-extra");
                parameters.Add(p => p.BodyClass, "body-extra");
                parameters.Add(p => p.ContainerClass, "container-extra");
                parameters.AddUnmatched("data-test-id", "ContentPanel");
            });

            var root = GetRootElement(target);
            var container = target.FindComponent<MudContainer>();

            root.GetAttribute("data-test-id").Should().Be("ContentPanel");
            root.ClassName.Should().Be("content-panel root-extra");
            root.Children.Should().HaveCount(2);
            root.Children[0].ClassName.Should().Be("content-panel__toolbar content-panel__toolbar--scroll toolbar-extra");
            root.Children[1].ClassName.Should().Be("content-panel__body body-extra");
            root.TextContent.Trim().Should().Be("ToolbarBody");
            container.Instance.MaxWidth.Should().Be(MaxWidth.Large);
            container.Instance.Class.Should().Be("content-panel__container container-extra");
        }

        [Fact]
        public void GIVEN_WhitespaceClasses_WHEN_Rendered_THEN_IgnoresWhitespaceClassValues()
        {
            var target = TestContext.Render<ContentPanel>(parameters =>
            {
                parameters.Add(p => p.ToolbarContent, CreateChildContent("Toolbar"));
                parameters.Add(p => p.UseContainer, true);
                parameters.Add(p => p.RootClass, " ");
                parameters.Add(p => p.ToolbarClass, string.Empty);
                parameters.Add(p => p.BodyClass, " ");
                parameters.Add(p => p.ContainerClass, string.Empty);
            });

            var root = GetRootElement(target);
            var container = target.FindComponent<MudContainer>();

            root.ClassName.Should().Be("content-panel");
            root.Children[0].ClassName.Should().Be("content-panel__toolbar");
            root.Children[1].ClassName.Should().Be("content-panel__body");
            container.Instance.Class.Should().Be("content-panel__container");
        }

        private static IElement GetRootElement(IRenderedComponent<ContentPanel> target)
        {
            return target.Nodes.OfType<IElement>().Single();
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
