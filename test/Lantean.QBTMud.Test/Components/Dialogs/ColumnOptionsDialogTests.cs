using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.Dialogs
{
    public sealed class ColumnOptionsDialogTests : RazorComponentTestBase<ColumnOptionsDialog<string>>
    {
        [Fact]
        public async Task GIVEN_OrderWithMissingColumn_WHEN_Rendered_THEN_RendersExistingColumnsOnly()
        {
            var columns = new List<ColumnDefinition<string>>
            {
                new ColumnDefinition<string>("Name", v => v),
                new ColumnDefinition<string>("Age", v => v),
            };
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            var order = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "missing", 0 },
            };

            var provider = TestContext.Render<MudDialogProvider>();
            var dialogService = TestContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ColumnOptionsDialog<string>.Columns), columns },
                { nameof(ColumnOptionsDialog<string>.SelectedColumns), selected },
                { nameof(ColumnOptionsDialog<string>.Order), order },
            };

            await dialogService.ShowAsync<ColumnOptionsDialog<string>>("Column Options", parameters);

            provider.WaitForState(() => provider.FindComponents<MudDialog>().Count == 1);

            var dialog = provider.FindComponent<MudDialog>();

            FindComponentByTestId<MudCheckBox<bool>>(dialog, "Column-name").Should().NotBeNull();
            FindComponentByTestId<MudCheckBox<bool>>(dialog, "Column-age").Should().NotBeNull();
        }

        [Fact]
        public async Task GIVEN_SelectedColumns_WHEN_Unchecked_THEN_ResultExcludesColumn()
        {
            var columns = new List<ColumnDefinition<string>>
            {
                new ColumnDefinition<string>("Name", v => v),
                new ColumnDefinition<string>("Age", v => v),
            };
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

            var provider = TestContext.Render<MudDialogProvider>();
            var dialogService = TestContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ColumnOptionsDialog<string>.Columns), columns },
                { nameof(ColumnOptionsDialog<string>.SelectedColumns), selected },
            };

            var reference = await dialogService.ShowAsync<ColumnOptionsDialog<string>>("Column Options", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var ageCheckbox = FindComponentByTestId<MudCheckBox<bool>>(dialog, "Column-age");
            await dialog.InvokeAsync(() => ageCheckbox.Instance.ValueChanged.InvokeAsync(false));

            var saveButton = dialog.FindAll("button").Single(b => b.TextContent.Trim() == "Save");
            await dialog.InvokeAsync(() => saveButton.Click());

            var result = await reference.Result;
            result.Should().NotBeNull();
            result!.Data.Should().NotBeNull();
            result.Canceled.Should().BeFalse();
            var (selectedColumns, widths, order) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result.Data!;
            selectedColumns.Should().Contain("name");
            selectedColumns.Should().NotContain("age");
            widths.Should().BeEmpty();
            order.Should().ContainKey("name");
        }

        [Fact]
        public async Task GIVEN_WidthChangedToAuto_WHEN_Saved_THEN_StoresNullWidth()
        {
            var columns = new List<ColumnDefinition<string>>
            {
                new ColumnDefinition<string>("Name", v => v, width: 50),
                new ColumnDefinition<string>("Age", v => v, width: 50),
            };
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

            var provider = TestContext.Render<MudDialogProvider>();
            var dialogService = TestContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ColumnOptionsDialog<string>.Columns), columns },
                { nameof(ColumnOptionsDialog<string>.SelectedColumns), selected },
            };

            var reference = await dialogService.ShowAsync<ColumnOptionsDialog<string>>("Column Options", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var ageWidthField = FindComponentByTestId<MudTextField<string>>(dialog, "Width-age");
            await dialog.InvokeAsync(() => ageWidthField.Instance.ValueChanged.InvokeAsync("auto"));

            var saveButton = dialog.FindAll("button").Single(b => b.TextContent.Trim() == "Save");
            await dialog.InvokeAsync(() => saveButton.Click());

            var result = await reference.Result;
            result.Should().NotBeNull();
            result!.Data.Should().NotBeNull();
            var (_, widths, _) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result.Data!;
            widths.Should().ContainKey("age");
            widths["age"].Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_OrderChangedWithButtons_WHEN_Saved_THEN_OrderPersisted()
        {
            var columns = new List<ColumnDefinition<string>>
            {
                new ColumnDefinition<string>("Name", v => v),
                new ColumnDefinition<string>("Age", v => v),
            };
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

            var provider = TestContext.Render<MudDialogProvider>();
            var dialogService = TestContext.Services.GetRequiredService<IDialogService>();

            var parameters = new DialogParameters
            {
                { nameof(ColumnOptionsDialog<string>.Columns), columns },
                { nameof(ColumnOptionsDialog<string>.SelectedColumns), selected },
            };

            var reference = await dialogService.ShowAsync<ColumnOptionsDialog<string>>("Column Options", parameters);

            var dialog = provider.FindComponent<MudDialog>();
            var downButton = FindComponentByTestId<MudIconButton>(dialog, "Down-name");
            await dialog.InvokeAsync(() => downButton.Instance.OnClick.InvokeAsync(null));

            var saveButton = dialog.FindAll("button").Single(b => b.TextContent.Trim() == "Save");
            await dialog.InvokeAsync(() => saveButton.Click());

            var result = await reference.Result;
            result.Should().NotBeNull();
            result!.Data.Should().NotBeNull();
            var (_, _, order) = ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))result.Data!;
            order["age"].Should().Be(0);
            order["name"].Should().Be(1);
        }
    }
}
