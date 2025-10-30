using AwesomeAssertions;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Test.Infrastructure;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MudBlazor;

namespace Lantean.QBTMud.Test.Components.UI
{
    public sealed class DynamicTableTests : IDisposable
    {
        private readonly ComponentTestContext _context;

        public DynamicTableTests()
        {
            _context = new ComponentTestContext();
        }

        [Fact]
        public void GIVEN_DefaultDefinitions_WHEN_Rendered_THEN_ShouldRenderColumnsAndRows()
        {
            var selectedColumns = new HashSet<string>();
            var sortColumn = string.Empty;
            var sortDirection = SortDirection.None;

            var localStorageMock = _context.AddSingletonMock<Blazored.LocalStorage.ILocalStorageService>(MockBehavior.Loose);

            var target = RenderDynamicTable(builder =>
            {
                builder.Add(p => p.ColumnDefinitions, CreateColumnDefinitions());
                builder.Add(p => p.Items, CreateItems());
                builder.Add(p => p.SelectedColumnsChanged, EventCallback.Factory.Create<HashSet<string>>(this, value => selectedColumns = value));
                builder.Add(p => p.SortColumnChanged, EventCallback.Factory.Create<string>(this, value => sortColumn = value));
                builder.Add(p => p.SortDirectionChanged, EventCallback.Factory.Create<SortDirection>(this, value => sortDirection = value));
            });

            target.WaitForAssertion(() =>
            {
                target.FindAll("th").Count.Should().Be(3);
            });

            target.FindAll("tbody tr").Count.Should().Be(2);

            selectedColumns.Should().Contain("id");
            selectedColumns.Should().Contain("name");
            selectedColumns.Should().Contain("value");
            sortColumn.Should().Be("id");
            sortDirection.Should().Be(SortDirection.Ascending);

            localStorageMock.Invocations.Count.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GIVEN_MultiSelectionRows_WHEN_Clicked_THEN_ShouldRespectModifierKeys()
        {
            var items = CreateItems();
            var selectedItemsChanged = new HashSet<SampleItem>();
            var target = RenderDynamicTable(builder =>
            {
                builder.Add(p => p.ColumnDefinitions, CreateColumnDefinitions());
                builder.Add(p => p.Items, items);
                builder.Add(p => p.MultiSelection, true);
                builder.Add(p => p.OnRowClick, EventCallback.Factory.Create<TableRowClickEventArgs<SampleItem>>(this, _ => Task.CompletedTask));
                builder.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<HashSet<SampleItem>>(this, value => selectedItemsChanged = value));
            });

            target.WaitForAssertion(() =>
            {
                target.Find("tbody tr");
            });

            var table = target.FindComponent<MudTable<SampleItem>>();
            var firstItem = items[0];
            var secondItem = items[1];

            var rows = target.FindComponents<MudTr>();

            await target.InvokeAsync(() => table.Instance.OnRowClick.InvokeAsync(new TableRowClickEventArgs<SampleItem>(new MouseEventArgs(), rows[0].Instance, firstItem)));
            target.Instance.SelectedItems.Should().Contain(firstItem);

            await target.InvokeAsync(() => table.Instance.OnRowClick.InvokeAsync(new TableRowClickEventArgs<SampleItem>(new MouseEventArgs { CtrlKey = true }, rows[1].Instance, secondItem)));
            target.Instance.SelectedItems.Should().Contain(firstItem);
            target.Instance.SelectedItems.Should().Contain(secondItem);

            await target.InvokeAsync(() => table.Instance.OnRowClick.InvokeAsync(new TableRowClickEventArgs<SampleItem>(new MouseEventArgs { AltKey = true }, rows[0].Instance, firstItem)));
            target.Instance.SelectedItems.Should().HaveCount(1);
            target.Instance.SelectedItems.Should().Contain(firstItem);

            await target.InvokeAsync(() => target.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<SampleItem> { firstItem }));
            selectedItemsChanged.Should().HaveCount(1);
            selectedItemsChanged.Should().Contain(firstItem);
        }

