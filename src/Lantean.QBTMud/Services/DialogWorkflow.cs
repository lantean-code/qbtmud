using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Filter;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services.Localization;
using MudBlazor;
using QbtCookie = Lantean.QBitTorrentClient.Models.ApplicationCookie;
using ShareLimitAction = Lantean.QBitTorrentClient.Models.ShareLimitAction;

namespace Lantean.QBTMud.Services
{
    public sealed class DialogWorkflow : IDialogWorkflow
    {
        private const string _addNewTorrentDialogContext = "AddNewTorrentDialog";
        private const string _appContext = "App";
        private const string _confirmRecheckContext = "confirmRecheckDialog";
        private const string _downloadFromUrlContext = "downloadFromURL";
        private const string _peersAdditionContext = "PeersAdditionDialog";
        private const string _speedLimitContext = "SpeedLimit";
        private const string _trackersAdditionContext = "TrackersAdditionDialog";
        private const string _transferListContext = "TransferListWidget";

        private const long _maxFileSize = 4194304;

        public static readonly DialogOptions ConfirmDialogOptions = new()
        {
            BackgroundClass = "background-blur",
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        public static readonly DialogOptions FormDialogOptions = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Medium,
            BackgroundClass = "background-blur",
            FullWidth = true,
        };

        public static readonly DialogOptions FullScreenDialogOptions = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.ExtraExtraLarge,
            BackgroundClass = "background-blur",
            FullWidth = true,
        };

