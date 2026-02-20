using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.Dialogs;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Interop;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMud.Components
{
    public partial class TorrentActions : IAsyncDisposable
    {
        private const string _transferListContext = "TransferListWidget";
        private const string _appContext = "AppTorrentActions";

        private bool _disposedValue;
        private bool _deleteShortcutRegistered;

        private List<UIAction>? _actions;

        [Inject]
        public IApiClient ApiClient { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        [Inject]
        public IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        public ISnackbarWorkflow SnackbarWorkflow { get; set; } = default!;

        [Inject]
        public ITorrentDataManager DataManager { get; set; } = default!;

        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        protected IKeyboardService KeyboardService { get; set; } = default!;

        [Inject]
        protected ILanguageLocalizer LanguageLocalizer { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public IEnumerable<string> Hashes { get; set; } = default!;

        [Parameter]
        public string? PrimaryHash { get; set; }

        /// <summary>
        /// If true this component will render as a <see cref="MudToolBar"/> otherwise will render as a <see cref="MudMenu"/>.
        /// </summary>
        [Parameter]
        public RenderType RenderType { get; set; }

        [Parameter, EditorRequired]
        public Dictionary<string, Torrent> Torrents { get; set; } = default!;

        [Parameter, EditorRequired]
        public QBitTorrentClient.Models.Preferences? Preferences { get; set; }

        [Parameter, EditorRequired]
        public HashSet<string> Tags { get; set; } = default!;

        [Parameter, EditorRequired]
        public Dictionary<string, Category> Categories { get; set; } = default!;

        [Parameter]
        public IMudDialogInstance? MudDialog { get; set; }

        [Parameter]
        public UIAction? ParentAction { get; set; }

        public MudMenu? ActionsMenu { get; set; }

        protected bool Disabled => !Hashes.Any();

        protected bool OverlayVisible { get; set; }

        protected override void OnInitialized()
        {
            _actions =
            [
                new("start", TranslateTransferList("Start"), Icons.Material.Filled.PlayArrow, Color.Success, CreateCallback(Start)),
                new("stop", TranslateTransferList("Stop"), Icons.Material.Filled.Stop, Color.Warning, CreateCallback(Stop)),
                new("forceStart", TranslateTransferList("Force Start"), Icons.Material.Filled.Forward, Color.Warning, CreateCallback(ForceStart)),
                new("delete", TranslateTransferList("Remove"), Icons.Material.Filled.Delete, Color.Error, CreateCallback(Remove), separatorBefore: true),
                new("setLocation", TranslateTransferList("Set location..."), Icons.Material.Filled.MyLocation, Color.Info, CreateCallback(SetLocation), separatorBefore: true),
                new("rename", TranslateTransferList("Rename..."), Icons.Material.Filled.DriveFileRenameOutline, Color.Info, CreateCallback(Rename)),
                new("renameFiles", TranslateTransferList("Rename Files..."), Icons.Material.Filled.DriveFileRenameOutline, Color.Warning, CreateCallback(RenameFiles)),
                new("category", TranslateTransferList("Category"), Icons.Material.Filled.List, Color.Info, CreateCallback(ShowCategories)),
                new("tags", TranslateTransferList("Tags"), Icons.Material.Filled.Label, Color.Info, CreateCallback(ShowTags)),
                new("autoTorrentManagement", TranslateTransferList("Automatic Torrent Management"), Icons.Material.Filled.Check, Color.Info, CreateCallback(ToggleAutoTMM), autoClose: false),
                new("downloadLimit", TranslateTransferList("Limit download rate..."), Icons.Material.Filled.KeyboardDoubleArrowDown, Color.Success, CreateCallback(LimitDownloadRate), separatorBefore: true),
                new("uploadLimit", TranslateTransferList("Limit upload rate..."), Icons.Material.Filled.KeyboardDoubleArrowUp, Color.Warning, CreateCallback(LimitUploadRate)),
                new("shareRatio", TranslateTransferList("Limit share ratio..."), Icons.Material.Filled.Percent, Color.Info, CreateCallback(LimitShareRatio)),
                new("superSeeding", TranslateTransferList("Super seeding mode"), Icons.Material.Filled.Check, Color.Info, CreateCallback(ToggleSuperSeeding), autoClose: false),
                new("sequentialDownload", TranslateTransferList("Download in sequential order"), Icons.Material.Filled.Check, Color.Info, CreateCallback(DownloadSequential), separatorBefore: true, autoClose: false),
                new("firstLastPiecePrio", TranslateTransferList("Download first and last pieces first"), Icons.Material.Filled.Check, Color.Info, CreateCallback(DownloadFirstLast), autoClose : false),
                new("forceRecheck", TranslateTransferList("Force recheck"), Icons.Material.Filled.Loop, Color.Info, CreateCallback(ForceRecheck), separatorBefore: true),
                new("forceReannounce", TranslateTransferList("Force reannounce"), Icons.Material.Filled.BroadcastOnHome, Color.Info, CreateCallback(ForceReannounce)),
                new("queue", TranslateTransferList("Queue"), Icons.Material.Filled.Queue, Color.Transparent,
                [
                    new("queueTop", TranslateTransferList("Move to top"), Icons.Material.Filled.VerticalAlignTop, Color.Inherit, CreateCallback(MoveToTop)),
                    new("queueUp", TranslateTransferList("Move up"), Icons.Material.Filled.ArrowUpward, Color.Inherit, CreateCallback(MoveUp)),
                    new("queueDown", TranslateTransferList("Move down"), Icons.Material.Filled.ArrowDownward, Color.Inherit, CreateCallback(MoveDown)),
                    new("queueBottom", TranslateTransferList("Move to bottom"), Icons.Material.Filled.VerticalAlignBottom, Color.Inherit, CreateCallback(MoveToBottom)),
                ], separatorBefore: true),
                new("copy", TranslateTransferList("Copy"), Icons.Material.Filled.FolderCopy, Color.Info,
                [
                    new("copyName", TranslateTransferList("Name"), Icons.Material.Filled.TextFields, Color.Info, CreateCallback(() => Copy(t => t.Name))),
                    new("copyHashv1", TranslateTransferList("Info hash v1"), Icons.Material.Filled.Tag, Color.Info, CreateCallback(() => Copy(t => t.InfoHashV1))),
                    new("copyHashv2", TranslateTransferList("Info hash v2"), Icons.Material.Filled.Tag, Color.Info, CreateCallback(() => Copy(t => t.InfoHashV2))),
                    new("copyMagnet", TranslateTransferList("Magnet link"), Icons.Material.Filled.TextFields, Color.Info, CreateCallback(() => Copy(t => t.MagnetUri))),
                    new("copyId", TranslateTransferList("Torrent ID"), Icons.Material.Filled.TextFields, Color.Info, CreateCallback(() => Copy(t => t.Hash))),
                    new("copyComment", TranslateTransferList("Comment"), Icons.Material.Filled.TextFields, Color.Info, CreateCallback(() => Copy(t => t.Comment))),
                    new("copyContentPath", TranslateTransferList("Content Path"), Icons.Material.Filled.TextFields, Color.Info, CreateCallback(() => Copy(t => t.ContentPath))),
                ]),
                new("export", TranslateTransferList("Export .torrent"), Icons.Material.Filled.SaveAlt, Color.Info, CreateCallback(Export)),
            ];
        }

        protected override void OnParametersSet()
        {
            foreach (var hash in Hashes)
            {
                if (Torrents.TryGetValue(hash, out var torrent))
                {
                    TagState[hash] = torrent.Tags.ToHashSet();
                    if (!string.IsNullOrEmpty(torrent.Category))
                    {
                        CategoryState[hash] = torrent.Category;
                    }
                    else
                    {
                        CategoryState.Remove(hash);
                    }
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                return;
            }

            if (ShouldAlwaysRegisterDeleteShortcut())
            {
                await RegisterDeleteShortcutAsync();
            }
            else
            {
                // Shortcut is registered only while the actions menu is open.
                await UnregisterDeleteShortcutAsync();
            }
        }

        protected async Task OverlayVisibleChanged(bool value)
        {
            OverlayVisible = value;
            if (!value && ActionsMenu is not null)
            {
                await ActionsMenu.CloseMenuAsync();
            }
        }

        protected async Task ActionsMenuOpenChanged(bool value)
        {
            OverlayVisible = value;
            if (RenderType == RenderType.Menu || RenderType == RenderType.MenuWithoutActivator)
            {
                if (value)
                {
                    await RegisterDeleteShortcutAsync();
                }
                else
                {
                    await UnregisterDeleteShortcutAsync();
                }
            }
        }

        protected async Task Stop()
        {
            await ApiClient.StopTorrents(hashes: Hashes.ToArray());
            SnackbarWorkflow.ShowTransientMessage(TranslateApp("Torrent stopped."));
        }

        protected async Task Start()
        {
            await ApiClient.StartTorrents(hashes: Hashes.ToArray());
            SnackbarWorkflow.ShowTransientMessage(TranslateApp("Torrent started."));
        }

        protected async Task ForceStart()
        {
            await ApiClient.SetForceStart(value: true, all: null, hashes: Hashes.ToArray());
            SnackbarWorkflow.ShowTransientMessage(TranslateApp("Torrent force started."));
        }

        protected async Task Remove()
        {
            var deleted = await DialogWorkflow.InvokeDeleteTorrentDialog(Preferences?.ConfirmTorrentDeletion == true, Hashes.ToArray());

            if (deleted)
            {
                NavigationManager.NavigateToHome();
            }
        }

        private Task RemoveViaShortcut()
        {
            if (Disabled)
            {
                return Task.CompletedTask;
            }

            if (!ShouldAlwaysRegisterDeleteShortcut() && !ActionsMenuVisible())
            {
                return Task.CompletedTask;
            }

            return Remove();
        }

        private bool ActionsMenuVisible()
        {
            if ((RenderType != RenderType.Menu && RenderType != RenderType.MenuWithoutActivator) || ActionsMenu is null)
            {
                return false;
            }

            return OverlayVisible;
        }

        private bool ShouldAlwaysRegisterDeleteShortcut()
        {
            return RenderType == RenderType.Toolbar
                || RenderType == RenderType.ToolbarContents
                || RenderType == RenderType.MixedToolbar
                || RenderType == RenderType.MixedToolbarContents
                || RenderType == RenderType.InitialIconsOnly;
        }

        private async Task RegisterDeleteShortcutAsync()
        {
            if (_deleteShortcutRegistered)
            {
                return;
            }

            await KeyboardService.RegisterKeypressEvent("Delete", k => RemoveViaShortcut());
            _deleteShortcutRegistered = true;
        }

        private async Task UnregisterDeleteShortcutAsync()
        {
            if (!_deleteShortcutRegistered)
            {
                return;
            }

            await KeyboardService.UnregisterKeypressEvent("Delete");
            _deleteShortcutRegistered = false;
        }

        protected async Task SetLocation()
        {
            string? savePath = null;
            if (Hashes.Any() && Torrents.TryGetValue(Hashes.First(), out var torrent))
            {
                savePath = torrent.SavePath;
            }

            await DialogWorkflow.InvokeStringFieldDialog(
                TranslateTransferList("Set location"),
                TranslateTransferList("Location:"),
                savePath,
                v => ApiClient.SetTorrentLocation(location: v, all: null, hashes: Hashes.ToArray()));
        }

        protected async Task Rename()
        {
            string? name = null;
            string hash = Hashes.First();
            if (Hashes.Any() && Torrents.TryGetValue(hash, out var torrent))
            {
                name = torrent.Name;
            }
            await DialogWorkflow.InvokeStringFieldDialog(
                TranslateTransferList("Rename"),
                TranslateTransferList("New name:"),
                name,
                v => ApiClient.SetTorrentName(v, hash));
        }

        protected async Task RenameFiles()
        {
            await DialogWorkflow.InvokeRenameFilesDialog(Hashes.First());
        }

        protected async Task SetCategory(string category)
        {
            await ApiClient.SetTorrentCategory(category: category, all: null, hashes: Hashes.ToArray());
        }

        protected async Task ToggleAutoTMM()
        {
            var stateChanges = ToggleTorrentState(torrent => torrent.AutomaticTorrentManagement, (torrent, value) => torrent.AutomaticTorrentManagement = value);
            var disableHashes = stateChanges.Where(change => change.PreviousValue).Select(change => change.Hash).ToArray();
            var enableHashes = stateChanges.Where(change => !change.PreviousValue).Select(change => change.Hash).ToArray();

            try
            {
                if (disableHashes.Length > 0)
                {
                    await ApiClient.SetAutomaticTorrentManagement(enable: false, all: null, hashes: disableHashes);
                }

                if (enableHashes.Length > 0)
                {
                    await ApiClient.SetAutomaticTorrentManagement(enable: true, all: null, hashes: enableHashes);
                }
            }
            catch
            {
                RevertTorrentState(stateChanges, (torrent, value) => torrent.AutomaticTorrentManagement = value);
                throw;
            }
        }

        protected async Task LimitDownloadRate()
        {
            long downloadLimit = -1;
            string hash = Hashes.First();
            if (Hashes.Any() && Torrents.TryGetValue(hash, out var torrent))
            {
                downloadLimit = torrent.DownloadLimit;
            }

            await DialogWorkflow.InvokeDownloadRateDialog(downloadLimit, Hashes);
        }

        protected async Task LimitUploadRate()
        {
            long uploadLimit = -1;
            string hash = Hashes.First();
            if (Hashes.Any() && Torrents.TryGetValue(hash, out var torrent))
            {
                uploadLimit = torrent.UploadLimit;
            }

            await DialogWorkflow.InvokeUploadRateDialog(uploadLimit, Hashes);
        }

        protected async Task LimitShareRatio()
        {
            var torrents = new List<Torrent>();
            foreach (var hash in Hashes)
            {
                if (Torrents.TryGetValue(hash, out var torrent))
                {
                    torrents.Add(torrent);
                }
            }

            await DialogWorkflow.InvokeShareRatioDialog(torrents);
        }

        protected async Task ToggleSuperSeeding()
        {
            var stateChanges = ToggleTorrentState(torrent => torrent.SuperSeeding, (torrent, value) => torrent.SuperSeeding = value);
            var disableHashes = stateChanges.Where(change => change.PreviousValue).Select(change => change.Hash).ToArray();
            var enableHashes = stateChanges.Where(change => !change.PreviousValue).Select(change => change.Hash).ToArray();

            try
            {
                if (disableHashes.Length > 0)
                {
                    await ApiClient.SetSuperSeeding(value: false, all: null, hashes: disableHashes);
                }

                if (enableHashes.Length > 0)
                {
                    await ApiClient.SetSuperSeeding(value: true, all: null, hashes: enableHashes);
                }
            }
            catch
            {
                RevertTorrentState(stateChanges, (torrent, value) => torrent.SuperSeeding = value);
                throw;
            }
        }

        protected async Task ForceRecheck()
        {
            await DialogWorkflow.ForceRecheckAsync(Hashes, Preferences?.ConfirmTorrentRecheck == true);
        }

        protected async Task ForceReannounce()
        {
            await ApiClient.ReannounceTorrents(all: null, trackers: null, hashes: Hashes.ToArray());
        }

        protected async Task MoveToTop()
        {
            await ApiClient.MaxTorrentPriority(all: null, hashes: Hashes.ToArray());
        }

        protected async Task MoveUp()
        {
            await ApiClient.IncreaseTorrentPriority(all: null, hashes: Hashes.ToArray());
        }

        protected async Task MoveDown()
        {
            await ApiClient.DecreaseTorrentPriority(all: null, hashes: Hashes.ToArray());
        }

        protected async Task MoveToBottom()
        {
            await ApiClient.MinTorrentPriority(all: null, hashes: Hashes.ToArray());
        }

        protected async Task Copy(string value)
        {
            await JSRuntime.WriteToClipboard(value);
        }

        protected async Task Copy(Func<Torrent, object?> selector)
        {
            await Copy(string.Join(Environment.NewLine, GetTorrents().Select(selector)));
            if (ActionsMenu is not null)
            {
                await ActionsMenu.CloseMenuAsync();
            }
        }

        protected async Task Export()
        {
            foreach (var torrent in GetTorrents())
            {
                var url = await ApiClient.GetExportUrl(torrent.Hash);
                await JSRuntime.FileDownload(url, $"{torrent.Name}.torrent");
                await Task.Delay(200);
            }
        }

        protected async Task ShowTags()
        {
            var parameters = new DialogParameters
            {
                { nameof(ManageTagsDialog.Hashes), Hashes }
            };

            await DialogService.ShowAsync<ManageTagsDialog>(
                TranslateApp("Manage Torrent Tags"),
                parameters,
                Services.DialogWorkflow.FormDialogOptions);
        }

        protected async Task ShowCategories()
        {
            var parameters = new DialogParameters
            {
                { nameof(ManageCategoriesDialog.Hashes), Hashes }
            };

            await DialogService.ShowAsync<ManageCategoriesDialog>(
                TranslateApp("Manage Torrent Categories"),
                parameters,
                Services.DialogWorkflow.FormDialogOptions);
        }

        protected async Task DownloadSequential()
        {
            var stateChanges = ToggleTorrentState(torrent => torrent.SequentialDownload, (torrent, value) => torrent.SequentialDownload = value);

            try
            {
                await ApiClient.ToggleSequentialDownload(all: null, hashes: Hashes.ToArray());
            }
            catch
            {
                RevertTorrentState(stateChanges, (torrent, value) => torrent.SequentialDownload = value);
                throw;
            }
        }

        protected async Task DownloadFirstLast()
        {
            var stateChanges = ToggleTorrentState(torrent => torrent.FirstLastPiecePriority, (torrent, value) => torrent.FirstLastPiecePriority = value);

            try
            {
                await ApiClient.SetFirstLastPiecePriority(all: null, hashes: Hashes.ToArray());
            }
            catch
            {
                RevertTorrentState(stateChanges, (torrent, value) => torrent.FirstLastPiecePriority = value);
                throw;
            }
        }

        protected async Task SubMenuTouch(UIAction action)
        {
            await DialogWorkflow.ShowSubMenu(Hashes, action, Torrents, Preferences, Tags, Categories);
        }

        private IEnumerable<Torrent> GetTorrents()
        {
            foreach (var hash in Hashes)
            {
                if (Torrents.TryGetValue(hash, out var torrent))
                {
                    yield return torrent;
                }
            }
        }

        private IEnumerable<UIAction> Actions => GetActions();

        private IEnumerable<UIAction> GetActions()
        {
            var allAreSequentialDownload = true;
            var thereAreSequentialDownload = false;
            var allAreFirstLastPiecePrio = true;
            var thereAreFirstLastPiecePrio = false;
            var allAreDownloaded = true;
            var allAreStopped = true;
            var thereAreStopped = false;
            var allAreForceStart = true;
            var thereAreForceStart = false;
            var allAreSuperSeeding = true;
            var allAreAutoTmm = true;
            var thereAreAutoTmm = false;

            Torrent? firstTorrent = null;
            foreach (var torrent in GetTorrents())
            {
                firstTorrent ??= torrent;
                if (!torrent.SequentialDownload)
                {
                    allAreSequentialDownload = false;
                }
                else
                {
                    thereAreSequentialDownload = true;
                }

                if (!torrent.FirstLastPiecePriority)
                {
                    allAreFirstLastPiecePrio = false;
                }
                else
                {
                    thereAreFirstLastPiecePrio = true;
                }

                if (torrent.Progress < 0.999999) // not downloaded
                {
                    allAreDownloaded = false;
                }
                else if (!torrent.SuperSeeding)
                {
                    allAreSuperSeeding = false;
                }

                if (!string.Equals(torrent.State, "stoppedUP", StringComparison.Ordinal) &&
                    !string.Equals(torrent.State, "stoppedDL", StringComparison.Ordinal))
                {
                    allAreStopped = false;
                }
                else
                {
                    thereAreStopped = true;
                }

                if (!torrent.ForceStart)
                {
                    allAreForceStart = false;
                }
                else
                {
                    thereAreForceStart = true;
                }

                if (torrent.AutomaticTorrentManagement)
                {
                    thereAreAutoTmm = true;
                }
                else
                {
                    allAreAutoTmm = false;
                }
            }

            bool showSequentialDownload = true;
            if (!allAreSequentialDownload && thereAreSequentialDownload)
            {
                showSequentialDownload = false;
            }

            bool showAreFirstLastPiecePrio = true;
            if (!allAreFirstLastPiecePrio && thereAreFirstLastPiecePrio)
            {
                showAreFirstLastPiecePrio = false;
            }

            var actionStates = new Dictionary<string, ActionState>();

            var showRenameFiles = Hashes.Count() == 1 && firstTorrent!.MetaDownloaded();
            if (!showRenameFiles)
            {
                actionStates["renameFiles"] = ActionState.Hidden;
            }

            if (allAreDownloaded)
            {
                actionStates["downloadLimit"] = ActionState.Hidden;
                actionStates["uploadLimit"] = ActionState.HasSeperator;
                actionStates["sequentialDownload"] = ActionState.Hidden;
                actionStates["firstLastPiecePrio"] = ActionState.Hidden;
                actionStates["superSeeding"] = new ActionState { IsChecked = allAreSuperSeeding };
            }
            else
            {
                if (!showSequentialDownload && showAreFirstLastPiecePrio)
                {
                    actionStates["firstLastPiecePrio"] = ActionState.HasSeperator;
                }

                if (!showSequentialDownload)
                {
                    actionStates["sequentialDownload"] = ActionState.Hidden;
                }

                if (!showAreFirstLastPiecePrio)
                {
                    actionStates["firstLastPiecePrio"] = ActionState.Hidden;
                }

                if (!actionStates.TryGetValue("sequentialDownload", out var sequentialDownload))
                {
                    actionStates["sequentialDownload"] = new ActionState { IsChecked = allAreSequentialDownload };
                }
                else
                {
                    sequentialDownload.IsChecked = allAreSequentialDownload;
                }

                if (!actionStates.TryGetValue("firstLastPiecePrio", out var firstLastPiecePrio))
                {
                    actionStates["firstLastPiecePrio"] = new ActionState { IsChecked = allAreFirstLastPiecePrio };
                }
                else
                {
                    firstLastPiecePrio.IsChecked = allAreFirstLastPiecePrio;
                }

                actionStates["superSeeding"] = ActionState.Hidden;
            }

            if (allAreStopped)
            {
                actionStates["stop"] = ActionState.Hidden;
            }
            else if (allAreForceStart)
            {
                actionStates["forceStart"] = ActionState.Hidden;
            }
            else if (!thereAreStopped && !thereAreForceStart)
            {
                actionStates["start"] = ActionState.Hidden;
            }

            if (actionStates.TryGetValue("start", out ActionState? startActionState))
            {
                startActionState.TextOverride = TranslateTransferList("Start");
            }
            else
            {
                actionStates["start"] = new ActionState { TextOverride = TranslateTransferList("Start") };
            }

            if (actionStates.TryGetValue("stop", out ActionState? stopActionState))
            {
                stopActionState.TextOverride = TranslateTransferList("Stop");
            }
            else
            {
                actionStates["stop"] = new ActionState { TextOverride = TranslateTransferList("Stop") };
            }

            if (!allAreAutoTmm && thereAreAutoTmm)
            {
                actionStates["autoTorrentManagement"] = ActionState.Hidden;
            }
            else
            {
                actionStates["autoTorrentManagement"] = new ActionState { IsChecked = allAreAutoTmm };
            }

            if (Preferences?.QueueingEnabled == false)
            {
                actionStates["queue"] = ActionState.Hidden;
            }

            if (Categories.Count == 0)
            {
                actionStates["category"] = ActionState.Hidden;
            }

            var filteredActions = Filter(actionStates);

            foreach (var action in filteredActions)
            {
                if (action.Name == "tags")
                {
                    var hasAppliedTags = ApplyTags(action);
                    if (!hasAppliedTags)
                    {
                        continue;
                    }
                }
                if (action.Name == "category")
                {
                    var hasAppliedCategories = ApplyCategories(action);
                    if (!hasAppliedCategories)
                    {
                        continue;
                    }
                }
                yield return action;
            }
        }

        private bool ApplyTags(UIAction action)
        {
            if (!Hashes.Any())
            {
                return false;
            }
            if (Tags.Count == 0)
            {
                return false;
            }
            action.Children = Tags.Select(tag =>
            {
                var state = GetTagSelectionState(tag);
                return new UIAction(
                    name: $"tag-{tag}",
                    text: tag,
                    icon: GetTagIcon(state),
                    color: GetTagColor(state),
                    callback: CreateCallback(() => ToggleTag(tag)),
                    autoClose: false
                );
            });
            return true;
        }

        private bool ApplyCategories(UIAction action)
        {
            if (!Hashes.Any())
            {
                return false;
            }
            if (Categories.Count == 0)
            {
                return false;
            }
            action.Children = Categories.Values.Select(category =>
            {
                var state = GetCategorySelectionState(category.Name);
                return new UIAction(
                    name: $"category-{category.Name}",
                    text: category.Name,
                    icon: GetTagIcon(state),
                    color: GetTagColor(state),
                    callback: CreateCallback(() => ToggleCategory(category.Name)),
                    autoClose: false
                );
            });
            return true;
        }

        private Dictionary<string, HashSet<string>> TagState { get; set; } = [];

        private Dictionary<string, string?> CategoryState { get; set; } = [];

        private TagSelectionState GetTagSelectionState(string tag)
        {
            var hasTag = false;
            var missingTag = false;

            foreach (var hash in Hashes)
            {
                if (TagState.TryGetValue(hash, out var tags) && tags.Contains(tag))
                {
                    hasTag = true;
                }
                else
                {
                    missingTag = true;
                }

                if (hasTag && missingTag)
                {
                    return TagSelectionState.Partial;
                }
            }

            if (hasTag)
            {
                return TagSelectionState.All;
            }

            return TagSelectionState.None;
        }

        private TagSelectionState GetCategorySelectionState(string category)
        {
            var hasCategory = false;
            var missingCategory = false;

            foreach (var hash in Hashes)
            {
                if (CategoryState.TryGetValue(hash, out var currentCategory) && currentCategory == category)
                {
                    hasCategory = true;
                }
                else
                {
                    missingCategory = true;
                }

                if (hasCategory && missingCategory)
                {
                    return TagSelectionState.Partial;
                }
            }

            if (hasCategory)
            {
                return TagSelectionState.All;
            }

            return TagSelectionState.None;
        }

        private string GetTagIcon(TagSelectionState state)
        {
            return state switch
            {
                TagSelectionState.All => Icons.Material.Filled.Check,
                TagSelectionState.Partial => Icons.Material.Filled.HorizontalRule,
                _ => Icons.Material.Filled.Check
            };
        }

        private Color GetTagColor(TagSelectionState state)
        {
            return state switch
            {
                TagSelectionState.All => Color.Info,
                TagSelectionState.Partial => Color.Warning,
                _ => Color.Transparent
            };
        }

        private async Task ToggleTag(string tag)
        {
            var selectedHashes = Hashes.ToArray();
            if (selectedHashes.Length == 0)
            {
                return;
            }

            var allHaveTag = GetTagSelectionState(tag) == TagSelectionState.All;

            if (allHaveTag)
            {
                await ApiClient.RemoveTorrentTag(tag, selectedHashes);

                foreach (var hash in selectedHashes)
                {
                    if (TagState.TryGetValue(hash, out var tags))
                    {
                        tags.Remove(tag);
                    }
                }
            }
            else
            {
                await ApiClient.AddTorrentTag(tag, selectedHashes);

                foreach (var hash in selectedHashes)
                {
                    if (!TagState.TryGetValue(hash, out var tags))
                    {
                        tags = [];
                        TagState[hash] = tags;
                    }

                    tags.Add(tag);
                }
            }
        }

        private enum TagSelectionState
        {
            None,
            Partial,
            All,
        }

        private async Task ToggleCategory(string category)
        {
            var selectedHashes = Hashes.ToArray();
            if (selectedHashes.Length == 0)
            {
                return;
            }

            var allHaveCategory = GetCategorySelectionState(category) == TagSelectionState.All;

            if (allHaveCategory)
            {
                await ApiClient.SetTorrentCategory(category: string.Empty, all: null, hashes: selectedHashes);

                foreach (var hash in selectedHashes)
                {
                    CategoryState.Remove(hash);
                }
            }
            else
            {
                await ApiClient.SetTorrentCategory(category: category, all: null, hashes: selectedHashes);

                foreach (var hash in selectedHashes)
                {
                    CategoryState[hash] = category;
                }
            }
        }

        private IReadOnlyList<TorrentStateChange> ToggleTorrentState(Func<Torrent, bool> selector, Action<Torrent, bool> setter)
        {
            var stateChanges = new List<TorrentStateChange>();
            var seenHashes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var torrent in GetTorrents())
            {
                if (!seenHashes.Add(torrent.Hash))
                {
                    continue;
                }

                var previousValue = selector(torrent);
                setter(torrent, !previousValue);
                stateChanges.Add(new TorrentStateChange(torrent.Hash, previousValue));
            }

            if (stateChanges.Count > 0)
            {
                StateHasChanged();
            }

            return stateChanges;
        }

        private void RevertTorrentState(IEnumerable<TorrentStateChange> changes, Action<Torrent, bool> setter)
        {
            var reverted = false;

            foreach (var change in changes)
            {
                if (Torrents.TryGetValue(change.Hash, out var torrent))
                {
                    setter(torrent, change.PreviousValue);
                    reverted = true;
                }
            }

            if (reverted)
            {
                StateHasChanged();
            }
        }

        private readonly record struct TorrentStateChange(string Hash, bool PreviousValue);

        private IEnumerable<UIAction> Filter(Dictionary<string, ActionState> actionStates)
        {
            if (_actions is null)
            {
                yield break;
            }
            foreach (var action in _actions)
            {
                if (!actionStates.TryGetValue(action.Name, out var actionState))
                {
                    yield return action;
                }
                else
                {
                    if (actionState.Show is null || actionState.Show.Value)
                    {
                        var act = action with { };
                        if (actionState.HasSeparator.HasValue)
                        {
                            act.SeparatorBefore = actionState.HasSeparator.Value;
                        }
                        if (actionState.IsChecked.HasValue)
                        {
                            act.IsChecked = actionState.IsChecked.Value;
                        }
                        if (actionState.TextOverride is not null)
                        {
                            act.Text = actionState.TextOverride;
                        }

                        yield return act;
                    }
                }
            }
        }

        private sealed class ActionState
        {
            public bool? Show { get; set; }

            public bool? HasSeparator { get; set; }

            public bool? IsChecked { get; set; }

            public string? TextOverride { get; set; }

            public static readonly ActionState Hidden = new() { Show = false };

            public static readonly ActionState HasSeperator = new() { HasSeparator = true };
        }

        private EventCallback CreateCallback(Func<Task> action)
        {
            if (MudDialog is not null)
            {
                return EventCallback.Factory.Create(this, async () =>
                {
                    await action();
                    MudDialog?.Close();
                });
            }
            else
            {
                return EventCallback.Factory.Create(this, action);
            }
        }

        private string TranslateTransferList(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate(_transferListContext, source, arguments);
        }

        private string TranslateApp(string source, params object[] arguments)
        {
            return LanguageLocalizer.Translate(_appContext, source, arguments);
        }

        public async ValueTask DisposeAsync()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            await DisposeAsync(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual async Task DisposeAsync(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    await UnregisterDeleteShortcutAsync();
                }

                _disposedValue = true;
            }
        }
    }

    public enum RenderType
    {
        /// <summary>
        /// Renders toolbar contents without the <see cref="MudToolBar"/> wrapper.
        /// </summary>
        ToolbarContents,

        /// <summary>
        /// Renders a <see cref="MudToolBar"/>.
        /// </summary>
        Toolbar,

        /// <summary>
        /// Renders a <see cref="MudMenu"/>.
        /// </summary>
        Menu,

        /// <summary>
        /// Renders a <see cref="MudToolBar"/> with <see cref="MudIconButton"/> for basic actions and a <see cref="MudMenu"/> for actions with children.
        /// </summary>
        MixedToolbarContents,

        /// <summary>
        /// Renders toolbar contents without the <see cref="MudToolBar"/> wrapper with <see cref="MudIconButton"/> for basic actions and a <see cref="MudMenu"/> for actions with children.
        /// </summary>
        MixedToolbar,

        InitialIconsOnly,

        Children,

        MenuWithoutActivator,

        MenuItems,
    }
}
