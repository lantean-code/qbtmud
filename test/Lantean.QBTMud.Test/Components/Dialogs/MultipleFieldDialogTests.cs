using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class MultipleFieldDialogTests : RazorComponentTestBase<MultipleFieldDialog>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly MultipleFieldDialogTestDriver _target;

        public MultipleFieldDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new MultipleFieldDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_InitialValues_WHEN_Saved_THEN_ReturnsValues()
        {
            var values = new HashSet<string> { "Alpha" };
            var dialog = await _target.RenderDialogAsync("Label", values);

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "MultipleFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var resultValues = (HashSet<string>)result.Data!;
            resultValues.Should().ContainSingle(value => value == "Alpha");
        }

        [Fact]
        public async Task GIVEN_ValuesUpdated_WHEN_Saved_THEN_OriginalValuesRemain()
        {
            var dialog = await _target.RenderDialogAsync("Label", new HashSet<string> { "Alpha" });

            await dialog.Component.InvokeAsync(() => dialog.Component.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                { nameof(MultipleFieldDialog.Label), "Label" },
                { nameof(MultipleFieldDialog.Values), new HashSet<string> { "Beta" } },
            })));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "MultipleFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var resultValues = (HashSet<string>)result!.Data!;
            resultValues.Should().ContainSingle(value => value == "Alpha");
        }

        [Fact]
        public async Task GIVEN_EmptyValue_WHEN_AddInvoked_THEN_ValueNotAdded()
        {
            var dialog = await _target.RenderDialogAsync("Label", new HashSet<string>());

            var input = FindComponentByTestId<MudTextField<string>>(dialog.Component, "MultipleFieldInput");
            input.Find("input").Change(string.Empty);

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "MultipleFieldAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "MultipleFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var resultValues = (HashSet<string>)result!.Data!;
            resultValues.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ValueAddedAndRemoved_WHEN_Saved_THEN_ValueRemoved()
        {
            var dialog = await _target.RenderDialogAsync("Label", new HashSet<string>());

            var input = FindComponentByTestId<MudTextField<string>>(dialog.Component, "MultipleFieldInput");
            input.Find("input").Change("Value");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "MultipleFieldAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var deleteButton = FindComponentByTestId<MudIconButton>(dialog.Component, "MultipleFieldDelete-Value");
            await deleteButton.Find("button").ClickAsync(new MouseEventArgs());

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "MultipleFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var resultValues = (HashSet<string>)result!.Data!;
            resultValues.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_ValueAdded_WHEN_Saved_THEN_ReturnsNewValue()
        {
            var dialog = await _target.RenderDialogAsync("Label", new HashSet<string>());

            var input = FindComponentByTestId<MudTextField<string>>(dialog.Component, "MultipleFieldInput");
            input.Find("input").Change("Value");

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "MultipleFieldAdd");
            await addButton.Find("button").ClickAsync(new MouseEventArgs());

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "MultipleFieldSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var resultValues = (HashSet<string>)result!.Data!;
            resultValues.Should().ContainSingle(value => value == "Value");
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync("Label", new HashSet<string>());

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "MultipleFieldCancel");
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

            var dialog = await _target.RenderDialogAsync("Label", new HashSet<string>());

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
        }
    }

    internal sealed class MultipleFieldDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public MultipleFieldDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<MultipleFieldDialogRenderContext> RenderDialogAsync(string label, HashSet<string> values)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(MultipleFieldDialog.Label), label },
                { nameof(MultipleFieldDialog.Values), values },
            };

            var reference = await dialogService.ShowAsync<MultipleFieldDialog>("Multiple Field", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<MultipleFieldDialog>();

            return new MultipleFieldDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class MultipleFieldDialogRenderContext
    {
        public MultipleFieldDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<MultipleFieldDialog> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<MultipleFieldDialog> Component { get; }

        public IDialogReference Reference { get; }
    }
}