        public static readonly DialogOptions NonBlurConfirmDialogOptions = new()
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
        };

        public static readonly DialogOptions NonBlurFormDialogOptions = new()
        {
            CloseButton = true,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
        };

        private readonly IDialogService _dialogService;
        private readonly IApiClient _apiClient;
        private readonly ISnackbarWorkflow _snackbarWorkflow;
        private readonly ILanguageLocalizer _languageLocalizer;
        private readonly IAppSettingsService _appSettingsService;

        public DialogWorkflow(
            IDialogService dialogService,
            IApiClient apiClient,
            ISnackbarWorkflow snackbarWorkflow,
            ILanguageLocalizer languageLocalizer,
            IAppSettingsService appSettingsService)
        {
            _dialogService = dialogService;
            _apiClient = apiClient;
            _snackbarWorkflow = snackbarWorkflow;
            _languageLocalizer = languageLocalizer;
            _appSettingsService = appSettingsService;
        }

        /// <inheritdoc />
        public async Task<string?> InvokeAddCategoryDialog(string? initialCategory = null, string? initialSavePath = null)
        {
            var parameters = new DialogParameters();
            if (initialCategory is not null)
            {
                parameters.Add(nameof(CategoryPropertiesDialog.Category), initialCategory);
            }

            if (initialSavePath is not null)
            {
                parameters.Add(nameof(CategoryPropertiesDialog.SavePath), initialSavePath);
            }

            var reference = await _dialogService.ShowAsync<CategoryPropertiesDialog>(_languageLocalizer.Translate("TransferListWidget", "New Category"), parameters, NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            var category = (Category)dialogResult.Data;
            await _apiClient.AddCategory(category.Name, category.SavePath);

            return category.Name;
        }

        /// <inheritdoc />
        public async Task InvokeAddTorrentFileDialog()
        {
            var result = await _dialogService.ShowAsync<AddTorrentFileDialog>(
                _languageLocalizer.Translate(_addNewTorrentDialogContext, "Add torrent"),
                FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var options = (AddTorrentFileOptions)dialogResult.Data;
            var streams = new List<Stream>();
            var files = new Dictionary<string, Stream>();

            foreach (var file in options.Files)
            {
                try
                {
                    var stream = file.OpenReadStream(_maxFileSize);
                    streams.Add(stream);

                    var fileName = GetUniqueFileName(file.Name, files.Keys);
                    files.Add(fileName, stream);
                }
                catch (Exception exception)
                {
                    await DisposeStreamsAsync(streams);
                    _snackbarWorkflow.ShowTransientMessage(TranslateApp("Unable to read \"%1\": %2", file.Name, exception.Message), Severity.Error);
                    return;
                }
            }

            var addTorrentParams = CreateAddTorrentParams(options);
            addTorrentParams.Torrents = files;

            QBitTorrentClient.Models.AddTorrentResult addTorrentResult;
            try
            {
                addTorrentResult = await _apiClient.AddTorrent(addTorrentParams);
            }
            catch (HttpRequestException)
            {
                _snackbarWorkflow.ShowTransientMessage(TranslateApp("Unable to add torrent. Please try again."), Severity.Error);
                return;
            }
            finally
            {
                foreach (var stream in streams)
                {
                    await stream.DisposeAsync();
                }
            }

            await ShowAddTorrentSnackbarMessage(addTorrentResult);
        }

        private static string GetUniqueFileName(string fileName, IEnumerable<string> existingNames)
        {
            if (!existingNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
            {
                return fileName;
            }

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var counter = 1;

            while (true)
            {
                var candidate = $"{nameWithoutExtension} ({counter}){extension}";
                if (!existingNames.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                {
                    return candidate;
                }

                counter++;
            }
        }

        /// <inheritdoc />
        public async Task InvokeAddTorrentLinkDialog(string? url = null)
        {
            var parameters = new DialogParameters
            {
                { nameof(AddTorrentLinkDialog.Url), url },
            };

            var result = await _dialogService.ShowAsync<AddTorrentLinkDialog>(
                _languageLocalizer.Translate(_downloadFromUrlContext, "Download from URLs"),
                parameters,
                FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var options = (AddTorrentLinkOptions)dialogResult.Data;
            var addTorrentParams = CreateAddTorrentParams(options);
            addTorrentParams.Urls = options.Urls;

            QBitTorrentClient.Models.AddTorrentResult addTorrentResult;
            try
            {
                addTorrentResult = await _apiClient.AddTorrent(addTorrentParams);
            }
            catch (HttpRequestException)
            {
                _snackbarWorkflow.ShowTransientMessage(TranslateApp("Unable to add torrent. Please try again."), Severity.Error);
                return;
            }

            await ShowAddTorrentSnackbarMessage(addTorrentResult);
        }

        /// <inheritdoc />
        public async Task<bool> InvokeDeleteTorrentDialog(bool confirmTorrentDeletion, params string[] hashes)
        {
            return await InvokeDeleteTorrentDialog(confirmTorrentDeletion, false, hashes);
        }

        /// <inheritdoc />
        public async Task<bool> InvokeDeleteTorrentDialog(bool confirmTorrentDeletion, bool deleteTorrentContentFiles, params string[] hashes)
        {
            if (hashes.Length == 0)
            {
                return false;
            }

            if (!confirmTorrentDeletion)
            {
                await _apiClient.DeleteTorrents(null, deleteTorrentContentFiles, hashes);
                return true;
            }

            string? torrentName = null;
            if (hashes.Length == 1)
            {
                try
                {
                    var torrents = await _apiClient.GetTorrentList(hashes: hashes[0]);
                    torrentName = torrents.FirstOrDefault()?.Name;
                }
                catch (HttpRequestException)
                {
                    torrentName = null;
                }

                if (string.IsNullOrWhiteSpace(torrentName))
                {
                    torrentName = hashes[0];
                }
            }

            var parameters = new DialogParameters
            {
                { nameof(DeleteDialog.Count), hashes.Length },
                { nameof(DeleteDialog.DefaultDeleteFiles), deleteTorrentContentFiles },
                { nameof(DeleteDialog.SaveDeleteFilesPreference), new Func<bool, Task>(SaveDeleteFilesPreference) },
            };
            if (hashes.Length == 1)
            {
                parameters.Add(nameof(DeleteDialog.TorrentName), torrentName);
            }

            var dialogTitle = _languageLocalizer.Translate("confirmDeletionDlg", "Remove torrent(s)");
            var reference = await _dialogService.ShowAsync<DeleteDialog>(dialogTitle, parameters, ConfirmDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return false;
            }

            if (dialogResult.Data is not bool deleteFiles)
            {
                return false;
            }

            await _apiClient.DeleteTorrents(null, deleteFiles, hashes);
            return true;

            async Task SaveDeleteFilesPreference(bool deleteFiles)
            {
                await _apiClient.SetApplicationPreferences(new QBitTorrentClient.Models.UpdatePreferences
                {
                    DeleteTorrentContentFiles = deleteFiles,
                });
            }
        }

        /// <inheritdoc />
        public async Task ForceRecheckAsync(IEnumerable<string> hashes, bool confirmTorrentRecheck)
        {
            var hashArray = hashes?.ToArray() ?? Array.Empty<string>();
            if (hashArray.Length == 0)
            {
                return;
            }

            if (confirmTorrentRecheck)
            {
                var content = _languageLocalizer.Translate(_confirmRecheckContext, "Are you sure you want to recheck the selected torrent(s)?");
                var confirmed = await ShowConfirmDialog(
                    _languageLocalizer.Translate(_confirmRecheckContext, "Recheck confirmation"),
                    content);
                if (!confirmed)
                {
                    return;
                }
            }

            await _apiClient.RecheckTorrents(null, hashArray);
        }

        /// <inheritdoc />
        public async Task InvokeDownloadRateDialog(long rate, IEnumerable<string> hashes)
        {
            var appliedRate = await ShowRateLimitDialog(
                rate,
                _languageLocalizer.Translate(_transferListContext, "Torrent Download Speed Limiting"),
                _languageLocalizer.Translate(_speedLimitContext, "Download limit:"),
                4096L);
            if (!appliedRate.HasValue)
            {
                return;
            }

            await _apiClient.SetTorrentDownloadLimit(appliedRate.Value, null, hashes.ToArray());
        }

        /// <inheritdoc />
        public async Task<long?> InvokeGlobalDownloadRateDialog(long rate)
        {
            var appliedRate = await ShowRateLimitDialog(
                rate,
                _languageLocalizer.Translate("MainWindow", "Global Download Speed Limit"),
                _languageLocalizer.Translate(_speedLimitContext, "Download limit:"),
                10000L);
            if (!appliedRate.HasValue)
            {
                return null;
            }

            await _apiClient.SetGlobalDownloadLimit(appliedRate.Value);

            return appliedRate.Value;
        }

        /// <inheritdoc />
        public async Task<string?> InvokeEditCategoryDialog(string categoryName)
        {
            var category = (await _apiClient.GetAllCategories()).FirstOrDefault(c => c.Key == categoryName).Value;
            var parameters = new DialogParameters
            {
                { nameof(CategoryPropertiesDialog.Category), category?.Name },
                { nameof(CategoryPropertiesDialog.SavePath), category?.SavePath },
            };

            var reference = await _dialogService.ShowAsync<CategoryPropertiesDialog>(_languageLocalizer.Translate("TransferListWidget", "Edit Category"), parameters, NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            var updatedCategory = (Category)dialogResult.Data;
            await _apiClient.EditCategory(updatedCategory.Name, updatedCategory.SavePath);

            return updatedCategory.Name;
        }

        /// <inheritdoc />
        public async Task InvokeRenameFilesDialog(string hash)
        {
            var parameters = new DialogParameters
            {
                { nameof(RenameFilesDialog.Hash), hash },
            };

            await _dialogService.ShowAsync<RenameFilesDialog>(
                _languageLocalizer.Translate(_transferListContext, "Renaming"),
                parameters,
                FullScreenDialogOptions);
        }

        /// <inheritdoc />
        public async Task InvokeRssRulesDialog()
        {
            await _dialogService.ShowAsync<RssRulesDialog>(_languageLocalizer.Translate("AutomatedRssDownloader", "Rss Downloader"), FullScreenDialogOptions);
        }

        /// <inheritdoc />
        public async Task InvokeSetLocationDialog(string? savePath, IEnumerable<string> hashes)
        {
            var hashArray = hashes?.ToArray() ?? Array.Empty<string>();
            if (hashArray.Length == 0)
            {
                return;
            }

            var parameters = new DialogParameters
            {
                { nameof(SetLocationDialog.Location), savePath },
            };

            var result = await _dialogService.ShowAsync<SetLocationDialog>(
                _languageLocalizer.Translate(_transferListContext, "Set location"),
                parameters,
                FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var location = (string)dialogResult.Data;
            await _apiClient.SetTorrentLocation(location: location, all: null, hashes: hashArray);
        }

        /// <inheritdoc />
        public async Task InvokeShareRatioDialog(IEnumerable<Torrent> torrents)
        {
            var torrentList = torrents.ToList();
            if (torrentList.Count == 0)
            {
                return;
            }

            var shareRatioValues = torrentList
                .Select(t => new ShareRatioMax
                {
                    InactiveSeedingTimeLimit = t.InactiveSeedingTimeLimit,
                    MaxInactiveSeedingTime = t.MaxInactiveSeedingTime,
                    MaxRatio = t.MaxRatio,
                    MaxSeedingTime = t.MaxSeedingTime,
                    RatioLimit = t.RatioLimit,
                    SeedingTimeLimit = t.SeedingTimeLimit,
                    ShareLimitAction = t.ShareLimitAction,
                })
                .ToList();

            var referenceValue = shareRatioValues[0];
            var torrentsHaveSameShareRatio = shareRatioValues.Distinct().Count() == 1;

            var parameters = new DialogParameters
            {
                { nameof(ShareRatioDialog.Value), torrentsHaveSameShareRatio ? referenceValue : null },
                { nameof(ShareRatioDialog.CurrentValue), referenceValue },
            };

            var result = await _dialogService.ShowAsync<ShareRatioDialog>(_languageLocalizer.Translate("UpDownRatioDialog", "Torrent Upload/Download Ratio Limiting"), parameters, FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var shareRatio = (ShareRatio)dialogResult.Data;
            await _apiClient.SetTorrentShareLimit(
                shareRatio.RatioLimit,
                shareRatio.SeedingTimeLimit,
                shareRatio.InactiveSeedingTimeLimit,
                shareRatio.ShareLimitAction ?? ShareLimitAction.Default,
                hashes: torrentList.Select(t => t.Hash).ToArray());
        }

        /// <inheritdoc />
        public async Task InvokeStringFieldDialog(string title, string label, string? value, Func<string, Task> onSuccess)
        {
            var result = await ShowStringFieldDialog(title, label, value);
            if (result is not null)
            {
                await onSuccess(result);
            }
        }

        private static async Task DisposeStreamsAsync(List<Stream> streams)
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }

            streams.Clear();
        }

        /// <inheritdoc />
        public async Task InvokeUploadRateDialog(long rate, IEnumerable<string> hashes)
        {
            var appliedRate = await ShowRateLimitDialog(
                rate,
                _languageLocalizer.Translate(_transferListContext, "Torrent Upload Speed Limiting"),
                _languageLocalizer.Translate(_speedLimitContext, "Upload limit:"),
                4096L);
            if (!appliedRate.HasValue)
            {
                return;
            }

            await _apiClient.SetTorrentUploadLimit(appliedRate.Value, null, hashes.ToArray());
        }

        /// <inheritdoc />
        public async Task<long?> InvokeGlobalUploadRateDialog(long rate)
        {
            var appliedRate = await ShowRateLimitDialog(
                rate,
                _languageLocalizer.Translate("MainWindow", "Global Upload Speed Limit"),
                _languageLocalizer.Translate(_speedLimitContext, "Upload limit:"),
                10000L);
            if (!appliedRate.HasValue)
            {
                return null;
            }

            await _apiClient.SetGlobalUploadLimit(appliedRate.Value);

            return appliedRate.Value;
        }

        /// <inheritdoc />
        public async Task<HashSet<QBitTorrentClient.Models.PeerId>?> ShowAddPeersDialog()
        {
            var reference = await _dialogService.ShowAsync<AddPeerDialog>(
                _languageLocalizer.Translate(_peersAdditionContext, "Add Peers"),
                NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            return (HashSet<QBitTorrentClient.Models.PeerId>)dialogResult.Data;
        }

        /// <inheritdoc />
        public async Task<HashSet<string>?> ShowAddTagsDialog()
        {
            var reference = await _dialogService.ShowAsync<AddTagDialog>(
                _languageLocalizer.Translate(_transferListContext, "Add tags"),
                NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            return (HashSet<string>)dialogResult.Data;
        }

        /// <inheritdoc />
        public async Task<HashSet<string>?> ShowAddTrackersDialog()
        {
            var reference = await _dialogService.ShowAsync<AddTrackerDialog>(
                _languageLocalizer.Translate(_trackersAdditionContext, "Add trackers"),
                NonBlurFormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            return (HashSet<string>)dialogResult.Data;
        }

        /// <inheritdoc />
        public async Task<(HashSet<string> SelectedColumns, Dictionary<string, int?> ColumnWidths, Dictionary<string, int> ColumnOrder)> ShowColumnsOptionsDialog<T>(
            List<ColumnDefinition<T>> columnDefinitions,
            HashSet<string> selectedColumns,
            Dictionary<string, int?> widths,
            Dictionary<string, int> order)
        {
            var parameters = new DialogParameters
            {
                { nameof(ColumnOptionsDialog<>.Columns), columnDefinitions },
                { nameof(ColumnOptionsDialog<>.SelectedColumns), selectedColumns },
                { nameof(ColumnOptionsDialog<>.Widths), widths },
                { nameof(ColumnOptionsDialog<>.Order), order },
            };

            var reference = await _dialogService.ShowAsync<ColumnOptionsDialog<T>>(
                _languageLocalizer.Translate("AppColumnOptionsDialog", "Choose Columns"),
                parameters,
                FormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return default;
            }

            return ((HashSet<string>, Dictionary<string, int?>, Dictionary<string, int>))dialogResult.Data;
        }

        /// <inheritdoc />
        public async Task<bool> ShowConfirmDialog(string title, string content)
        {
            var parameters = new DialogParameters
            {
                { nameof(ConfirmDialog.Content), content },
            };

            var result = await _dialogService.ShowAsync<ConfirmDialog>(title, parameters, ConfirmDialogOptions);
            var dialogResult = await result.Result;

            return dialogResult is not null && !dialogResult.Canceled;
        }

        /// <inheritdoc />
        public async Task ShowConfirmDialog(string title, string content, Func<Task> onSuccess)
        {
            var parameters = new DialogParameters
            {
                { nameof(ConfirmDialog.Content), content },
            };

            var result = await _dialogService.ShowAsync<ConfirmDialog>(title, parameters, ConfirmDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            await onSuccess();
        }

        /// <inheritdoc />
        public async Task ShowConfirmDialog(string title, string content, Action onSuccess)
        {
            await ShowConfirmDialog(
                title,
                content,
                () =>
                {
                    onSuccess();
                    return Task.CompletedTask;
                });
        }

        /// <inheritdoc />
        public async Task<List<PropertyFilterDefinition<T>>?> ShowFilterOptionsDialog<T>(List<PropertyFilterDefinition<T>>? propertyFilterDefinitions)
        {
            var parameters = new DialogParameters
            {
                { nameof(FilterOptionsDialog<>.FilterDefinitions), propertyFilterDefinitions },
            };

            var result = await _dialogService.ShowAsync<FilterOptionsDialog<T>>(
                _languageLocalizer.Translate("AppFilterOptionsDialog", "Filters"),
                parameters,
                FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            return (List<PropertyFilterDefinition<T>>?)dialogResult.Data;
        }

        /// <inheritdoc />
        public async Task<string?> ShowPathBrowserDialog(string title, string? initialPath, QBitTorrentClient.Models.DirectoryContentMode mode, bool allowFolderSelection)
        {
            var parameters = new DialogParameters
            {
                { nameof(PathBrowserDialog.InitialPath), initialPath },
                { nameof(PathBrowserDialog.Mode), mode },
                { nameof(PathBrowserDialog.AllowFolderSelection), allowFolderSelection },
            };

            var reference = await _dialogService.ShowAsync<PathBrowserDialog>(title, parameters, FormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            if (dialogResult.Data is not string selectedPath || string.IsNullOrWhiteSpace(selectedPath))
            {
                return null;
            }

            return selectedPath;
        }

        /// <inheritdoc />
        public async Task<string?> ShowStringFieldDialog(string title, string label, string? value)
        {
            var parameters = new DialogParameters
            {
                { nameof(StringFieldDialog.Label), label },
                { nameof(StringFieldDialog.Value), value },
            };

            var result = await _dialogService.ShowAsync<StringFieldDialog>(title, parameters, FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            return (string)dialogResult.Data;
        }

        /// <inheritdoc />
        public async Task<QbtCookie?> ShowCookiePropertiesDialog(string title, QbtCookie? cookie)
        {
            var parameters = new DialogParameters();
            if (cookie is not null)
            {
                parameters.Add(nameof(CookiePropertiesDialog.Cookie), cookie);
            }

            var reference = await _dialogService.ShowAsync<CookiePropertiesDialog>(title, parameters, FormDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            return dialogResult.Data as QbtCookie;
        }

        /// <inheritdoc />
        public async Task ShowSubMenu(IEnumerable<string> hashes, UIAction parent, Dictionary<string, Torrent> torrents, QBitTorrentClient.Models.Preferences? preferences, HashSet<string> tags, Dictionary<string, Category> categories)
        {
            var parameters = new DialogParameters
            {
                { nameof(SubMenuDialog.ParentAction), parent },
                { nameof(SubMenuDialog.Hashes), hashes },
                { nameof(SubMenuDialog.Torrents), torrents },
                { nameof(SubMenuDialog.Preferences), preferences },
                { nameof(SubMenuDialog.Tags), tags },
                { nameof(SubMenuDialog.Categories), categories },
            };

            await _dialogService.ShowAsync<SubMenuDialog>(parent.Text, parameters, FormDialogOptions);
        }

        /// <inheritdoc />
        public async Task<bool> ShowSearchPluginsDialog()
        {
            var reference = await _dialogService.ShowAsync<SearchPluginsDialog>(_languageLocalizer.Translate("PluginSelectDlg", "Search plugins"), FullScreenDialogOptions);
            var dialogResult = await reference.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return false;
            }

            return dialogResult.Data is bool changed && changed;
        }

        /// <summary>
        /// Shows a theme preview dialog.
        /// </summary>
        /// <param name="theme">The theme to preview.</param>
        /// <param name="isDarkMode">Whether to start the preview in dark mode.</param>
        public async Task ShowThemePreviewDialog(MudTheme theme, bool isDarkMode)
        {
            if (theme is null)
            {
                throw new ArgumentNullException(nameof(theme));
            }

            var parameters = new DialogParameters
            {
                { nameof(ThemePreviewDialog.Theme), theme },
                { nameof(ThemePreviewDialog.IsDarkMode), isDarkMode }
            };
            var options = FullScreenDialogOptions with
            {
                FullScreen = false,
                NoHeader = true,
                FullWidth = false
            };

            await _dialogService.ShowAsync<ThemePreviewDialog>(_languageLocalizer.Translate("AppThemePreviewDialog", "Theme Preview"), parameters, options);
        }

        private async Task ShowAddTorrentSnackbarMessage(QBitTorrentClient.Models.AddTorrentResult result)
        {
            var settings = await GetAppSettingsSafeAsync();
            if (settings.NotificationsEnabled && settings.TorrentAddedNotificationsEnabled && !settings.TorrentAddedSnackbarsEnabledWithNotifications)
            {
                ShowFailureOnlyAddTorrentSnackbarMessage(result);
                return;
            }

            ShowDefaultAddTorrentSnackbarMessage(result);
        }

        private void ShowFailureOnlyAddTorrentSnackbarMessage(QBitTorrentClient.Models.AddTorrentResult result)
        {
            if (result.FailureCount <= 0)
            {
                return;
            }

            string failureMessage;
            if (result.SupportsAsync)
            {
                failureMessage = result.FailureCount == 1
                    ? TranslateApp("failed to add %1 torrent", result.FailureCount)
                    : TranslateApp("failed to add %1 torrents", result.FailureCount);
            }
            else
            {
                failureMessage = TranslateApp("failed to add torrent(s)");
            }

            var message = char.ToUpperInvariant(failureMessage[0]) + failureMessage[1..] + ".";
            _snackbarWorkflow.ShowTransientMessage(message, Severity.Error);
        }

        private void ShowDefaultAddTorrentSnackbarMessage(QBitTorrentClient.Models.AddTorrentResult result)
        {
            var fragments = new List<string>(3);
            if (result.SuccessCount > 0)
            {
                if (result.SupportsAsync)
                {
                    fragments.Add(result.SuccessCount == 1
                        ? TranslateApp("Added %1 torrent", result.SuccessCount)
                        : TranslateApp("Added %1 torrents", result.SuccessCount));
                }
                else
                {
                    fragments.Add(TranslateApp("Added torrent(s)"));
                }
            }

            if (result.FailureCount > 0)
            {
                string failureMessage;
                if (result.SupportsAsync)
                {
                    failureMessage = result.FailureCount == 1
                        ? TranslateApp("failed to add %1 torrent", result.FailureCount)
                        : TranslateApp("failed to add %1 torrents", result.FailureCount);
                }
                else
                {
                    failureMessage = TranslateApp("failed to add torrent(s)");
                }

                if (fragments.Count == 0)
                {
                    failureMessage = char.ToUpperInvariant(failureMessage[0]) + failureMessage[1..];
                }

                fragments.Add(failureMessage);
            }

            if (result.SupportsAsync && result.PendingCount > 0)
            {
                fragments.Add(result.PendingCount == 1
                    ? TranslateApp("Pending %1 torrent", result.PendingCount)
                    : TranslateApp("Pending %1 torrents", result.PendingCount));
            }

            if (fragments.Count == 0)
            {
                fragments.Add(TranslateApp("No torrents processed"));
            }

            var message = string.Join(" and ", fragments) + '.';

            var severity = Severity.Success;
            if (result.SuccessCount > 0 && result.FailureCount > 0)
            {
                severity = Severity.Warning;
            }
            else if (result.FailureCount > 0)
            {
                severity = Severity.Error;
            }
            else if (result.PendingCount > 0)
            {
                severity = Severity.Info;
            }

            _snackbarWorkflow.ShowTransientMessage(message, severity);
        }

        private async Task<AppSettings> GetAppSettingsSafeAsync()
        {
            try
            {
                return await _appSettingsService.GetSettingsAsync();
            }
            catch
            {
                return AppSettings.Default.Clone();
            }
        }

        private async Task<long?> ShowRateLimitDialog(long rate, string title, string label, long max)
        {
            Func<long, string> valueDisplayFunc = v => v <= 0 ? "∞" : v.ToString();
            Func<string, long> valueGetFunc = v => v == "∞" ? 0L : long.Parse(v);

            var parameters = new DialogParameters
            {
                { nameof(SliderFieldDialog<>.Min), 0L },
                { nameof(SliderFieldDialog<>.Max), max },
                { nameof(SliderFieldDialog<>.Value), Math.Max(rate, 0L) / 1024 },
                { nameof(SliderFieldDialog<>.ValueDisplayFunc), valueDisplayFunc },
                { nameof(SliderFieldDialog<>.ValueGetFunc), valueGetFunc },
                { nameof(SliderFieldDialog<>.Label), label },
                { nameof(SliderFieldDialog<>.Adornment), Adornment.End },
                { nameof(SliderFieldDialog<>.AdornmentText), _languageLocalizer.Translate(_speedLimitContext, "KiB/s") },
            };

            var result = await _dialogService.ShowAsync<SliderFieldDialog<long>>(
                title,
                parameters,
                FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return null;
            }

            var kibs = (long)dialogResult.Data;
            return kibs * 1024;
        }

        private string TranslateApp(string source, params object[] arguments)
        {
            return _languageLocalizer.Translate(_appContext, source, arguments);
        }

        private static QBitTorrentClient.Models.AddTorrentParams CreateAddTorrentParams(TorrentOptions options)
        {
            var addTorrentParams = new QBitTorrentClient.Models.AddTorrentParams
            {
                AddToTopOfQueue = options.AddToTopOfQueue,
                AutoTorrentManagement = options.TorrentManagementMode,
                Category = options.Category,
                DownloadLimit = options.DownloadLimit,
                FirstLastPiecePriority = options.DownloadFirstAndLastPiecesFirst,
                InactiveSeedingTimeLimit = options.InactiveSeedingTimeLimit,
                RatioLimit = options.RatioLimit,
                RenameTorrent = options.RenameTorrent,
                SeedingTimeLimit = options.SeedingTimeLimit,
                SequentialDownload = options.DownloadInSequentialOrder,
                SkipChecking = options.SkipHashCheck,
                Stopped = !options.StartTorrent,
                Tags = options.Tags,
                UploadLimit = options.UploadLimit,
            };

            if (!string.IsNullOrWhiteSpace(options.ContentLayout))
            {
                addTorrentParams.ContentLayout = Enum.Parse<QBitTorrentClient.Models.TorrentContentLayout>(options.ContentLayout);
            }

            if (!string.IsNullOrWhiteSpace(options.DownloadPath))
            {
                addTorrentParams.DownloadPath = options.DownloadPath;
            }

            if (!options.TorrentManagementMode)
            {
                addTorrentParams.SavePath = options.SavePath;
            }

            if (!string.IsNullOrWhiteSpace(options.Cookie))
            {
                addTorrentParams.Cookie = options.Cookie;
            }

            if (!string.IsNullOrWhiteSpace(options.ShareLimitAction))
            {
                addTorrentParams.ShareLimitAction = Enum.Parse<QBitTorrentClient.Models.ShareLimitAction>(options.ShareLimitAction);
            }

            if (!string.IsNullOrWhiteSpace(options.StopCondition))
            {
                addTorrentParams.StopCondition = Enum.Parse<QBitTorrentClient.Models.StopCondition>(options.StopCondition);
            }

            if (options.UseDownloadPath.HasValue)
            {
                addTorrentParams.UseDownloadPath = options.UseDownloadPath;
            }

            return addTorrentParams;
        }
    }
}
