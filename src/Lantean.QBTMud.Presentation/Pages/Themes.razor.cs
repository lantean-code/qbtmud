using System.Globalization;
using System.Text.Json;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Core;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Core.Theming;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MudBlazor.Utilities;

namespace Lantean.QBTMud.Pages
{
    public partial class Themes
    {
        private const long _maxThemeUploadBytes = 1024 * 1024;
        private const string _themeColumnId = "theme";
        private const string _descriptionColumnId = "description";
        private const string _sourceColumnId = "source";
        private const string _colorsColumnId = "colors";
        private const string _actionsColumnId = "actions";

        private readonly Dictionary<string, RenderFragment<RowContext<ThemeCatalogItem>>> _columnRenderFragments = [];
        private StorageType _localThemeStorageType = StorageType.LocalStorage;
        private bool _isBusy;

        [Inject]
        protected IThemeManagerService ThemeManagerService { get; set; } = default!;

        [Inject]
        protected IThemeFontCatalog ThemeFontCatalog { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter(Name = "IsDarkMode")]
        public bool IsDarkMode { get; set; }

        protected DynamicTable<ThemeCatalogItem>? Table { get; set; }

        protected IEnumerable<ThemeCatalogItem> ThemeEntries
        {
            get { return ThemeManagerService.Themes; }
        }

        protected ThemeCatalogItem? SelectedThemeEntry
        {
            get
            {
                var currentThemeId = ThemeManagerService.CurrentThemeId;
                if (string.IsNullOrWhiteSpace(currentThemeId))
                {
                    return null;
                }

                return ThemeEntries.FirstOrDefault(theme => string.Equals(theme.Id, currentThemeId, StringComparison.Ordinal));
            }
        }

        protected bool IsBusy
        {
            get { return _isBusy; }
        }

        protected IEnumerable<ColumnDefinition<ThemeCatalogItem>> Columns
        {
            get { return GetColumnDefinitions(); }
        }

        public Themes()
        {
            _columnRenderFragments.Add(_themeColumnId, NameColumn);
            _columnRenderFragments.Add(_descriptionColumnId, DescriptionColumn);
            _columnRenderFragments.Add(_sourceColumnId, SourceColumn);
            _columnRenderFragments.Add(_actionsColumnId, ActionsColumn);
            _columnRenderFragments.Add(_colorsColumnId, ColorsColumn);
        }

        protected override async Task OnInitializedAsync()
        {
            await ThemeManagerService.EnsureInitialized();
            await ThemeManagerService.EnsureRepositoryThemesLoaded();
            _localThemeStorageType = await ThemeManagerService.GetLocalThemeStorageTypeAsync();
            if (ThemeManagerService.LastReloadHadRepositoryIssues)
            {
                SnackbarWorkflow.ShowTransientMessage(Translate("Unable to load theme repository. Showing bundled and local themes only."), Severity.Warning);
            }
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task ReloadThemes()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            try
            {
                await ThemeManagerService.ReloadServerThemes();
                _localThemeStorageType = await ThemeManagerService.GetLocalThemeStorageTypeAsync();
                if (ThemeManagerService.LastReloadHadRepositoryIssues)
                {
                    SnackbarWorkflow.ShowTransientMessage(Translate("Unable to load theme repository. Showing bundled and local themes only."), Severity.Warning);
                }
            }
            finally
            {
                _isBusy = false;
            }
        }

        protected async Task ApplyTheme(ThemeCatalogItem theme)
        {
            if (_isBusy)
            {
                return;
            }

            if (!CanApplyTheme(theme))
            {
                return;
            }

            _isBusy = true;
            try
            {
                await ThemeManagerService.ApplyTheme(theme.Id);
            }
            finally
            {
                _isBusy = false;
            }
        }

        protected async Task PreviewTheme(ThemeCatalogItem theme)
        {
            if (_isBusy)
            {
                return;
            }

            var previewEntries = Table?.GetOrderedItemsSnapshot() ?? ThemeEntries.ToList();

            var request = new ThemePreviewDialogRequest(
                ThemeDisplayHelper.CreatePreviewItems(previewEntries, _localThemeStorageType, value => Translate(value)),
                theme.Id,
                ThemePreviewDialogMode.Catalogue,
                IsDarkMode)
            {
                CurrentThemeId = ThemeManagerService.CurrentThemeId,
                ApplyThemeAsync = ApplyPreviewThemeAsync
            };

            await DialogWorkflow.ShowThemePreviewDialog(request);
        }

