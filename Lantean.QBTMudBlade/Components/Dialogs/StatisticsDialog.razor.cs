using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class StatisticsDialog
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;
    }
}