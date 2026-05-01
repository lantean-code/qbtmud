using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QBittorrent.ApiClient;
using QBittorrent.ApiClient.Models;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class SearchPluginsDialog
    {
        private const string _appContext = "AppTemp";

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected IApiFeedbackWorkflow ApiFeedbackWorkflow { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [CascadingParameter]
        private IMudDialogInstance MudDialog { get; set; } = default!;

        protected List<SearchPlugin> Plugins { get; set; } = [];

        protected HashSet<string> SelectedPluginNames { get; set; } = [];

        protected string? InstallUrl { get; set; }

        protected string? InstallLocalPath { get; set; }

        protected bool OperationInProgress { get; set; }

        private bool _hasChanges;
        private bool _loading;

        protected bool IsBusy => _loading || OperationInProgress;

        protected bool HasSelection => SelectedPluginNames.Count > 0;

        protected override async Task OnInitializedAsync()
        {
            await LoadPlugins();
        }

        private async Task LoadPlugins()
        {
            _loading = true;
            try
            {
                var pluginsResult = await ApiClient.GetSearchPluginsAsync();
                if (pluginsResult.IsFailure)
                {
                    Plugins = [];
                    SelectedPluginNames = [];
                    await ApiFeedbackWorkflow.HandleFailureAsync(
                        pluginsResult,
                        message => TranslateApp("Failed to load search plugins: %1", message ?? string.Empty));
                    return;
                }

                var plugins = pluginsResult.Value;
                Plugins = [.. plugins];
                SelectedPluginNames = [];
            }
            finally
            {
                _loading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task InstallFromUrl()
        {
            var source = InstallUrl?.Trim();
            if (string.IsNullOrEmpty(source))
            {
                return;
            }

            var success = await RunOperation(() => ApiClient.InstallSearchPluginsAsync(sources: [source]), () => TranslateApp("Plugin install queued."), true);
            if (success)
            {
                InstallUrl = string.Empty;
            }
        }

        protected async Task InstallFromPath()
        {
            var source = InstallLocalPath?.Trim();
            if (string.IsNullOrEmpty(source))
            {
                return;
            }

            var success = await RunOperation(() => ApiClient.InstallSearchPluginsAsync(sources: [source]), () => TranslateApp("Plugin install queued."), true);
            if (success)
            {
                InstallLocalPath = string.Empty;
            }
        }

        protected async Task EnableSelected()
        {
            if (!HasSelection)
            {
                return;
            }

            var names = SelectedPluginNames.ToArray();
            await RunOperation(() => ApiClient.EnableSearchPluginsAsync(names: names), () => TranslateApp("Enabled %1 plugin(s).", names.Length));
        }

        protected async Task DisableSelected()
        {
            if (!HasSelection)
            {
                return;
            }

            var names = SelectedPluginNames.ToArray();
            await RunOperation(() => ApiClient.DisableSearchPluginsAsync(names: names), () => TranslateApp("Disabled %1 plugin(s).", names.Length));
        }

        protected async Task UninstallSelected()
        {
            if (!HasSelection)
            {
                return;
            }

            var names = SelectedPluginNames.ToArray();
            await RunOperation(() => ApiClient.UninstallSearchPluginsAsync(names: names), () => TranslateApp("Removed %1 plugin(s).", names.Length));
        }

        protected async Task UpdateAll()
        {
            if (Plugins.Count == 0)
            {
                return;
            }

            await RunOperation(() => ApiClient.UpdateSearchPluginsAsync(), () => TranslateApp("Plugin update queued."));
        }

        protected async Task TogglePlugin(SearchPlugin plugin, bool enable)
        {
            if (OperationInProgress)
            {
                return;
            }

            var previous = plugin.Enabled;
            plugin.Enabled = enable;

            bool success;
            if (enable)
            {
                success = await RunOperation(() => ApiClient.EnableSearchPluginsAsync([plugin.Name]), () => TranslateApp("Enabled %1.", plugin.FullName), false);
            }
            else
            {
                success = await RunOperation(() => ApiClient.DisableSearchPluginsAsync([plugin.Name]), () => TranslateApp("Disabled %1.", plugin.FullName), false);
            }

            if (!success)
            {
                plugin.Enabled = previous;
            }
        }

        protected async Task RefreshPlugins()
        {
            await LoadPlugins();
        }

        protected bool IsSelected(SearchPlugin plugin)
        {
            return SelectedPluginNames.Contains(plugin.Name);
        }

        protected void ToggleSelection(SearchPlugin plugin)
        {
            if (!SelectedPluginNames.Add(plugin.Name))
            {
                SelectedPluginNames.Remove(plugin.Name);
            }

            StateHasChanged();
        }

        protected string GetSelectionIcon(SearchPlugin plugin)
        {
            return IsSelected(plugin) ? Icons.Material.Filled.CheckBox : Icons.Material.Outlined.CheckBoxOutlineBlank;
        }

        protected Color GetSelectionColor(SearchPlugin plugin)
        {
            return IsSelected(plugin) ? Color.Primary : Color.Default;
        }

        protected static string GetEnabledIcon(SearchPlugin plugin)
        {
            return plugin.Enabled ? Icons.Material.Filled.ToggleOn : Icons.Material.Outlined.ToggleOff;
        }

        protected static Color GetEnabledColor(SearchPlugin plugin)
        {
            return plugin.Enabled ? Color.Success : Color.Default;
        }

        protected void CloseDialog()
        {
            MudDialog.Close(DialogResult.Ok(_hasChanges));
        }

        private async Task<bool> RunOperation(Func<Task<ApiResult>> operation, Func<string> successMessage, bool refresh = true)
        {
            OperationInProgress = true;
            var result = await operation();
            if (result.IsFailure)
            {
                await ApiFeedbackWorkflow.HandleFailureAsync(
                    result,
                    message => TranslateApp("Search plugin operation failed: %1", message ?? string.Empty));
                OperationInProgress = false;
                return false;
            }

            SnackbarWorkflow.ShowTransientMessage(successMessage(), Severity.Success);
            _hasChanges = true;
            if (refresh)
            {
                await LoadPlugins();
            }

            OperationInProgress = false;
            return true;
        }

        private string TranslateApp(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate(_appContext, source, arguments);
        }
    }
}
