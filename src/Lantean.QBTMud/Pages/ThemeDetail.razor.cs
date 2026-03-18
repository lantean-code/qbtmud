using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Theming;
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
                    ("Black", ThemePaletteColor.Black),
                    ("White", ThemePaletteColor.White),
                    ("Primary", ThemePaletteColor.Primary),
                    ("Primary Contrast Text", ThemePaletteColor.PrimaryContrastText),
                    ("Secondary", ThemePaletteColor.Secondary),
                    ("Secondary Contrast Text", ThemePaletteColor.SecondaryContrastText),
                    ("Tertiary", ThemePaletteColor.Tertiary),
                    ("Tertiary Contrast Text", ThemePaletteColor.TertiaryContrastText),
                    ("Info", ThemePaletteColor.Info),
                    ("Info Contrast Text", ThemePaletteColor.InfoContrastText),
                    ("Success", ThemePaletteColor.Success),
                    ("Success Contrast Text", ThemePaletteColor.SuccessContrastText),
                    ("Warning", ThemePaletteColor.Warning),
                    ("Warning Contrast Text", ThemePaletteColor.WarningContrastText),
                    ("Error", ThemePaletteColor.Error),
                    ("Error Contrast Text", ThemePaletteColor.ErrorContrastText),
                    ("Dark", ThemePaletteColor.Dark),
                    ("Dark Contrast Text", ThemePaletteColor.DarkContrastText)
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
                    ("Table Lines", ThemePaletteColor.TableLines),
                    ("Table Striped", ThemePaletteColor.TableStriped),
                    ("Table Hover", ThemePaletteColor.TableHover),
                    ("Divider", ThemePaletteColor.Divider),
                    ("Divider Light", ThemePaletteColor.DividerLight),
                    ("Skeleton", ThemePaletteColor.Skeleton)
                }
            ),
            (
                "Neutral Scale",
                new List<(string, ThemePaletteColor)>
                {
                    ("Gray Default", ThemePaletteColor.GrayDefault),
                    ("Gray Light", ThemePaletteColor.GrayLight),
                    ("Gray Lighter", ThemePaletteColor.GrayLighter),
                    ("Gray Dark", ThemePaletteColor.GrayDark),
                    ("Gray Darker", ThemePaletteColor.GrayDarker),
                    ("Overlay Dark", ThemePaletteColor.OverlayDark),
                    ("Overlay Light", ThemePaletteColor.OverlayLight)
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

        private static readonly IReadOnlyList<(string Title, IReadOnlyList<(string Name, ThemePaletteDerivedColor Color)> Items)> _derivedColorGroups =
        [
            (
                "Derived Colors",
                new List<(string, ThemePaletteDerivedColor)>
                {
                    ("Primary Darken", ThemePaletteDerivedColor.PrimaryDarken),
                    ("Primary Lighten", ThemePaletteDerivedColor.PrimaryLighten),
                    ("Secondary Darken", ThemePaletteDerivedColor.SecondaryDarken),
                    ("Secondary Lighten", ThemePaletteDerivedColor.SecondaryLighten),
                    ("Tertiary Darken", ThemePaletteDerivedColor.TertiaryDarken),
                    ("Tertiary Lighten", ThemePaletteDerivedColor.TertiaryLighten),
                    ("Info Darken", ThemePaletteDerivedColor.InfoDarken),
                    ("Info Lighten", ThemePaletteDerivedColor.InfoLighten),
                    ("Success Darken", ThemePaletteDerivedColor.SuccessDarken),
                    ("Success Lighten", ThemePaletteDerivedColor.SuccessLighten),
                    ("Warning Darken", ThemePaletteDerivedColor.WarningDarken),
                    ("Warning Lighten", ThemePaletteDerivedColor.WarningLighten),
                    ("Error Darken", ThemePaletteDerivedColor.ErrorDarken),
                    ("Error Lighten", ThemePaletteDerivedColor.ErrorLighten),
                    ("Dark Darken", ThemePaletteDerivedColor.DarkDarken),
                    ("Dark Lighten", ThemePaletteDerivedColor.DarkLighten)
                }
            )
        ];

        private static readonly IReadOnlyList<(string Title, IReadOnlyList<(string Name, ThemePaletteScalar Scalar, double Min, double Max, double Step)> Items)> _scalarGroups =
        [
            (
                "Effects",
                new List<(string, ThemePaletteScalar, double, double, double)>
                {
                    ("Border Opacity", ThemePaletteScalar.BorderOpacity, 0, 1, 0.01),
                    ("Hover Opacity", ThemePaletteScalar.HoverOpacity, 0, 1, 0.01),
                    ("Ripple Opacity", ThemePaletteScalar.RippleOpacity, 0, 1, 0.01),
                    ("Ripple Opacity Secondary", ThemePaletteScalar.RippleOpacitySecondary, 0, 1, 0.01)
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

        protected IReadOnlyList<(string Title, IReadOnlyList<(string Name, ThemePaletteDerivedColor Color)> Items)> DerivedColorGroups
        {
            get { return _derivedColorGroups; }
        }

        protected IReadOnlyList<(string Title, IReadOnlyList<(string Name, ThemePaletteScalar Scalar, double Min, double Max, double Step)> Items)> ScalarGroups
        {
            get { return _scalarGroups; }
        }

        protected override async Task OnParametersSetAsync()
        {
            await ThemeManagerService.EnsureInitialized();
            await LoadTheme();

            if (_theme is null)
            {
                await ThemeManagerService.EnsureRepositoryThemesLoaded();
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
            await DialogWorkflow.ShowThemePreviewDialog(previewTheme, IsDarkMode);
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

        protected ThemeDerivedColorState GetDerivedPaletteColor(ThemePaletteDerivedColor colorType, bool useDarkPalette)
        {
            if (_editorTheme is null)
            {
                return new ThemeDerivedColorState(new MudColor("#000000"), true);
            }

            return ThemePaletteDerivedColorHelper.GetColor(_editorTheme, colorType, useDarkPalette);
        }

        protected void UpdateDerivedPaletteColor(ThemePaletteDerivedColor colorType, MudColor value, bool useDarkPalette)
        {
            if (_editorTheme is null)
            {
                return;
            }

            ThemePaletteDerivedColorHelper.SetColor(_editorTheme, colorType, value.ToString(MudColorOutputFormats.RGB), useDarkPalette);
            _hasChanges = true;
        }

        protected void ResetDerivedPaletteColor(ThemePaletteDerivedColor colorType, bool useDarkPalette)
        {
            if (_editorTheme is null)
            {
                return;
            }

            ThemePaletteDerivedColorHelper.ResetColor(_editorTheme, colorType, useDarkPalette);
            _hasChanges = true;
        }

        protected double GetScalarValue(ThemePaletteScalar scalarType, bool useDarkPalette)
        {
            if (_editorTheme is null)
            {
                return 0;
            }

            return ThemePaletteScalarHelper.GetValue(_editorTheme, scalarType, useDarkPalette);
        }

        protected void UpdateScalarValue(ThemePaletteScalar scalarType, double value, bool useDarkPalette)
        {
            if (_editorTheme is null)
            {
                return;
            }

            ThemePaletteScalarHelper.SetValue(_editorTheme, scalarType, value, useDarkPalette);
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
            return theme.Source == ThemeSource.Repository
                ? Translate("Repository")
                : Translate("Bundled");
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
    }
}
