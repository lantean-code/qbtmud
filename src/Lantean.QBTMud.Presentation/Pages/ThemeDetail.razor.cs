using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Core.Theming;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;

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
        private ThemeDefinition? _editorTheme;
        private string _editorName = string.Empty;
        private string _editorDescription = string.Empty;
        private string _editorFont = string.Empty;
        private string? _nameError;
        private string? _fontError;
        private bool _hasChanges;
        private bool _isBusy;
        private bool _isLoading;
        private StorageType _localThemeStorageType = StorageType.LocalStorage;
        private readonly HashSet<string> _loadedFonts = new(StringComparer.OrdinalIgnoreCase);

        [Inject]
        protected IThemeManagerService ThemeManagerService { get; set; } = default!;

        [Inject]
        protected IThemeFontCatalog ThemeFontCatalog { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Parameter]
        public string ThemeId { get; set; } = string.Empty;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter(Name = "IsDarkMode")]
        public bool IsDarkMode { get; set; }

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

        protected bool HasFontError
        {
            get { return !string.IsNullOrWhiteSpace(_fontError); }
        }

        protected bool HasNameError
        {
            get { return !string.IsNullOrWhiteSpace(_nameError); }
        }

        protected string FontError
        {
            get { return _fontError ?? string.Empty; }
        }

        protected string NameError
        {
            get { return _nameError ?? string.Empty; }
        }

        protected string EditorName
        {
            get { return _editorName; }
        }

        protected string EditorFont
        {
            get { return _editorFont; }
        }

        protected string EditorDescription
        {
            get { return _editorDescription; }
        }

        protected string ThemeTitle
        {
            get { return _theme?.Name ?? Translate("Theme Details"); }
        }

        protected IReadOnlyList<(string Title, IReadOnlyList<(string Name, ThemePaletteColor Color)> Items)> ColorGroups
        {
            get { return _colorGroups; }
        }

        protected override async Task OnParametersSetAsync()
        {
            await ThemeManagerService.EnsureInitialized();
            _localThemeStorageType = await ThemeManagerService.GetLocalThemeStorageTypeAsync();
            await LoadTheme();

            if (_theme is null)
            {
                await ThemeManagerService.EnsureRepositoryThemesLoaded();
                _localThemeStorageType = await ThemeManagerService.GetLocalThemeStorageTypeAsync();
                await LoadTheme();
            }
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateTo("./themes");
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

        protected async Task ShowPreview()
        {
            if (_theme is null)
            {
                return;
            }

            var previewTheme = _editorTheme?.Theme ?? _theme.Theme.Theme;
            var selectedThemeId = await DialogWorkflow.ShowThemePreviewDialog(
                [
                    new ThemePreviewDialogItem(
                        _theme.Id,
                        _editorName,
                        ThemeDisplayHelper.GetSourceLabel(_theme, _localThemeStorageType, value => Translate(value)),
                        ThemeSerialization.CloneTheme(previewTheme))
                ],
                _theme.Id,
                ThemePreviewDialogMode.Details,
                IsDarkMode,
                currentThemeId: ThemeManagerService.CurrentThemeId,
                canSaveAndApply: CanEditTheme && !_isBusy && !HasNameError);
            if (string.IsNullOrWhiteSpace(selectedThemeId))
            {
                return;
            }

            await SaveAndApplyFromPreviewAsync();
        }

        protected Task NameChanged(string value)
        {
            _editorName = value;

            _nameError = string.IsNullOrWhiteSpace(value) ? Translate("Name is required.") : null;

            _hasChanges = true;
            return Task.CompletedTask;
        }

        protected Task DescriptionChanged(string value)
        {
            _editorDescription = value;
            _hasChanges = true;
            return Task.CompletedTask;
        }

        protected Task FontChanged(string value)
        {
            if (_editorTheme is null)
            {
                return Task.CompletedTask;
            }

            if (!ThemeFontCatalog.TryGetFontUrl(value, out _))
            {
                _fontError = Translate("Select a valid Google font family.");
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

            return LoadFontPreviews(fonts, cancellationToken);
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

        protected string GetFontPreviewStyle(string fontFamily)
        {
            if (string.IsNullOrWhiteSpace(fontFamily))
            {
                return string.Empty;
            }

            return $"font-family: '{fontFamily}', var(--mud-typography-default-family);";
        }

        protected bool IsThemeApplied(ThemeCatalogItem theme)
        {
            return ThemeManagerService.CurrentThemeId == theme.Id;
        }

        protected string GetReadOnlySourceLabel(ThemeCatalogItem theme)
        {
            return ThemeDisplayHelper.GetSourceLabel(theme, _localThemeStorageType, value => Translate(value));
        }

        protected Color GetSourceChipColor(ThemeCatalogItem theme)
        {
            return ThemeDisplayHelper.GetSourceChipColor(theme);
        }

        protected async Task RenameTheme()
        {
            if (!CanEditTheme || _theme is null || _hasChanges || _isBusy)
            {
                return;
            }

            await RunBusy(async () =>
            {
                var name = await DialogWorkflow.ShowStringFieldDialog(Translate("Rename Theme"), Translate("Name"), _theme.Name);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                var definition = ThemeSerialization.CloneDefinition(_theme.Theme);
                definition.Id = _theme.Id;
                definition.Name = name.Trim();

                await ThemeManagerService.SaveLocalTheme(definition);
                await RefreshTheme();
            });
        }

        protected async Task DuplicateTheme()
        {
            if (_theme is null || _hasChanges || _isBusy)
            {
                return;
            }

            await RunBusy(async () =>
            {
                var clone = await ThemeActionHelper.DuplicateThemeAsync(_theme, DialogWorkflow, (value, args) => Translate(value, args));
                if (clone is null)
                {
                    return;
                }

                await ThemeManagerService.SaveLocalTheme(clone);
                NavigateToThemeDetails(clone.Id);
            });
        }

        protected async Task ExportTheme()
        {
            if (_theme is null || _hasChanges || _isBusy)
            {
                return;
            }

            await RunBusy(() => ThemeActionHelper.ExportThemeAsync(_theme, JSRuntime, (value, args) => Translate(value, args)));
        }

        protected async Task DeleteTheme()
        {
            if (!CanEditTheme || _theme is null || _isBusy)
            {
                return;
            }

            await RunBusy(async () =>
            {
                var confirmed = await DialogWorkflow.ShowConfirmDialog(Translate("Delete theme?"), Translate("Delete '%1'?", [_theme.Name]));
                if (!confirmed)
                {
                    return;
                }

                await ThemeManagerService.DeleteLocalTheme(_theme.Id);
                NavigateBack();
            });
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

        private void NavigateToThemeDetails(string themeId)
        {
            var escaped = Uri.EscapeDataString(themeId);
            NavigationManager.NavigateTo($"./themes/{escaped}");
        }

        private async Task SaveEditsInternal(bool applyAfterSave)
        {
            if (!CanEditTheme || _theme is null || _editorTheme is null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_editorName))
            {
                _nameError = Translate("Name is required.");
                return;
            }

            await RunBusy(async () =>
            {
                var definition = new ThemeDefinition
                {
                    Id = _theme.Id,
                    Name = _editorName.Trim(),
                    Description = string.IsNullOrWhiteSpace(_editorDescription) ? string.Empty : _editorDescription.Trim(),
                    Theme = _editorTheme.Theme,
                    RTL = _editorTheme.RTL,
                    FontFamily = _editorTheme.FontFamily,
                    DefaultBorderRadius = _editorTheme.DefaultBorderRadius,
                    DefaultElevation = _editorTheme.DefaultElevation,
                    AppBarElevation = _editorTheme.AppBarElevation,
                    DrawerElevation = _editorTheme.DrawerElevation,
                    DrawerClipMode = _editorTheme.DrawerClipMode
                };

                await ThemeManagerService.SaveLocalTheme(definition);
                await RefreshTheme();

                if (applyAfterSave)
                {
                    await ThemeManagerService.ApplyTheme(_theme.Id);
                }
            });
        }

        private async Task<bool> SaveAndApplyFromPreviewAsync()
        {
            if (!CanEditTheme || _theme is null || _editorTheme is null || HasNameError)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_editorName))
            {
                _nameError = Translate("Name is required.");
                return false;
            }

            await SaveEditsInternal(true);
            return true;
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
            _editorTheme = ThemeSerialization.CloneDefinition(theme.Theme);
            _editorName = theme.Name;
            _editorDescription = _editorTheme.Description;

            var fontFamily = string.IsNullOrWhiteSpace(_editorTheme.FontFamily) ? "Nunito Sans" : _editorTheme.FontFamily;
            if (!ThemeFontCatalog.TryGetFontUrl(fontFamily, out _))
            {
                fontFamily = "Nunito Sans";
            }

            ThemeFontHelper.ApplyFont(_editorTheme, fontFamily);
            _editorFont = fontFamily;
            _nameError = null;
            _fontError = null;
            _hasChanges = false;
        }

        private async Task<IEnumerable<string>> LoadFontPreviews(IEnumerable<string> fonts, CancellationToken cancellationToken)
        {
            var results = fonts.ToList();
            foreach (var font in results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_loadedFonts.Contains(font))
                {
                    continue;
                }

                if (!ThemeFontCatalog.TryGetFontUrl(font, out var url))
                {
                    continue;
                }

                _loadedFonts.Add(font);
                var fontId = ThemeFontCatalog.BuildFontId(font);
                await JSRuntime.LoadGoogleFont(url, fontId);
            }

            return results;
        }

        private string Translate(string value)
        {
            return LanguageLocalizer.Translate("AppThemeDetail", value);
        }

        private string Translate(string value, params object[] args)
        {
            return LanguageLocalizer.Translate("AppThemeDetail", value, args);
        }
    }
}
