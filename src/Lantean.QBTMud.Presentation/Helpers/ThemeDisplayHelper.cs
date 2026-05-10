using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Core.Theming;
using MudBlazor;

namespace Lantean.QBTMud.Helpers
{
    internal static class ThemeDisplayHelper
    {
        public static string GetSourceLabel(ThemeCatalogItem theme, StorageType localThemeStorageType, Func<string, string> translate)
        {
            ArgumentNullException.ThrowIfNull(theme);
            ArgumentNullException.ThrowIfNull(translate);

            return theme.Source switch
            {
                ThemeSource.Local when localThemeStorageType == StorageType.ClientData => translate("Client Data"),
                ThemeSource.Local => translate("Local Storage"),
                ThemeSource.Repository => translate("Repository"),
                _ => translate("Bundled")
            };
        }

        public static Color GetSourceChipColor(ThemeCatalogItem theme)
        {
            ArgumentNullException.ThrowIfNull(theme);

            return theme.Source switch
            {
                ThemeSource.Local => Color.Default,
                ThemeSource.Repository => Color.Secondary,
                _ => Color.Info
            };
        }

        public static IReadOnlyList<ThemePreviewDialogItem> CreatePreviewItems(
            IEnumerable<ThemeCatalogItem> themes,
            StorageType localThemeStorageType,
            Func<string, string> translate)
        {
            ArgumentNullException.ThrowIfNull(themes);
            ArgumentNullException.ThrowIfNull(translate);

            return themes
                .Select(theme => new ThemePreviewDialogItem(
                    theme.Id,
                    theme.Name,
                    GetSourceLabel(theme, localThemeStorageType, translate),
                    ThemeSerialization.CloneTheme(theme.Theme.Theme)))
                .ToList();
        }
    }
}
