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

        protected HashSet<string> SelectedColumns { get; set; } = [];

        protected override void OnParametersSet()
        {
            if (SelectedColumns.Count == 0)
            {
                foreach (var column in Columns.Where(c => c.Enabled))
                {
                    SelectedColumns.Add(column.Id);
                }
            }
        }

        protected void SetSelected(bool selected, string id) 
        {
            if (selected)
            {
                SelectedColumns.Add(id);
            }
            else
            {
                SelectedColumns.Remove(id);
            }
        }

        protected void Cancel(MouseEventArgs args)
        {
            MudDialog.Cancel();
        }

        protected void Submit(MouseEventArgs args)
        {
            MudDialog.Close(DialogResult.Ok(SelectedColumns));
        }
    }
}