        protected async Task DeleteTheme(ThemeCatalogItem theme)
        {
            if (_isBusy)
            {
                return;
            }

            if (!CanEditTheme(theme))
            {
                return;
            }

            _isBusy = true;
            try
            {
                var confirmed = await DialogWorkflow.ShowConfirmDialog(Translate("Delete theme?"), Translate("Delete '%1'?", theme.Name));
                if (!confirmed)
                {
                    return;
                }

                await ThemeManagerService.DeleteLocalTheme(theme.Id);
            }
            finally
            {
                _isBusy = false;
            }
        }

        protected async Task CreateTheme()
        {
            var name = await DialogWorkflow.ShowStringFieldDialog(Translate("New Theme"), Translate("Name"), null);
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var baseTheme = ThemeManagerService.CurrentTheme?.Theme;
            if (baseTheme is null)
            {
                baseTheme = new ThemeDefinition { FontFamily = "Nunito Sans" };
                ThemeFontHelper.ApplyFont(baseTheme, baseTheme.FontFamily);
            }

            var clone = ThemeSerialization.CloneDefinition(baseTheme);
            ThemeFontHelper.ApplyFont(clone, clone.FontFamily);

            clone.Id = Guid.NewGuid().ToString("N");
            clone.Name = name.Trim();
            clone.Description = string.Empty;

            await ThemeManagerService.SaveLocalTheme(clone);
            NavigateToDetails(clone.Id);
        }

