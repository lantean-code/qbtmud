using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.ThemeManager;
using MudBlazor.Utilities;
using System.Text;

namespace Lantean.QBTMud.Pages
{
    public partial class ThemeDetail
    {
        private static readonly IReadOnlyList<(string Title, IReadOnlyList<(string Name, ThemePaletteColor Color)> Items)> _colorGroups =
        [
            (
                "Theme Colors",
                new List<(string, ThemePaletteColor)>
                {
                    ("Primary", ThemePaletteColor.Primary),
                    ("Secondary", ThemePaletteColor.Secondary),
                    ("Tertiary", ThemePaletteColor.Tertiary),
                    ("Info", ThemePaletteColor.Info),
                    ("Success", ThemePaletteColor.Success),
                    ("Warning", ThemePaletteColor.Warning),
                    ("Error", ThemePaletteColor.Error),
                    ("Dark", ThemePaletteColor.Dark)
                }
            ),
            (
                "Components",
                new List<(string, ThemePaletteColor)>
                {
                    ("Appbar Text", ThemePaletteColor.AppbarText),
                    ("Appbar Background", ThemePaletteColor.AppbarBackground),
                    ("Drawer Text", ThemePaletteColor.DrawerText),
                    ("Drawer Icons", ThemePaletteColor.DrawerIcon),
                    ("Drawer Background", ThemePaletteColor.DrawerBackground)
                }
            ),
            (
                "General",
                new List<(string, ThemePaletteColor)>
                {
                    ("Surface", ThemePaletteColor.Surface),
                    ("Background", ThemePaletteColor.Background),
                    ("Background Gray", ThemePaletteColor.BackgroundGray),
                    ("Lines Default", ThemePaletteColor.LinesDefault),
                    ("Lines Inputs", ThemePaletteColor.LinesInputs),
                    ("Divider", ThemePaletteColor.Divider),
                    ("Divider Light", ThemePaletteColor.DividerLight)
                }
            ),
            (
                "Text & Actions",
                new List<(string, ThemePaletteColor)>
                {
                    ("Text Primary", ThemePaletteColor.TextPrimary),
                    ("Text Secondary", ThemePaletteColor.TextSecondary),
                    ("Text Disabled", ThemePaletteColor.TextDisabled),
                    ("Action Default", ThemePaletteColor.ActionDefault),
                    ("Action Disabled", ThemePaletteColor.ActionDisabled),
                    ("Disabled Background", ThemePaletteColor.ActionDisabledBackground)
                }
            )
        ];

        private ThemeCatalogItem? _theme;
        private ThemeManagerTheme? _editorTheme;
        private string _editorFont = string.Empty;
        private string? _fontError;
        private bool _hasChanges;
        private bool _isBusy;
        private bool _isLoading;

        [Inject]
        protected IThemeManagerService ThemeManagerService { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Parameter]
        public string ThemeId { get; set; } = string.Empty;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        protected ThemeCatalogItem? Theme
        {
            get { return _theme; }
        }

        protected bool IsLoading
        {
            get { return _isLoading; }
        }

        protected bool HasChanges
        {
            get { return _hasChanges; }
        }

        protected bool IsBusy
        {
            get { return _isBusy; }
        }

        protected bool CanEditTheme
        {
            get { return _theme is not null && !_theme.IsReadOnly; }
        }

        protected bool CanApplyTheme
        {
            get { return _theme is not null && !IsThemeApplied(_theme); }
        }

        protected bool HasFontError
        {
            get { return !string.IsNullOrWhiteSpace(_fontError); }
        }

        protected string FontError
        {
            get { return _fontError ?? string.Empty; }
        }

        protected string EditorFont
        {
            get { return _editorFont; }
        }

        protected string ThemeTitle
        {
            get { return _theme?.Name ?? "Theme Details"; }
        }

        protected IReadOnlyList<(string Title, IReadOnlyList<(string Name, ThemePaletteColor Color)> Items)> ColorGroups
        {
            get { return _colorGroups; }
        }

        protected override async Task OnParametersSetAsync()
        {
            await ThemeManagerService.EnsureInitialized();
            await LoadTheme();
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("./themes");
        }

        protected async Task ApplyTheme()
        {
            if (_theme is null)
            {
                return;
            }

            await RunBusy(async () => await ThemeManagerService.ApplyTheme(_theme.Id));
        }

        protected async Task DuplicateTheme()
        {
            if (_theme is null)
            {
                return;
            }

            await RunBusy(async () =>
            {
                var defaultName = $"{_theme.Name} Copy";
                var name = await DialogWorkflow.ShowStringFieldDialog("Duplicate Theme", "Name", defaultName);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                var clone = ThemeSerialization.CloneTheme(_theme.Theme);
                var definition = new ThemeDefinition
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = name.Trim(),
                    Theme = clone
                };

                await ThemeManagerService.SaveLocalTheme(definition);
                NavigateToDetails(definition.Id);
            });
        }

        protected async Task ExportTheme()
        {
            if (_theme is null)
            {
                return;
            }

            await RunBusy(async () =>
            {
                var definition = new ThemeDefinition
                {
                    Id = _theme.Id,
                    Name = _theme.Name,
                    Theme = _theme.Theme
                };

                var json = ThemeSerialization.SerializeDefinition(definition, writeIndented: true);
                var safeName = SanitizeFileName(_theme.Name);
                var dataUrl = BuildJsonDataUrl(json);

                await JSRuntime.FileDownload(dataUrl, $"{safeName}.json");
            });
        }

