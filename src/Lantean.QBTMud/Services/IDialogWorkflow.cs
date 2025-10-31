using Lantean.QBTMud.Filter;
using Lantean.QBTMud.Models;

namespace Lantean.QBTMud.Helpers
{
    public interface IDialogWorkflow
    {
        Task<string?> InvokeAddCategoryDialog(string? initialCategory = null, string? initialSavePath = null);

        Task InvokeAddTorrentFileDialog();

        Task InvokeAddTorrentLinkDialog(string? url = null);

        Task<bool> InvokeDeleteTorrentDialog(bool confirmTorrentDeletion, params string[] hashes);

        Task ForceRecheckAsync(IEnumerable<string> hashes, bool confirmTorrentRecheck);

        Task InvokeDownloadRateDialog(long rate, IEnumerable<string> hashes);

        Task<string?> InvokeEditCategoryDialog(string categoryName);

        Task InvokeRenameFilesDialog(string hash);

        Task InvokeRssRulesDialog();

        Task InvokeShareRatioDialog(IEnumerable<Torrent> torrents);

        Task InvokeStringFieldDialog(string title, string label, string? value, Func<string, Task> onSuccess);

        Task InvokeUploadRateDialog(long rate, IEnumerable<string> hashes);

        Task<HashSet<Lantean.QBitTorrentClient.Models.PeerId>?> ShowAddPeersDialog();

        Task<HashSet<string>?> ShowAddTagsDialog();

        Task<HashSet<string>?> ShowAddTrackersDialog();

        Task<(HashSet<string> SelectedColumns, Dictionary<string, int?> ColumnWidths, Dictionary<string, int> ColumnOrder)> ShowColumnsOptionsDialog<T>(List<ColumnDefinition<T>> columnDefinitions, HashSet<string> selectedColumns, Dictionary<string, int?> widths, Dictionary<string, int> order);

        Task<bool> ShowConfirmDialog(string title, string content);

        Task ShowConfirmDialog(string title, string content, Func<Task> onSuccess);

        Task ShowConfirmDialog(string title, string content, Action onSuccess);

        Task<List<PropertyFilterDefinition<T>>?> ShowFilterOptionsDialog<T>(List<PropertyFilterDefinition<T>>? propertyFilterDefinitions);

        Task<string?> ShowStringFieldDialog(string title, string label, string? value);

        Task ShowSubMenu(IEnumerable<string> hashes, UIAction parent, Dictionary<string, Torrent> torrents, Lantean.QBitTorrentClient.Models.Preferences? preferences);

        Task<bool> ShowSearchPluginsDialog();
    }
}
