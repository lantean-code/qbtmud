using Lantean.QBTMud.Filter;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Helpers
{
    public interface IDialogWorkflow
    {
        /// <summary>
        /// Shows the add category dialog and creates the category when confirmed.
        /// </summary>
        /// <param name="initialCategory">The initial category name to prefill.</param>
        /// <param name="initialSavePath">The initial save path to prefill.</param>
        /// <returns>The created category name, or <c>null</c> if canceled.</returns>
        Task<string?> InvokeAddCategoryDialog(string? initialCategory = null, string? initialSavePath = null);

        /// <summary>
        /// Shows the dialog for uploading local torrent files.
        /// </summary>
        Task InvokeAddTorrentFileDialog();

        /// <summary>
        /// Shows the dialog for adding torrents from URLs.
        /// </summary>
        /// <param name="url">The initial URL to prefill.</param>
        Task InvokeAddTorrentLinkDialog(string? url = null);

        /// <summary>
        /// Prompts for torrent deletion and performs the deletion when confirmed.
        /// </summary>
        /// <param name="confirmTorrentDeletion">Whether confirmation is required before deletion.</param>
        /// <param name="hashes">The torrent hashes to delete.</param>
        /// <returns><c>true</c> if deletion was executed; otherwise, <c>false</c>.</returns>
        Task<bool> InvokeDeleteTorrentDialog(bool confirmTorrentDeletion, params string[] hashes);

        /// <summary>
        /// Forces a recheck for the specified torrents.
        /// </summary>
        /// <param name="hashes">The torrent hashes to recheck.</param>
        /// <param name="confirmTorrentRecheck">Whether confirmation is required before recheck.</param>
        Task ForceRecheckAsync(IEnumerable<string> hashes, bool confirmTorrentRecheck);

        /// <summary>
        /// Shows the dialog for setting the download rate limit.
        /// </summary>
        /// <param name="rate">The current rate limit in bytes per second.</param>
        /// <param name="hashes">The torrent hashes to update.</param>
        Task InvokeDownloadRateDialog(long rate, IEnumerable<string> hashes);

        /// <summary>
        /// Shows the edit category dialog and applies changes when confirmed.
        /// </summary>
        /// <param name="categoryName">The category name to edit.</param>
        /// <returns>The updated category name, or <c>null</c> if canceled.</returns>
        Task<string?> InvokeEditCategoryDialog(string categoryName);

        /// <summary>
        /// Shows the rename files dialog for a torrent.
        /// </summary>
        /// <param name="hash">The torrent hash.</param>
        Task InvokeRenameFilesDialog(string hash);

        /// <summary>
        /// Shows the RSS rules dialog.
        /// </summary>
        Task InvokeRssRulesDialog();

        /// <summary>
        /// Shows the share ratio dialog and applies changes to the selected torrents.
        /// </summary>
        /// <param name="torrents">The torrents to update.</param>
        Task InvokeShareRatioDialog(IEnumerable<Torrent> torrents);

        /// <summary>
        /// Shows a string input dialog and invokes the callback when confirmed.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="label">The input label.</param>
        /// <param name="value">The initial value.</param>
        /// <param name="onSuccess">The callback invoked when a value is confirmed.</param>
        Task InvokeStringFieldDialog(string title, string label, string? value, Func<string, Task> onSuccess);

        /// <summary>
        /// Shows the dialog for setting the upload rate limit.
        /// </summary>
        /// <param name="rate">The current rate limit in bytes per second.</param>
        /// <param name="hashes">The torrent hashes to update.</param>
        Task InvokeUploadRateDialog(long rate, IEnumerable<string> hashes);

        /// <summary>
        /// Shows the add peers dialog.
        /// </summary>
        /// <returns>The selected peers, or <c>null</c> if canceled.</returns>
        Task<HashSet<Lantean.QBitTorrentClient.Models.PeerId>?> ShowAddPeersDialog();

        /// <summary>
        /// Shows the add tags dialog.
        /// </summary>
        /// <returns>The selected tags, or <c>null</c> if canceled.</returns>
        Task<HashSet<string>?> ShowAddTagsDialog();

        /// <summary>
        /// Shows the add trackers dialog.
        /// </summary>
        /// <returns>The selected trackers, or <c>null</c> if canceled.</returns>
        Task<HashSet<string>?> ShowAddTrackersDialog();

        /// <summary>
        /// Shows the column options dialog.
        /// </summary>
        /// <param name="columnDefinitions">The available column definitions.</param>
        /// <param name="selectedColumns">The currently selected columns.</param>
        /// <param name="widths">The current column widths.</param>
        /// <param name="order">The current column order.</param>
        /// <returns>The updated selections, widths, and order.</returns>
        Task<(HashSet<string> SelectedColumns, Dictionary<string, int?> ColumnWidths, Dictionary<string, int> ColumnOrder)> ShowColumnsOptionsDialog<T>(
            List<ColumnDefinition<T>> columnDefinitions,
            HashSet<string> selectedColumns,
            Dictionary<string, int?> widths,
            Dictionary<string, int> order);

        /// <summary>
        /// Shows a confirmation dialog.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="content">The dialog content.</param>
        /// <returns><c>true</c> if confirmed; otherwise, <c>false</c>.</returns>
        Task<bool> ShowConfirmDialog(string title, string content);

        /// <summary>
        /// Shows a confirmation dialog and invokes the callback when confirmed.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="content">The dialog content.</param>
        /// <param name="onSuccess">The callback invoked when confirmed.</param>
        Task ShowConfirmDialog(string title, string content, Func<Task> onSuccess);

        /// <summary>
        /// Shows a confirmation dialog and invokes the callback when confirmed.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="content">The dialog content.</param>
        /// <param name="onSuccess">The callback invoked when confirmed.</param>
        Task ShowConfirmDialog(string title, string content, Action onSuccess);

        /// <summary>
        /// Shows the filter options dialog.
        /// </summary>
        /// <param name="propertyFilterDefinitions">The current filter definitions.</param>
        /// <returns>The updated filter definitions, or <c>null</c> if canceled.</returns>
        Task<List<PropertyFilterDefinition<T>>?> ShowFilterOptionsDialog<T>(List<PropertyFilterDefinition<T>>? propertyFilterDefinitions);

        /// <summary>
        /// Shows the path browser dialog and returns the selected path.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="initialPath">The initial path to browse.</param>
        /// <param name="mode">The browse mode for directory content.</param>
        /// <param name="allowFolderSelection">Whether selecting the current folder is allowed.</param>
        /// <returns>The selected path, or <c>null</c> if the dialog was canceled.</returns>
        Task<string?> ShowPathBrowserDialog(string title, string? initialPath, Lantean.QBitTorrentClient.Models.DirectoryContentMode mode, bool allowFolderSelection);

        /// <summary>
        /// Shows a string input dialog.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="label">The input label.</param>
        /// <param name="value">The initial value.</param>
        /// <returns>The entered value, or <c>null</c> if canceled.</returns>
        Task<string?> ShowStringFieldDialog(string title, string label, string? value);

        /// <summary>
        /// Shows the submenu dialog for torrent actions.
        /// </summary>
        /// <param name="hashes">The selected torrent hashes.</param>
        /// <param name="parent">The parent action.</param>
        /// <param name="torrents">The current torrent map.</param>
        /// <param name="preferences">The client preferences.</param>
        /// <param name="tags">The available tags.</param>
        /// <param name="categories">The available categories.</param>
        Task ShowSubMenu(
            IEnumerable<string> hashes,
            UIAction parent,
            Dictionary<string, Torrent> torrents,
            QBitTorrentClient.Models.Preferences? preferences,
            HashSet<string> tags,
            Dictionary<string, Category> categories);

        /// <summary>
        /// Shows the search plugins dialog.
        /// </summary>
        /// <returns><c>true</c> when changes were made; otherwise, <c>false</c>.</returns>
        Task<bool> ShowSearchPluginsDialog();
    }
}
