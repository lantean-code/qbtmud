using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Filter;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;
using FilterOperator = Lantean.QBTMud.Filter.FilterOperator;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class FilterOptionsDialogTests : RazorComponentTestBase<FilterOptionsDialog<FilterItem>>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly FilterOptionsDialogTestDriver _target;

        public FilterOptionsDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new FilterOptionsDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_NoDefinitions_WHEN_Rendered_THEN_ShowsColumnPrompt()
        {
            var dialog = await _target.RenderDialogAsync(null);

            FindComponentByTestId<MudSelect<string>>(dialog.Component, "FilterNewColumn").Should().NotBeNull();
            FindComponentByTestId<MudSelect<string>>(dialog.Component, "FilterNewOperator").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_DefinitionUpdated_WHEN_Saved_THEN_DefinitionReflectsChanges()
        {
            var operators = FilterOperator.GetOperatorByDataType(typeof(string));
            var initialOperator = operators[0];
            var updatedOperator = operators[1];
            var ignoredOperator = operators[2];
            var definitions = new List<PropertyFilterDefinition<FilterItem>>
            {
                new("Name", initialOperator, "Value"),
            };

            var dialog = await _target.RenderDialogAsync(definitions);

            var operatorSelect = FindComponentByTestId<MudSelect<string>>(dialog.Component, "FilterOperator-Name");
            await dialog.Component.InvokeAsync(() => operatorSelect.Instance.ValueChanged.InvokeAsync(updatedOperator));
            await dialog.Component.InvokeAsync(() => operatorSelect.Instance.ValueChanged.InvokeAsync(ignoredOperator));

            var valueField = FindComponentByTestId<MudTextField<object>>(dialog.Component, "FilterValue-Name");
            await dialog.Component.InvokeAsync(() => valueField.Instance.ValueChanged.InvokeAsync("Updated"));
            await dialog.Component.InvokeAsync(() => valueField.Instance.ValueChanged.InvokeAsync("Ignored"));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "FilterSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var resultDefinitions = (List<PropertyFilterDefinition<FilterItem>>)result.Data!;
            resultDefinitions.Should().ContainSingle();
            resultDefinitions[0].Operator.Should().Be(initialOperator);
            resultDefinitions[0].Value.Should().Be("Value");
        }

        [Fact]
        public async Task GIVEN_DefinitionRemoved_WHEN_Saved_THEN_DefinitionListEmpty()
        {
            var initialOperator = FilterOperator.GetOperatorByDataType(typeof(string))[0];
            var definitions = new List<PropertyFilterDefinition<FilterItem>>
            {
                new("Name", initialOperator, "Value"),
            };

            var dialog = await _target.RenderDialogAsync(definitions);

            var removeButton = FindComponentByTestId<MudIconButton>(dialog.Component, "FilterRemove-Name");
            await dialog.Component.InvokeAsync(() => removeButton.Instance.OnClick.InvokeAsync(null));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "FilterSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var resultDefinitions = (List<PropertyFilterDefinition<FilterItem>>)result!.Data!;
            resultDefinitions.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_NewDefinitionEntered_WHEN_Added_THEN_DefinitionIncluded()
        {
            var dialog = await _target.RenderDialogAsync(null);
            var operatorValue = FilterOperator.GetOperatorByDataType(typeof(string))[0];

            var columnSelect = FindComponentByTestId<MudSelect<string>>(dialog.Component, "FilterNewColumn");
            await dialog.Component.InvokeAsync(() => columnSelect.Instance.ValueChanged.InvokeAsync("Name"));

            var operatorSelect = FindComponentByTestId<MudSelect<string>>(dialog.Component, "FilterNewOperator");
            await dialog.Component.InvokeAsync(() => operatorSelect.Instance.ValueChanged.InvokeAsync(operatorValue));

            var valueField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "FilterNewValue");
            await dialog.Component.InvokeAsync(() => valueField.Instance.ValueChanged.InvokeAsync("Value"));

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "FilterAdd");
            await dialog.Component.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync(null));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "FilterSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var resultDefinitions = (List<PropertyFilterDefinition<FilterItem>>)result!.Data!;
            resultDefinitions.Should().ContainSingle();
            resultDefinitions[0].Column.Should().Be("Name");
            resultDefinitions[0].Operator.Should().Be(operatorValue);
            resultDefinitions[0].Value.Should().Be("Value");
        }

        [Fact]
        public async Task GIVEN_ColumnOnly_WHEN_AddInvoked_THEN_NoDefinitionsAdded()
        {
            var dialog = await _target.RenderDialogAsync(null);

            var columnSelect = FindComponentByTestId<MudSelect<string>>(dialog.Component, "FilterNewColumn");
            await dialog.Component.InvokeAsync(() => columnSelect.Instance.ValueChanged.InvokeAsync("Name"));

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "FilterAdd");
            await dialog.Component.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync(null));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "FilterSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var resultDefinitions = (List<PropertyFilterDefinition<FilterItem>>)result!.Data!;
            resultDefinitions.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_OperatorOnly_WHEN_AddInvoked_THEN_NoDefinitionsAdded()
        {
            var dialog = await _target.RenderDialogAsync(null);
            var operatorValue = FilterOperator.GetOperatorByDataType(typeof(string))[0];

            var operatorSelect = FindComponentByTestId<MudSelect<string>>(dialog.Component, "FilterNewOperator");
            await dialog.Component.InvokeAsync(() => operatorSelect.Instance.ValueChanged.InvokeAsync(operatorValue));

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "FilterAdd");
            await dialog.Component.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync(null));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "FilterSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var resultDefinitions = (List<PropertyFilterDefinition<FilterItem>>)result!.Data!;
            resultDefinitions.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_DuplicateDefinition_WHEN_AddInvoked_THEN_DuplicateIgnored()
        {
            var initialOperator = FilterOperator.GetOperatorByDataType(typeof(string))[0];
            var definitions = new List<PropertyFilterDefinition<FilterItem>>
            {
                new("Name", initialOperator, "Value"),
            };

            var dialog = await _target.RenderDialogAsync(definitions);

            var columnSelect = FindComponentByTestId<MudSelect<string>>(dialog.Component, "FilterNewColumn");
            await dialog.Component.InvokeAsync(() => columnSelect.Instance.ValueChanged.InvokeAsync("Name"));

            var operatorSelect = FindComponentByTestId<MudSelect<string>>(dialog.Component, "FilterNewOperator");
            await dialog.Component.InvokeAsync(() => operatorSelect.Instance.ValueChanged.InvokeAsync(initialOperator));

            var addButton = FindComponentByTestId<MudIconButton>(dialog.Component, "FilterAdd");
            await dialog.Component.InvokeAsync(() => addButton.Instance.OnClick.InvokeAsync(null));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "FilterSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var resultDefinitions = (List<PropertyFilterDefinition<FilterItem>>)result!.Data!;
            resultDefinitions.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_NewDefinitionNotAdded_WHEN_Saved_THEN_DefinitionIncluded()
        {
            var dialog = await _target.RenderDialogAsync(null);
            var operatorValue = FilterOperator.GetOperatorByDataType(typeof(string))[0];

            var columnSelect = FindComponentByTestId<MudSelect<string>>(dialog.Component, "FilterNewColumn");
            await dialog.Component.InvokeAsync(() => columnSelect.Instance.ValueChanged.InvokeAsync("Name"));

            var operatorSelect = FindComponentByTestId<MudSelect<string>>(dialog.Component, "FilterNewOperator");
            await dialog.Component.InvokeAsync(() => operatorSelect.Instance.ValueChanged.InvokeAsync(operatorValue));

            var valueField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "FilterNewValue");
            await dialog.Component.InvokeAsync(() => valueField.Instance.ValueChanged.InvokeAsync("Value"));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "FilterSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var resultDefinitions = (List<PropertyFilterDefinition<FilterItem>>)result!.Data!;
            resultDefinitions.Should().ContainSingle();
            resultDefinitions[0].Column.Should().Be("Name");
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var dialog = await _target.RenderDialogAsync(null);

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "FilterCancel");
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

            var dialog = await _target.RenderDialogAsync(null);

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
        }
    }

    public sealed class FilterItem
    {
        public string Name { get; set; } = string.Empty;

        public int Size { get; set; }
    }

    internal sealed class FilterOptionsDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public FilterOptionsDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<FilterOptionsDialogRenderContext> RenderDialogAsync(List<PropertyFilterDefinition<FilterItem>>? definitions)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters();

            if (definitions is not null)
            {
                parameters.Add(nameof(FilterOptionsDialog<FilterItem>.FilterDefinitions), definitions);
            }

            var reference = await dialogService.ShowAsync<FilterOptionsDialog<FilterItem>>("Filter", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<FilterOptionsDialog<FilterItem>>();

            return new FilterOptionsDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class FilterOptionsDialogRenderContext
    {
        public FilterOptionsDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<FilterOptionsDialog<FilterItem>> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<FilterOptionsDialog<FilterItem>> Component { get; }

        public IDialogReference Reference { get; }
    }
}