        protected async Task RenameTheme()
        {
            if (!CanEditTheme || _theme is null)
            {
                return;
            }

            await RunBusy(async () =>
            {
                var name = await DialogWorkflow.ShowStringFieldDialog("Rename Theme", "Name", _theme.Name);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                var definition = new ThemeDefinition
                {
                    Id = _theme.Id,
                    Name = name.Trim(),
                    Theme = _theme.Theme
                };

                await ThemeManagerService.SaveLocalTheme(definition);
                await RefreshTheme();
            });
        }

        protected async Task DeleteTheme()
        {
            if (!CanEditTheme || _theme is null)
            {
                return;
            }

            await RunBusy(async () =>
            {
                var confirmed = await DialogWorkflow.ShowConfirmDialog("Delete theme?", $"Delete '{_theme.Name}'?");
                if (!confirmed)
                {
                    return;
                }

                await ThemeManagerService.DeleteLocalTheme(_theme.Id);
                NavigateBack();
            });
        }

        protected void ResetEdits()
        {
            if (_theme is null)
            {
                return;
            }

            SetTheme(_theme);
        }

        protected async Task SaveEdits()
        {
            if (!CanEditTheme || _theme is null || _editorTheme is null)
            {
                return;
            }

            await SaveEditsInternal(false);
        }

        protected async Task SaveAndApplyEdits()
        {
            await SaveEditsInternal(true);
        }

        protected Task FontChanged(string value)
        {
            if (_editorTheme is null)
            {
                return Task.CompletedTask;
            }

            if (!ThemeFontCatalog.TryGetFontUrl(value, out _))
            {
                _fontError = "Select a valid Google font family.";
                return Task.CompletedTask;
            }

            _fontError = null;
            _editorFont = value;
            ThemeFontHelper.ApplyFont(_editorTheme, value);
            _hasChanges = true;
            return Task.CompletedTask;
        }

        protected Task<IEnumerable<string>> SearchFonts(string value, CancellationToken cancellationToken)
        {
            IEnumerable<string> fonts = ThemeFontCatalog.SuggestedFonts;
            if (!string.IsNullOrWhiteSpace(value))
            {
                fonts = fonts.Where(font => font.Contains(value, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(fonts);
        }

        protected MudColor GetPaletteColor(ThemePaletteColor colorType, bool useDarkPalette)
        {
            if (_editorTheme is null)
            {
                return new MudColor("#000000");
            }

            return ThemePaletteHelper.GetColor(_editorTheme, colorType, useDarkPalette);
        }

        protected void UpdatePaletteColor(ThemePaletteColor colorType, MudColor value, bool useDarkPalette)
        {
            if (_editorTheme is null)
            {
                return;
            }

            ThemePaletteHelper.SetColor(_editorTheme, colorType, value.ToString(), useDarkPalette);
            _hasChanges = true;
        }

        protected bool IsThemeApplied(ThemeCatalogItem theme)
        {
            return ThemeManagerService.CurrentThemeId == theme.Id;
        }

        private Task LoadTheme()
        {
            if (string.IsNullOrWhiteSpace(ThemeId))
            {
                _theme = null;
                return Task.CompletedTask;
            }

            _isLoading = true;
            try
            {
                var theme = ThemeManagerService.Themes.FirstOrDefault(item => item.Id == ThemeId);
                if (theme is null)
                {
                    _theme = null;
                    return Task.CompletedTask;
                }

                SetTheme(theme);
            }
            finally
            {
                _isLoading = false;
            }

            return Task.CompletedTask;
        }

        private async Task RefreshTheme()
        {
            if (_theme is null)
            {
                return;
            }

            var updated = ThemeManagerService.Themes.FirstOrDefault(item => item.Id == _theme.Id);
            if (updated is null)
            {
                NavigateBack();
                return;
            }

            SetTheme(updated);
            await InvokeAsync(StateHasChanged);
        }

        private async Task SaveEditsInternal(bool applyAfterSave)
        {
            if (!CanEditTheme || _theme is null || _editorTheme is null)
            {
                return;
            }

            await RunBusy(async () =>
            {
                var definition = new ThemeDefinition
                {
                    Id = _theme.Id,
                    Name = _theme.Name,
                    Theme = _editorTheme
                };

                await ThemeManagerService.SaveLocalTheme(definition);
                await RefreshTheme();

                if (applyAfterSave)
                {
                    await ThemeManagerService.ApplyTheme(_theme.Id);
                }
            });
        }

        private async Task RunBusy(Func<Task> action)
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            try
            {
                await action();
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void SetTheme(ThemeCatalogItem theme)
        {
            _theme = theme;
            _editorTheme = ThemeSerialization.CloneTheme(theme.Theme);

            var fontFamily = string.IsNullOrWhiteSpace(_editorTheme.FontFamily) ? "Nunito Sans" : _editorTheme.FontFamily;
            if (!ThemeFontCatalog.TryGetFontUrl(fontFamily, out _))
            {
                fontFamily = "Nunito Sans";
            }

            ThemeFontHelper.ApplyFont(_editorTheme, fontFamily);
            _editorFont = fontFamily;
            _fontError = null;
            _hasChanges = false;
        }

        private void NavigateToDetails(string themeId)
        {
            var escaped = Uri.EscapeDataString(themeId);
            NavigationManager.NavigateTo($"./themes/{escaped}");
        }

        private static string BuildJsonDataUrl(string json)
        {
            var escaped = Uri.EscapeDataString(json);
            return $"data:application/json;charset=utf-8,{escaped}";
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(name.Length);

            foreach (var ch in name)
            {
                builder.Append(invalidChars.Contains(ch) ? '-' : ch);
            }

            var sanitized = builder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return "theme";
            }

            return sanitized;
        }
    }
}
