using Lantean.QBitTorrentClient;
using Lantean.QBTMudBlade.Interop;
using Lantean.QBTMudBlade.Models;
using Lantean.QBTMudBlade.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lantean.QBTMudBlade.Components
{
    public partial class TorrentActions
    {
        [Inject]
        public IApiClient ApiClient { get; set; } = default!;

        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        public IDialogService DialogService { get; set; } = default!;

        [Inject]
        public ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        public IDataManager DataManager { get; set; } = default!;

        [Inject]
        public IClipboardService ClipboardService { get; set; } = default!;

        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [Parameter]
        [EditorRequired]
        public IEnumerable<string> Hashes { get; set; } = default!;

        /// <summary>
        /// If true this component will render as a <see cref="MudToolBar"/> otherwise will render as a <see cref="MudMenu"/>.
        /// </summary>
        [Parameter]
        public ParentType Type { get; set; }

        [CascadingParameter]
        public MainData MainData { get; set; } = default!;

        protected async Task Pause()
        {
            await ApiClient.PauseTorrents(Hashes);
            Snackbar.Add("Torrent paused.");
        }

        protected async Task Resume()
        {
            await ApiClient.ResumeTorrents(Hashes);
            Snackbar.Add("Torrent resumed.");
        }

        protected async Task Remove()
        {
            await DialogService.InvokeDeleteTorrentDialog(ApiClient, Hashes.ToArray());
        }

        protected async Task SetLocation()
        {
            string? savePath = null;
            if (Hashes.Any() && MainData.Torrents.TryGetValue(Hashes.First(), out var torrent))
            {
                savePath = torrent.SavePath;
            }
            await DialogService.ShowSingleFieldDialog("Set Location", "Location", savePath, v => ApiClient.SetTorrentLocation(v, null, Hashes.ToArray()));
        }

        protected async Task Rename()
        {
            string? name = null;
            string hash = Hashes.First();
            if (Hashes.Any() && MainData.Torrents.TryGetValue(hash, out var torrent))
            {
                name = torrent.Name;
            }
            await DialogService.ShowSingleFieldDialog("Rename", "Location", name, v => ApiClient.SetTorrentName(v, hash));
        }

        protected async Task RenameFiles()
        {
            await DialogService.InvokeRenameFilesDialog(ApiClient, Hashes.First());
        }

        protected async Task SetCategory(string category)
        {
            await ApiClient.SetTorrentCategory(category, null, Hashes.ToArray());
        }

        protected async Task AddCategory()
        {
            await DialogService.InvokeAddCategoryDialog(ApiClient, Hashes);
        }

        protected async Task ResetCategory()
        {
            await ApiClient.SetTorrentCategory("", null, Hashes.ToArray());
        }

        protected async Task AddTag()
        {
            await DialogService.ShowSingleFieldDialog("Add Tags", "Comma-separated tags", "", v => ApiClient.AddTorrentTags(v.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), null, Hashes.ToArray()));
        }

        protected async Task RemoveTags()
        {
            var torrents = GetTorrents();

            foreach (var torrent in torrents)
            {
                await ApiClient.RemoveTorrentTags(torrent.Tags, null, torrent.Hash);
            }
        }

        protected async Task ToggleTag(string tag)
        {
            var torrents = GetTorrents();

            await ApiClient.RemoveTorrentTag(tag, torrents.Where(t => t.Tags.Contains(tag)).Select(t => t.Hash));
            await ApiClient.AddTorrentTag(tag, torrents.Where(t => !t.Tags.Contains(tag)).Select(t => t.Hash));
        }

        protected async Task ToggleAutoTMM()
        {
            var torrents = GetTorrents();

            await ApiClient.SetAutomaticTorrentManagement(false, null, torrents.Where(t => t.AutomaticTorrentManagement).Select(t => t.Hash).ToArray());
            await ApiClient.SetAutomaticTorrentManagement(true, null, torrents.Where(t => !t.AutomaticTorrentManagement).Select(t => t.Hash).ToArray());
        }

        protected async Task LimitUploadRate()
        {
            long uploadLimit = -1;
            string hash = Hashes.First();
            if (Hashes.Any() && MainData.Torrents.TryGetValue(hash, out var torrent))
            {
                uploadLimit = torrent.UploadLimit;
            }

            await DialogService.InvokeUploadRateDialog(ApiClient, uploadLimit, Hashes);
        }

        protected async Task LimitShareRatio()
        {
            float ratioLimit = -1;
            string hash = Hashes.First();
            if (Hashes.Any() && MainData.Torrents.TryGetValue(hash, out var torrent))
            {
                ratioLimit = torrent.RatioLimit;
            }

            await DialogService.InvokeShareRatioDialog(ApiClient, ratioLimit, Hashes);
        }

        protected async Task ToggleSuperSeeding()
        {
            var torrents = GetTorrents();

            await ApiClient.SetSuperSeeding(false, null, torrents.Where(t => t.SuperSeeding).Select(t => t.Hash).ToArray());
            await ApiClient.SetSuperSeeding(true, null, torrents.Where(t => !t.SuperSeeding).Select(t => t.Hash).ToArray());
        }

        protected async Task ForceRecheck()
        {
            await ApiClient.RecheckTorrents(null, Hashes.ToArray());
        }

        protected async Task ForceReannounce()
        {
            await ApiClient.ReannounceTorrents(null, Hashes.ToArray());
        }

        protected async Task MoveToTop()
        {
            await ApiClient.MaximalTorrentPriority(null, Hashes.ToArray());
        }

        protected async Task MoveUp()
        {
            await ApiClient.IncreaseTorrentPriority(null, Hashes.ToArray());
        }

        protected async Task MoveDown()
        {
            await ApiClient.DecreaseTorrentPriority(null, Hashes.ToArray());
        }

        protected async Task MoveToBottom()
        {
            await ApiClient.MinimalTorrentPriority(null, Hashes.ToArray());
        }

        protected async Task Copy(string value)
        {
            await ClipboardService.WriteToClipboard(value);
        }

        protected async Task Copy(Func<Torrent, object?> selector)
        {
            await Copy(string.Join(Environment.NewLine, GetTorrents().Select(selector)));
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

        private IEnumerable<Torrent> GetTorrents()
        {
            foreach (var hash in Hashes)
            {
                if (MainData.Torrents.TryGetValue(hash, out var torrent))
                {
                    yield return torrent;
                }
            }
        }

        private IEnumerable<Action> GetOptions()
        {
            if (!Hashes.Any())
            {
                return [];
            }

            var firstTorrent = MainData.Torrents[Hashes.First()];

            var categories = new List<Action>
            {
                new Action("New", Icons.Material.Filled.Add, Color.Info, EventCallback.Factory.Create(this, AddCategory)),
                new Action("Reset", Icons.Material.Filled.Remove, Color.Error, EventCallback.Factory.Create(this, ResetCategory)),
                new Divider()
            };
            categories.AddRange(MainData.Categories.Select(c => new Action(c.Value.Name, Icons.Material.Filled.List, Color.Info, EventCallback.Factory.Create(this, () => SetCategory(c.Key)))));

            var tags = new List<Action>
            {
                new Action("Add", Icons.Material.Filled.Add, Color.Info, EventCallback.Factory.Create(this, AddTag)),
                new Action("Remove All", Icons.Material.Filled.Remove, Color.Error, EventCallback.Factory.Create(this, RemoveTags)),
                new Divider()
            };
            tags.AddRange(MainData.Tags.Select(t => new Action(t, firstTorrent.Tags.Contains(t) ? Icons.Material.Filled.CheckBox : Icons.Material.Filled.CheckBoxOutlineBlank, Color.Default, EventCallback.Factory.Create(this, () => ToggleTag(t)))));

            var options = new List<Action>
            {
                new Action("Pause", Icons.Material.Filled.Pause, Color.Warning, EventCallback.Factory.Create(this, Pause)),
                new Action("Resume", Icons.Material.Filled.PlayArrow, Color.Success, EventCallback.Factory.Create(this, Resume)),
                new Divider(),
                new Action("Remove", Icons.Material.Filled.Delete, Color.Error, EventCallback.Factory.Create(this, Remove)),
                new Divider(),
                new Action("Set location", Icons.Material.Filled.MyLocation, Color.Info, EventCallback.Factory.Create(this, SetLocation)),
                new Action("Rename", Icons.Material.Filled.DriveFileRenameOutline, Color.Info, EventCallback.Factory.Create(this, Rename)),
                new Action("Category", Icons.Material.Filled.List, Color.Info, categories),
                new Action("Tags", Icons.Material.Filled.Label, Color.Info, tags),
                new Action("Automatic Torrent Management", Icons.Material.Filled.Check, firstTorrent.AutomaticTorrentManagement ? Color.Info : Color.Transparent, EventCallback.Factory.Create(this, ToggleAutoTMM)),
                new Divider(),
                new Action("Limit upload rate", Icons.Material.Filled.KeyboardDoubleArrowUp, Color.Info, EventCallback.Factory.Create(this, LimitUploadRate)),
                new Action("Limit share ratio", Icons.Material.Filled.Percent, Color.Warning, EventCallback.Factory.Create(this, LimitShareRatio)),
                new Action("Super seeding mode", Icons.Material.Filled.Check, firstTorrent.SuperSeeding ? Color.Info : Color.Transparent, EventCallback.Factory.Create(this, ToggleSuperSeeding)),
                new Divider(),
                new Action("Force recheck", Icons.Material.Filled.Loop, Color.Info, EventCallback.Factory.Create(this, ForceRecheck)),
                new Action("Force reannounce", Icons.Material.Filled.BroadcastOnHome, Color.Info, EventCallback.Factory.Create(this, ForceReannounce)),
                new Divider(),
                new Action("Queue", Icons.Material.Filled.Queue, Color.Transparent, new List<Action>
                {
                    new Action("Move to top", Icons.Material.Filled.VerticalAlignTop, Color.Inherit, EventCallback.Factory.Create(this, MoveToTop)),
                    new Action("Move up", Icons.Material.Filled.ArrowUpward, Color.Inherit, EventCallback.Factory.Create(this, MoveUp)),
                    new Action("Move down", Icons.Material.Filled.ArrowDownward, Color.Inherit, EventCallback.Factory.Create(this, MoveDown)),
                    new Action("Move to bottom", Icons.Material.Filled.VerticalAlignBottom, Color.Inherit, EventCallback.Factory.Create(this, MoveToBottom)),
                }),
                new Action("Copy", Icons.Material.Filled.FolderCopy, Color.Info, new List<Action>
                {
                    new Action("Name", Icons.Material.Filled.TextFields, Color.Info, EventCallback.Factory.Create(this, () => Copy(t => t.Name))),
                    new Action("Info hash v1", Icons.Material.Filled.Tag, Color.Info, EventCallback.Factory.Create(this, () => Copy(t => t.InfoHashV1))),
                    new Action("Info hash v2", Icons.Material.Filled.Tag, Color.Info, EventCallback.Factory.Create(this, () => Copy(t => t.InfoHashV2))),
                    new Action("Magnet link", Icons.Material.Filled.TextFields, Color.Info, EventCallback.Factory.Create(this, () => Copy(t => t.MagnetUri))),
                    new Action("Torrent ID", Icons.Material.Filled.TextFields, Color.Info, EventCallback.Factory.Create(this, () => Copy(t => t.Hash))),
                }),
                new Action("Export", Icons.Material.Filled.SaveAlt, Color.Info, EventCallback.Factory.Create(this, Export)),
            };

            return options;
        }
    }

    public enum ParentType
    {
        Toolbar,
        StandaloneToolbar,
        Menu,
    }

    public class Divider : Action
    {
        public Divider() : base("-", default!, Color.Default, default(EventCallback))
        {
        }
    }

    public class Action
    {
        public Action(string name, string icon, Color color, EventCallback callback)
        {
            Name = name;
            Icon = icon;
            Color = color;
            Callback = callback;
            Children = [];
        }

        public Action(string name, string icon, Color color, IEnumerable<Action> children)
        {
            Name = name;
            Icon = icon;
            Color = color;
            Callback = default;
            Children = children;
        }

        public string Name { get; }

        public string Icon { get; }

        public Color Color { get; }

        public EventCallback Callback { get; }

        public IEnumerable<Action> Children { get; }
    }
}