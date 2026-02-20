using Lantean.QBTMud.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.RegularExpressions;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class WizardStep
    {
        private const string PrimaryAccentCss = "var(--mud-palette-primary)";

        private static readonly Regex _hexColorPattern = new Regex(
            "^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$",
            RegexOptions.Compiled);

        private static readonly Regex _functionalColorPattern = new Regex(
            "^(?:rgb|rgba|hsl|hsla|hwb|lab|lch|oklab|oklch|color|color-mix)\\s*\\(",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _keywordColorPattern = new Regex(
            "^[a-zA-Z][a-zA-Z0-9-]*$",
            RegexOptions.Compiled);

        private string _resolvedAccentCss = PrimaryAccentCss;

        [Inject]
        private ILogger<WizardStep> Logger { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public WizardAccentColor Accent { get; set; } = WizardAccentColor.FromPalette(Color.Primary);

        [Parameter]
        [EditorRequired]
        public string Icon { get; set; } = string.Empty;

        [Parameter]
        [EditorRequired]
        public string Title { get; set; } = string.Empty;

        [Parameter]
        [EditorRequired]
        public string Subtitle { get; set; } = string.Empty;

        [Parameter]
        public Severity AlertSeverity { get; set; } = Severity.Info;

        [Parameter]
        public string? AlertText { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        private bool HasAlert
        {
            get
            {
                return !string.IsNullOrWhiteSpace(AlertText);
            }
        }

        private string RootStyle
        {
            get
            {
                return FormattableString.Invariant($"--wizard-step-accent: {_resolvedAccentCss};");
            }
        }

        private string AvatarStyle
        {
            get
            {
                return "background-color: var(--wizard-step-accent);";
            }
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            _resolvedAccentCss = ResolveAccentCss();
        }

        private string ResolveAccentCss()
        {
            if (Accent is null)
            {
                Logger.LogWarning("WizardStep accent configuration is missing. Falling back to primary accent color.");
                return PrimaryAccentCss;
            }

            if (Accent.Kind == WizardAccentColorKind.Palette)
            {
                if (!Accent.PaletteColor.HasValue)
                {
                    Logger.LogWarning("WizardStep accent kind '{AccentKind}' requires a palette color. Falling back to primary accent color.", Accent.Kind);
                    return PrimaryAccentCss;
                }

                return MapPaletteColor(Accent.PaletteColor.Value);
            }

            if (Accent.Kind == WizardAccentColorKind.Css)
            {
                if (!TryResolveCssColor(Accent.CssColor, out var cssColor))
                {
                    Logger.LogWarning("WizardStep CSS accent color '{CssColor}' is invalid. Falling back to primary accent color.", Accent.CssColor);
                    return PrimaryAccentCss;
                }

                return cssColor;
            }

            Logger.LogWarning("WizardStep accent kind '{AccentKind}' is not supported. Falling back to primary accent color.", Accent.Kind);
            return PrimaryAccentCss;
        }

        private static string MapPaletteColor(Color color)
        {
            switch (color)
            {
                case Color.Primary:
                    return "var(--mud-palette-primary)";

                case Color.Secondary:
                    return "var(--mud-palette-secondary)";

                case Color.Tertiary:
                    return "var(--mud-palette-tertiary)";

                case Color.Info:
                    return "var(--mud-palette-info)";

                case Color.Success:
                    return "var(--mud-palette-success)";

                case Color.Warning:
                    return "var(--mud-palette-warning)";

                case Color.Error:
                    return "var(--mud-palette-error)";

                case Color.Dark:
                    return "var(--mud-palette-dark)";

                case Color.Surface:
                    return "var(--mud-palette-surface)";

                case Color.Transparent:
                    return "transparent";

                case Color.Inherit:
                    return "inherit";

                case Color.Default:
                default:
                    return PrimaryAccentCss;
            }
        }

        private static bool TryResolveCssColor(string? cssColor, out string resolvedCssColor)
        {
            resolvedCssColor = PrimaryAccentCss;

            if (string.IsNullOrWhiteSpace(cssColor))
            {
                return false;
            }

            var candidate = cssColor.Trim();

            if (!IsLikelyValidCssColor(candidate))
            {
                return false;
            }

            resolvedCssColor = candidate;
            return true;
        }

        private static bool IsLikelyValidCssColor(string cssColor)
        {
            if (cssColor.Contains(';') || cssColor.Contains('{') || cssColor.Contains('}'))
            {
                return false;
            }

            if (_hexColorPattern.IsMatch(cssColor))
            {
                return true;
            }

            if (cssColor.StartsWith("var(", StringComparison.OrdinalIgnoreCase) && cssColor.EndsWith(')'))
            {
                return true;
            }

            if (_functionalColorPattern.IsMatch(cssColor) && cssColor.EndsWith(')'))
            {
                return true;
            }

            if (_keywordColorPattern.IsMatch(cssColor))
            {
                return true;
            }

            return false;
        }
    }
}
