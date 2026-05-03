using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Extensions;

namespace Lantean.QBTMud.Components.UI
{
    public partial class TickSwitch<T>
    {
        private const string _hiddenStyle = "display:none;";

        private string? _styleBeforeHidden;

        private bool _isHiddenApplied;

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            var currentValue = this.GetState(x => x.Value);
            if (currentValue is null)
            {
                ApplyHiddenState();
                return;
            }

            if (_isHiddenApplied)
            {
                Style = _styleBeforeHidden;
                _styleBeforeHidden = null;
                _isHiddenApplied = false;
            }

            if (currentValue is bool boolValue)
            {
                ThumbIcon = boolValue ? Icons.Material.Filled.Done : Icons.Material.Filled.Close;
                ThumbIconColor = boolValue ? Color.Success : Color.Error;
            }
        }

        protected override async Task OnChange(ChangeEventArgs args)
        {
            if (GetDisabledState() || GetReadOnlyState())
            {
                return;
            }

            Touched = true;
            await ValueChanged.InvokeAsync(ConvertGet((bool?)args.Value));
            await BeginValidateAsync();
            FieldChanged(ReadValue);
        }

        private void ApplyHiddenState()
        {
            if (_isHiddenApplied)
            {
                return;
            }

            _styleBeforeHidden = Style;
            Style = AppendHiddenStyle(Style);
            _isHiddenApplied = true;
        }

        private static string AppendHiddenStyle(string? style)
        {
            if (string.IsNullOrWhiteSpace(style))
            {
                return _hiddenStyle;
            }

            var trimmed = style.Trim();
            if (trimmed.EndsWith(";"))
            {
                return $"{trimmed}{_hiddenStyle}";
            }

            return $"{trimmed};{_hiddenStyle}";
        }
    }
}
