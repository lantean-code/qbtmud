using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class ColumnOptionsDialog<T>
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public List<ColumnDefinition<T>> Columns { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public HashSet<string> SelectedColumns { get; set; } = default!;

        [Parameter]
        public Dictionary<string, int?> Widths { get; set; } = [];

        protected HashSet<string> SelectedColumnsInternal { get; set; } = [];

        protected override void OnParametersSet()
        {
            if (SelectedColumnsInternal.Count == 0)
            {
                if (SelectedColumns.Count != 0)
                {
                    foreach (var selectedColumn in SelectedColumns)
                    {
                        SelectedColumnsInternal.Add(selectedColumn);
                    }
                }
                else
                {
                    foreach (var column in Columns.Where(c => c.Enabled))
                    {
                        SelectedColumns.Add(column.Id);
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
                    Widths.Remove(id);
                }
                else
                {
                    Widths[id] = width;
                }
            }
            else
            {
                if (defaultWidth is null)
                {
                    Widths.Remove(id);
                }
                else
                {
                    Widths[id] = null;
                }
            }
        }

        protected void MoveUp(int index)
        {
            if (index == 0)
            {
                return;
            }

            (Columns[index], Columns[index - 1]) = (Columns[index - 1], Columns[index]);
        }

        protected void MoveDown(int index)
        {
            if (index >= Columns.Count)
            {
                return;
            }

            (Columns[index], Columns[index + 1]) = (Columns[index + 1], Columns[index]);
        }

        protected string GetValue(int? value, string columnId)
        {
            if (Widths.TryGetValue(columnId, out var newWidth))
            {
                value = newWidth;
            }

            if (!value.HasValue)
            {
                return "";
            }

            if (value.Value <= 0)
            {
                return "auto";
            }

            return value.Value.ToString();
        }

        protected void Cancel(MouseEventArgs args)
        {
            MudDialog.Cancel();
        }

        protected void Submit(MouseEventArgs args)
        {
            MudDialog.Close(DialogResult.Ok((SelectedColumnsInternal, Widths)));
        }
    }
}