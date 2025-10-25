using System.Linq;
using System.Net.Http;
using Lantean.QBitTorrentClient;
using Lantean.QBitTorrentClient.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Components.Dialogs
{
    public partial class SearchPluginsDialog
    {
        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

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
                var response = await ApiClient.GetSearchPlugins();
                Plugins = response is null ? [] : response.ToList();
                SelectedPluginNames = [];
            }
            catch (Exception exception)
            {
                Snackbar.Add($"Failed to load search plugins: {exception.Message}", Severity.Error);
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

            var success = await RunOperation(() => ApiClient.InstallSearchPlugins(source), "Plugin install queued.", true);
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

            var success = await RunOperation(() => ApiClient.InstallSearchPlugins(source), "Plugin install queued.", true);
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
            await RunOperation(() => ApiClient.EnableSearchPlugins(names), $"Enabled {names.Length} plugin(s).");
        }

        protected async Task DisableSelected()
        {
            if (!HasSelection)
            {
                return;
            }

            var names = SelectedPluginNames.ToArray();
            await RunOperation(() => ApiClient.DisableSearchPlugins(names), $"Disabled {names.Length} plugin(s).");
        }

        protected async Task UninstallSelected()
        {
            if (!HasSelection)
            {
                return;
            }

            var names = SelectedPluginNames.ToArray();
            await RunOperation(() => ApiClient.UninstallSearchPlugins(names), $"Removed {names.Length} plugin(s).");
        }

        protected async Task UpdateAll()
        {
            if (Plugins.Count == 0)
            {
                return;
            }

            await RunOperation(() => ApiClient.UpdateSearchPlugins(), "Plugin update queued.");
        }

        protected async Task TogglePlugin(SearchPlugin plugin, bool enable)
        {
            if (OperationInProgress)
            {
                return;
            }

            var previous = plugin.Enabled;
            plugin.Enabled = enable;

            var success = await RunOperation(
                enable
                    ? () => ApiClient.EnableSearchPlugins(plugin.Name)
                    : () => ApiClient.DisableSearchPlugins(plugin.Name),
                enable ? $"Enabled {plugin.FullName}." : $"Disabled {plugin.FullName}.",
                false);

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

        protected string GetEnabledIcon(SearchPlugin plugin)
        {
            return plugin.Enabled ? Icons.Material.Filled.ToggleOn : Icons.Material.Outlined.ToggleOff;
        }

        protected Color GetEnabledColor(SearchPlugin plugin)
        {
            return plugin.Enabled ? Color.Success : Color.Default;
        }

        protected string GetLastUpdatedText(SearchPlugin plugin)
        {
            // qBittorrent's search/plugins API does not expose the last update timestamp,
            // so we display a placeholder until the client model is extended.
            return "Not available";
        }

        protected void CloseDialog()
        {
            MudDialog.Close(DialogResult.Ok(_hasChanges));
        }

        private async Task<bool> RunOperation(Func<Task> operation, string successMessage, bool refresh = true)
        {
            OperationInProgress = true;
            try
            {
                await operation();
                Snackbar.Add(successMessage, Severity.Success);
                _hasChanges = true;
                if (refresh)
                {
                    await LoadPlugins();
                }
                return true;
            }
            catch (HttpRequestException exception)
            {
                Snackbar.Add($"Search plugin operation failed: {exception.Message}", Severity.Error);
            }
            catch (InvalidOperationException exception)
            {
                Snackbar.Add($"Search plugin operation failed: {exception.Message}", Severity.Error);
            }
            finally
            {
                OperationInProgress = false;
            }

            return false;
        }
    }
}
