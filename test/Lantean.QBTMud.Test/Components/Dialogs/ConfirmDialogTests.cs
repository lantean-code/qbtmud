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
    public sealed class ConfirmDialogTests : RazorComponentTestBase<ConfirmDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly ConfirmDialogTestDriver _target;

        public ConfirmDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new ConfirmDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_ContentProvided_WHEN_Rendered_THEN_RendersDefaultButtons()
        {
            var dialog = await _target.RenderDialogAsync("Content");

            dialog.Component.Instance.Content.Should().NotBeNull();
            GetChildContentText(FindComponentByTestId<MudButton>(dialog.Component, "ConfirmCancel").Instance.ChildContent).Should().Be("Cancel");
            GetChildContentText(FindComponentByTestId<MudButton>(dialog.Component, "ConfirmOk").Instance.ChildContent).Should().Be("OK");
        }

        [Fact]
        public async Task GIVEN_CustomButtonText_WHEN_Rendered_THEN_RendersProvidedButtons()
        {
            var dialog = await _target.RenderDialogAsync("Content", "SuccessText", "CancelText");

            GetChildContentText(FindComponentByTestId<MudButton>(dialog.Component, "ConfirmOk").Instance.ChildContent).Should().Be("SuccessText");
            GetChildContentText(FindComponentByTestId<MudButton>(dialog.Component, "ConfirmCancel").Instance.ChildContent).Should().Be("CancelText");
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync("Content");

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "ConfirmCancel");
            await cancelButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_SubmitInvoked_THEN_ResultOk()
        {
            var dialog = await _target.RenderDialogAsync("Content");

            var okButton = FindComponentByTestId<MudButton>(dialog.Component, "ConfirmOk");
            await okButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(true);
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

            var dialog = await _target.RenderDialogAsync("Content");

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(true);
        }
    }

    internal sealed class ConfirmDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public ConfirmDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<ConfirmDialogRenderContext> RenderDialogAsync(
            string content,
            string? successText = null,
            string? cancelText = null)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ConfirmDialog.Content), content },
            };

            if (successText is not null)
            {
                parameters.Add(nameof(ConfirmDialog.SuccessText), successText);
            }

            if (cancelText is not null)
            {
                parameters.Add(nameof(ConfirmDialog.CancelText), cancelText);
            }

            var reference = await dialogService.ShowAsync<ConfirmDialog>("Confirm", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<ConfirmDialog>();

            return new ConfirmDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class ConfirmDialogRenderContext
    {
        public ConfirmDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<ConfirmDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<ConfirmDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
