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
    public sealed class ColumnOptionsDialogTests : RazorComponentTestBase<ColumnOptionsDialog<string>>
    {
        private readonly IKeyboardService _keyboardService;
        private readonly ColumnOptionsDialogTestDriver _target;

        public ColumnOptionsDialogTests()
        {
            _keyboardService = Mock.Of<IKeyboardService>(service =>
                service.Focus() == Task.CompletedTask
                && service.UnFocus() == Task.CompletedTask
                && service.RegisterKeypressEvent(It.IsAny<KeyboardEvent>(), It.IsAny<Func<KeyboardEvent, Task>>()) == Task.CompletedTask
                && service.UnregisterKeypressEvent(It.IsAny<KeyboardEvent>()) == Task.CompletedTask);

            TestContext.Services.RemoveAll<IKeyboardService>();
            TestContext.Services.AddSingleton(_keyboardService);

            _target = new ColumnOptionsDialogTestDriver(TestContext);
        }

        [Fact]
        public async Task GIVEN_OrderWithMissingColumn_WHEN_Rendered_THEN_RendersExistingColumnsOnly()
        {
            var columns = CreateColumns(
                ("Name", null, true),
                ("Age", null, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            var order = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "missing", 0 },
            };

            var dialog = await _target.RenderDialogAsync(columns, selected, order: order);

            FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "Column-name").Should().NotBeNull();
            FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "Column-age").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_OrderIncludesKnownColumn_WHEN_Saved_THEN_OrderUsesProvidedEntry()
        {
            var columns = CreateColumns(
                ("Name", null, true),
                ("Age", null, true),
                ("Size", null, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            var order = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "age", 0 },
            };

            var dialog = await _target.RenderDialogAsync(columns, selected, order: order);

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var (_, _, resultOrder) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result!.Data!;
            resultOrder["age"].Should().Be(0);
            resultOrder["name"].Should().Be(1);
            resultOrder["size"].Should().Be(2);
        }

        [Fact]
        public async Task GIVEN_SelectedColumnsEmpty_WHEN_Rendered_THEN_DefaultsToEnabledColumns()
        {
            var columns = CreateColumns(
                ("Name", null, true),
                ("Age", null, false));
            var selected = new HashSet<string>(StringComparer.Ordinal);

            var dialog = await _target.RenderDialogAsync(columns, selected);

            FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "Column-name").Instance.GetState(x => x.Value).Should().BeTrue();
            FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "Column-age").Instance.GetState(x => x.Value).Should().BeFalse();

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
            var showResult = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result.Data!;
            showResult.Item1.Should().ContainSingle(item => item == "name");
        }

        [Fact]
        public async Task GIVEN_SelectionToggled_WHEN_Saved_THEN_SelectedColumnsUpdated()
        {
            var columns = CreateColumns(
                ("Name", null, true),
                ("Age", null, true));
            var selected = new HashSet<string>(StringComparer.Ordinal)
            {
                "name",
            };

            var dialog = await _target.RenderDialogAsync(columns, selected);

            var ageCheckbox = FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "Column-age");
            ageCheckbox.Find("input").Change(true);

            var nameCheckbox = FindComponentByTestId<MudCheckBox<bool>>(dialog.Component, "Column-name");
            nameCheckbox.Find("input").Change(false);

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var (resultSelected, _, _) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result!.Data!;
            resultSelected.Should().ContainSingle(item => item == "age");
        }

        [Fact]
        public async Task GIVEN_DefaultWidthsAndOverrides_WHEN_Rendered_THEN_FieldsShowExpectedValues()
        {
            var columns = CreateColumns(
                ("Name", 50, true),
                ("Age", null, true),
                ("Size", 10, true),
                ("Type", null, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            var widths = new Dictionary<string, int?>(StringComparer.Ordinal)
            {
                { "age", 20 },
                { "size", 0 },
            };

            var dialog = await _target.RenderDialogAsync(columns, selected, widths: widths);

            FindComponentByTestId<MudTextField<string>>(dialog.Component, "Width-name").Instance.GetState(x => x.Value).Should().Be("50");
            FindComponentByTestId<MudTextField<string>>(dialog.Component, "Width-age").Instance.GetState(x => x.Value).Should().Be("20");
            FindComponentByTestId<MudTextField<string>>(dialog.Component, "Width-size").Instance.GetState(x => x.Value).Should().Be("auto");
            FindComponentByTestId<MudTextField<string>>(dialog.Component, "Width-type").Instance.GetState(x => x.Value).Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_WidthMatchesDefault_WHEN_Saved_THEN_WidthCleared()
        {
            var columns = CreateColumns(
                ("Name", 50, true),
                ("Age", null, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            var widths = new Dictionary<string, int?>(StringComparer.Ordinal)
            {
                { "name", 60 },
            };

            var dialog = await _target.RenderDialogAsync(columns, selected, widths: widths);

            var nameWidthField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "Width-name");
            await dialog.Component.InvokeAsync(() => nameWidthField.Instance.ValueChanged.InvokeAsync("50"));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var (_, resultWidths, _) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result!.Data!;
            resultWidths.Should().NotContainKey("name");
        }

        [Fact]
        public async Task GIVEN_DefaultWidthPresent_WHEN_SetToAuto_THEN_WidthStoredAsNull()
        {
            var columns = CreateColumns(
                ("Name", 50, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

            var dialog = await _target.RenderDialogAsync(columns, selected);

            var widthField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "Width-name");
            await dialog.Component.InvokeAsync(() => widthField.Instance.OnAdornmentClick.InvokeAsync(new MouseEventArgs()));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var (_, resultWidths, _) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result!.Data!;
            resultWidths.Should().ContainKey("name");
            resultWidths["name"].Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_ColumnRemoved_WHEN_WidthChanged_THEN_WidthCleared()
        {
            var columns = CreateColumns(
                ("Name", 50, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            var widths = new Dictionary<string, int?>(StringComparer.Ordinal)
            {
                { "name", 120 },
            };

            var dialog = await _target.RenderDialogAsync(columns, selected, widths: widths);

            columns.Clear();

            var widthField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "Width-name");
            await dialog.Component.InvokeAsync(() => widthField.Instance.ValueChanged.InvokeAsync("auto"));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var (_, resultWidths, _) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result!.Data!;
            resultWidths.Should().NotContainKey("name");
        }

        [Fact]
        public async Task GIVEN_WidthUpdated_WHEN_Saved_THEN_WidthStored()
        {
            var columns = CreateColumns(
                ("Name", 50, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

            var dialog = await _target.RenderDialogAsync(columns, selected);

            var widthField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "Width-name");
            await dialog.Component.InvokeAsync(() => widthField.Instance.ValueChanged.InvokeAsync("80"));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var (_, resultWidths, _) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result!.Data!;
            resultWidths.Should().ContainKey("name");
            resultWidths["name"].Should().Be(80);
        }

        [Fact]
        public async Task GIVEN_DefaultWidthNull_WHEN_SetToAuto_THEN_WidthRemoved()
        {
            var columns = CreateColumns(
                ("Name", null, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            var widths = new Dictionary<string, int?>(StringComparer.Ordinal)
            {
                { "name", 120 },
            };

            var dialog = await _target.RenderDialogAsync(columns, selected, widths: widths);

            var widthField = FindComponentByTestId<MudTextField<string>>(dialog.Component, "Width-name");
            await dialog.Component.InvokeAsync(() => widthField.Instance.ValueChanged.InvokeAsync("auto"));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var (_, resultWidths, _) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result!.Data!;
            resultWidths.Should().NotContainKey("name");
        }

        [Fact]
        public async Task GIVEN_MoveUpInvoked_WHEN_Saved_THEN_OrderUpdated()
        {
            var columns = CreateColumns(
                ("Name", null, true),
                ("Age", null, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

            var dialog = await _target.RenderDialogAsync(columns, selected);

            var upButton = FindComponentByTestId<MudIconButton>(dialog.Component, "Up-age");
            await dialog.Component.InvokeAsync(() => upButton.Instance.OnClick.InvokeAsync(null));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var (_, _, order) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result!.Data!;
            order["age"].Should().Be(0);
            order["name"].Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_OrderChangedWithButtons_WHEN_Saved_THEN_OrderPersisted()
        {
            var columns = CreateColumns(
                ("Name", null, true),
                ("Age", null, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

            var dialog = await _target.RenderDialogAsync(columns, selected);

            var downButton = FindComponentByTestId<MudIconButton>(dialog.Component, "Down-name");
            await dialog.Component.InvokeAsync(() => downButton.Instance.OnClick.InvokeAsync(null));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var (_, _, order) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result!.Data!;
            order["age"].Should().Be(0);
            order["name"].Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_CustomOrder_WHEN_ResetOrderInvokedAndSaved_THEN_DefaultOrderPersisted()
        {
            var columns = CreateColumns(
                ("Name", null, true),
                ("Age", null, true),
                ("Size", null, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            var order = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "age", 0 },
                { "size", 1 },
                { "name", 2 },
            };

            var dialog = await _target.RenderDialogAsync(columns, selected, order: order);

            var resetButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsResetOrder");
            await resetButton.Find("button").ClickAsync(new MouseEventArgs());

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var (_, _, resultOrder) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result!.Data!;
            resultOrder["name"].Should().Be(0);
            resultOrder["age"].Should().Be(1);
            resultOrder["size"].Should().Be(2);
        }

        [Fact]
        public async Task GIVEN_EdgeMovesInvoked_WHEN_Saved_THEN_OrderUnchanged()
        {
            var columns = CreateColumns(
                ("Name", null, true),
                ("Age", null, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

            var dialog = await _target.RenderDialogAsync(columns, selected);

            var upButton = FindComponentByTestId<MudIconButton>(dialog.Component, "Up-name");
            await dialog.Component.InvokeAsync(() => upButton.Instance.OnClick.InvokeAsync(null));

            var downButton = FindComponentByTestId<MudIconButton>(dialog.Component, "Down-age");
            await dialog.Component.InvokeAsync(() => downButton.Instance.OnClick.InvokeAsync(null));

            var saveButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsSave");
            await saveButton.Find("button").ClickAsync(new MouseEventArgs());

            var result = await dialog.Reference.Result;
            var (_, _, order) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result!.Data!;
            order["name"].Should().Be(0);
            order["age"].Should().Be(1);
        }

        [Fact]
        public async Task GIVEN_DialogOpen_WHEN_CancelInvoked_THEN_ResultCanceled()
        {
            var columns = CreateColumns(
                ("Name", null, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

            var dialog = await _target.RenderDialogAsync(columns, selected);

            var cancelButton = FindComponentByTestId<MudButton>(dialog.Component, "ColumnOptionsCancel");
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

            var columns = CreateColumns(
                ("Name", null, true));
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

            var dialog = await _target.RenderDialogAsync(columns, selected);

            dialog.Component.WaitForAssertion(() => submitHandler.Should().NotBeNull());

            await dialog.Component.InvokeAsync(() => submitHandler!(new KeyboardEvent("Enter")));

            var result = await dialog.Reference.Result;
            result!.Canceled.Should().BeFalse();
        }

        private static List<ColumnDefinition<string>> CreateColumns(params (string Header, int? Width, bool Enabled)[] definitions)
        {
            var columns = new List<ColumnDefinition<string>>();

            foreach (var definition in definitions)
            {
                var column = new ColumnDefinition<string>(definition.Header, value => value, width: definition.Width);
                column.Enabled = definition.Enabled;
                columns.Add(column);
            }

            return columns;
        }
    }

    internal sealed class ColumnOptionsDialogTestDriver
    {
        private readonly ComponentTestContext _testContext;

        public ColumnOptionsDialogTestDriver(ComponentTestContext testContext)
        {
            _testContext = testContext;
        }

        public async Task<ColumnOptionsDialogRenderContext> RenderDialogAsync(
            List<ColumnDefinition<string>> columns,
            HashSet<string> selectedColumns,
            Dictionary<string, int?>? widths = null,
            Dictionary<string, int>? order = null)
        {
            var provider = _testContext.Render<MudDialogProvider>();
            var dialogService = _testContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ColumnOptionsDialog<string>.Columns), columns },
                { nameof(ColumnOptionsDialog<string>.SelectedColumns), selectedColumns },
            };

            if (widths is not null)
            {
                parameters.Add(nameof(ColumnOptionsDialog<string>.Widths), widths);
            }

            if (order is not null)
            {
                parameters.Add(nameof(ColumnOptionsDialog<string>.Order), order);
            }

            var reference = await dialogService.ShowAsync<ColumnOptionsDialog<string>>("Column Options", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var component = provider.FindComponent<ColumnOptionsDialog<string>>();

            return new ColumnOptionsDialogRenderContext(provider, dialog, component, reference);
        }
    }

    internal sealed class ColumnOptionsDialogRenderContext
    {
        public ColumnOptionsDialogRenderContext(
            IRenderedComponent<MudDialogProvider> provider,
            IRenderedComponent<MudDialog> dialog,
            IRenderedComponent<ColumnOptionsDialog<string>> component,
            IDialogReference reference)
        {
            Provider = provider;
            Dialog = dialog;
            Component = component;
            Reference = reference;
        }

        public IRenderedComponent<MudDialogProvider> Provider { get; }

        public IRenderedComponent<MudDialog> Dialog { get; }

        public IRenderedComponent<ColumnOptionsDialog<string>> Component { get; }

        public IDialogReference Reference { get; }
    }
}
