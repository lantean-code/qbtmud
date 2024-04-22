using Lantean.QBTMudBlade.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class AddCategoryDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        protected string? Category { get; set; }

        protected string SavePath { get; set; } = "";

        protected void Cancel(MouseEventArgs args)
        {
            MudDialog.Cancel();
        }

        protected void Submit(MouseEventArgs args)
        {
            if (Category is null)
            {
                return;
            }
            MudDialog.Close(DialogResult.Ok(new Category(Category, SavePath)));
        }
    }
}