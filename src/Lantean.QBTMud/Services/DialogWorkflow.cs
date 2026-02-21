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
        private const string AddNewTorrentDialogContext = "AddNewTorrentDialog";
        private const string AppContext = "App";
        private const string ConfirmRecheckContext = "confirmRecheckDialog";
        private const string DownloadFromUrlContext = "downloadFromURL";
        private const string PeersAdditionContext = "PeersAdditionDialog";
        private const string SpeedLimitContext = "SpeedLimit";
        private const string TrackersAdditionContext = "TrackersAdditionDialog";
        private const string TransferListContext = "TransferListWidget";

        private const long MaxFileSize = 4194304;

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
                _languageLocalizer.Translate(AddNewTorrentDialogContext, "Add torrent"),
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
                    var stream = file.OpenReadStream(MaxFileSize);
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
                _languageLocalizer.Translate(DownloadFromUrlContext, "Download from URLs"),
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
            if (hashes.Length == 0)
            {
                return false;
            }

            if (!confirmTorrentDeletion)
            {
                await _apiClient.DeleteTorrents(null, false, hashes);
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

            await _apiClient.DeleteTorrents(null, (bool)dialogResult.Data, hashes);
            return true;
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
                var content = _languageLocalizer.Translate(ConfirmRecheckContext, "Are you sure you want to recheck the selected torrent(s)?");
                var confirmed = await ShowConfirmDialog(
                    _languageLocalizer.Translate(ConfirmRecheckContext, "Recheck confirmation"),
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
            Func<long, string> valueDisplayFunc = v => v == Limits.NoLimit ? "∞" : v.ToString();
            Func<string, long> valueGetFunc = v => v == "∞" ? Limits.NoLimit : long.Parse(v);

            var parameters = new DialogParameters
            {
                { nameof(SliderFieldDialog<long>.Min), -1L },
                { nameof(SliderFieldDialog<long>.Max), 4096L },
                { nameof(SliderFieldDialog<long>.Value), rate / 1024 },
                { nameof(SliderFieldDialog<long>.ValueDisplayFunc), valueDisplayFunc },
                { nameof(SliderFieldDialog<long>.ValueGetFunc), valueGetFunc },
                { nameof(SliderFieldDialog<long>.Label), _languageLocalizer.Translate(SpeedLimitContext, "Download limit:") },
                { nameof(SliderFieldDialog<long>.Adornment), Adornment.End },
                { nameof(SliderFieldDialog<long>.AdornmentText), _languageLocalizer.Translate(SpeedLimitContext, "KiB/s") },
            };

            var result = await _dialogService.ShowAsync<SliderFieldDialog<long>>(
                _languageLocalizer.Translate(TransferListContext, "Torrent Download Speed Limiting"),
                parameters,
                FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var kibs = (long)dialogResult.Data;
            await _apiClient.SetTorrentDownloadLimit(kibs * 1024, null, hashes.ToArray());
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
                _languageLocalizer.Translate(TransferListContext, "Renaming"),
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
                _languageLocalizer.Translate(TransferListContext, "Set location"),
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
            Func<long, string> valueDisplayFunc = v => v == Limits.NoLimit ? "∞" : v.ToString();
            Func<string, long> valueGetFunc = v => v == "∞" ? Limits.NoLimit : long.Parse(v);

            var parameters = new DialogParameters
            {
                { nameof(SliderFieldDialog<long>.Min), -1L },
                { nameof(SliderFieldDialog<long>.Max), 4096L },
                { nameof(SliderFieldDialog<long>.Value), rate / 1024 },
                { nameof(SliderFieldDialog<long>.ValueDisplayFunc), valueDisplayFunc },
                { nameof(SliderFieldDialog<long>.ValueGetFunc), valueGetFunc },
                { nameof(SliderFieldDialog<long>.Label), _languageLocalizer.Translate(SpeedLimitContext, "Upload limit:") },
                { nameof(SliderFieldDialog<long>.Adornment), Adornment.End },
                { nameof(SliderFieldDialog<long>.AdornmentText), _languageLocalizer.Translate(SpeedLimitContext, "KiB/s") },
            };

            var result = await _dialogService.ShowAsync<SliderFieldDialog<long>>(
                _languageLocalizer.Translate(TransferListContext, "Torrent Upload Speed Limiting"),
                parameters,
                FormDialogOptions);
            var dialogResult = await result.Result;
            if (dialogResult is null || dialogResult.Canceled || dialogResult.Data is null)
            {
                return;
            }

            var kibs = (long)dialogResult.Data;
            await _apiClient.SetTorrentUploadLimit(kibs * 1024, null, hashes.ToArray());
        }

        /// <inheritdoc />
        public async Task<HashSet<QBitTorrentClient.Models.PeerId>?> ShowAddPeersDialog()
        {
            var reference = await _dialogService.ShowAsync<AddPeerDialog>(
                _languageLocalizer.Translate(PeersAdditionContext, "Add Peers"),
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
                _languageLocalizer.Translate(TransferListContext, "Add tags"),
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
                _languageLocalizer.Translate(TrackersAdditionContext, "Add trackers"),
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
                { nameof(ColumnOptionsDialog<T>.Columns), columnDefinitions },
                { nameof(ColumnOptionsDialog<T>.SelectedColumns), selectedColumns },
                { nameof(ColumnOptionsDialog<T>.Widths), widths },
                { nameof(ColumnOptionsDialog<T>.Order), order },
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
                { nameof(FilterOptionsDialog<T>.FilterDefinitions), propertyFilterDefinitions },
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

        private string TranslateApp(string source, params object[] arguments)
        {
            return _languageLocalizer.Translate(AppContext, source, arguments);
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
