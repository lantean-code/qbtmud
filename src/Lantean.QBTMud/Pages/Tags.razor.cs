using Lantean.QBitTorrentClient;
using Lantean.QBTMud.Components.UI;
using Lantean.QBTMud.Helpers;
using Lantean.QBTMud.Models;
using Lantean.QBTMud.Services;
using Lantean.QBTMud.Services.Localization;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lantean.QBTMud.Pages
{
    public partial class Tags
    {
        private const string ActionsColumnId = "actions";

        private readonly Dictionary<string, RenderFragment<RowContext<string>>> _columnRenderFragments = [];
        private IReadOnlyList<string>? _tags;
        private bool _isBusy;

        [Inject]
        protected IApiClient ApiClient { get; set; } = default!;

        [Inject]
        protected IDialogWorkflow DialogWorkflow { get; set; } = default!;

        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        protected ILocalStorageService LocalStorage { get; set; } = default!;

        [Inject]
        protected IWebUiLocalizer WebUiLocalizer { get; set; } = default!;

        [CascadingParameter(Name = "DrawerOpen")]
        public bool DrawerOpen { get; set; }

        [CascadingParameter]
        public MainData? MainData { get; set; }

        protected IEnumerable<string>? Results
        {
            get
            {
                if (_tags is not null)
                {
                    return _tags;
                }

                return MainData?.Tags;
            }
        }

        protected DynamicTable<string>? Table { get; set; }

        public Tags()
        {
            _columnRenderFragments.Add(ActionsColumnId, ActionsColumn);
        }

        protected void NavigateBack()
        {
            NavigationManager.NavigateToHome();
        }

        protected async Task Reload()
        {
            if (_isBusy)
            {
                return;
            }

            _isBusy = true;
            try
            {
                _tags = await ApiClient.GetAllTags();
            }
            finally
            {
                _isBusy = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task DeleteTag(string? tag)
        {
            if (tag is null)
            {
                return;
            }
            await ApiClient.DeleteTags(tag);
        }

        protected async Task AddTag()
        {
            var tag = await DialogWorkflow.ShowStringFieldDialog(
                WebUiLocalizer.Translate("TagFilterWidget", "New Tag"),
                WebUiLocalizer.Translate("TagFilterWidget", "Tag:"),
                null);

            if (tag is null)
            {
                return;
            }

            var existingTags = await ApiClient.GetAllTags();
            if (existingTags.Contains(tag))
            {
                return;
            }

            await ApiClient.CreateTags([tag]);
        }

        protected IEnumerable<ColumnDefinition<string>> Columns => GetColumnDefinitions();

        private IEnumerable<ColumnDefinition<string>> GetColumnDefinitions()
        {
            foreach (var columnDefinition in ColumnsDefinitions)
            {
                if (_columnRenderFragments.TryGetValue(columnDefinition.Id, out var fragment))
                {
                    columnDefinition.RowTemplate = fragment;
                }

                yield return columnDefinition;
            }
        }

        private List<ColumnDefinition<string>> ColumnsDefinitions => BuildColumnsDefinitions();

        private List<ColumnDefinition<string>> BuildColumnsDefinitions()
        {
            return
            [
                new ColumnDefinition<string>(WebUiLocalizer.Translate("TransferListModel", "Name"), l => l, id: "id"),
                new ColumnDefinition<string>(Translate("Actions"), l => l, id: ActionsColumnId)
            ];
        }

        private string Translate(string source, params object[] arguments)
        {
            return WebUiLocalizer.Translate("AppTags", source, arguments);
        }
    }
}
