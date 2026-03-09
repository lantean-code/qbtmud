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
        public async Task GIVEN_ShowInstallButtonTrue_WHEN_ButtonsClicked_THEN_ShouldInvokeInstallAndDismissCallbacks()
        {
            var installClicks = 0;
            var dismissClicks = 0;

            var target = TestContext.Render<PwaInstallPromptSnackbarContent>(parameters =>
            {
                parameters.Add(component => component.Message, "Message");
                parameters.Add(component => component.InstallLabel, "Install");
                parameters.Add(component => component.DismissLabel, "Don't show again");
                parameters.Add(component => component.ShowInstallButton, true);
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
            buttons.Should().HaveCount(2);

            await target.InvokeAsync(() => buttons[0].Instance.OnClick.InvokeAsync(new MouseEventArgs()));
            await target.InvokeAsync(() => buttons[1].Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            installClicks.Should().Be(1);
            dismissClicks.Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_ShowInstallButtonFalse_WHEN_DismissClicked_THEN_ShouldRenderOnlyDismissButtonAndInvokeDismissCallback()
        {
            var installClicks = 0;
            var dismissClicks = 0;

            var target = TestContext.Render<PwaInstallPromptSnackbarContent>(parameters =>
            {
                parameters.Add(component => component.Message, "Message");
                parameters.Add(component => component.InstallLabel, "Install");
                parameters.Add(component => component.DismissLabel, "Don't show again");
                parameters.Add(component => component.ShowInstallButton, false);
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
            buttons.Should().HaveCount(1);

            await target.InvokeAsync(() => buttons[0].Instance.OnClick.InvokeAsync(new MouseEventArgs()));

            installClicks.Should().Be(0);
            dismissClicks.Should().Be(1);
        }
    }
}
