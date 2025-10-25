using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Models
{
    public record UIAction
    {
        private readonly Color _color;

        public UIAction(string name, string text, string? icon, Color color, string href, bool separatorBefore = false)
        {
            Name = name;
            Text = text;
            Icon = icon;
            _color = color;
            Href = href;
            SeparatorBefore = separatorBefore;
            Children = [];
        }

        public UIAction(string name, string text, string? icon, Color color, EventCallback callback, bool separatorBefore = false)
        {
            Name = name;
            Text = text;
            Icon = icon;
            _color = color;
            Callback = callback;
            SeparatorBefore = separatorBefore;
            Children = [];
        }

        public UIAction(string name, string text, string? icon, Color color, IEnumerable<UIAction> children, bool useTextButton = false, bool separatorBefore = false)
        {
            Name = name;
            Text = text;
            Icon = icon;
            _color = color;
            Callback = default;
            Children = children;
            UseTextButton = useTextButton;
            SeparatorBefore = separatorBefore;
        }

        public string Name { get; }

        public string Text { get; set; }

        public string? Icon { get; }

        public Color Color => IsChecked is null || IsChecked.Value ? _color : Color.Transparent;

        public EventCallback Callback { get; }

        public string? Href { get; }

        public bool SeparatorBefore { get; set; }

        public IEnumerable<UIAction> Children { get; }

        public bool UseTextButton { get; }

        public bool? IsChecked { get; internal set; }
    }
}