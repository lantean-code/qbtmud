using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using System.Numerics;

namespace Lantean.QBTMudBlade.Components.Dialogs
{
    public partial class NumericFieldDialog<T> where T : struct, INumber<T>
    {
        [CascadingParameter]
        public MudDialogInstance MudDialog { get; set; } = default!;

        [Parameter]
        public string? Label { get; set; }

        [Parameter]
        public T Value { get; set; }

        [Parameter]
        public T Min { get; set; } = T.Zero;

        [Parameter]
        public T Max { get; set; } = T.One;

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public Func<T, string>? ValueDisplayFunc { get; set; }

        [Parameter]
        public Func<string, T>? ValueGetFunc { get; set; }

        private string? GetDisplayValue()
        {
            var value = ValueDisplayFunc?.Invoke(Value);
            return value is null ? Value.ToString() : value;
        }

        protected void ValueChanged(string value)
        {
            if (ValueGetFunc is not null)
            {
                Value = ValueGetFunc.Invoke(value);

                return;
            }

            if (T.TryParse(value, null, out var result))
            {
                Value = result;
            }
            else
            {
                Value = Min;
            }
        }

        protected void Cancel(MouseEventArgs args)
        {
            MudDialog.Cancel();
        }

        protected void Submit(MouseEventArgs args)
        {
            MudDialog.Close(DialogResult.Ok(Value));
        }
    }
}