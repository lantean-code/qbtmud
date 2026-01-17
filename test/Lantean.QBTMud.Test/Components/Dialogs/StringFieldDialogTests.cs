using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
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
    public sealed class StringFieldDialogTests : RazorComponentTestBase<StringFieldDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly StringFieldDialogTestDriver _target;

        public StringFieldDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new StringFieldDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_ValueChanged_WHEN_SaveClicked_THEN_ResultOkWithValue()
        {
            var dialog = await _target.RenderDialogAsync("Label", "Value");

            var input = FindComponentByTestId<MudTextField<string>>(dialog.Component, "StringFieldInput");
            await dialog.Component.InvokeAsync(() => input.Instance.ValueChanged.InvokeAsync("UpdatedValue"));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "StringFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be("UpdatedValue");
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync("Label", "Value");

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "StringFieldCancel");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_KeyboardSubmit_WHEN_EnterPressed_THEN_ResultOk()
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

            var dialog = await _target.RenderDialogAsync("Label", "Value");

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
        }
    }

    internal sealed class StringFieldDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public StringFieldDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<StringFieldDialogRenderContext> RenderDialogAsync(string? label, string? value)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(StringFieldDialog.Label), label },
                { nameof(StringFieldDialog.Value), value },
            };

            var reference = await dialogService.ShowAsync<StringFieldDialog>("String Field", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<StringFieldDialog>();

            return new StringFieldDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class StringFieldDialogRenderContext
    {
        public StringFieldDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<StringFieldDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<StringFieldDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
