using AwesomeAssertions;
using Blazored.LocalStorage;
using Bunit;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
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
    public sealed class DynamicTableTests : RazorComponentTestBase
    {
        [Fact]
        public void GIVEN_DefaultDefinitions_WHEN_Rendered_THEN_ShouldRenderColumnsAndRows()
        {
            var selectedColumns = new HashSet<string>();
            var sortColumn = string.Empty;
            var sortDirection = SortDirection.None;

            var localStorageMock = TestContext.AddSingletonMock<ILocalStorageService>(MockBehavior.Loose);

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
                target.FindComponents<MudTr>().Should().NotBeEmpty();
            });

            var rows = target.FindComponents<MudTr>().Select(r => r.Find("tr")).ToList();
            var firstItem = items[0];
            var secondItem = items[1];

            await target.InvokeAsync(() => rows[0].Click());
            target.Instance.SelectedItems.Should().Contain(firstItem);

            await target.InvokeAsync(() => rows[1].Click(new MouseEventArgs { CtrlKey = true }));
            target.Instance.SelectedItems.Should().Contain(firstItem);
            target.Instance.SelectedItems.Should().Contain(secondItem);

            await target.InvokeAsync(() => rows[0].Click(new MouseEventArgs { AltKey = true }));
            target.Instance.SelectedItems.Should().HaveCount(1);
            target.Instance.SelectedItems.Should().Contain(firstItem);

            await target.InvokeAsync(() => target.Instance.SelectedItemsChanged.InvokeAsync(new HashSet<SampleItem> { firstItem }));
            selectedItemsChanged.Should().HaveCount(1);
            selectedItemsChanged.Should().Contain(firstItem);
        }

        [Fact]
        public async Task GIVEN_SelectOnRowClickDisabled_WHEN_ItemNotSelected_THEN_NoSelectionChange()
        {
            var items = CreateItems();
            var selectedEvents = new List<HashSet<SampleItem>>();
            var target = RenderDynamicTable(builder =>
            {
                builder.Add(p => p.ColumnDefinitions, CreateColumnDefinitions());
                builder.Add(p => p.Items, items);
                builder.Add(p => p.SelectOnRowClick, false);
                builder.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<HashSet<SampleItem>>(this, value => selectedEvents.Add(new HashSet<SampleItem>(value))));
            });

            target.WaitForAssertion(() => target.FindComponents<MudTr>().Should().NotBeEmpty());

            var row = target.FindComponents<MudTr>().First().Find("tr");
            await target.InvokeAsync(() => row.Click());

            selectedEvents.Should().ContainSingle();
            selectedEvents.Single().Should().BeEmpty();
            target.Instance.SelectedItems.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVENTestContextActions_WHEN_Triggered_THEN_ShouldInvokeHandlers()
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
                target.FindComponent<TdExtended>();
            });

            var cell = target.FindComponent<TdExtended>();

            await cell.Find("td").TriggerEventAsync("oncontextmenu", new MouseEventArgs());
            await cell.Find("td").TriggerEventAsync("onlongpress", new LongPressEventArgs());

            contextInvoked.Should().BeTrue();
            longPressInvoked.Should().BeTrue();
        }

        [Fact]
        public async Task GIVEN_MultipleRows_WHEN_ContextOrLongPressTriggered_THEN_ShouldProvideCorrectCell()
        {
            var contextArgs = new List<TableDataContextMenuEventArgs<SampleItem>>();
            var longPressArgs = new List<TableDataLongPressEventArgs<SampleItem>>();

            var target = TestContext.Render<DynamicTable<SampleItem>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, new[] { new ColumnDefinition<SampleItem>("Name", item => item.Name) });
                parameters.Add(p => p.TableId, "CellLookup");
                parameters.Add(p => p.Items, CreateItems());
                parameters.Add(p => p.OnTableDataContextMenu, EventCallback.Factory.Create<TableDataContextMenuEventArgs<SampleItem>>(this, args =>
                {
                    contextArgs.Add(args);
                    return Task.CompletedTask;
                }));
                parameters.Add(p => p.OnTableDataLongPress, EventCallback.Factory.Create<TableDataLongPressEventArgs<SampleItem>>(this, args =>
                {
                    longPressArgs.Add(args);
                    return Task.CompletedTask;
                }));
            });

            target.WaitForAssertion(() =>
            {
                target.FindComponents<TdExtended>().Count.Should().Be(2);
            });

            var cells = target.FindComponents<TdExtended>();

            await cells[0].Find("td").TriggerEventAsync("oncontextmenu", new MouseEventArgs());
            await cells[1].Find("td").TriggerEventAsync("onlongpress", new LongPressEventArgs());

            contextArgs.Should().ContainSingle();
            contextArgs[0].Data.Should().BeSameAs(cells[0].Instance);

            longPressArgs.Should().ContainSingle();
            longPressArgs[0].Data.Should().BeSameAs(cells[1].Instance);
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

            var localStorageMock = new Mock<ILocalStorageService>(MockBehavior.Loose);
            localStorageMock.Setup(s => s.GetItemAsync<HashSet<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(new ValueTask<HashSet<string>?>(result: null));
            localStorageMock.Setup(s => s.GetItemAsync<Dictionary<string, int?>>(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(new ValueTask<Dictionary<string, int?>?>(result: null));
            localStorageMock.Setup(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>())).Returns(new ValueTask());
            localStorageMock.Setup(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, int?>>(), It.IsAny<CancellationToken>())).Returns(new ValueTask());
            localStorageMock.Setup(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, int>>(), It.IsAny<CancellationToken>())).Returns(new ValueTask());

            TestContext.Services.RemoveAll(typeof(IDialogService));
            TestContext.Services.AddSingleton(dialogServiceMock.Object);
            TestContext.Services.RemoveAll(typeof(ILocalStorageService));
            TestContext.Services.AddSingleton(localStorageMock.Object);

            var target = RenderDynamicTable(builder =>
            {
                builder.Add(p => p.ColumnDefinitions, CreateColumnDefinitions());
                builder.Add(p => p.Items, CreateItems());
            });

            await target.InvokeAsync(() => target.Instance.ShowColumnOptionsDialog());

            dialogServiceMock.VerifyAll();
            localStorageMock.Verify(s => s.SetItemAsync(It.Is<string>(key => key.Contains("ColumnSelection", StringComparison.Ordinal)), It.Is<HashSet<string>>(value => value.Contains("name")), It.IsAny<CancellationToken>()), Times.Once);
            localStorageMock.Verify(s => s.SetItemAsync(It.Is<string>(key => key.Contains("ColumnWidths", StringComparison.Ordinal)), It.Is<Dictionary<string, int?>>(value => value["name"] == 64), It.IsAny<CancellationToken>()), Times.Once);
            localStorageMock.Verify(s => s.SetItemAsync(It.Is<string>(key => key.Contains("ColumnOrder", StringComparison.Ordinal)), It.Is<Dictionary<string, int>>(value => value["name"] == 1), It.IsAny<CancellationToken>()), Times.Once);
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

        [Fact]
        public async Task GIVEN_EmptyPersistedSelection_WHEN_Rendered_THEN_NoSelectionChangeRaised()
        {
            var tableId = "NoSelectionChange";
            var selectionKey = $"DynamicTable{typeof(TestRow).Name}.ColumnSelection.{tableId}";
            await TestContext.LocalStorage.SetItemAsync(selectionKey, new HashSet<string>());

            var selectionEvents = new List<HashSet<string>>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumns());
                parameters.Add(p => p.TableId, tableId);
                parameters.Add(p => p.Items, new List<TestRow> { new TestRow { Name = "Only", Age = 1, Score = 1 } });
                parameters.Add(p => p.SelectedColumnsChanged, EventCallback.Factory.Create<HashSet<string>>(this, value => selectionEvents.Add(new HashSet<string>(value))));
            });

            target.FindComponents<MudTh>().Should().BeEmpty();
            selectionEvents.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_PersistedState_WHEN_Render_THEN_RestoresSelectionSortAndWidths()
        {
            var tableId = "Main";
            var columnSelectionKey = $"DynamicTable{typeof(TestRow).Name}.ColumnSelection.{tableId}";
            var columnSortKey = $"DynamicTable{typeof(TestRow).Name}.ColumnSort.{tableId}";
            var columnWidthsKey = $"DynamicTable{typeof(TestRow).Name}.ColumnWidths.{tableId}";
            var columnOrderKey = $"DynamicTable{typeof(TestRow).Name}.ColumnOrder.{tableId}";

            await TestContext.LocalStorage.SetItemAsync(columnSelectionKey, new HashSet<string>(new[] { "score", "age" }, StringComparer.Ordinal));
            await TestContext.LocalStorage.SetItemAsStringAsync(columnSortKey, "{\"SortColumn\":\"age\",\"SortDirection\":2}");
            await TestContext.LocalStorage.SetItemAsync(columnWidthsKey, new Dictionary<string, int?> { ["age"] = 120, ["score"] = 90 });
            await TestContext.LocalStorage.SetItemAsync(columnOrderKey, new Dictionary<string, int> { ["score"] = 0, ["age"] = 1 });

            var selectedColumnsChanges = new List<HashSet<string>>();
            var sortColumnChanges = new List<string?>();
            var sortDirectionChanges = new List<SortDirection>();
            var columns = CreateColumns();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, tableId);
                parameters.Add(p => p.Items, new List<TestRow>
                {
                    new TestRow { Name = "Name", Age = 20, Score = 50 },
                    new TestRow { Name = "Name", Age = 30, Score = 90 }
                });
                parameters.Add(p => p.SelectedColumnsChanged, EventCallback.Factory.Create<HashSet<string>>(this, value => selectedColumnsChanges.Add(new HashSet<string>(value, StringComparer.Ordinal))));
                parameters.Add(p => p.SortColumnChanged, EventCallback.Factory.Create<string>(this, value => sortColumnChanges.Add(value)));
                parameters.Add(p => p.SortDirectionChanged, EventCallback.Factory.Create<SortDirection>(this, value => sortDirectionChanges.Add(value)));
            });

            target.WaitForAssertion(() =>
            {
                selectedColumnsChanges.Should().ContainSingle();
                selectedColumnsChanges[0].Should().BeEquivalentTo(new[] { "score", "age" });
            });

            sortColumnChanges.Should().ContainSingle();
            sortColumnChanges[0].Should().Be("age");

            sortDirectionChanges.Should().ContainSingle();
            sortDirectionChanges[0].Should().Be(SortDirection.Descending);

            var ageSortLabel = FindSortLabel(target, "Age");
            ageSortLabel.Instance.SortDirection.Should().Be(SortDirection.Descending);

            columns.Single(c => c.Header == "Age").Width.Should().Be(120);
            columns.Single(c => c.Header == "Score").Width.Should().Be(90);
        }

        [Fact]
        public async Task GIVEN_PersistedColumnOrder_WHEN_Rendered_THEN_OrderRestored()
        {
            var tableId = "PersistedOrder";
            var columnOrderKey = $"DynamicTable{typeof(TestRow).Name}.ColumnOrder.{tableId}";
            var columnOrder = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "score", 0 },
                { "name", 1 },
                { "age", 2 }
            };

            await TestContext.LocalStorage.SetItemAsync(columnOrderKey, columnOrder);

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumns());
                parameters.Add(p => p.TableId, tableId);
                parameters.Add(p => p.Items, new List<TestRow>
                {
                    new TestRow { Name = "First", Age = 10, Score = 20 }
                });
            });

            target.WaitForAssertion(() =>
            {
                var headers = target.FindComponents<MudTh>().Select(component => component.Find("th").TextContent.Trim()).ToList();
                headers.Should().HaveCount(3);
                headers[0].Should().Be("Score");
                headers[1].Should().Be("Name");
                headers[2].Should().Be("Age");
            });
        }

        [Fact]
        public async Task GIVEN_SortableColumn_WHEN_HeaderClicked_THEN_PersistsSortAndRaisesEvents()
        {
            var tableId = "SortHeader";
            var columnSortKey = $"DynamicTable{typeof(TestRow).Name}.ColumnSort.{tableId}";

            var sortColumnEvents = new List<string?>();
            var sortDirectionEvents = new List<SortDirection>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumns());
                parameters.Add(p => p.TableId, tableId);
                parameters.Add(p => p.Items, new List<TestRow>
                {
                    new TestRow { Name = "First", Age = 25, Score = 10 },
                    new TestRow { Name = "Second", Age = 30, Score = 20 }
                });
                parameters.Add(p => p.SortColumnChanged, EventCallback.Factory.Create<string>(this, value => sortColumnEvents.Add(value)));
                parameters.Add(p => p.SortDirectionChanged, EventCallback.Factory.Create<SortDirection>(this, value => sortDirectionEvents.Add(value)));
            });

            var header = target.FindAll("span.mud-table-sort-label").Single(element => element.TextContent.Contains("Age", StringComparison.Ordinal));
            await target.InvokeAsync(() => header.Click());

            target.WaitForAssertion(() =>
            {
                sortColumnEvents.Should().NotBeEmpty();
                sortColumnEvents.Last().Should().Be("age");
                sortDirectionEvents.Should().NotBeEmpty();
                sortDirectionEvents.Last().Should().NotBe(SortDirection.None);
                TestContext.LocalStorage.Snapshot().Should().ContainKey(columnSortKey);
            });
        }

        [Fact]
        public async Task GIVEN_SortDirectionNone_WHEN_SortLabelInvoked_THEN_DoesNotPersist()
        {
            var tableId = "NoSortPersist";
            var sortColumnEvents = new List<string?>();
            var sortDirectionEvents = new List<SortDirection>();

            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Loose);
            dialogWorkflowMock.Setup(d => d.ShowColumnsOptionsDialog(It.IsAny<List<ColumnDefinition<TestRow>>>(), It.IsAny<HashSet<string>>(), It.IsAny<Dictionary<string, int?>>(), It.IsAny<Dictionary<string, int>>()))
                .ReturnsAsync(default((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>)));

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumns());
                parameters.Add(p => p.TableId, tableId);
                parameters.Add(p => p.Items, new List<TestRow> { new TestRow { Name = "Only", Age = 5, Score = 5 } });
                parameters.Add(p => p.SortColumnChanged, EventCallback.Factory.Create<string>(this, value => sortColumnEvents.Add(value)));
                parameters.Add(p => p.SortDirectionChanged, EventCallback.Factory.Create<SortDirection>(this, value => sortDirectionEvents.Add(value)));
            });

            var initialColumnEvents = sortColumnEvents.Count;
            var initialDirectionEvents = sortDirectionEvents.Count;

            var ageSortLabel = FindSortLabel(target, "Age");
            await target.InvokeAsync(() => ageSortLabel.Instance.SortDirectionChanged.InvokeAsync(SortDirection.None));

            sortColumnEvents.Count.Should().Be(initialColumnEvents);
            sortDirectionEvents.Count.Should().Be(initialDirectionEvents);
            var storedSort = await TestContext.LocalStorage.GetItemAsync<object>($"DynamicTable{typeof(TestRow).Name}.ColumnSort.{tableId}");
            storedSort.Should().BeNull();
        }

        [Fact]
        public async Task GIVEN_RepeatedSorts_WHEN_ClickDifferentHeaders_THEN_SortColumnUpdates()
        {
            var sortColumnEvents = new List<string?>();
            var sortDirectionEvents = new List<SortDirection>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumns());
                parameters.Add(p => p.TableId, "MultiSort");
                parameters.Add(p => p.Items, new List<TestRow>
                {
                    new TestRow { Name = "First", Age = 25, Score = 10 },
                    new TestRow { Name = "Second", Age = 30, Score = 20 }
                });
                parameters.Add(p => p.SortColumnChanged, EventCallback.Factory.Create<string>(this, value => sortColumnEvents.Add(value)));
                parameters.Add(p => p.SortDirectionChanged, EventCallback.Factory.Create<SortDirection>(this, value => sortDirectionEvents.Add(value)));
            });

            var ageHeader = target.FindAll("span.mud-table-sort-label").Single(element => element.TextContent.Contains("Age", StringComparison.Ordinal));
            await target.InvokeAsync(() => ageHeader.Click());

            var scoreHeader = target.FindAll("span.mud-table-sort-label").Single(element => element.TextContent.Contains("Score", StringComparison.Ordinal));
            await target.InvokeAsync(() => scoreHeader.Click());

            sortColumnEvents.Should().ContainInOrder("age", "score");
            sortDirectionEvents.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GIVEN_RepeatedSorts_WHEN_ClickSameHeaderTwice_THEN_SortDirectionChangesOncePerClick()
        {
            var sortDirectionEvents = new List<SortDirection>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumns());
                parameters.Add(p => p.TableId, "ToggleSort");
                parameters.Add(p => p.Items, new List<TestRow>
                {
                    new TestRow { Name = "First", Age = 25, Score = 10 },
                    new TestRow { Name = "Second", Age = 30, Score = 20 }
                });
                parameters.Add(p => p.SortDirectionChanged, EventCallback.Factory.Create<SortDirection>(this, value => sortDirectionEvents.Add(value)));
            });

            var ageHeader = target.FindAll("span.mud-table-sort-label").Single(element => element.TextContent.Contains("Age", StringComparison.Ordinal));
            await target.InvokeAsync(() => ageHeader.Click());
            await target.InvokeAsync(() => ageHeader.Click());

            sortDirectionEvents.Should().HaveCount(2);
            sortDirectionEvents[0].Should().NotBe(SortDirection.None);
            sortDirectionEvents[1].Should().NotBe(SortDirection.None);
        }

        [Fact]
        public async Task GIVEN_ShowColumnsDialogReturnsDefault_WHEN_Invoked_THEN_NoStateChanges()
        {
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            dialogWorkflowMock
                .Setup(d => d.ShowColumnsOptionsDialog(It.IsAny<List<ColumnDefinition<TestRow>>>(), It.IsAny<HashSet<string>>(), It.IsAny<Dictionary<string, int?>>(), It.IsAny<Dictionary<string, int>>()))
                .ReturnsAsync(default((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>)));

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumns());
                parameters.Add(p => p.TableId, "DialogDefault");
                parameters.Add(p => p.Items, new List<TestRow> { new TestRow { Name = "Row", Age = 1, Score = 1 } });
            });

            var before = TestContext.LocalStorage.Snapshot();
            await target.InvokeAsync(() => target.Instance.ShowColumnOptionsDialog());
            var after = TestContext.LocalStorage.Snapshot();

            after.Should().BeEquivalentTo(before);
            dialogWorkflowMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_ShowColumnsDialogWidthChangedOnly_WHEN_Invoked_THEN_WidthPersisted()
        {
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var columns = CreateColumns();
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            var widths = new Dictionary<string, int?>(StringComparer.Ordinal);
            var order = new Dictionary<string, int>(StringComparer.Ordinal);
            var changedWidths = new Dictionary<string, int?>(widths, StringComparer.Ordinal)
            {
                [columns[0].Id] = 200
            };

            dialogWorkflowMock
                .Setup(d => d.ShowColumnsOptionsDialog(It.IsAny<List<ColumnDefinition<TestRow>>>(), It.IsAny<HashSet<string>>(), It.IsAny<Dictionary<string, int?>>(), It.IsAny<Dictionary<string, int>>()))
                .ReturnsAsync((selected, changedWidths, order));

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "WidthOnly");
                parameters.Add(p => p.Items, new List<TestRow> { new TestRow { Name = "Row", Age = 1, Score = 1 } });
            });

            await target.InvokeAsync(() => target.Instance.ShowColumnOptionsDialog());

            var snapshot = TestContext.LocalStorage.Snapshot();
            var widthsKey = snapshot.Keys.Single(k => k.Contains("ColumnWidths", StringComparison.Ordinal));
            var storedWidths = await TestContext.LocalStorage.GetItemAsync<Dictionary<string, int?>>(widthsKey);
            storedWidths.Should().BeEquivalentTo(changedWidths);
            dialogWorkflowMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_ShowColumnsDialogUnchanged_WHEN_Invoked_THEN_NoPersistence()
        {
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var localStorageMock = TestContext.AddSingletonMock<ILocalStorageService>(MockBehavior.Loose);
            localStorageMock.Setup(s => s.GetItemAsync<HashSet<string>>(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(new ValueTask<HashSet<string>?>(result: null));
            localStorageMock.Setup(s => s.GetItemAsync<Dictionary<string, int?>>(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(new ValueTask<Dictionary<string, int?>?>(result: null));

            dialogWorkflowMock
                .Setup(d => d.ShowColumnsOptionsDialog(It.IsAny<List<ColumnDefinition<TestRow>>>(), It.IsAny<HashSet<string>>(), It.IsAny<Dictionary<string, int?>>(), It.IsAny<Dictionary<string, int>>()))
                .ReturnsAsync((List<ColumnDefinition<TestRow>> cols, HashSet<string> selected, Dictionary<string, int?> widths, Dictionary<string, int> order) => (selected, widths, order));

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumns());
                parameters.Add(p => p.TableId, "Unchanged");
                parameters.Add(p => p.Items, new List<TestRow> { new TestRow { Name = "Row", Age = 1, Score = 1 } });
            });

            await target.InvokeAsync(() => target.Instance.ShowColumnOptionsDialog());

            localStorageMock.Verify(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<HashSet<string>>(), It.IsAny<CancellationToken>()), Times.Never);
            localStorageMock.Verify(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, int?>>(), It.IsAny<CancellationToken>()), Times.Never);
            localStorageMock.Verify(s => s.SetItemAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, int>>(), It.IsAny<CancellationToken>()), Times.Never);
            dialogWorkflowMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_MultiSelection_WHEN_RowClicksWithModifiers_THEN_UpdatesSelectedItems()
        {
            var columns = CreateColumns();
            var items = new List<TestRow>
            {
                new TestRow { Name = "Name", Age = 10, Score = 1 },
                new TestRow { Name = "Name", Age = 20, Score = 2 }
            };
            var selections = new List<HashSet<TestRow>>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "Selection");
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.MultiSelection, true);
                parameters.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<HashSet<TestRow>>(this, value => selections.Add(new HashSet<TestRow>(value))));
            });

            var rows = target.FindAll("tbody tr");
            rows.Count.Should().Be(2);

            await target.InvokeAsync(() => rows[0].Click());
            target.WaitForAssertion(() =>
            {
                selections.Should().NotBeEmpty();
                selections.Last().Should().ContainSingle().And.Contain(items[0]);
            });

            await target.InvokeAsync(() => rows[1].Click(new MouseEventArgs { CtrlKey = true }));
            target.WaitForAssertion(() =>
            {
                selections.Last().Should().HaveCount(2);
                selections.Last().Should().Contain(items[0]);
                selections.Last().Should().Contain(items[1]);
            });

            await target.InvokeAsync(() => rows[0].Click(new MouseEventArgs { CtrlKey = true }));
            target.WaitForAssertion(() =>
            {
                selections.Last().Should().ContainSingle().And.Contain(items[1]);
            });

            await target.InvokeAsync(() => rows[0].Click(new MouseEventArgs { AltKey = true }));
            target.WaitForAssertion(() =>
            {
                selections.Last().Should().ContainSingle().And.Contain(items[0]);
            });
        }

        [Fact]
        public async Task GIVEN_SuppressClickWindow_WHEN_RowClickedWithinWindow_THEN_Ignored()
        {
            var columns = CreateColumns();
            var items = new List<TestRow> { new TestRow { Name = "N", Age = 1, Score = 1 } };
            var rowClicks = new List<TableRowClickEventArgs<TestRow>>();
            var selectedEvents = new List<HashSet<TestRow>>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "Suppress");
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.MultiSelection, false);
                parameters.Add(p => p.SelectOnRowClick, true);
                parameters.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<HashSet<TestRow>>(this, value => selectedEvents.Add(new HashSet<TestRow>(value))));
                parameters.Add(p => p.OnRowClick, EventCallback.Factory.Create<TableRowClickEventArgs<TestRow>>(this, args => rowClicks.Add(args)));
            });

            var row = target.FindComponents<MudTr>().First().Find("tr");
            var cell = target.FindComponent<TdExtended>();

            await target.InvokeAsync(() => cell.Find("td").TriggerEventAsync("onlongpress", new LongPressEventArgs()));
            await target.InvokeAsync(() => row.Click());

            rowClicks.Should().BeEmpty();
            selectedEvents.Should().BeEmpty();
            target.Instance.SelectedItems.Should().BeEmpty();
        }

        [Fact]
        public async Task GIVEN_SelectOnRowClickEnabled_WHEN_ItemPreviouslySelected_THEN_SelectionNotDuplicated()
        {
            var columns = CreateColumns();
            var item = new TestRow { Name = "One", Age = 1, Score = 1 };
            var items = new List<TestRow> { item };
            var selectedItemsEvents = new List<HashSet<TestRow>>();
            var selectedItemEvents = new List<TestRow>();
            var rowClicks = new List<TableRowClickEventArgs<TestRow>>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "SingleSelect");
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.MultiSelection, false);
                parameters.Add(p => p.SelectOnRowClick, true);
                parameters.Add(p => p.SelectedItems, new HashSet<TestRow>(new[] { item }));
                parameters.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<HashSet<TestRow>>(this, value => selectedItemsEvents.Add(new HashSet<TestRow>(value))));
                parameters.Add(p => p.SelectedItemChanged, EventCallback.Factory.Create<TestRow>(this, value => selectedItemEvents.Add(value)));
                parameters.Add(p => p.OnRowClick, EventCallback.Factory.Create<TableRowClickEventArgs<TestRow>>(this, args => rowClicks.Add(args)));
            });

            var row = target.FindComponents<MudTr>().First().Find("tr");
            await target.InvokeAsync(() => row.Click());

            selectedItemsEvents.Should().NotBeEmpty();
            selectedItemsEvents.Last().Should().ContainSingle().And.Contain(item);
            selectedItemEvents.Should().BeEmpty();
            rowClicks.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_SelectOnRowClickEnabled_WHEN_ItemNotSelected_THEN_SelectedItemChangedRaised()
        {
            var columns = CreateColumns();
            var item = new TestRow { Name = "Two", Age = 2, Score = 2 };
            var items = new List<TestRow> { item };
            var selectedItemsEvents = new List<HashSet<TestRow>>();
            var selectedItemEvents = new List<TestRow>();
            var rowClicks = new List<TableRowClickEventArgs<TestRow>>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "SingleSelectNew");
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.MultiSelection, false);
                parameters.Add(p => p.SelectOnRowClick, true);
                parameters.Add(p => p.SelectedItems, new HashSet<TestRow>());
                parameters.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<HashSet<TestRow>>(this, value => selectedItemsEvents.Add(new HashSet<TestRow>(value))));
                parameters.Add(p => p.SelectedItemChanged, EventCallback.Factory.Create<TestRow>(this, value => selectedItemEvents.Add(value)));
                parameters.Add(p => p.OnRowClick, EventCallback.Factory.Create<TableRowClickEventArgs<TestRow>>(this, args => rowClicks.Add(args)));
            });

            var row = target.FindComponents<MudTr>().First().Find("tr");
            await target.InvokeAsync(() => row.Click());

            selectedItemsEvents.Should().NotBeEmpty();
            selectedItemsEvents.Last().Should().ContainSingle().And.Contain(item);
            selectedItemEvents.Should().ContainSingle().And.Contain(item);
            rowClicks.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_SelectOnRowClickDisabled_WHEN_ItemAlreadySelected_THEN_NoChange()
        {
            var columns = CreateColumns();
            var item = new TestRow { Name = "Persist", Age = 2, Score = 2 };
            var selectedEvents = new List<HashSet<TestRow>>();
            var selectionEvents = new List<HashSet<TestRow>>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "NoSelectChange");
                parameters.Add(p => p.Items, new List<TestRow> { item });
                parameters.Add(p => p.MultiSelection, true);
                parameters.Add(p => p.SelectOnRowClick, false);
                parameters.Add(p => p.SelectedItems, new HashSet<TestRow>(new[] { item }));
                parameters.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<HashSet<TestRow>>(this, value =>
                {
                    selectedEvents.Add(new HashSet<TestRow>(value));
                    selectionEvents.Add(new HashSet<TestRow>(value));
                }));
            });

            var row = target.FindComponents<MudTr>().First().Find("tr");
            await target.InvokeAsync(() => row.Click());

            selectedEvents.Should().NotBeEmpty();
            selectedEvents.Last().Should().ContainSingle().And.Contain(item);
            target.Instance.SelectedItems.Should().ContainSingle().And.Contain(item);
        }

        [Fact]
        public async Task GIVEN_MultiSelectionAndItemSelected_WHEN_ClickWithoutModifiers_THEN_SelectionUnchanged()
        {
            var columns = CreateColumns();
            var item = new TestRow { Name = "KeepSelected", Age = 3, Score = 3 };
            var items = new List<TestRow> { item };
            var selectionEvents = new List<HashSet<TestRow>>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "KeepSelected");
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.MultiSelection, true);
                parameters.Add(p => p.SelectedItems, new HashSet<TestRow>(new[] { item }));
                parameters.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<HashSet<TestRow>>(this, value => selectionEvents.Add(new HashSet<TestRow>(value))));
            });

            var row = target.FindComponents<MudTr>().First().Find("tr");
            await target.InvokeAsync(() => row.Click());

            selectionEvents.Should().NotBeEmpty();
            selectionEvents.Last().Should().ContainSingle().And.Contain(item);
            target.Instance.SelectedItems.Should().ContainSingle().And.Contain(item);
        }

        [Fact]
        public async Task GIVEN_MultiSelectionAndNewItem_WHEN_ClickWithoutModifiers_THEN_ReplacesSelection()
        {
            var columns = CreateColumns();
            var first = new TestRow { Name = "First", Age = 1, Score = 1 };
            var second = new TestRow { Name = "Second", Age = 2, Score = 2 };
            var selectedEvents = new List<HashSet<TestRow>>();
            var rowClicks = new List<TableRowClickEventArgs<TestRow>>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "ReplaceSelection");
                parameters.Add(p => p.Items, new List<TestRow> { first, second });
                parameters.Add(p => p.MultiSelection, true);
                parameters.Add(p => p.SelectedItems, new HashSet<TestRow>(new[] { first }));
                parameters.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<HashSet<TestRow>>(this, value => selectedEvents.Add(new HashSet<TestRow>(value))));
                parameters.Add(p => p.OnRowClick, EventCallback.Factory.Create<TableRowClickEventArgs<TestRow>>(this, args => rowClicks.Add(args)));
            });

            var rows = target.FindComponents<MudTr>().Select(r => r.Find("tr")).ToList();
            await target.InvokeAsync(() => rows[1].Click());

            selectedEvents.Should().NotBeEmpty();
            selectedEvents.Last().Should().ContainSingle().And.Contain(second);
            target.Instance.SelectedItems.Should().ContainSingle().And.Contain(second);
            rowClicks.Should().ContainSingle();
        }

        [Fact]
        public async Task GIVEN_DialogOrderChangedOnly_WHEN_ShowColumnsOptionsDialog_THEN_OrderPersisted()
        {
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var columns = CreateColumns();
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            var widths = columns.ToDictionary(c => c.Id, _ => (int?)null, StringComparer.Ordinal);
            var originalOrder = columns.Select((c, i) => (c.Id, i)).ToDictionary(t => t.Id, t => t.i, StringComparer.Ordinal);
            var changedOrder = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { columns[1].Id, 0 },
                { columns[0].Id, 1 },
                { columns[2].Id, 2 }
            };

            dialogWorkflowMock
                .Setup(d => d.ShowColumnsOptionsDialog(It.IsAny<List<ColumnDefinition<TestRow>>>(), It.IsAny<HashSet<string>>(), It.IsAny<Dictionary<string, int?>>(), It.IsAny<Dictionary<string, int>>()))
                .ReturnsAsync((selected, widths, changedOrder));

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "OrderOnly");
                parameters.Add(p => p.Items, new List<TestRow> { new TestRow { Name = "Row", Age = 1, Score = 1 } });
            });

            await target.InvokeAsync(() => target.Instance.ShowColumnOptionsDialog());

            var snapshot = TestContext.LocalStorage.Snapshot();
            snapshot.Keys.Should().Contain(k => k.Contains("ColumnOrder", StringComparison.Ordinal));
            var orderKey = snapshot.Keys.Single(k => k.Contains("ColumnOrder", StringComparison.Ordinal));
            var storedOrder = await TestContext.LocalStorage.GetItemAsync<Dictionary<string, int>>(orderKey);
            storedOrder.Should().BeEquivalentTo(changedOrder);
            dialogWorkflowMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_LongPress_WHEN_RowClickBeforeTimeout_THEN_RowClickSuppressed()
        {
            var columns = CreateColumns();
            var items = new List<TestRow>
            {
                new TestRow { Name = "Name", Age = 5, Score = 3 }
            };
            var longPressEvents = new List<TableDataLongPressEventArgs<TestRow>>();
            var selections = new List<HashSet<TestRow>>();
            var rowClicks = new List<TableRowClickEventArgs<TestRow>>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "LongPress");
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.MultiSelection, true);
                parameters.Add(p => p.OnTableDataLongPress, EventCallback.Factory.Create<TableDataLongPressEventArgs<TestRow>>(this, args => longPressEvents.Add(args)));
                parameters.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<HashSet<TestRow>>(this, value => selections.Add(new HashSet<TestRow>(value))));
                parameters.Add(p => p.OnRowClick, EventCallback.Factory.Create<TableRowClickEventArgs<TestRow>>(this, args => rowClicks.Add(args)));
            });

            var cell = target.FindComponents<TdExtended>().First().Find("td");
            await target.InvokeAsync(() => cell.TriggerEventAsync("onlongpress", new LongPressEventArgs { ClientX = 1, ClientY = 2 }));

            target.WaitForAssertion(() =>
            {
                longPressEvents.Should().ContainSingle();
                longPressEvents[0].Item.Should().Be(items[0]);
                longPressEvents[0].Data.Should().NotBeNull();
            });

            var row = target.FindComponents<MudTr>().First().Find("tr");
            await target.InvokeAsync(() => row.Click());

            target.WaitForAssertion(() =>
            {
                selections.Should().BeEmpty();
                rowClicks.Should().BeEmpty();
            });
        }

        [Fact]
        public async Task GIVEN_SuppressWindowExpired_WHEN_RowClicked_THEN_RowClickProcessed()
        {
            var columns = CreateColumns();
            var items = new List<TestRow> { new TestRow { Name = "Delayed", Age = 4, Score = 4 } };
            var selections = new List<HashSet<TestRow>>();
            var rowClicks = new List<TableRowClickEventArgs<TestRow>>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "SuppressExpired");
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.MultiSelection, false);
                parameters.Add(p => p.SelectOnRowClick, true);
                parameters.Add(p => p.SelectedItemsChanged, EventCallback.Factory.Create<HashSet<TestRow>>(this, value => selections.Add(new HashSet<TestRow>(value))));
                parameters.Add(p => p.OnRowClick, EventCallback.Factory.Create<TableRowClickEventArgs<TestRow>>(this, args => rowClicks.Add(args)));
            });

            var cell = target.FindComponents<TdExtended>().First().Find("td");
            await target.InvokeAsync(() => cell.TriggerEventAsync("onlongpress", new LongPressEventArgs()));
            await Task.Delay(600);

            var row = target.FindComponents<MudTr>().First().Find("tr");
            await target.InvokeAsync(() => row.Click());

            target.WaitForAssertion(() =>
            {
                selections.Should().NotBeEmpty();
                selections.Last().Should().ContainSingle().And.Contain(items[0]);
                rowClicks.Should().ContainSingle();
            });
        }

        [Fact]
        public void GIVEN_PreSortedItems_WHEN_Rendered_THEN_OrderRemainsUnchanged()
        {
            var items = new List<TestRow>
            {
                new TestRow { Name = "B", Age = 2, Score = 2 },
                new TestRow { Name = "A", Age = 1, Score = 1 }
            };

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumns());
                parameters.Add(p => p.TableId, "PreSorted");
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.PreSorted, true);
            });

            var firstCell = target.FindComponent<TdExtended>().Find("td");
            firstCell.TextContent.Should().Be("B");
        }

        [Fact]
        public void GIVEN_SelectedRowAndSelectOnRowClick_WHEN_Rendered_THEN_RowStyleHighlighted()
        {
            var columns = CreateColumns();
            var item = new TestRow { Name = "Styled", Age = 1, Score = 1 };

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "RowStyle");
                parameters.Add(p => p.Items, new List<TestRow> { item });
                parameters.Add(p => p.SelectOnRowClick, true);
                parameters.Add(p => p.SelectedItems, new HashSet<TestRow>(new[] { item }));
            });

            var row = target.FindComponents<MudTr>().First();
            row.Markup.Should().Contain("background-color");
        }

        [Fact]
        public void GIVEN_RowClassFuncProvided_WHEN_Rendered_THEN_RowClassApplied()
        {
            var columns = CreateColumns();
            var items = new List<TestRow> { new TestRow { Name = "Row", Age = 1, Score = 1 } };

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "RowClassFunc");
                parameters.Add(p => p.Items, items);
                parameters.Add(p => p.RowClassFunc, new Func<TestRow, int, string>((_, index) => $"row-{index}"));
            });

            var row = target.FindComponents<MudTr>().Single();
            row.Markup.Should().Contain("row-0");
        }

        [Fact]
        public void GIVEN_ClassFuncOnly_WHEN_Rendered_THEN_FuncClassApplied()
        {
            var columns = new[]
            {
                new ColumnDefinition<TestRow>("Name", row => row.Name)
                {
                    ClassFunc = _ => "func-class"
                }
            };
            var items = new List<TestRow> { new TestRow { Name = "Row", Age = 1, Score = 1 } };

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "FuncClassOnly");
                parameters.Add(p => p.Items, items);
            });

            var cell = target.FindComponent<TdExtended>();
            cell.Markup.Should().Contain("func-class");
        }

        [Fact]
        public void GIVEN_ClassFuncWidthAndContextMenu_WHEN_Rendered_THEN_ClassesComposed()
        {
            var column = new ColumnDefinition<TestRow>("WithClass", r => r.Name, context => builder =>
            {
                builder.AddContent(0, context.Data.Name);
            })
            {
                Class = "base",
                ClassFunc = _ => "extra",
                Width = 120
            };

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, new[] { column });
                parameters.Add(p => p.TableId, "ClassFunc");
                parameters.Add(p => p.Items, new List<TestRow> { new TestRow { Name = "Cell", Age = 1, Score = 1 } });
                parameters.Add(p => p.OnTableDataContextMenu, EventCallback.Factory.Create<TableDataContextMenuEventArgs<TestRow>>(this, _ => Task.CompletedTask));
            });

            var cell = target.FindComponent<TdExtended>();
            cell.Instance.Class.Should().Contain("base");
            cell.Instance.Class.Should().Contain("extra");
            cell.Instance.Class.Should().Contain("overflow-cell");
            cell.Instance.Class.Should().Contain("no-default-context-menu");
        }

        [Fact]
        public void GIVEN_IconOnlyColumn_WHEN_Rendered_THEN_HeaderEmptyAndSortable()
        {
            var columns = new[]
            {
                new ColumnDefinition<TestRow>("Icon", row => row.Score, context => builder =>
                {
                    builder.AddContent(0, context.Data.Score);
                })
                {
                    IconOnly = true
                }
            };

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "IconOnly");
                parameters.Add(p => p.Items, new List<TestRow> { new TestRow { Name = "Row", Age = 1, Score = 1 } });
            });

            var header = target.FindComponents<MudTh>().Single();
            header.Markup.Should().NotContain("Icon");
            target.FindComponents<SortLabel>().Should().HaveCount(1);
        }

        [Fact]
        public void GIVEN_NoRowClassFunc_WHEN_Rendered_THEN_RowClassEmpty()
        {
            var column = new ColumnDefinition<TestRow>("Plain", r => r.Name, context => builder =>
            {
                builder.AddContent(0, context.Data.Name);
            })
            {
                Class = null,
                ClassFunc = null,
                Width = null
            };

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, new[] { column });
                parameters.Add(p => p.TableId, "RowClassEmpty");
                parameters.Add(p => p.Items, new List<TestRow> { new TestRow { Name = "Empty", Age = 1, Score = 1 } });
            });

            var cell = target.FindComponent<TdExtended>();
            string.IsNullOrEmpty(cell.Instance.Class).Should().BeTrue();
        }

        [Fact]
        public void GIVEN_ItemsNull_WHEN_Rendered_THEN_TableHandlesNullItems()
        {
            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumns());
                parameters.Add(p => p.TableId, "NullItems");
                parameters.Add(p => p.Items, null);
            });

            target.FindComponents<MudTr>().Should().BeEmpty();
        }

        [Fact]
        public void GIVEN_NoPersistedSelectionOrSort_WHEN_Rendered_THEN_DefaultSortApplied()
        {
            var columns = CreateColumns();
            var sortEvents = new List<string?>();
            var directionEvents = new List<SortDirection>();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "DefaultSort");
                parameters.Add(p => p.Items, new List<TestRow>
                {
                    new TestRow { Name = "B", Age = 2, Score = 2 },
                    new TestRow { Name = "A", Age = 1, Score = 1 }
                });
                parameters.Add(p => p.SortColumnChanged, EventCallback.Factory.Create<string>(this, value => sortEvents.Add(value)));
                parameters.Add(p => p.SortDirectionChanged, EventCallback.Factory.Create<SortDirection>(this, value => directionEvents.Add(value)));
            });

            sortEvents.Should().Contain("name");
            directionEvents.Should().Contain(SortDirection.Ascending);
        }

        [Fact]
        public void GIVEN_NonSortableColumn_WHEN_Rendered_THEN_HeaderHasNoSortLabelAndItemsUnsorted()
        {
            var columns = CreateColumns().ToArray();
            columns[0].SortSelector = null!;

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "NonSortable");
                parameters.Add(p => p.Items, new List<TestRow>
                {
                    new TestRow { Name = "Second", Age = 2, Score = 2 },
                    new TestRow { Name = "First", Age = 1, Score = 1 }
                });
            });

            target.FindComponents<SortLabel>().Should().NotContain(component => component.Markup.Contains("Name", StringComparison.Ordinal));

            var rows = target.FindComponents<MudTr>();
            rows[0].Markup.Should().Contain("Second");
            rows[1].Markup.Should().Contain("First");
        }

        [Fact]
        public async Task GIVEN_PersistedSortWithMissingColumn_WHEN_Rendered_THEN_ItemsReturnedUnsorted()
        {
            var tableId = "MissingSort";
            var sortKey = $"DynamicTable{typeof(TestRow).Name}.ColumnSort.{tableId}";
            await TestContext.LocalStorage.SetItemAsStringAsync(sortKey, "{\"SortColumn\":\"missing\",\"SortDirection\":2}");

            var items = new List<TestRow>
            {
                new TestRow { Name = "First", Age = 1, Score = 1 },
                new TestRow { Name = "Second", Age = 2, Score = 2 }
            };

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumns());
                parameters.Add(p => p.TableId, tableId);
                parameters.Add(p => p.Items, items);
            });

            var rows = target.FindComponents<MudTr>();
            rows[0].Markup.Should().Contain("First");
            rows[1].Markup.Should().Contain("Second");
        }

        [Fact]
        public async Task GIVEN_StoredColumnOrderWithMissingId_WHEN_ReRendered_THEN_MissingSkippedAndRemainingAppended()
        {
            var dialogWorkflowMock = TestContext.AddSingletonMock<IDialogWorkflow>(MockBehavior.Strict);
            var columns = CreateColumns();
            var selected = columns.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
            var widths = columns.ToDictionary(c => c.Id, _ => (int?)null, StringComparer.Ordinal);
            var changedOrder = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "missing", 0 },
                { columns[1].Id, 1 }
            };

            dialogWorkflowMock
                .Setup(d => d.ShowColumnsOptionsDialog(It.IsAny<List<ColumnDefinition<TestRow>>>(), It.IsAny<HashSet<string>>(), It.IsAny<Dictionary<string, int?>>(), It.IsAny<Dictionary<string, int>>()))
                .ReturnsAsync((selected, widths, changedOrder));

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, columns);
                parameters.Add(p => p.TableId, "OrderWithMissing");
                parameters.Add(p => p.Items, new List<TestRow> { new TestRow { Name = "Row", Age = 1, Score = 1 } });
            });

            await target.InvokeAsync(() => target.Instance.ShowColumnOptionsDialog());
            target.Render();

            var headers = target.FindComponents<MudTh>().Select(h => h.Find("th")).ToList();
            headers.Should().HaveCount(3);
            headers[0].TextContent.Should().Contain("Age");
            headers[1].TextContent.Should().Contain("Name");
            headers[2].TextContent.Should().Contain("Score");
            dialogWorkflowMock.VerifyAll();
        }

        [Fact]
        public async Task GIVEN_ColumnDefinitionsUpdated_WHEN_ParametersSet_THEN_ColumnsRegenerated()
        {
            var originalColumns = CreateColumns();

            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, originalColumns);
                parameters.Add(p => p.TableId, "UpdateColumns");
                parameters.Add(p => p.Items, new List<TestRow> { new TestRow { Name = "Row", Age = 1, Score = 2 } });
                parameters.Add(p => p.Hover, true);
            });

            target.FindComponents<MudTh>().Should().HaveCount(3);
            target.Instance.Hover.Should().BeTrue();

            var updatedColumns = originalColumns.Take(2).ToArray();
            target.Render(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, updatedColumns);
                parameters.Add(p => p.TableId, "UpdateColumns");
                parameters.Add(p => p.Items, target.Instance.Items);
                parameters.Add(p => p.Hover, target.Instance.Hover);
            });

            var headers = target.FindComponents<MudTh>();
            headers.Should().HaveCount(2);
            headers.Should().OnlyContain(h => h.Markup.Contains("Name", StringComparison.Ordinal) || h.Markup.Contains("Age", StringComparison.Ordinal));
        }

        [Fact]
        public void GIVEN_HoverDisabled_WHEN_Rendered_THEN_TableHoverIsFalse()
        {
            var target = TestContext.Render<DynamicTable<TestRow>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumns());
                parameters.Add(p => p.TableId, "HoverDisabled");
                parameters.Add(p => p.Items, new List<TestRow> { new TestRow { Name = "Row", Age = 1, Score = 1 } });
                parameters.Add(p => p.Hover, false);
            });

            var mudTable = target.FindComponent<MudTable<TestRow>>();
            mudTable.Instance.Hover.Should().BeFalse();
        }

        [Fact]
        public async Task GIVEN_NullRowClickEvent_WHEN_Raised_THEN_Ignored()
        {
            var rowClicks = new List<TableRowClickEventArgs<SampleItem>>();

            var target = TestContext.Render<DynamicTable<SampleItem>>(parameters =>
            {
                parameters.Add(p => p.ColumnDefinitions, CreateColumnDefinitions());
                parameters.Add(p => p.TableId, "NullRowClick");
                parameters.Add(p => p.Items, CreateItems());
                parameters.Add(p => p.OnRowClick, EventCallback.Factory.Create<TableRowClickEventArgs<SampleItem>>(this, args => { rowClicks.Add(args); return Task.CompletedTask; }));
            });

            var table = target.FindComponent<MudTable<SampleItem>>();
            var row = target.FindComponents<MudTr>().First();
            var args = new TableRowClickEventArgs<SampleItem>(new MouseEventArgs(), row.Instance, item: null);

            await target.InvokeAsync(() => table.Instance.OnRowClick.InvokeAsync(args));

            rowClicks.Should().BeEmpty();
            target.Instance.SelectedItems.Should().BeEmpty();
        }

        private IRenderedComponent<DynamicTable<SampleItem>> RenderDynamicTable(Action<ComponentParameterCollectionBuilder<DynamicTable<SampleItem>>> configure)
        {
            return TestContext.Render<DynamicTable<SampleItem>>(configure);
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

        private static IRenderedComponent<SortLabel> FindSortLabel(IRenderedComponent<DynamicTable<TestRow>> target, string header)
        {
            return target.FindComponents<SortLabel>().First(component => component.Markup.Contains(header, StringComparison.Ordinal));
        }

        private static ColumnDefinition<TestRow>[] CreateColumns()
        {
            RenderFragment<RowContext<TestRow>> nameTemplate = context => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "data-test-id", TestIdHelper.For("NameCell"));
                builder.AddContent(2, context.Data.Name);
                builder.CloseElement();
            };

            RenderFragment<RowContext<TestRow>> ageTemplate = context => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "data-test-id", TestIdHelper.For("AgeCell"));
                builder.AddContent(2, context.Data.Age);
                builder.CloseElement();
            };

            RenderFragment<RowContext<TestRow>> scoreTemplate = context => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "data-test-id", TestIdHelper.For("ScoreCell"));
                builder.AddContent(2, context.Data.Score);
                builder.CloseElement();
            };

            return
            [
                new ColumnDefinition<TestRow>("Name", row => row.Name, nameTemplate),
                new ColumnDefinition<TestRow>("Age", row => row.Age, ageTemplate),
                new ColumnDefinition<TestRow>("Score", row => row.Score, scoreTemplate)
            ];
        }

        private sealed record SampleItem(int Id, string Name, int Value);

        public sealed class TestRow
        {
            public string Name { get; set; } = string.Empty;

            public int Age { get; set; }

            public int Score { get; set; }
        }
    }
}
