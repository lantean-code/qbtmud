using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Models
{
    public record UIAction
    {
        private readonly Color _color;

        public UIAction(string name, string text, string? icon, Color color, string href, bool separatorBefore = false, bool autoClose = true)
        {
            Name = name;
            Text = text;
            Icon = icon;
            _color = color;
            Href = href;
            SeparatorBefore = separatorBefore;
            AutoClose = autoClose;
            Children = [];
        }

        public UIAction(string name, string text, string? icon, Color color, EventCallback callback, bool separatorBefore = false, bool autoClose = true)
        {
            Name = name;
            Text = text;
            Icon = icon;
            _color = color;
            Callback = callback;
            SeparatorBefore = separatorBefore;
            AutoClose = autoClose;
            Children = [];
        }

        public UIAction(string name, string text, string? icon, Color color, IEnumerable<UIAction> children, bool useTextButton = false, bool separatorBefore = false, bool autoClose = true)
        {
            Name = name;
            Text = text;
            Icon = icon;
            _color = color;
            Callback = default;
            Children = children;
            UseTextButton = useTextButton;
            SeparatorBefore = separatorBefore;
            AutoClose = autoClose;
        }

        public string Name { get; }

        public string Text { get; set; }

        public string? Icon { get; }

        public Color Color => IsChecked is null || IsChecked.Value ? _color : Color.Transparent;

        public EventCallback Callback { get; }

        public string? Href { get; }

        public bool SeparatorBefore { get; set; }

        public bool AutoClose { get; set; } = true;

        public IEnumerable<UIAction> Children { get; internal set; }

        public bool UseTextButton { get; }

        public bool? IsChecked { get; internal set; }
    }
}
