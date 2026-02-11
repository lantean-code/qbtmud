using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class ColumnOptionsDialog<T>
    {
        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public List<ColumnDefinition<T>> Columns { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public HashSet<string> SelectedColumns { get; set; } = default!;

        [Parameter]
        public Dictionary<string, int?> Widths { get; set; } = [];

        [Parameter]
        public Dictionary<string, int> Order { get; set; } = [];

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        protected HashSet<string> SelectedColumnsInternal { get; set; } = [];

        protected Dictionary<string, int?> WidthsInternal { get; set; } = [];

        protected Dictionary<string, int> OrderInternal { get; set; } = [];

        protected override void OnParametersSet()
        {
            if (SelectedColumnsInternal.Count == 0)
            {
                var source = SelectedColumns.Count != 0
                    ? SelectedColumns
                    : Columns.Where(c => c.Enabled).Select(c => c.Id);

                foreach (var selectedColumn in source)
                {
                    SelectedColumnsInternal.Add(selectedColumn);
                }
            }

            if (WidthsInternal.Count == 0)
            {
                foreach (var width in Widths)
                {
                    WidthsInternal[width.Key] = width.Value;
                }
            }

            if (OrderInternal.Count == 0)
            {
                if (Order.Count == 0)
                {
                    for (int i = 0; i < Columns.Count; i++)
                    {
                        var column = Columns[i];
                        OrderInternal.Add(column.Id, i);
                    }
                }
                else
                {
                    var knownColumns = new HashSet<string>(Columns.Select(c => c.Id), StringComparer.Ordinal);
                    var index = 0;

                    foreach (var order in Order.OrderBy(o => o.Value))
                    {
                        if (!knownColumns.Contains(order.Key))
                        {
                            continue;
                        }

                        OrderInternal[order.Key] = index++;
                    }

                    foreach (var column in Columns)
                    {
                        if (OrderInternal.ContainsKey(column.Id))
                        {
                            continue;
                        }

                        OrderInternal[column.Id] = index++;
                    }
                }
            }
        }

        protected void SetSelected(bool selected, string id)
        {
            if (selected)
            {
                SelectedColumnsInternal.Add(id);
            }
            else
            {
                SelectedColumnsInternal.Remove(id);
            }
        }

        protected void SetWidth(string? value, string id)
        {
            var column = Columns.Find(c => c.Id == id);
            var defaultWidth = column?.Width;

            if (int.TryParse(value, out var width))
            {
                if (width == defaultWidth)
                {
                    WidthsInternal.Remove(id);
                }
                else
                {
                    WidthsInternal[id] = width;
                }
            }
            else
            {
                if (defaultWidth is null)
                {
                    WidthsInternal.Remove(id);
                }
                else
                {
                    WidthsInternal[id] = null;
                }
            }
        }

        protected void MoveUp(int index)
        {
            if (index == 0)
            {
                return;
            }

            var currentId = OrderInternal.FirstOrDefault(o => o.Value == index).Key;
            var otherId = OrderInternal.FirstOrDefault(o => o.Value == index - 1).Key;

            if (currentId is null || otherId is null)
            {
                return;
            }

            OrderInternal[otherId] = index;
            OrderInternal[currentId] = index - 1;

            //(Columns[index], Columns[index - 1]) = (Columns[index - 1], Columns[index]);

            StateHasChanged();
        }

        protected void MoveDown(int index)
        {
            if (index < 0 || index >= Columns.Count - 1)
            {
                return;
            }

            var currentId = OrderInternal.FirstOrDefault(o => o.Value == index).Key;
            var otherId = OrderInternal.FirstOrDefault(o => o.Value == index + 1).Key;

            if (currentId is null || otherId is null)
            {
                return;
            }

            OrderInternal[otherId] = index;
            OrderInternal[currentId] = index + 1;

            //(Columns[index], Columns[index + 1]) = (Columns[index + 1], Columns[index]);

            StateHasChanged();
        }

        protected void ResetOrder()
        {
            OrderInternal.Clear();

            for (var i = 0; i < Columns.Count; i++)
            {
                OrderInternal[Columns[i].Id] = i;
            }

            StateHasChanged();
        }

        protected string GetValue(int? value, string columnId)
        {
            if (WidthsInternal.TryGetValue(columnId, out var newWidth))
            {
                value = newWidth;
            }

            if (!value.HasValue)
            {
                return "";
            }

            if (value.Value <= 0)
            {
                return Translate("auto");
            }

            return value.Value.ToString();
        }

        private string[] OrderedColumns => GetOrderedColumns();

        private string[] GetOrderedColumns()
        {
            return OrderInternal.OrderBy(x => x.Value).Select(x => x.Key).ToArray();
        }

        protected void Cancel()
        {
            MudDialog.Cancel();
        }

        protected void Submit()
        {
            MudDialog.Close(DialogResult.Ok((SelectedColumnsInternal, WidthsInternal, OrderInternal)));
        }

        protected override Task Submit(KeyboardEvent keyboardEvent)
        {
            Submit();

            return Task.CompletedTask;
        }

        private string Translate(string value)
        {
            return WebUiLocalizer.Translate("AppColumnOptionsDialog", value);
        }
    }
}
