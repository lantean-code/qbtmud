using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class SetLocationDialogTests : RazorComponentTestBase<SetLocationDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly SetLocationDialogTestDriver _target;

        public SetLocationDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new SetLocationDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_ValueChanged_WHEN_SaveClicked_THEN_ResultOkWithLocation()
        {
            var dialog = await _target.RenderDialogAsync("Location");

            var path = FindComponentByTestId<PathAutocomplete>(dialog.Component, "SetLocationPath");
            await dialog.Component.InvokeAsync(() => path.Instance.ValueChanged.InvokeAsync("UpdatedLocation"));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "SetLocationSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be("UpdatedLocation");
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync("Location");

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "SetLocationCancel");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_KeyboardSubmit_WHEN_EnterPressed_THEN_ResultOkWithLocation()
        {
            Func<KeyboardEvent, Task>? submitHandler = null;
            var keyboardMock = Mock.Get(_keyboardService);
            keyboardMock
                .Setup(service => service.RegisterKeypressEvent(It.Is<KeyboardEvent>(e => e.Key == "Enter" && !e.CtrlKey), It.IsAny<Func<KeyboardEvent, Task>>()))
                .Callback<KeyboardEvent, Func<KeyboardEvent, Task>>((_, handler) =>
                {
                    submitHandler = handler;
                })
                .Returns(Task.CompletedTask);

            var dialog = await _target.RenderDialogAsync("Location");

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be("Location");
        }
    }

    internal sealed class SetLocationDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public SetLocationDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<SetLocationDialogRenderContext> RenderDialogAsync(string? location)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(SetLocationDialog.Location), location },
            };

            var reference = await dialogService.ShowAsync<SetLocationDialog>("Set Location", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<SetLocationDialog>();

            return new SetLocationDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class SetLocationDialogRenderContext
    {
        public SetLocationDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<SetLocationDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<SetLocationDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
