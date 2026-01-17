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
    public sealed class NumericFieldDialogTests : RazorComponentTestBase<NumericFieldDialog<int>>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly NumericFieldDialogTestDriver _target;

        public NumericFieldDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new NumericFieldDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_ValueDisplayFuncProvided_WHEN_Rendered_THEN_DisplaysValue()
        {
            var dialog = await _target.RenderDialogAsync("Label", 5, value => "Display");

            dialog.Component.Markup.Should().Contain("Display");
        }

        [Fact]
        public async Task GIVEN_ValueDisplayFuncReturnsNull_WHEN_Rendered_THEN_UsesValueToString()
        {
            var dialog = await _target.RenderDialogAsync("Label", 7, _ => null!);

            dialog.Component.Markup.Should().Contain("7");
        }

        [Fact]
        public async Task GIVEN_ValueGetFuncProvided_WHEN_Changed_THEN_UsesValueGetFunc()
        {
            var dialog = await _target.RenderDialogAsync("Label", 1, null, value => 42);

            var input = FindComponentByTestId<MudNumericField<string>>(dialog.Component, "NumericFieldInput");
            input.Find("input").Change("99");

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "NumericFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(42);
        }

        [Fact]
        public async Task GIVEN_ValidValue_WHEN_Changed_THEN_UsesParsedValue()
        {
            var dialog = await _target.RenderDialogAsync("Label", 1);

            var input = FindComponentByTestId<MudNumericField<string>>(dialog.Component, "NumericFieldInput");
            input.Find("input").Change("12");

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "NumericFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(12);
        }

        [Fact]
        public async Task GIVEN_InvalidValue_WHEN_Changed_THEN_UsesMin()
        {
            var dialog = await _target.RenderDialogAsync("Label", 1, null, null, 5);

            var input = FindComponentByTestId<MudNumericField<string>>(dialog.Component, "NumericFieldInput");
            input.Find("input").Change("not-number");

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "NumericFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            result.Data.Should().Be(5);
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync("Label", 1);

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "NumericFieldCancel");
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

            var dialog = await _target.RenderDialogAsync("Label", 1);

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
        }
    }

    internal sealed class NumericFieldDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public NumericFieldDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<NumericFieldDialogRenderContext> RenderDialogAsync(
            string label,
            int value,
            Func<int, string>? valueDisplayFunc = null,
            Func<string, int>? valueGetFunc = null,
            int? min = null)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(NumericFieldDialog<int>.Label), label },
                { nameof(NumericFieldDialog<int>.Value), value },
            };

            if (valueDisplayFunc is not null)
            {
                parameters.Add(nameof(NumericFieldDialog<int>.ValueDisplayFunc), valueDisplayFunc);
            }

            if (valueGetFunc is not null)
            {
                parameters.Add(nameof(NumericFieldDialog<int>.ValueGetFunc), valueGetFunc);
            }

            if (min.HasValue)
            {
                parameters.Add(nameof(NumericFieldDialog<int>.Min), min.Value);
            }

            var reference = await dialogService.ShowAsync<NumericFieldDialog<int>>("Numeric Field", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<NumericFieldDialog<int>>();

            return new NumericFieldDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class NumericFieldDialogRenderContext
    {
        public NumericFieldDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<NumericFieldDialog<int>> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<NumericFieldDialog<int>> Component { get; }

        public IDialogReference Reference { get; }
    }
}
