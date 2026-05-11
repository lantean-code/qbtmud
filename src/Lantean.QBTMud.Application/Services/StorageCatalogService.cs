using Lantean.QBTMud.Core.Models;

namespace Lantean.QBTMud.Application.Services
{
    /// <summary>
    /// Default implementation of <see cref="IStorageCatalogService"/>.
    /// </summary>
    public sealed class StorageCatalogService : IStorageCatalogService
    {
        private readonly IReadOnlyList<StorageCatalogGroupDefinition> _groups;
        private readonly IReadOnlyList<StorageCatalogItemDefinition> _items;
        private readonly IReadOnlyDictionary<string, StorageCatalogItemDefinition> _exactMatchLookup;
        private readonly IReadOnlyList<StorageCatalogItemDefinition> _prefixItems;

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageCatalogService"/> class.
        /// </summary>
        public StorageCatalogService()
        {
            _groups = BuildGroups();
            _items = _groups
                .SelectMany(group => group.Items)
                .ToList();
            _exactMatchLookup = _items
                .Where(item => item.MatchMode == StorageCatalogItemMatchMode.ExactKey)
                .ToDictionary(item => item.MatchPattern, item => item, StringComparer.Ordinal);
            _prefixItems = _items
                .Where(item => item.MatchMode == StorageCatalogItemMatchMode.PrefixPattern)
                .OrderByDescending(item => item.MatchPattern.Length)
                .ToList();
        }

