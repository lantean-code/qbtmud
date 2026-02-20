using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Lantean.QBTMud.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Utilities;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Lantean.QBTMud.Pages
{
    public partial class Themes
    {
        private const long MaxThemeUploadBytes = 1024 * 1024;
        private const string ThemeColumnId = "theme";
        private const string DescriptionColumnId = "description";
        private const string ColorsColumnId = "colors";
        private const string ActionsColumnId = "actions";

        private readonly Dictionary<string, RenderFragment<RowContext<ThemeCatalogItem>>> _columnRenderFragments = [];
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
        protected IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

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
            _columnRenderFragments.Add(ThemeColumnId, NameColumn);
            _columnRenderFragments.Add(DescriptionColumnId, DescriptionColumn);
            _columnRenderFragments.Add(ActionsColumnId, ActionsColumn);
            _columnRenderFragments.Add(ColorsColumnId, ColorsColumn);
        }

        protected override async Task OnInitializedAsync()
        {
            await ThemeManagerService.EnsureInitialized();
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

        protected async Task DuplicateTheme(ThemeCatalogItem theme)
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            try
            {
                var defaultName = Translate("%1 Copy", theme.Name);
                var name = await DialogWorkflow.ShowStringFieldDialog(Translate("Duplicate Theme"), Translate("Name"), defaultName);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                var clone = ThemeSerialization.CloneDefinition(theme.Theme);
                clone.Id = Guid.NewGuid().ToString("N");
                clone.Name = name.Trim();

                await ThemeManagerService.SaveLocalTheme(clone);
                NavigateToDetails(clone.Id);
            }
            finally
            {
                _isBusy = false;
            }
        }

        protected async Task ExportTheme(ThemeCatalogItem theme)
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            try
            {
                var definition = ThemeSerialization.CloneDefinition(theme.Theme);
                definition.Id = theme.Id;
                definition.Name = theme.Name;

                var json = ThemeSerialization.SerializeDefinition(definition, writeIndented: true);
                var safeName = SanitizeFileName(theme.Name);
                var dataUrl = BuildJsonDataUrl(json);

                await JSRuntime.FileDownload(dataUrl, $"{safeName}.json");
            }
            finally
            {
                _isBusy = false;
            }
        }

        protected async Task RenameTheme(ThemeCatalogItem theme)
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
                var name = await DialogWorkflow.ShowStringFieldDialog(Translate("Rename Theme"), Translate("Name"), theme.Name);
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                var definition = ThemeSerialization.CloneDefinition(theme.Theme);
                definition.Id = theme.Id;
                definition.Name = name.Trim();

                await ThemeManagerService.SaveLocalTheme(definition);
            }
            finally
            {
                _isBusy = false;
            }
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
                await using var stream = file.OpenReadStream(MaxThemeUploadBytes);
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

        private string SanitizeFileName(string name)
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
                return Translate("theme");
            }

            return sanitized;
        }

        private string Translate(string value, params object[] args)
        {
            return LanguageLocalizer.Translate("AppThemes", value, args);
        }

        private static string BuildJsonDataUrl(string json)
        {
            var escaped = Uri.EscapeDataString(json);
            return $"data:application/json;charset=utf-8,{escaped}";
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
            new ColumnDefinition<ThemeCatalogItem>("Theme", t => t.Name, id: ThemeColumnId),
            new ColumnDefinition<ThemeCatalogItem>("Description", t => t.Theme.Description, id: DescriptionColumnId),
            new ColumnDefinition<ThemeCatalogItem>("Source", t => t.Source.ToString(), id: "source"),
            new ColumnDefinition<ThemeCatalogItem>("Colors", t => t.Name, tdClass: "no-wrap", width: 160, id: ColorsColumnId),
            new ColumnDefinition<ThemeCatalogItem>("Actions", t => t.Name, tdClass: "no-wrap", id: ActionsColumnId)
        ];
    }
}
