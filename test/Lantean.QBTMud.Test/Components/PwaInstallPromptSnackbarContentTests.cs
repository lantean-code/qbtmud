using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components
{
    public sealed class PwaInstallPromptSnackbarContentTests : RazorComponentTestBase<PwaInstallPromptSnackbarContent>
    {
        [Fact]
        public async Task GIVEN_CanPromptInstallTrue_WHEN_ButtonsClicked_THEN_ShouldRenderLocalizedContentAndInvokeCallbacks()
        {
            var installClicks = 0;
            var dismissClicks = 0;

            var target = TestContext.Render<PwaInstallPromptSnackbarContent>(parameters =>
            {
                parameters.Add(component => component.CanPromptInstall, true);
                parameters.Add(component => component.ShowIosInstructions, false);
                parameters.Add(component => component.OnInstallClicked, EventCallback.Factory.Create<MouseEventArgs>(this, () =>
                {
                    installClicks++;
                }));
                parameters.Add(component => component.OnDismissClicked, EventCallback.Factory.Create<MouseEventArgs>(this, () =>
                {
                    dismissClicks++;
                }));
            });

            var buttons = target.FindComponents<MudButton>();
            var message = target.FindComponent<MudText>();
            buttons.Should().HaveCount(2);
            GetChildContentText(message.Instance.ChildContent).Should().Be("Install qBittorrent Web UI for quicker access and a native-like experience.");
            GetChildContentText(buttons[0].Instance.ChildContent).Should().Be("Install");
            GetChildContentText(buttons[1].Instance.ChildContent).Should().Be("Don't show again");

            await target.InvokeAsync(() => buttons[0].Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            await target.InvokeAsync(() => buttons[1].Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            installClicks.Should().Be(1);
            dismissClicks.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_IosInstructionsWithoutPrompt_WHEN_DismissClicked_THEN_ShouldRenderLocalizedInstructionsAndInvokeDismissCallback()
        {
            var installClicks = 0;
            var dismissClicks = 0;

            var target = TestContext.Render<PwaInstallPromptSnackbarContent>(parameters =>
            {
                parameters.Add(component => component.CanPromptInstall, false);
                parameters.Add(component => component.ShowIosInstructions, true);
                parameters.Add(component => component.OnInstallClicked, EventCallback.Factory.Create<MouseEventArgs>(this, () =>
                {
                    installClicks++;
                }));
                parameters.Add(component => component.OnDismissClicked, EventCallback.Factory.Create<MouseEventArgs>(this, () =>
                {
                    dismissClicks++;
                }));
            });

            var buttons = target.FindComponents<MudButton>();
            var message = target.FindComponent<MudText>();
            buttons.Should().HaveCount(1);
            GetChildContentText(message.Instance.ChildContent).Should().Be("Install qBittorrent Web UI from your browser menu for quicker access. On iPhone or iPad, tap Share, then Add to Home Screen.");
            GetChildContentText(buttons[0].Instance.ChildContent).Should().Be("Don't show again");

            await target.InvokeAsync(() => buttons[0].Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            installClicks.Should().Be(0);
            dismissClicks.Should().Be(1);
        }
    }
}