        /// <inheritdoc />
        public IReadOnlyList<StorageCatalogGroupDefinition> Groups
        {
            get
            {
                return _groups;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<StorageCatalogItemDefinition> Items
        {
            get
            {
                return _items;
            }
        }

        /// <inheritdoc />
        public StorageCatalogItemDefinition? MatchItemByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            var normalizedKey = key.Trim();
            if (_exactMatchLookup.TryGetValue(normalizedKey, out var exactMatch))
            {
                return exactMatch;
            }

            if (string.Equals(normalizedKey, AppSettings.LegacyStorageKey, StringComparison.Ordinal)
                && _exactMatchLookup.TryGetValue(AppSettings.StorageKey, out var appSettingsMatch))
            {
                return appSettingsMatch;
            }

            return _prefixItems.FirstOrDefault(prefixItem => normalizedKey.StartsWith(prefixItem.MatchPattern, StringComparison.Ordinal));
        }

        private static IReadOnlyList<StorageCatalogGroupDefinition> BuildGroups()
        {
            return
            [
                new StorageCatalogGroupDefinition(
                    id: "general",
                    displayNameSource: "General",
                    items:
                    [
                        new StorageCatalogItemDefinition("general.app-settings", "general", "App settings", StorageCatalogItemMatchMode.ExactKey, "AppSettings.State.v2", StorageItemSerializationMode.Json),
                        new StorageCatalogItemDefinition("general.drawer-open", "general", "Drawer open state", StorageCatalogItemMatchMode.ExactKey, "MainLayout.DrawerOpen", StorageItemSerializationMode.Json),
                        new StorageCatalogItemDefinition("general.legacy-dark-mode", "general", "Legacy dark mode state", StorageCatalogItemMatchMode.ExactKey, "MainLayout.IsDarkMode", StorageItemSerializationMode.Json)
                    ]),
                new StorageCatalogGroupDefinition(
                    id: "themes",
                    displayNameSource: "Themes",
                    items:
                    [
                        new StorageCatalogItemDefinition("themes.selected-theme", "themes", "Selected theme", StorageCatalogItemMatchMode.ExactKey, "ThemeManager.SelectedThemeId", StorageItemSerializationMode.Json),
                        new StorageCatalogItemDefinition("themes.selected-theme-definition", "themes", "Selected theme snapshot", StorageCatalogItemMatchMode.ExactKey, "ThemeManager.SelectedThemeDefinition", StorageItemSerializationMode.Json),
                        new StorageCatalogItemDefinition("themes.local-themes", "themes", "Custom themes", StorageCatalogItemMatchMode.ExactKey, "ThemeManager.LocalThemes", StorageItemSerializationMode.Json)
                    ]),
                new StorageCatalogGroupDefinition(
                    id: "language",
                    displayNameSource: "Language",
                    items:
                    [
                        new StorageCatalogItemDefinition("language.preferred-locale", "language", "Preferred locale", StorageCatalogItemMatchMode.ExactKey, "WebUiLocalization.PreferredLocale.v1", StorageItemSerializationMode.RawString)
                    ]),
                new StorageCatalogGroupDefinition(
                    id: "wizard",
                    displayNameSource: "Welcome wizard",
                    items:
                    [
                        new StorageCatalogItemDefinition("wizard.state", "wizard", "Wizard step state", StorageCatalogItemMatchMode.ExactKey, "WelcomeWizard.State.v2", StorageItemSerializationMode.Json),
                        new StorageCatalogItemDefinition("wizard.completed", "wizard", "Wizard completion flag", StorageCatalogItemMatchMode.ExactKey, "WelcomeWizard.Completed.v1", StorageItemSerializationMode.Json)
                    ]),
                new StorageCatalogGroupDefinition(
                    id: "filters",
                    displayNameSource: "Filters",
                    items:
                    [
                        new StorageCatalogItemDefinition("filters.status", "filters", "Selected status filter", StorageCatalogItemMatchMode.ExactKey, "FiltersNav.Selection.Status", StorageItemSerializationMode.RawString),
                        new StorageCatalogItemDefinition("filters.category", "filters", "Selected category filter", StorageCatalogItemMatchMode.ExactKey, "FiltersNav.Selection.Category", StorageItemSerializationMode.RawString),
                        new StorageCatalogItemDefinition("filters.tag", "filters", "Selected tag filter", StorageCatalogItemMatchMode.ExactKey, "FiltersNav.Selection.Tag", StorageItemSerializationMode.RawString),
                        new StorageCatalogItemDefinition("filters.tracker", "filters", "Selected tracker filter", StorageCatalogItemMatchMode.ExactKey, "FiltersNav.Selection.Tracker", StorageItemSerializationMode.RawString)
                    ]),
                new StorageCatalogGroupDefinition(
                    id: "jobs",
                    displayNameSource: "Jobs and logs",
                    items:
                    [
                        new StorageCatalogItemDefinition("jobs.search-preferences", "jobs", "Search preferences", StorageCatalogItemMatchMode.ExactKey, "Search.Preferences", StorageItemSerializationMode.Json),
                        new StorageCatalogItemDefinition("jobs.search-metadata", "jobs", "Search jobs", StorageCatalogItemMatchMode.ExactKey, "Search.Jobs", StorageItemSerializationMode.Json),
                        new StorageCatalogItemDefinition("jobs.log-types", "jobs", "Log type selection", StorageCatalogItemMatchMode.ExactKey, "Log.SelectedTypes", StorageItemSerializationMode.Json),
                        new StorageCatalogItemDefinition("jobs.speed-history", "jobs", "Speed history", StorageCatalogItemMatchMode.ExactKey, "SpeedHistory.State", StorageItemSerializationMode.Json)
                    ]),
                new StorageCatalogGroupDefinition(
                    id: "dialogs",
                    displayNameSource: "Dialogs",
                    items:
                    [
                        new StorageCatalogItemDefinition("dialogs.torrent-creator", "dialogs", "Torrent creator form", StorageCatalogItemMatchMode.ExactKey, "TorrentCreator.FormState", StorageItemSerializationMode.Json),
                        new StorageCatalogItemDefinition("dialogs.rename-files", "dialogs", "Rename files preferences", StorageCatalogItemMatchMode.ExactKey, "RenameFilesDialog.MultiRenamePreferences", StorageItemSerializationMode.Json)
                    ]),
                new StorageCatalogGroupDefinition(
                    id: "tables",
                    displayNameSource: "Tables",
                    items:
                    [
                        new StorageCatalogItemDefinition("tables.dynamic", "tables", "Dynamic table preferences", StorageCatalogItemMatchMode.PrefixPattern, "DynamicTable", StorageItemSerializationMode.Json)
                    ])
            ];
        }
    }
}