        [Fact]
        public async Task GIVEN_ContextActions_WHEN_Triggered_THEN_ShouldInvokeHandlers()
        {
            var contextInvoked = false;
            var longPressInvoked = false;

            var target = RenderDynamicTable(builder =>
            {
                builder.Add(p => p.ColumnDefinitions, CreateColumnDefinitions());
                builder.Add(p => p.Items, CreateItems());
                builder.Add(p => p.OnTableDataContextMenu, EventCallback.Factory.Create<TableDataContextMenuEventArgs<SampleItem>>(this, _ =>
                {
                    contextInvoked = true;
                    return Task.CompletedTask;
                }));
                builder.Add(p => p.OnTableDataLongPress, EventCallback.Factory.Create<TableDataLongPressEventArgs<SampleItem>>(this, _ =>
                {
                    longPressInvoked = true;
                    return Task.CompletedTask;
                }));
            });

            target.WaitForAssertion(() =>
            {
                target.Find("tbody td");
            });

            var cell = target.Find("tbody td");

            await cell.TriggerEventAsync("oncontextmenu", new MouseEventArgs());
            await cell.TriggerEventAsync("onlongpress", new LongPressEventArgs());

            contextInvoked.Should().BeTrue();
            longPressInvoked.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_ShowColumnOptionsDialog_WHEN_ResultReturned_THEN_ShouldPersistState()
        {
            var dialogServiceMock = new Mock<IDialogService>(MockBehavior.Strict);
            var dialogReferenceMock = new Mock<IDialogReference>(MockBehavior.Strict);
            var dialogData = (new HashSet<string> { "name" }, new Dictionary<string, int?> { { "name", 64 } }, new Dictionary<string, int> { { "name", 1 } });
            var dialogResult = DialogResult.Ok(dialogData);

            dialogReferenceMock.SetupGet(d => d.Result).Returns(Task.FromResult<DialogResult?>(dialogResult));
            dialogServiceMock
                .Setup(d => d.ShowAsync<ColumnOptionsDialog<SampleItem>>(
                    It.IsAny<string>(),
                    It.IsAny<DialogParameters>(),
                    It.IsAny<DialogOptions>()))
                .ReturnsAsync(dialogReferenceMock.Object);

            var localStorageMock = new Mock<Blazored.LocalStorage.ILocalStorageService>(MockBehavior.Loose);
            localStorageMock.Setup(s => s.GetItemAsync<HashSet<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(new ValueTask<HashSet<string>?>(result: null));
            localStorageMock.Setup(s => s.GetItemAsync<Dictionary<string, int?>>(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(new ValueTask<Dictionary<string, int?>?>(result: null));
            localStorageMock.Setup(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>())).Returns(new ValueTask());
            localStorageMock.Setup(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, int?>>(), It.IsAny<CancellationToken>())).Returns(new ValueTask());
            localStorageMock.Setup(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, int>>(), It.IsAny<CancellationToken>())).Returns(new ValueTask());

            _context.Services.RemoveAll(typeof(IDialogService));
            _context.Services.AddSingleton(dialogServiceMock.Object);
            _context.Services.RemoveAll(typeof(Blazored.LocalStorage.ILocalStorageService));
            _context.Services.AddSingleton(localStorageMock.Object);

            var target = RenderDynamicTable(builder =>
            {
                builder.Add(p => p.ColumnDefinitions, CreateColumnDefinitions());
                builder.Add(p => p.Items, CreateItems());
            });

            await target.InvokeAsync(() => target.Instance.ShowColumnOptionsDialog());

            dialogServiceMock.VerifyAll();
            localStorageMock.Verify(s => s.SetItemAsync(It.Is<string>(key => key.Contains("ColumnSelection")), It.Is<HashSet<string>>(value => value.Contains("name")), It.IsAny<CancellationToken>()), Times.Once);
            localStorageMock.Verify(s => s.SetItemAsync(It.Is<string>(key => key.Contains("ColumnWidths")), It.Is<Dictionary<string, int?>>(value => value["name"] == 64), It.IsAny<CancellationToken>()), Times.Once);
            localStorageMock.Verify(s => s.SetItemAsync(It.Is<string>(key => key.Contains("ColumnOrder")), It.Is<Dictionary<string, int>>(value => value["name"] == 1), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void GIVEN_ColumnFilterRemovesAll_WHEN_Rendered_THEN_ShouldReturnEmptyColumns()
        {
            var target = RenderDynamicTable(builder =>
            {
                builder.Add(p => p.ColumnDefinitions, CreateColumnDefinitions());
                builder.Add(p => p.Items, CreateItems());
                builder.Add(p => p.ColumnFilter, new Func<ColumnDefinition<SampleItem>, bool>(_ => false));
            });

            target.FindAll("th").Count.Should().Be(0);
        }

        private IRenderedComponent<DynamicTable<SampleItem>> RenderDynamicTable(Action<ComponentParameterCollectionBuilder<DynamicTable<SampleItem>>> configure)
        {
            return _context.RenderComponent<DynamicTable<SampleItem>>(configure);
        }

        private static IReadOnlyList<ColumnDefinition<SampleItem>> CreateColumnDefinitions()
        {
            var columns = new[]
            {
                new ColumnDefinition<SampleItem>("Id", item => item.Id) { Width = 80 },
                new ColumnDefinition<SampleItem>("Name", item => item.Name) { Class = "name-class", ClassFunc = item => item.Value > 5 ? "highlight" : null },
                new ColumnDefinition<SampleItem>("Value", item => item.Value) { IconOnly = false }
            };

            return columns;
        }

        private static IReadOnlyList<SampleItem> CreateItems()
        {
            return
            [
                new SampleItem(1, "Item1", 3),
                new SampleItem(2, "Item2", 7)
            ];
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private sealed record SampleItem(int Id, string Name, int Value);
    }
}