using System.Diagnostics;
using System.Text.Json;
using Lantean.QBTMud.Application.Services;
using Lantean.QBTMud.Application.Services.Localization;
using Lantean.QBTMud.Core;
using Lantean.QBTMud.Core.Models;
using Lantean.QBTMud.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMud.Components.AppSettingsTabs
{
    public partial class StorageAppSettingsTab
    {
        private readonly HashSet<string> _expandedStorageOverrideGroups = new(StringComparer.Ordinal);

        [Parameter]
        [EditorRequired]
        public StorageRoutingSettings StorageRoutingSettings { get; set; } = default!;

        [Parameter]
        public bool IsActive { get; set; }

        [Parameter]
        public int ReloadToken { get; set; }

        [Parameter]
        public EventCallback StorageRoutingChanged { get; set; }

        [Parameter]
        public EventCallback<bool> BusyChanged { get; set; }

        [Inject]
        protected IStorageDiagnosticsService StorageDiagnosticsService { get; set; } = default!;

        [Inject]
        protected IStorageCatalogService StorageCatalogService { get; set; } = default!;

        [Inject]
        protected IWebApiCapabilityService WebApiCapabilityService { get; set; } = default!;

        [Inject]
        protected ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        protected IReadOnlyList<AppStorageEntry> StorageEntries { get; private set; } = Array.Empty<AppStorageEntry>();

        protected IReadOnlyList<StorageCatalogGroupDefinition> StorageGroups => StorageCatalogService.Groups;

        protected bool SupportsClientData => WebApiCapabilityState.SupportsClientData;

        protected bool IsStorageBusy { get; private set; }

        protected bool IsLoadingInitialStorageEntries => IsActive && !_hasLoadedInitialStorageEntries && IsStorageBusy;

        protected WebApiCapabilityState WebApiCapabilityState { get; private set; } = new(rawWebApiVersion: null, parsedWebApiVersion: null, supportsClientData: false);

        private int _loadedReloadToken = -1;
        private bool _hasLoadedCapabilityState;
        private bool _hasLoadedInitialStorageEntries;

        protected override async Task OnParametersSetAsync()
        {
            if (!IsActive)
            {
                return;
            }

            var reloadChanged = _loadedReloadToken != ReloadToken;
            if (reloadChanged)
            {
                _loadedReloadToken = ReloadToken;
            }

            if (!_hasLoadedCapabilityState || reloadChanged)
            {
                WebApiCapabilityState = await GetWebApiCapabilityStateAsync();
                _hasLoadedCapabilityState = true;
            }

            if (!_hasLoadedInitialStorageEntries || reloadChanged)
            {
                await RefreshStorageEntriesAsync();
                _hasLoadedInitialStorageEntries = true;
            }
        }

        protected async Task OnMasterStorageTypeChanged(StorageType storageType)
        {
            if (StorageRoutingSettings.MasterStorageType == storageType
                && StorageRoutingSettings.GroupStorageTypes.Count == 0
                && StorageRoutingSettings.ItemStorageTypes.Count == 0)
            {
                return;
            }

            StorageRoutingSettings.MasterStorageType = storageType;
            StorageRoutingSettings.GroupStorageTypes.Clear();
            StorageRoutingSettings.ItemStorageTypes.Clear();

            await NotifyStorageRoutingChangedAsync();
        }

        protected StorageType GetGroupStorageTypeValue(string groupId)
        {
            if (StorageRoutingSettings.GroupStorageTypes.TryGetValue(groupId, out var storageType))
            {
                return storageType;
            }

            return StorageRoutingSettings.MasterStorageType;
        }

        protected async Task OnGroupStorageTypeChanged(string groupId, StorageType storageType)
        {
            var group = StorageGroups.FirstOrDefault(candidate => string.Equals(candidate.Id, groupId, StringComparison.Ordinal));
            var clearedOverrides = group is null
                ? 0
                : ClearGroupItemOverrides(group);

            var storageTypeChanged = false;
            if (storageType == StorageRoutingSettings.MasterStorageType)
            {
                storageTypeChanged = StorageRoutingSettings.GroupStorageTypes.Remove(groupId);
            }
            else if (!StorageRoutingSettings.GroupStorageTypes.TryGetValue(groupId, out var configuredStorageType)
                || configuredStorageType != storageType)
            {
                StorageRoutingSettings.GroupStorageTypes[groupId] = storageType;
                storageTypeChanged = true;
            }

            if (!storageTypeChanged && clearedOverrides == 0)
            {
                return;
            }

            await NotifyStorageRoutingChangedAsync();

            if (group is null)
            {
                return;
            }

            var groupName = TranslateSettings(group.DisplayNameSource);
            if (clearedOverrides > 0)
            {
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Applied to all items in %1; cleared %2 item overrides.", groupName, clearedOverrides), Severity.Info);
                return;
            }

            SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Applied to all items in %1.", groupName), Severity.Info);
        }

        protected StorageType GetItemStorageTypeValue(StorageCatalogItemDefinition item)
        {
            if (StorageRoutingSettings.ItemStorageTypes.TryGetValue(item.Id, out var storageType))
            {
                return storageType;
            }

            return GetGroupStorageTypeValue(item.GroupId);
        }

        protected async Task OnItemStorageTypeChanged(StorageCatalogItemDefinition item, StorageType storageType)
        {
            var groupStorageType = GetGroupStorageTypeValue(item.GroupId);
            if (storageType == groupStorageType)
            {
                if (StorageRoutingSettings.ItemStorageTypes.Remove(item.Id))
                {
                    await NotifyStorageRoutingChangedAsync();
                }

                return;
            }

            if (StorageRoutingSettings.ItemStorageTypes.TryGetValue(item.Id, out var configuredValue)
                && configuredValue == storageType)
            {
                return;
            }

            StorageRoutingSettings.ItemStorageTypes[item.Id] = storageType;
            await NotifyStorageRoutingChangedAsync();
        }

        protected int GetGroupOverrideCount(StorageCatalogGroupDefinition group)
        {
            return group.Items.Count(item => StorageRoutingSettings.ItemStorageTypes.ContainsKey(item.Id));
        }

        protected bool IsGroupOverridesExpanded(string groupId)
        {
            return _expandedStorageOverrideGroups.Contains(groupId);
        }

        protected void SetGroupOverridesExpanded(string groupId, bool expanded)
        {
            if (string.IsNullOrWhiteSpace(groupId))
            {
                return;
            }

            if (expanded)
            {
                _expandedStorageOverrideGroups.Add(groupId);
            }
            else
            {
                _expandedStorageOverrideGroups.Remove(groupId);
            }
        }

        protected string GetGroupOverridesToggleText(StorageCatalogGroupDefinition group)
        {
            var groupName = TranslateSettings(group.DisplayNameSource);
            return IsGroupOverridesExpanded(group.Id)
                ? TranslateSettings("Hide %1 storage overrides", groupName)
                : TranslateSettings("Show %1 storage overrides", groupName);
        }

        protected async Task OnClearGroupOverrides(string groupId)
        {
            var group = StorageGroups.FirstOrDefault(candidate => string.Equals(candidate.Id, groupId, StringComparison.Ordinal));
            if (group is null)
            {
                return;
            }

            var clearedOverrides = ClearGroupItemOverrides(group);
            if (clearedOverrides == 0)
            {
                return;
            }

            await NotifyStorageRoutingChangedAsync();

            SnackbarWorkflow.ShowTransientMessage(
                TranslateSettings("Cleared %1 item overrides in %2.", clearedOverrides, TranslateSettings(group.DisplayNameSource)),
                Severity.Info);
        }

        protected string GetStorageTypeDisplayText(StorageType storageType)
        {
            return GetStorageTypeOptionText(storageType);
        }

        protected string GetStorageTypeOptionText(StorageType storageType)
        {
            return storageType switch
            {
                StorageType.ClientData => TranslateSettings("qBittorrent client data"),
                _ => TranslateSettings("Browser local storage")
            };
        }

        protected string GetWebApiVersionText()
        {
            if (string.IsNullOrWhiteSpace(WebApiCapabilityState.RawWebApiVersion))
            {
                return TranslateSettings("Unavailable");
            }

            return WebApiCapabilityState.RawWebApiVersion;
        }

        protected string GetClientDataSupportText()
        {
            return SupportsClientData
                ? TranslateSettings("Supported")
                : TranslateSettings("Not supported");
        }

        protected Color GetClientDataSupportColor()
        {
            return SupportsClientData
                ? Color.Success
                : Color.Warning;
        }

        protected async Task RefreshStorageEntriesAsync()
        {
            if (IsStorageBusy)
            {
                return;
            }

            await SetStorageBusyAsync(true);
            await InvokeAsync(StateHasChanged);

            try
            {
                StorageEntries = await StorageDiagnosticsService.GetEntriesAsync();
            }
            catch (HttpRequestException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to load storage entries."), Severity.Error);
            }
            catch (JsonException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to load storage entries."), Severity.Error);
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to load storage entries."), Severity.Error);
            }
            catch (JSException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to load storage entries."), Severity.Error);
            }
            finally
            {
                await SetStorageBusyAsync(false);
            }
        }

        protected async Task RemoveStorageEntryAsync(StorageType storageType, string key)
        {
            if (IsStorageBusy)
            {
                return;
            }

            await SetStorageBusyAsync(true);

            try
            {
                await StorageDiagnosticsService.RemoveEntryAsync(storageType, key);
                StorageEntries = await StorageDiagnosticsService.GetEntriesAsync();
            }
            catch (HttpRequestException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to remove storage entry."), Severity.Error);
            }
            catch (JsonException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to remove storage entry."), Severity.Error);
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to remove storage entry."), Severity.Error);
            }
            catch (JSException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to remove storage entry."), Severity.Error);
            }
            finally
            {
                await SetStorageBusyAsync(false);
            }
        }

        protected async Task ClearStorageEntriesAsync()
        {
            if (IsStorageBusy)
            {
                return;
            }

            await SetStorageBusyAsync(true);

            try
            {
                var removed = await StorageDiagnosticsService.ClearEntriesAsync();
                StorageEntries = await StorageDiagnosticsService.GetEntriesAsync();
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Removed %1 storage entries.", removed), Severity.Info);
                NavigationManager.NavigateToHome(forceLoad: true);
            }
            catch (HttpRequestException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to clear storage entries."), Severity.Error);
            }
            catch (JsonException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to clear storage entries."), Severity.Error);
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to clear storage entries."), Severity.Error);
            }
            catch (JSException exception)
            {
                Debug.WriteLine(exception);
                SnackbarWorkflow.ShowTransientMessage(TranslateSettings("Unable to clear storage entries."), Severity.Error);
            }
            finally
            {
                await SetStorageBusyAsync(false);
            }
        }

        private async Task SetStorageBusyAsync(bool value)
        {
            if (IsStorageBusy == value)
            {
                return;
            }

            IsStorageBusy = value;
            await BusyChanged.InvokeAsync(value);
        }

        private async Task NotifyStorageRoutingChangedAsync()
        {
            await StorageRoutingChanged.InvokeAsync();
        }

        private async Task<WebApiCapabilityState> GetWebApiCapabilityStateAsync()
        {
            try
            {
                return await WebApiCapabilityService.GetCapabilityStateAsync();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                return new WebApiCapabilityState(rawWebApiVersion: null, parsedWebApiVersion: null, supportsClientData: false);
            }
            catch (JsonException)
            {
                return new WebApiCapabilityState(rawWebApiVersion: null, parsedWebApiVersion: null, supportsClientData: false);
            }
            catch (InvalidOperationException)
            {
                return new WebApiCapabilityState(rawWebApiVersion: null, parsedWebApiVersion: null, supportsClientData: false);
            }
        }

        private int ClearGroupItemOverrides(StorageCatalogGroupDefinition group)
        {
            var itemIdsWithOverrides = group.Items
                .Where(item => StorageRoutingSettings.ItemStorageTypes.ContainsKey(item.Id))
                .Select(item => item.Id)
                .ToList();

            foreach (var itemId in itemIdsWithOverrides)
            {
                _ = StorageRoutingSettings.ItemStorageTypes.Remove(itemId);
            }

            return itemIdsWithOverrides.Count;
        }

        private string TranslateSettings(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate("AppSettings", source, arguments);
        }
    }
}
