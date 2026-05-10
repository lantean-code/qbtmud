using System.Text;
using Lantean.QBTMud.Core.Interop;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Core.Theming;
using Lantean.QBTMud.Services;
using Microsoft.JSInterop;

namespace Lantean.QBTMud.Helpers
{
    internal static class ThemeActionHelper
    {
        public static async Task<ThemeDefinition?> DuplicateThemeAsync(ThemeCatalogItem theme, IDialogWorkflow dialogWorkflow, Func<string, object[], string> translate)
        {
            ArgumentNullException.ThrowIfNull(theme);
            ArgumentNullException.ThrowIfNull(dialogWorkflow);
            ArgumentNullException.ThrowIfNull(translate);

            var defaultName = translate("%1 Copy", [theme.Name]);
            var name = await dialogWorkflow.ShowStringFieldDialog(translate("Duplicate Theme", []), translate("Name", []), defaultName);
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var clone = ThemeSerialization.CloneDefinition(theme.Theme);
            clone.Id = Guid.NewGuid().ToString("N");
            clone.Name = name.Trim();
            return clone;
        }

        public static async Task ExportThemeAsync(ThemeCatalogItem theme, IJSRuntime jsRuntime, Func<string, object[], string> translate)
        {
            ArgumentNullException.ThrowIfNull(theme);
            ArgumentNullException.ThrowIfNull(jsRuntime);
            ArgumentNullException.ThrowIfNull(translate);

            var definition = ThemeSerialization.CloneDefinition(theme.Theme);
            definition.Id = theme.Id;
            definition.Name = theme.Name;

            var json = ThemeSerialization.SerializeDefinition(definition, writeIndented: true);
            var safeName = SanitizeFileName(theme.Name, translate);
            var dataUrl = BuildJsonDataUrl(json);

            await jsRuntime.FileDownload(dataUrl, $"{safeName}.json");
        }

        private static string SanitizeFileName(string name, Func<string, object[], string> translate)
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
                return translate("theme", []);
            }

            return sanitized;
        }

        private static string BuildJsonDataUrl(string json)
        {
            var escaped = Uri.EscapeDataString(json);
            return $"data:application/json;charset=utf-8,{escaped}";
        }
    }
}