        protected async Task ImportThemes(IReadOnlyList<IBrowserFile> files)
        {
            if (_isBusy)
            {
                return;
            }

            if (files.Count == 0)
            {
                return;
            }

            _isBusy = true;
            try
            {
                var file = files[0];
                await using var stream = file.OpenReadStream(_maxThemeUploadBytes);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                var definition = ThemeSerialization.DeserializeDefinition(json);
                if (definition is null)
                {
                    SnackbarWorkflow.ShowTransientMessage(Translate("Unable to import theme: invalid JSON."), Severity.Error);
                    return;
                }

                var normalized = NormalizeImport(definition, file.Name);
                await ThemeManagerService.SaveLocalTheme(normalized);
                NavigateToDetails(normalized.Id);
            }
            catch (IOException exception)
            {
                SnackbarWorkflow.ShowTransientMessage(Translate("Unable to import theme: %1", exception.Message), Severity.Error);
            }
            catch (JsonException exception)
            {
                SnackbarWorkflow.ShowTransientMessage(Translate("Unable to import theme: %1", exception.Message), Severity.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        protected void OpenThemeDetails(ThemeCatalogItem theme)
        {
            if (theme is null)
            {
                return;
            }

            NavigateToDetails(theme.Id);
        }

        protected string? RowClassFunc(ThemeCatalogItem theme, int index)
        {
            if (IsThemeApplied(theme))
            {
                return "theme-row--applied";
            }

            return null;
        }

        protected bool IsThemeApplied(ThemeCatalogItem theme)
        {
            return ThemeManagerService.CurrentThemeId == theme.Id;
        }

        protected bool CanApplyTheme(ThemeCatalogItem theme)
        {
            return !IsThemeApplied(theme);
        }

        protected bool CanEditTheme(ThemeCatalogItem theme)
        {
            return !theme.IsReadOnly;
        }

        protected string GetSourceLabel(ThemeCatalogItem theme)
        {
            return ThemeDisplayHelper.GetSourceLabel(theme, _localThemeStorageType, value => Translate(value));
        }

        protected Color GetSourceChipColor(ThemeCatalogItem theme)
        {
            return ThemeDisplayHelper.GetSourceChipColor(theme);
        }

        private void NavigateToDetails(string themeId)
        {
            var escaped = Uri.EscapeDataString(themeId);
            NavigationManager.NavigateTo($"./themes/{escaped}");
        }

        private IEnumerable<ColumnDefinition<ThemeCatalogItem>> GetColumnDefinitions()
        {
            foreach (var columnDefinition in ColumnsDefinitions)
            {
                if (_columnRenderFragments.TryGetValue(columnDefinition.Id, out var fragment))
                {
                    columnDefinition.RowTemplate = fragment;
                }

                columnDefinition.DisplayHeader = Translate(columnDefinition.Header);
                yield return columnDefinition;
            }
        }

        private ThemeDefinition NormalizeImport(ThemeDefinition definition, string fileName)
        {
            var name = string.IsNullOrWhiteSpace(definition.Name)
                ? Path.GetFileNameWithoutExtension(fileName)
                : definition.Name.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                name = Translate("Imported Theme");
            }

            var id = string.IsNullOrWhiteSpace(definition.Id)
                ? Guid.NewGuid().ToString("N")
                : definition.Id.Trim();

            if (ThemeManagerService.Themes.Any(theme => theme.Id == id))
            {
                id = Guid.NewGuid().ToString("N");
            }

            var theme = definition.Theme ?? new MudTheme();
            var description = string.IsNullOrWhiteSpace(definition.Description) ? string.Empty : definition.Description.Trim();
            var fontFamily = string.IsNullOrWhiteSpace(definition.FontFamily) ? "Nunito Sans" : definition.FontFamily;
            if (!ThemeFontCatalog.TryGetFontUrl(fontFamily, out _))
            {
                fontFamily = "Nunito Sans";
            }

            definition.FontFamily = fontFamily;
            ThemeFontHelper.ApplyFont(definition, fontFamily);

            return new ThemeDefinition
            {
                Id = id,
                Name = name,
                Description = description,
                Theme = theme,
                RTL = definition.RTL,
                FontFamily = definition.FontFamily,
                DefaultBorderRadius = definition.DefaultBorderRadius,
                DefaultElevation = definition.DefaultElevation,
                AppBarElevation = definition.AppBarElevation,
                DrawerElevation = definition.DrawerElevation,
                DrawerClipMode = definition.DrawerClipMode
            };
        }

        private async Task<bool> ApplyPreviewThemeAsync(string themeId)
        {
            if (_isBusy)
            {
                return false;
            }

            var theme = ThemeEntries.FirstOrDefault(entry => string.Equals(entry.Id, themeId, StringComparison.Ordinal));
            if (theme is null || !CanApplyTheme(theme))
            {
                return false;
            }

            _isBusy = true;
            try
            {
                await ThemeManagerService.ApplyTheme(themeId);
                return true;
            }
            finally
            {
                _isBusy = false;
            }
        }

        private string Translate(string value, params object[] args)
        {
            return LanguageLocalizer.Translate("AppThemes", value, args);
        }

        private static string GetSwatchStyle(ThemeCatalogItem theme, ThemePaletteColor colorType, bool useDarkPalette)
        {
            var color = ThemePaletteHelper.GetColor(theme.Theme, colorType, useDarkPalette);
            return $"background-color: {ToCssRgba(color)};";
        }

        private static string ToCssRgba(MudColor color)
        {
            var alpha = color.A / 255d;
            return $"rgba({color.R},{color.G},{color.B},{alpha.ToString(CultureInfo.InvariantCulture)})";
        }

        public static List<ColumnDefinition<ThemeCatalogItem>> ColumnsDefinitions { get; } =
        [
            new ColumnDefinition<ThemeCatalogItem>("Theme", t => t.Name, id: _themeColumnId),
            new ColumnDefinition<ThemeCatalogItem>("Description", t => t.Theme.Description, id: _descriptionColumnId),
            new ColumnDefinition<ThemeCatalogItem>("Source", t => t.Source.ToString(), id: _sourceColumnId),
            new ColumnDefinition<ThemeCatalogItem>("Colors", t => t.Name, tdClass: "no-wrap", width: 160, id: _colorsColumnId),
            new ColumnDefinition<ThemeCatalogItem>("Actions", t => t.Name, tdClass: "no-wrap", id: _actionsColumnId)
        ];
    }
}
