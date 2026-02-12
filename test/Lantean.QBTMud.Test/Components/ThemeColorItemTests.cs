using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class ThemeColorItemTests : RazorComponentTestBase<ThemeColorItem>
    {
        [Fact]
        public void GIVEN_EnabledItem_WHEN_Clicked_THEN_TogglesPopover()
        {
            var testId = TestIdHelper.For("ThemeColor")!;
            var target = TestContext.Render<ThemeColorItem>(parameters =>
            {
                parameters.Add(p => p.Name, "Name");
                parameters.Add(p => p.Color, new MudColor("#123456"));
                parameters.AddUnmatched("data-test-id", testId);
            });

            HasTestId(target, testId).Should().BeTrue();
            var popover = target.FindComponent<MudPopover>();

            popover.Instance.Open.Should().BeFalse();

            target.Find("div.theme-color-item__row").Click();

            popover.Instance.Open.Should().BeTrue();

            target.Find("div.theme-color-item__row").Click();

            popover.Instance.Open.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_DisabledItem_WHEN_Clicked_THEN_DoesNotOpen()
        {
            var testId = TestIdHelper.For("ThemeColor")!;
            var target = TestContext.Render<ThemeColorItem>(parameters =>
            {
                parameters.Add(p => p.Name, "Name");
                parameters.Add(p => p.Color, new MudColor("#123456"));
                parameters.Add(p => p.Disabled, true);
                parameters.AddUnmatched("data-test-id", testId);
            });

            HasTestId(target, testId).Should().BeTrue();
            var popover = target.FindComponent<MudPopover>();

            target.Find("div.theme-color-item__row").Click();

            popover.Instance.Open.Should().BeFalse();
        }

        [Fact]
        public void GIVEN_TransparentColor_WHEN_Rendered_THEN_UsesHexA()
        {
            var testId = TestIdHelper.For("ThemeColor")!;
            var target = TestContext.Render<ThemeColorItem>(parameters =>
            {
                parameters.Add(p => p.Name, "Name");
                parameters.Add(p => p.Color, new MudColor("#80FF0000"));
                parameters.AddUnmatched("data-test-id", testId);
            });

            HasTestId(target, testId).Should().BeTrue();
            var value = target
                .FindComponents<MudText>()
                .Single(text => text.Instance.Typo == Typo.caption);

            GetChildContentText(value.Instance.ChildContent).Should().Be("#80FF0000");
        }

        [Fact]
        public async Task GIVEN_ColorChangedCallback_WHEN_ColorPickerInvoked_THEN_RaisesEvent()
        {
            MudColor? received = null;
            var testId = TestIdHelper.For("ThemeColor")!;
            var popoverProvider = TestContext.Render<MudPopoverProvider>();
            var target = TestContext.Render<ThemeColorItem>(parameters =>
            {
                parameters.Add(p => p.Name, "Name");
                parameters.Add(p => p.Color, new MudColor("#123456"));
                parameters.Add(p => p.ColorChanged, EventCallback.Factory.Create<MudColor>(this, value => received = value));
                parameters.AddUnmatched("data-test-id", testId);
            });

            HasTestId(target, testId).Should().BeTrue();
            target.Find("div.theme-color-item__row").Click();
            var picker = popoverProvider.FindComponent<MudColorPicker>();
            await target.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(new MudColor("#654321")));

            received.Should().NotBeNull();
            received!.ToString().Should().Be(new MudColor("#654321").ToString());
        }
    }
}
